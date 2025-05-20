using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;
    [SerializeField] private MonoBehaviour[] scriptsToEnableForOwner;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            playerCamera.gameObject.SetActive(true);
            audioListener.enabled = true;

            foreach (var script in scriptsToEnableForOwner)
            {
                script.enabled = true;
            }
        }
        else
        {
            playerCamera.gameObject.SetActive(false);
            audioListener.enabled = false;

            foreach (var script in scriptsToEnableForOwner)
            {
                script.enabled = false;
            }
        }
    }
}