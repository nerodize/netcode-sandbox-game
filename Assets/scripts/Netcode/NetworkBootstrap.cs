using UnityEngine;
using Unity.Netcode;

public class NetworkBootstrap : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            NetworkManager.Singleton.StartHost();
            Debug.Log("▶ Host gestartet");
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            NetworkManager.Singleton.StartClient();
            Debug.Log("▶ Client gestartet");
        }
    }
}