using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            Debug.Log("OnClientConnectedCallback registriert");
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[Server] OnClientConnected für {clientId}");

        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[Server] Bin kein Server – ignoriere Spawn");
            return;
        }

        GameObject playerInstance = Instantiate(playerPrefab);
        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError("PlayerPrefab hat kein NetworkObject!");
            return;
        }

        netObj.SpawnAsPlayerObject(clientId);
        Debug.Log($"[Server] Spieler für Client {clientId} gespawnt.");
    }

}