using UnityEngine;

public class GunRecoil : MonoBehaviour
{
    public Vector3 recoilKickBack = new Vector3(0f, 0f, -0.1f); // Rückstoßrichtung
    public Vector3 recoilRotation = new Vector3(-2f, 1f, 0f);   // Hoch & leicht zur Seite
    public float returnSpeed = 5f;
    public float rotationSpeed = 7f;

    private Vector3 _originalPosition;
    private Quaternion _originalRotation;

    private Vector3 _currentKick;
    private Quaternion _currentRot;

    void Start()
    {
        _originalPosition = transform.localPosition;
        _originalRotation = transform.localRotation;
        _currentRot = _originalRotation;
    }

    void Update()
    {
        // Sanftes Zurückgleiten
        transform.localPosition = Vector3.Lerp(transform.localPosition, _originalPosition, Time.deltaTime * returnSpeed);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, _originalRotation, Time.deltaTime * rotationSpeed);
    }

    public void ApplyRecoil()
    {
        transform.localPosition += recoilKickBack;
        transform.localRotation *= Quaternion.Euler(recoilRotation);
    }
}