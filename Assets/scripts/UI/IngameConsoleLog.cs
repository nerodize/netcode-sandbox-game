using UnityEngine;
using TMPro;
using UnityEngine.UI;

class IngameConsoleLog : MonoBehaviour
{
    [SerializeField] private TMP_Text logText;
    //[SerializeField] private ScrollRect scrollRect;

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }
    
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        logText.text = $"{logString}, {stackTrace}, {type} \n";
        
        Canvas.ForceUpdateCanvases();
        //scrollRect.verticalNormalizedPosition = 0f;
    }
}
