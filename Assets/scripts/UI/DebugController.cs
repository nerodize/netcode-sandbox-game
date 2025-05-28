using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DebugController : MonoBehaviour
{
   private bool _showConsole;

   public void OnToggleDebug()
   {
      _showConsole = !_showConsole;
   }
   
   private void OnGUI()
   {
      if (!_showConsole) return;

      float y = 0f;
      
      GUI.Box(new Rect(0, y, Screen.width, 30), "");
   }
}