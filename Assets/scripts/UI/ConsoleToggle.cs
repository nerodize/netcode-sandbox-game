using TMPro;
using UnityEngine;
using UnityEngine.UI;

class ConsoleToggle : MonoBehaviour
{
    [SerializeField] private GameObject consolePanel;
    [SerializeField] private TMP_InputField inputField;
    private bool _isVisible = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            _isVisible = !_isVisible;
            consolePanel.SetActive(_isVisible);
            
            Cursor.visible = _isVisible;
            Cursor.lockState = _isVisible ? CursorLockMode.None : CursorLockMode.Locked;
            
            InputState.InputLocked = _isVisible;

            if (_isVisible)
                inputField.ActivateInputField();
        }
    }
}


public static class InputState
{
    public static bool inputLocked = false;

   public static bool InputLocked
   {
       get => inputLocked;
       set => inputLocked = value;
   }
}