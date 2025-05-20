using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public class DiagnoseNetworkManager : MonoBehaviour
{
    [SerializeField] private string serverAddress = "127.0.0.1"; // Ändern bei LAN
    [SerializeField] private ushort port = 7778;
    [SerializeField] private Button startServerButton;
    [SerializeField] private Button startClientButton;
    
    private void Awake()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {
            Debug.Log($"[GLOBAL] Client verbunden mit ID: {id}");
        };
    }
    
    private void Start()
    {
        ShowLocalIP();

        startServerButton.onClick.AddListener(() =>
        {
            var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            transport.ConnectionData.Port = port;

            bool started = NetworkManager.Singleton.StartServer();
            Debug.Log($"[Diagnose] Server gestartet: {started}");
        });

        
        startClientButton.onClick.AddListener(async () =>
        {
            var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            transport.ConnectionData.Address = serverAddress;
            transport.ConnectionData.Port = port;

            await Task.Delay(500);

            bool started = NetworkManager.Singleton.StartClient();
            Debug.Log($"Client gestartet: {started}");
        });
        
    }

    private async void Call()
    {
        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        transport.ConnectionData.Address = serverAddress;
        transport.ConnectionData.Port = port;

        Debug.Log($"[Diagnose] Pinge Server {serverAddress}...");
        bool pingSuccess = await PingAddress(serverAddress);
        Debug.Log($"[Diagnose] Ping erfolgreich: {pingSuccess}");

        bool started = NetworkManager.Singleton.StartClient();
        Debug.Log($"[Diagnose] Client gestartet: {started}");
    }

    private void ShowLocalIP()
    {
        string localIP = GetLocalIPAddress();
        Debug.Log($"[Diagnose] Lokale IP: {localIP}");
    }

    private string GetLocalIPAddress()
    {
        string localIP = "Unbekannt";
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530); // Google DNS
            IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            localIP = endPoint?.Address.ToString();
        }
        catch { }
        return localIP;
    }

    private async Task<bool> PingAddress(string ip)
    {
        try
        {
            using var ping = new System.Net.NetworkInformation.Ping();
            var reply = await ping.SendPingAsync(ip, 1000);
            return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }
}
