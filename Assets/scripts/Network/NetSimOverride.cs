using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetSimOverride : MonoBehaviour
{
    [Header("Testprofile")]
    [SerializeField] int rttMs = 200;     // round-trip time
    [SerializeField] int jitterMs = 0;
    [SerializeField] int lossPercent = 0;

    void Awake()
    {
        var utp = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        if (utp != null)
        {
            utp.SetDebugSimulatorParameters(
                packetDelay: Mathf.Max(0, rttMs / 2),
                packetJitter: Mathf.Max(0, jitterMs),
                dropRate: Mathf.Clamp(lossPercent, 0, 100)
            );
        }
        
        Debug.Log($"[NetSim] Applied: RTT≈{rttMs} ms, Jitter={jitterMs} ms, Loss={lossPercent}%");
    }
}