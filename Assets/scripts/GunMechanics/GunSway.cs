using IngameDebugConsole;
using UnityEngine;

public class GunSway : MonoBehaviour
{
    [Header("Position Sway")] 
    public float amount = 0.02f;
    public float maxAmount = 0.06f;
    public float smoothAmount = 6f;

    [Header("Rotation Sway")] 
    public float rotationAmount = 2f;
    public float maxRotationAmount = 5f;
    public float smoothRotation = 12f;

    [Header("Recoil")] 
    public float returnSpeed = 8f;

    [Space]
    public bool rotationX = true;
    public bool rotationY = true;
    public bool rotationZ = true;

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    private float _inputX;
    private float _inputY;

    private Vector3 _recoilOffset;
    private Quaternion _recoilRotation = Quaternion.identity;

    private void Start()
    {
        _initialPosition = transform.localPosition;
        _initialRotation = transform.localRotation;
    }

    private void Update()
    {
        if (DebugLogManager.IsConsoleOpen)
            return;
        
        CalculateSway();
        ApplySway();
    }

    private void CalculateSway()
    {
        _inputX = -Input.GetAxis("Mouse X");
        _inputY = -Input.GetAxis("Mouse Y");
    }

    private void ApplySway()
    {
        // --- Position ---
        float moveX = Mathf.Clamp(_inputX * amount, -maxAmount, maxAmount);
        float moveY = Mathf.Clamp(_inputY * amount, -maxAmount, maxAmount);
        Vector3 finalPosition = new Vector3(moveX, moveY, 0);

        Vector3 targetPosition = _initialPosition + finalPosition + _recoilOffset;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * smoothAmount);
        _recoilOffset = Vector3.Lerp(_recoilOffset, Vector3.zero, Time.deltaTime * returnSpeed);

        // --- Rotation ---
        float tiltY = Mathf.Clamp(_inputX * rotationAmount, -maxRotationAmount, maxRotationAmount);
        float tiltX = Mathf.Clamp(_inputY * rotationAmount, -maxRotationAmount, maxRotationAmount);

        Quaternion finalRotation = Quaternion.Euler(
            rotationX ? -tiltX : 0f,
            rotationY ? tiltY : 0f,
            rotationZ ? tiltY : 0f
        );

        Quaternion targetRotation = finalRotation * _initialRotation * _recoilRotation;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * smoothRotation);
        _recoilRotation = Quaternion.Slerp(_recoilRotation, Quaternion.identity, Time.deltaTime * returnSpeed);
    }

    // External call to add recoil
    public void ApplyRecoil(Vector3 positionKickback, Vector3 rotationKickback)
    {
        _recoilOffset += positionKickback;
        _recoilRotation *= Quaternion.Euler(rotationKickback);
    }
}
