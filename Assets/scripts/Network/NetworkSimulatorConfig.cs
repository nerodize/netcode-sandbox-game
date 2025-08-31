using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkSimulatorConfig : MonoBehaviour
{
    public int maxPacketCount = 1000;
    public int maxPacketSize = 2000;
    public int packetDelayMs = 40;
    public int packetJitterMs = 0;
    public int packetLossPercent = 0;

    void Awake()
    {
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;

        if (transport == null)
        {
            Debug.LogError("UnityTransport not found on NetworkManager.");
            return;
        }

        // Set up SimulatorParameters
        var simulatorParams = new SimulatorUtility.Parameters
        {
            MaxPacketCount = maxPacketCount,
            MaxPacketSize = maxPacketSize,
            PacketDelayMs = packetDelayMs,
            PacketJitterMs = packetJitterMs,
            PacketDropPercentage = packetLossPercent
        };

        // Set the simulator parameters to the UnityTransport
        //transport.SetSimulatorParameters(simulatorParams);

        Debug.Log($"[Simulator] MaxPacketCount set to {maxPacketCount}, Delay: {packetDelayMs}ms, Jitter: {packetJitterMs}ms");
    }
}