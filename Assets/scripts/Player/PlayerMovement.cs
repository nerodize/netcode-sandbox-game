using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using IngameDebugConsole;
using Utilities;
using System.Linq;

namespace Player
{
    public struct InputPayload : INetworkSerializable
    {
        public int tick;
        public Vector3 inputVector;
        public bool forceTeleport;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref inputVector);
            serializer.SerializeValue(ref forceTeleport);
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
        
        private Quaternion _previousRotation;
        private Vector3 _lastAngularVelocity;


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

        [SerializeField] private GameObject serverCube;
        [SerializeField] private GameObject clientCube;

        private bool _cheatQueued;

        private GameObject _clientCubeInstance;
        private GameObject _serverCubeInstance;
        /*
        private readonly NetworkVariable<Vector3> _networkPosition = new(writePerm: NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<Quaternion> _networkRotation = new(writePerm: NetworkVariableWritePermission.Owner);
        */

        #endregion

        void Awake()
        {
            playerInput.Enable();
            _input = playerInput;

            _clientCubeInstance = Instantiate(clientCube);
            _serverCubeInstance = Instantiate(serverCube);
            
            _timer = new NetworkTimer(k_serverTickRate);
            _clientStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
            _clientInputBuffer = new CircularBuffer<InputPayload>(k_bufferSize);
            _serverStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
            _serverInputQueue = new Queue<InputPayload>();
        }

        void Update()
        {
            if (!IsOwner) return;
            _timer.Update(Time.deltaTime);
            
            // cheat
            if (Input.GetKeyDown(KeyCode.Q))
            {
                _cheatQueued = true;
                Debug.Log("Q is pressed!"); //hier
            }
            /*
        if (!IsOwner)
        {
            transform.position = _networkPosition.Value;
            transform.rotation = _networkRotation.Value;

            return;
        }
        */

            /*
            _networkPosition.Value = transform.position;
            _networkRotation.Value = transform.rotation;
            */
        }

        void FixedUpdate()
        {
            if (!IsOwner) return;

            while (_timer.ShoudTick())
            {
                HandleClientTick();
                HandleServerTick();
            }
        }

        void HandleServerTick()
        {
            var bufferIndex = -1;
            while (_serverInputQueue.Count > 0)
            {
                InputPayload inputPayload = _serverInputQueue.Dequeue();
                
                bufferIndex = inputPayload.tick % k_bufferSize;
                
                StatePayload statePayload = SimulateMovement(inputPayload);
                
                // Hier müsste doch was gelogged werden...
                //Debug.Log($"Current Payload HST: {statePayload.ToString()}; Pos: {statePayload.position}");
                
                _serverStateBuffer.Add(statePayload, bufferIndex);
                _serverCubeInstance.transform.position = statePayload.position.With(y: 58);
            }
            

            if (bufferIndex == -1) return;
            SendToClientRpc(_serverStateBuffer.Get(bufferIndex));
        }

        StatePayload SimulateMovement(InputPayload inputPayload)
        {
            Quaternion currentRotation = transform.rotation;
            _lastAngularVelocity = AngularVelocityHelper.CalculateAngularVelocity(_previousRotation, currentRotation, Time.fixedDeltaTime);
            _previousRotation = currentRotation;

            Physics.simulationMode = SimulationMode.Script;

            if (inputPayload.forceTeleport) // oder inputPayload.forceTeleport
            {
                controller.enabled = false;
                transform.position += transform.forward * 20f;
                controller.enabled = true;
                Debug.Log($"[Teleport] New Pos: {transform.position}");
            }

            
            Move(inputPayload.inputVector);
            Physics.Simulate(Time.fixedDeltaTime);
            Physics.simulationMode = SimulationMode.FixedUpdate;

            return new StatePayload()
            {
                tick = inputPayload.tick,
                position = transform.position,
                rotation = transform.rotation,
                velocity = controller.velocity,
                angularVelocity = _lastAngularVelocity,
            };
        }

        bool ShouldReconcile()
        {
            bool isNewServerState = !_lastServerState.Equals(default);
            bool isLastStateUndefindedOrDifferent = _lastProcessedState.Equals(default)
                                                    || !_lastProcessedState.Equals(_lastServerState);

            //Debug.Log("Yes, you should do that");
            
            return isNewServerState && isLastStateUndefindedOrDifferent;
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
            positionError = Vector3.Distance(rewindState.position, _clientStateBuffer.Get(bufferIndex).position);

            if (positionError > reconciliationThreshold)
            {
                Debug.Break();
                ReconcileState(rewindState);
            }

            _lastProcessedState = _lastServerState;
        }
 
        [ClientRpc]
        void SendToClientRpc(StatePayload statePayload)
        {
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
            };

           _cheatQueued = false;
           
            _clientInputBuffer.Add(inputPayload, bufferIndex);
            SendToServerRpc(inputPayload);
            
            StatePayload statePayload = ProcessMovement(inputPayload);

            _clientCubeInstance.transform.position = statePayload.position.With(y: 57);
            
            _clientStateBuffer.Add(statePayload, bufferIndex);
            HandleServerReconciliation();
        }

        void ReconcileState(StatePayload rewindState)
        {
            Debug.Log("it happened!");
            controller.enabled = false;
            transform.position = rewindState.position;
            transform.rotation = rewindState.rotation;
            _velocity = rewindState.velocity;
            _lastAngularVelocity = rewindState.angularVelocity;
            controller.enabled = true;
            
            if (rewindState.Equals(_lastServerState)) return;
            
            _clientStateBuffer.Add(rewindState, rewindState.tick);

            int tickToReplay = _lastServerState.tick + 1;

            while (tickToReplay < _timer.CurrentTick)
            {
                int bufferIndex = tickToReplay % k_bufferSize;
                StatePayload statePayload = ProcessMovement(_clientInputBuffer.Get(bufferIndex));
                _clientStateBuffer.Add(statePayload, bufferIndex);
                tickToReplay++;
            }
        }

        [ServerRpc]
        void SendToServerRpc(InputPayload input)
        {
            _serverInputQueue.Enqueue(input);
        }

        StatePayload ProcessMovement(InputPayload input)
        {
            // dead code?
            Quaternion currentRotation = transform.rotation;
            _lastAngularVelocity = AngularVelocityHelper.CalculateAngularVelocity(_previousRotation, currentRotation, Time.fixedDeltaTime);
            _previousRotation = currentRotation;
            
            if (input.forceTeleport) 
            {
                controller.enabled = false;
                transform.position += transform.forward * 20f;
                controller.enabled = true;
                Debug.Log($"[Teleport] New Pos: {transform.position}");
            }
            
            Move(input.inputVector);

            return new StatePayload()
            {
                tick = input.tick,
                position = transform.position,
                rotation = transform.rotation,
                velocity = controller.velocity,
                angularVelocity = _lastAngularVelocity,
            };
        }

        void Move(Vector3 inputVector)
        {
            if (DebugLogManager.IsConsoleOpen)
                return;
            
            _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            
            if (_isGrounded && _velocity.y < 0f)
                _velocity.y = -2f;

            Vector3 move = transform.right * inputVector.x + transform.forward * inputVector.z; // <== Z statt Y

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