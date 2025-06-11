using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
using IngameDebugConsole;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;

public class NetworkOverlay : NetworkBehaviour
{
    private float _lastPingTime;
    private float _roundTripTime;
    private NetworkSimulator _networkSimulator;

    private static UnityTransport _transport;

    void Awake()
    {
        _networkSimulator = GetComponent<NetworkSimulator>();
    }
    
    void Start()
    {
        if (_transport == null)
        {
            _transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        }

        if (IsClient && IsOwner)
        {
            InvokeRepeating(nameof(SendPing), 1f, 1f);
        }
    }

    void OnGUI()
    {
        if (IsClient && IsOwner)
        {
            GUI.Label(new Rect(10, 40, 300, 20), $"Ping: {_roundTripTime * 500:F0} ms");
        }
    }

    void SendPing()
    {
        _lastPingTime = Time.time;
        RequestPingServerRpc();
    }

    [ServerRpc]
    void RequestPingServerRpc(ServerRpcParams rpcParams = default)
    {
        RespondPongClientRpc(rpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    void RespondPongClientRpc(ulong clientId)
    {
        if (IsOwner && clientId == NetworkManager.Singleton.LocalClientId)
        {
            _roundTripTime = Time.time - _lastPingTime;
        }
    }
}
    