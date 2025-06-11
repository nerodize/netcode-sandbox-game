using UnityEngine;
using UnityEngine.InputSystem;

using static Player.PlayerInputActions;

namespace Player {
    [CreateAssetMenu(fileName = "InputReader", menuName = "Player/Input Reader")]
    public class InputReader : ScriptableObject, IPlayerActions, ICharacterMovement {
        public Vector3 Move => new(
            inputActions.Player.Move.ReadValue<Vector2>().x, // strafe (A/D)
            0f,
            inputActions.Player.Move.ReadValue<Vector2>().y  // forward/backward (W/S)
        );
        public bool JumpPressed => inputActions.Player.Jump.ReadValue<float>() > 0;

        PlayerInputActions inputActions;
        
        void OnEnable() {
            if (inputActions == null) {
                inputActions = new PlayerInputActions();
                inputActions.Player.SetCallbacks(this);
            }
        }
        
        public void Enable() {
            inputActions.Enable();
        }
        
        public void OnMove(InputAction.CallbackContext context) {
            // noop
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            // noop
        }

        public void OnLook(InputAction.CallbackContext context) {
            // noop
        }

        public void OnFire(InputAction.CallbackContext context) {
            // noop
        }
    }
}