using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using IngameDebugConsole;
using Utilities;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.Android;
using UnityEngine.Serialization;

namespace Player
{
    public struct InputPayload : INetworkSerializable
    {
        public int tick;
        public Vector3 inputVector;
        public bool forceTeleport;
        public Vector3 position;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref inputVector);
            serializer.SerializeValue(ref forceTeleport);
            serializer.SerializeValue(ref position);
        }
    }

    public struct StatePayload : INetworkSerializable
    {
        public int tick;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref rotation);
            serializer.SerializeValue(ref velocity);   
            serializer.SerializeValue(ref angularVelocity);
        }
    }
    
    public class PlayerMovement : NetworkBehaviour
    {
        #region Variables

        [Header("Movement")] [SerializeField] private CharacterController controller;
        [SerializeField] private float speed = 12f;
        [SerializeField] private float jumpHeight = 3f;

        [Header("Jumping")] [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundDistance = 0.4f;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private InputReader playerInput;

        private Vector3 _velocity;
        private bool _isGrounded;

        private int _lastProcessedMovementTick = -1;
        
        private Quaternion _previousRotation;
        //private Vector3 _lastAngularVelocity;
        
        private ICharacterMovement _input;
        
        // Netcode general
        private NetworkTimer _timer;
        private const float k_serverTickRate = 60f;
        private const int k_bufferSize = 1024;
        
        // Netcode Client specific
        private CircularBuffer<StatePayload> _clientStateBuffer;
        private CircularBuffer<InputPayload> _clientInputBuffer;
        private StatePayload _lastServerState;
        private StatePayload _lastProcessedState;
        
        // Netcode server specific
        private CircularBuffer<StatePayload> _serverStateBuffer;
        Queue<InputPayload> _serverInputQueue;
        
        [Header("Netcode")]
        [SerializeField] private float reconciliationThreshold = 10f;
        private CountdownTimer _reconciliationTimer;
        [SerializeField] float reconcilationCooldownTime = 5f;
        
        [SerializeField] private GameObject serverCube;
        [SerializeField] private GameObject clientCube;

        private bool _cheatQueued;
        //public GameObject clientCubeInstance;
        //public GameObject serverCubeInstance;
        #endregion

        void Awake()
        {
            playerInput.Enable();
            _input = playerInput;

            //clientCubeInstance = Instantiate(clientCube);
            //serverCubeInstance = Instantiate(serverCube);
            
            _timer = new NetworkTimer(k_serverTickRate);
            _clientStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
            _clientInputBuffer = new CircularBuffer<InputPayload>(k_bufferSize);
            _serverStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
            _serverInputQueue = new Queue<InputPayload>();
            
            _reconciliationTimer = new CountdownTimer(reconcilationCooldownTime);
            
            
        }

        void Start()
        {
            clientCube.transform.SetParent(null);
            serverCube.transform.SetParent(null);
        }

        void Update()
        {
            //if (!IsOwner) return;
            _timer.Update(Time.deltaTime);
            _reconciliationTimer.Tick(Time.deltaTime);
            
            // cheat
            if (Input.GetKeyDown(KeyCode.Q))
            {
                _cheatQueued = true;
                Debug.Log("Q is pressed!"); 
            }
        }
        
        void FixedUpdate()
        {
            //_timer.Update(Time.deltaTime);
            if (IsHost && IsOwner)
            {
                while (_timer.ShouldTick())
                {
                    HandleServerTick();
                    HandleClientTick(); 
                }
                return;
            }

            if (IsClient && IsOwner)
            {
                while (_timer.ShouldTick())
                    HandleClientTick();
            }

            if (IsServer)
            {
                while (_timer.ShouldTick())
                    HandleServerTick();
            }
        }
        
        void HandleServerTick()
        {
            if (!IsServer) return;
            
            var bufferIndex = -1;
            
            while (_serverInputQueue.Count > 0)
            {
                InputPayload inputPayload = _serverInputQueue.Dequeue();
                
                bufferIndex = inputPayload.tick % k_bufferSize;
                
                StatePayload statePayload = ProcessMovement(inputPayload, false, false);//diff
                serverCube.transform.position = statePayload.position.With(y: 62);
                _serverStateBuffer.Add(statePayload, bufferIndex);
            }
            
            if (bufferIndex == -1) return;
            SendToClientRpc(_serverStateBuffer.Get(bufferIndex));
        }
        
        bool ShouldReconcile()
        {
            bool isNewServerState = !_lastServerState.Equals(default);
            bool isLastStateUndefindedOrDifferent = _lastProcessedState.Equals(default)
                                                    || !_lastProcessedState.Equals(_lastServerState);
            
            return isNewServerState && isLastStateUndefindedOrDifferent && !_reconciliationTimer.IsRunning;
        }

        void HandleServerReconciliation()
        {
            if (_lastServerState.tick <= 1) return;
            
            if (!ShouldReconcile()) return;
            
            float positionError;
            int bufferIndex;
            StatePayload rewindState = default;
            
            bufferIndex = _lastServerState.tick % k_bufferSize;
            if (bufferIndex - 1 < 0) return;

            rewindState = IsHost ? _serverStateBuffer.Get(bufferIndex - 1) : _lastServerState;
            StatePayload clientState = IsHost ? _clientStateBuffer.Get(bufferIndex - 1) : _clientStateBuffer.Get(bufferIndex); //diff
            positionError = Vector3.Distance(rewindState.position, clientState.position);
            
            if (positionError > reconciliationThreshold)
            {
                // Prüfe ob Rewind wirklich notwendig ist (verhindert doppelte)
                if (rewindState.position != transform.position)
                {
                    Debug.LogWarning($"[Reconciliation] @{rewindState.tick} ΔPos: {positionError:F3} → Rewinding to tick {_lastServerState.tick}");
                    ReconcileState(rewindState);
                    _lastProcessedState = rewindState;
                    _reconciliationTimer.Start();
                }
                else
                {
                    Debug.Log($"[Reconciliation skipped] No delta despite ΔPos > threshold");
                }
            }
        }
 
        [ClientRpc]
        void SendToClientRpc(StatePayload statePayload)
        {
            //serverCube.transform.position = statePayload.position.With(y: 10);
            if (!IsOwner) return;
            _lastServerState = statePayload;
        }

        void HandleClientTick()
        {
            if (!IsClient || !IsOwner) return;

            var currentTick = _timer.CurrentTick;
            var bufferIndex = currentTick % k_bufferSize;

            InputPayload inputPayload = new InputPayload()
            {
                tick = currentTick,
                inputVector = _input.Move,
                forceTeleport = _cheatQueued,
                position = transform.position,
            };
            _cheatQueued = false;

            _clientInputBuffer.Add(inputPayload, bufferIndex);
            SendToServerRpc(inputPayload);

            //TODO: warum wird hier nichts gelogged...
            // Neu: Nur simulieren, wenn es **kein Host** ist ODER wir ausdrücklich sagen: auch host simulieren
            //bool shouldSimulate = !IsHost || IsServer;
            
            StatePayload statePayload = ProcessMovement(inputPayload, false, true);
            clientCube.transform.position = statePayload.position.With(y: 60);
            _clientStateBuffer.Add(statePayload, bufferIndex);
          

            if (_lastServerState.tick == inputPayload.tick)
            {
                float posDiff = Vector3.Distance(transform.position, _lastServerState.position);
                float velDiff = Vector3.Distance(controller.velocity, _lastServerState.velocity);
                float rotDiff = Quaternion.Angle(transform.rotation, _lastServerState.rotation);

                Debug.Log($"[Tick {inputPayload.tick}] ΔPos: {posDiff:F4}, ΔVel: {velDiff:F4}, ΔRot: {rotDiff:F2}");

                if (posDiff > reconciliationThreshold)
                {
                    Debug.LogWarning($"[Desync] Significant position mismatch at tick {inputPayload.tick}");
                }
            }
            HandleServerReconciliation();
        }


        void ReconcileState(StatePayload rewindState)
        {
            Debug.Log("it happened!");
            Debug.LogWarning($"[Reconciliation] Rewinding to tick {_lastServerState.tick}, pos: {rewindState.position}, local: {transform.position}");

            controller.enabled = false;
            transform.position = rewindState.position;
            transform.rotation = rewindState.rotation;
            _velocity = rewindState.velocity;
            controller.enabled = true;
            
            if (!rewindState.Equals(_lastServerState)) return;
            
            _clientStateBuffer.Add(rewindState, rewindState.tick % k_bufferSize);

            int tickToReplay = _lastServerState.tick;

            while (tickToReplay < _timer.CurrentTick)
            {
                int bufferIndex = tickToReplay % k_bufferSize;
                StatePayload statePayload = ProcessMovement(_clientInputBuffer.Get(bufferIndex), true, false); 
                _clientStateBuffer.Add(statePayload, bufferIndex); 
                tickToReplay++;
            }
        }        
        
        [ServerRpc]
        void SendToServerRpc(InputPayload input)
        {
            //clientCube.transform.position = input.position.With(y: 11);
            _serverInputQueue.Enqueue(input);
        }
        
        StatePayload ProcessMovement(InputPayload input, bool isReplay, bool allowTeleport)
        {
            // vielleicht der Grund für komisches Host behavior
            if (_lastProcessedMovementTick != input.tick)
            {
                if (input.forceTeleport && !isReplay && allowTeleport)
                {
                    controller.enabled = false;
                    transform.position += transform.forward * 20f;
                    controller.enabled = true;
                    Debug.Log($"[Teleport] New Pos: {transform.position}");
                }

                Move(input.inputVector);
                _lastProcessedMovementTick = input.tick;
            }

            Debug.Log($"[CLIENT][Tick {input.tick}] Pos: {transform.position}, Vel: {controller.velocity}");
            
            return new StatePayload()
            {
                tick = input.tick,
                position = transform.position,
                rotation = transform.rotation,
                velocity = controller.velocity, 
            };
        }

        void Move(Vector3 inputVector)
        {
            if (DebugLogManager.IsConsoleOpen)
                return;
            
            _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            
            if (_isGrounded && _velocity.y < 0f)
                _velocity.y = -2f;

            Vector3 move = transform.right * inputVector.x + transform.forward * inputVector.z; 

            controller.Move(move * (speed * Time.fixedDeltaTime));

            if (playerInput.JumpPressed && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            _velocity.y += gravity * Time.fixedDeltaTime;
            controller.Move(_velocity * Time.fixedDeltaTime);
        }
    }
}