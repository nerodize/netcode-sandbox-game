using UnityEngine;
using IngameDebugConsole;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class ConsoleCommands : NetworkBehaviour
{
    [ConsoleMethod("sensitivity", "Sets the mouse sensitivity via console")]
    public static void SetMouseSensitivity(float sens)
    {
        FindFirstObjectByType<MouseLook>()?.SetSensitivity(sens);
        Debug.Log($"Sensitivity set to {sens}");
    }

    [ConsoleMethod("disconnect", "Disconnect from the server")]
    public static void Disconnect()
    {
        if (NetworkManager.Singleton == null) return;
        
        if (NetworkManager.Singleton.IsClient)
        {
            Unity.Netcode.NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("SampleScene");
        }
    }

    [ConsoleMethod("clear", "Clear the console")]
    public static void ClearConsole()
    {
        DebugLogManager.Instance.ClearLogs();
    }
}
