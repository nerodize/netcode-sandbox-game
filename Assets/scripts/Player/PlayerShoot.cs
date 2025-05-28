using UnityEngine;
using System;
using IngameDebugConsole;
using Unity.Netcode;

public class PlayerShoot : NetworkBehaviour
{
    public static Action shootInput;
    public static Action reloadInput;

    [SerializeField] private KeyCode reloadKey;

    private void Update()
    {
        if (!IsOwner) return;
        
        if (DebugLogManager.IsConsoleOpen)
            return;
        
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Mouse Click detected!!");
            // avoiding null reference Exception by disallowing no subs.
            shootInput?.Invoke();
        }

        if (Input.GetKeyDown(reloadKey))
            reloadInput?.Invoke();
    }
}
