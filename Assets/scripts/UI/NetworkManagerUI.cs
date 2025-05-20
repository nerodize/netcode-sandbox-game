using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

/// <summary>
/// Eigentlich sinnvoller im Netcode Folder...
/// </summary>
public class NetworkManagerUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button serverButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private GameObject lobbyUI;
    [SerializeField] private Camera lobbyCamera;
    
    [Header("Network Settings")]
    [SerializeField] private string serverAddress = "127.0.0.1";
    [SerializeField] private ushort port = 7778;

    private void Start()
    {
        ShowLocalIP();

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton ist null!");
            return;
        }
        
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        // Initialer UI Zustand
        if (lobbyCamera != null) lobbyCamera.gameObject.SetActive(true);
        else Debug.LogWarning("LobbyCamera fehlt!");

        if (lobbyUI != null) lobbyUI.SetActive(true);
        else Debug.LogWarning("LobbyUI fehlt!");
        
        if (serverButton != null)
        {
            serverButton.onClick.AddListener(() =>
            {
                var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                transport.ConnectionData.Port = port;

                bool started = NetworkManager.Singleton.StartServer();
                Debug.Log($"[UI] Server gestartet: {started}");
            });
        }

        if (hostButton != null)
        {
            hostButton.onClick.AddListener(() =>
            {
                var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                transport.ConnectionData.Address = serverAddress;
                transport.ConnectionData.Port = port;

                bool started = NetworkManager.Singleton.StartHost();
                Debug.Log($"[UI] Host gestartet: {started}");

                if (started) HideLobbyUI();
            });
        }

        if (clientButton != null)
        {
            clientButton.onClick.AddListener(async () =>
            {
                var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                transport.ConnectionData.Address = serverAddress;
                transport.ConnectionData.Port = port;

                Debug.Log($"[UI] Versuche Verbindung zu {serverAddress}:{port}...");

                // Might be useless => only makes it feel clunky
                await Task.Delay(500);

                bool started = NetworkManager.Singleton.StartClient();
                Debug.Log($"[UI] Client gestartet: {started}");

                if (started) HideLobbyUI();
            });
        }
    }

    private void HideLobbyUI()
    {
        if (lobbyUI != null) lobbyUI.SetActive(false);
        if (lobbyCamera != null) lobbyCamera.gameObject.SetActive(false);
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[GLOBAL] Client verbunden mit ID: {clientId}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[GLOBAL] Client getrennt mit ID: {clientId}");
    }

    private void ShowLocalIP()
    {
        string localIP = GetLocalIPAddress();
        Debug.Log($"[Diagnose] Lokale IP-Adresse: {localIP}");
    }

    private string GetLocalIPAddress()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530); 
            IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint?.Address.ToString();
        }
        catch
        {
            return "Unbekannt";
        }
    }
    
    // Debug Purposes
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
