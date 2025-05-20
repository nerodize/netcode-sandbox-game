using Unity.Netcode;
using UnityEngine;

public class PlayerSetup : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;

    private void Start()
    {
        bool isLocal = IsOwner;

        if (playerCamera != null)
            playerCamera.enabled = isLocal;

        if (audioListener != null)
            audioListener.enabled = isLocal;
    }
}