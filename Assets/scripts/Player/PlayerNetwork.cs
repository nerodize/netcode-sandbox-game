using UnityEngine;
using Unity.Netcode;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private GameObject cameraHolder;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            cameraHolder.SetActive(false);
            return;
        }

        cameraHolder.SetActive(true);
    }
}