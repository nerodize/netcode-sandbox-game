using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Unity.Netcode;
using UnityEngine;
using IngameDebugConsole;
using Utilities;
using System.Linq;
using System.Text;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine.Android;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

namespace Player
{
    public struct InputPayload : INetworkSerializable
    {
        public int tick;
        public Vector3 inputVector;
        public DateTime timestamp;
        public ulong networkObjectId;
        public bool forceTeleport;
        public Vector3 position;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref inputVector);
            serializer.SerializeValue(ref timestamp);
            serializer.SerializeValue(ref networkObjectId);
            serializer.SerializeValue(ref forceTeleport);
            serializer.SerializeValue(ref position);
        }
    }

    public struct StatePayload : INetworkSerializable
    {
        public int tick;
        public Vector3 position;
        public ulong networkObjectId;
        public Quaternion rotation;
        public Vector3 velocity;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref networkObjectId);
            serializer.SerializeValue(ref rotation);
            serializer.SerializeValue(ref velocity);
        }
    }
    
    public class PlayerMovement : NetworkBehaviour
    {
        #region Variables

        [Header("Movement")] 
        [SerializeField] private CharacterController controller;
        [SerializeField] private float speed = 12f;
        [SerializeField] private float jumpHeight = 3f;

        [Header("Jumping")] 
        [SerializeField] private Transform groundCheck;
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
        [SerializeField] private float reconciliationThreshold = 1f;
        [SerializeField] float reconcilationCooldownTime = 5f;
        [SerializeField] private float extrapolationLimit = 0.5f;
        [SerializeField] private float extrapolationMultiplier = 1.2f;
        [SerializeField] private GameObject serverSphere;
        [SerializeField] private GameObject clientSphere;
        ClientNetworkTransform _clientNetworkTransform;

        private StatePayload _extrapolationState;
        private CountdownTimer _extrapolationTimer;
        
        private CountdownTimer _reconciliationTimer;
        
        // Evaluation metrics
        // fields
        private StringBuilder _metricsBuffer = new StringBuilder();
        private string _metricsPath;
        private int _reconcileCount = 0;
        private float _posErrorSum = 0f;
        private int _posErrorCount = 0;
        private float _posErrorMax = 0f;
        private bool _reconciledThisTick = false;
        private float _lastPosError = 0f;
        private bool _cheatQueued;
        #endregion

        void Awake()
        {
            playerInput.Enable();
            _input = playerInput;
            _clientNetworkTransform = GetComponent<ClientNetworkTransform>();
            
            _timer = new NetworkTimer(k_serverTickRate);
            _clientStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
            _clientInputBuffer = new CircularBuffer<InputPayload>(k_bufferSize);
            _serverStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
            _serverInputQueue = new Queue<InputPayload>();
            
            _reconciliationTimer = new CountdownTimer(reconcilationCooldownTime);
            _extrapolationTimer = new CountdownTimer(extrapolationLimit);

            _reconciliationTimer.OnTimerStart += () =>
            {
                _extrapolationTimer.Stop();
            };
            
            _extrapolationTimer.OnTimerStart+= () =>
            {
                _reconciliationTimer.Stop();
                SwitchAuthorityMode(AuthorityMode.Server);
            };

            _extrapolationTimer.OnTimerStop += () =>
            {
                _extrapolationState = default;
                SwitchAuthorityMode(AuthorityMode.Client);
            };
        }

        void SwitchAuthorityMode(AuthorityMode mode)
        {
            _clientNetworkTransform.authorityMode = mode;
            bool shouldSync = mode == AuthorityMode.Client;
            _clientNetworkTransform.SyncPositionX = shouldSync;
            _clientNetworkTransform.SyncPositionY = shouldSync;
            _clientNetworkTransform.SyncPositionZ = shouldSync;
        }

        void Start()
        {
            clientSphere.transform.SetParent(null);
            serverSphere.transform.SetParent(null);
            
            Debug.Log(Application.persistentDataPath);
            
            var fname = $"metrics_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            _metricsPath = Path.Combine(Application.persistentDataPath, fname);

            // Header
            _metricsBuffer.AppendLine("client_ping;tick;time_s;is_host;client_id;client_x;client_y;client_z;server_x;server_y;server_z;pos_error;pos_error_sum;vel_error;extrap_running;reconciled;reconcile_count");//magnitude könnte hier noch rein
            // Schreibe Header sofort
            File.WriteAllText(_metricsPath, _metricsBuffer.ToString());
            _metricsBuffer.Clear();
        }

        void Update()
        {
            //if (!IsOwner) return;
            _timer.Update(Time.deltaTime);
            _reconciliationTimer.Tick(Time.deltaTime);
            _extrapolationTimer.Tick(Time.deltaTime);
            Extrapolate();
            
            // cheat
            if (Input.GetKeyDown(KeyCode.Q))
            {
                _cheatQueued = true;
                Debug.Log("Q is pressed!"); 
            }
        }
        
        void FixedUpdate()
        {
            _reconciledThisTick = false;
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
            
            // BUG: hierduch entseht das komische Sprungverhalten als Host
            // TODO: (wenn am Ende noch Zeit) Authority trennen am besten in den RPCs 
            if (IsServer)
            {
                while (_timer.ShouldTick())
                    HandleServerTick();
            }
            Extrapolate();
        }
        
        void OnApplicationQuit()
        {
            if (_metricsBuffer.Length > 0)
                File.AppendAllText(_metricsPath, _metricsBuffer.ToString());
            Debug.Log($"Metrics written to {_metricsPath}");
        }
        
        void HandleServerTick()
        {
            if (!IsServer) return;
            
            var bufferIndex = -1;
            InputPayload inputPayload = default;
            
            while (_serverInputQueue.Count > 0)
            {
                 inputPayload = _serverInputQueue.Dequeue();
                
                bufferIndex = inputPayload.tick % k_bufferSize;
                
                StatePayload statePayload = ProcessMovement(inputPayload, false, false);
                serverSphere.transform.position = statePayload.position.With(y: inputPayload.position.y + 3);
                _serverStateBuffer.Add(statePayload, bufferIndex);
            }
            
            if (bufferIndex == -1) return;
            SendToClientRpc(_serverStateBuffer.Get(bufferIndex));
            Debug.LogWarning($"in Server Tick: {CalculateLatencyInMs(inputPayload)}");
            HandleExtrapolation(_serverStateBuffer.Get(bufferIndex), 0.4f);
        }

        void Extrapolate()
        {
            if (IsClient && IsOwner &&  _extrapolationTimer.IsRunning)
            {
                Debug.LogWarning("Extrapolate()");
                // diff = || += ? (= müsste stimmen)
                Debug.LogWarning($"Extrap pos: {_extrapolationState.position}");
                
                Vector3 delta = _extrapolationState.position * Time.fixedDeltaTime;
                transform.position += delta.With(y: 0); 
                //serverSphere.transform.position = _extrapolationState.position.With(y: 53);
            }
        }

        void HandleExtrapolation(StatePayload latest, float latency) {
            if (ShouldExtrapolate(latency)) {
                if (_extrapolationState.position != default) {
                    latest = _extrapolationState;
                }
                
                Debug.LogWarning("HandleExtrapolation()");

                var posAdjustment = latest.velocity * (1 + 0.7f * extrapolationMultiplier);
                _extrapolationState.position = posAdjustment;
                _extrapolationState.rotation = latest.rotation; // oder einfach: transform.rotation
                _extrapolationState.velocity = latest.velocity;
                _extrapolationTimer.Start();
            } else {
                _extrapolationTimer.Stop();
            }
        }
        
        bool ShouldExtrapolate(float latency) => latency < extrapolationLimit && latency > Time.fixedDeltaTime;
        //bool ShouldExtrapolate(float latency) => latency > 0;

        bool ShouldReconcile()
        {
            bool isNewServerState = !_lastServerState.Equals(default);
            bool isLastStateUndefindedOrDifferent = _lastProcessedState.Equals(default)
                                                    || !_lastProcessedState.Equals(_lastServerState);
            
            return isNewServerState && isLastStateUndefindedOrDifferent && !_reconciliationTimer.IsRunning && !_extrapolationTimer.IsRunning;
        }

        bool HandleServerReconciliation()
        {
            if (_lastServerState.tick <= 1) return false;
            
            if (!ShouldReconcile()) return false;
            
            float positionError;
            int bufferIndex;
            StatePayload rewindState = default;
            
            bufferIndex = _lastServerState.tick % k_bufferSize;
            if (bufferIndex - 1 < 0) return false;

            rewindState = IsHost ? _serverStateBuffer.Get(bufferIndex - 1) : _lastServerState;
            StatePayload clientState = IsHost ? _clientStateBuffer.Get(bufferIndex - 1) : _clientStateBuffer.Get(bufferIndex); //diff
            positionError = Vector3.Distance(rewindState.position, clientState.position);
            
            if (positionError > reconciliationThreshold)
            {
                if (rewindState.position != transform.position)
                {
                    Debug.LogWarning($"[Reconciliation] @{rewindState.tick} ΔPos: {positionError:F3} → Rewinding to tick {_lastServerState.tick}");
                    ReconcileState(rewindState);
                    _lastProcessedState = rewindState;
                    _reconciledThisTick = true;
                    _reconcileCount++;
                    _lastPosError = positionError;
                    _reconciliationTimer.Start(); 
                }
                else
                {
                    Debug.Log($"[Reconciliation skipped] No delta despite ΔPos > threshold");
                }
            }
            return _reconciledThisTick;
        }

        static float CalculateLatencyInMs(InputPayload inputPayload)
        {
            return (DateTime.Now - inputPayload.timestamp).Milliseconds / 1000f;
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
                timestamp = DateTime.Now,
                networkObjectId = NetworkObjectId,
                inputVector = _input.Move,
                forceTeleport = _cheatQueued,
                position = transform.position,
            };
            _cheatQueued = false;

            _clientInputBuffer.Add(inputPayload, bufferIndex);
            SendToServerRpc(inputPayload);
            
            //bool shouldSimulate = !IsHost || IsServer;
            
            StatePayload statePayload = ProcessMovement(inputPayload, false, true);
            clientSphere.transform.position = statePayload.position.With(y: statePayload.position.y + 5);
            _clientStateBuffer.Add(statePayload, bufferIndex);
          

            // TODO: evaluate whether useful
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
            
            bool didReconcile = HandleServerReconciliation();
            
            Debug.LogWarning($"In Client Tick: {CalculateLatencyInMs(inputPayload)}");
            LogMetrics(currentTick, statePayload.position, statePayload.velocity, didReconcile); 
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
            //_reconciledThisTick = true;
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
                networkObjectId = input.networkObjectId,
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
            Vector3 horizontalVelocity = move.normalized * speed;
            
            _velocity.x = horizontalVelocity.x;
            _velocity.z = horizontalVelocity.z;
            
            if (playerInput.JumpPressed && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            // Falling
            _velocity.y += gravity * Time.fixedDeltaTime;
            controller.Move(_velocity * Time.fixedDeltaTime);
        }
        
        #region Metrics
        void LogMetrics(int tick, Vector3 clientPos, Vector3 clientVel, bool reconciled)
        {
            if (_lastServerState.Equals(default)) return; // kein Serverstate vorhanden

            var serverPos = _lastServerState.position;
            var serverVel = _lastServerState.velocity;

            float posError = reconciled ? _lastPosError : Vector3.Distance(clientPos, serverPos);
            float velError = Vector3.Distance(clientVel, serverVel);
            bool extrapRunning = _extrapolationTimer != null && _extrapolationTimer.IsRunning;
           
            //float reconcileMagnitude = 0f; // might be redundant

            // Update running aggregates
            _posErrorSum += posError;
            _posErrorCount++;
            if (posError > _posErrorMax) _posErrorMax = posError;

            // Line
            var line = $"{NetworkOverlay.roundTripTime * 500:F0}ms;{tick};{Time.realtimeSinceStartup:F3};{IsHost};{OwnerClientId};{clientPos.x:F3};{clientPos.y:F3};{clientPos.z:F3};" +
                       $"{serverPos.x:F3};{serverPos.y:F3};{serverPos.z:F3};{posError:F4};{_posErrorSum:F4};{velError:F4};" +
                       $"{(extrapRunning?1:0)};{(reconciled?1:0)};{_reconcileCount}"; //magnitude könnte hier ebenfalls noch stehen...

            // Buffer und periodisch flush
            _metricsBuffer.AppendLine(line);
            if (_metricsBuffer.Length > 16384) // flush alle ~16KB
            {
                File.AppendAllText(_metricsPath, _metricsBuffer.ToString());
                _metricsBuffer.Clear();
            }
        }
        #endregion
    }
}