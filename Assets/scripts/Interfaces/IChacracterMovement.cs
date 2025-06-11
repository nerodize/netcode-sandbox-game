using UnityEngine;

public interface ICharacterMovement {
    Vector3 Move { get; } // x = strafing, y = forward/back
    bool JumpPressed { get; }
    void Enable();
}