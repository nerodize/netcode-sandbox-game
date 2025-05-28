using IngameDebugConsole;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

public class GunBobbing : MonoBehaviour
{
    [Header("General")]
    public float smoothing = 6f;

    [Header("Idle Bobbing")]
    public float idleFrequency = 1.5f;
    public float idleAmplitude = 0.003f;

    [Header("Walk Bobbing")]
    public float walkFrequency = 6f;
    public float walkAmplitude = 0.01f;

    private Vector3 _startPosition;
    private float _bobTimer;

    private void Start()
    {
        _startPosition = transform.localPosition;
    }

    private void Update()
    {
        if (DebugLogManager.IsConsoleOpen)
            return;
        
        Vector2 movementInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        bool isMoving = movementInput.magnitude > 0.1f;

        if (isMoving)
        {
            ApplyBobbing(walkFrequency, walkAmplitude);
        }
        else
        {
            ApplyBobbing(idleFrequency, idleAmplitude);
        }
    }

    private void ApplyBobbing(float frequency, float amplitude)
    {
        _bobTimer += Time.deltaTime * frequency;
        float offsetY = Mathf.Sin(_bobTimer) * amplitude;
        float offsetX = Mathf.Cos(_bobTimer * 2f) * amplitude * 0.5f;

        Vector3 targetPosition = _startPosition + new Vector3(offsetX, offsetY, 0);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * smoothing);
    }
}