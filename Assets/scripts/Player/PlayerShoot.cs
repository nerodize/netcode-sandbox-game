using UnityEngine;
using System;

public class PlayerShoot : MonoBehaviour
{
    public static Action shootInput;
    public static Action reloadInput;

    [SerializeField] private KeyCode reloadKey;

    private void Update()
    {
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
