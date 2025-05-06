using UnityEngine;

public class WeaponAim : MonoBehaviour
{
    public Transform cameraTransform; // First Person Kamera
    public Transform muzzle;          // Leeres GameObject an Laufspitze
    public float maxDistance = 100f;

    void Update()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        Vector3 targetPoint = ray.origin + ray.direction * maxDistance;

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            targetPoint = hit.point;

        // Debug Lines (Scene View only)
        Debug.DrawLine(cameraTransform.position, targetPoint, Color.red);
        Debug.DrawLine(muzzle.position, targetPoint, Color.green);

        // Volle Richtung (auch mit y!)
        Vector3 direction = targetPoint - muzzle.position;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Smoothes Nachziehen, wirkt natürlicher
            muzzle.rotation = Quaternion.Slerp(muzzle.rotation, targetRotation, Time.deltaTime * 20f);
        }
    }

}