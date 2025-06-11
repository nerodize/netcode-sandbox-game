using UnityEngine;

/// <summary>
/// Hilfsmethode zur Berechnung der Winkelgeschwindigkeit zwischen zwei Rotationen.
/// </summary>
public static class AngularVelocityHelper
{
    /// <summary>
    /// Berechnet die Winkelgeschwindigkeit (angular velocity) basierend auf der Veränderung der Rotation zwischen zwei Zeitpunkten.
    /// </summary>
    /// <param name="previousRotation">Rotation beim vorherigen Tick</param>
    /// <param name="currentRotation">Aktuelle Rotation</param>
    /// <param name="deltaTime">Zeit zwischen den beiden Rotationen (z. B. Time.fixedDeltaTime)</param>
    /// <returns>Winkelgeschwindigkeit als Vektor in Grad/Sekunde</returns>
    public static Vector3 CalculateAngularVelocity(Quaternion previousRotation, Quaternion currentRotation, float deltaTime)
    {
        if (deltaTime <= 0f)
            return Vector3.zero;

        Quaternion deltaRotation = currentRotation * Quaternion.Inverse(previousRotation);
        deltaRotation.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);

        if (float.IsNaN(angleInDegrees) || rotationAxis == Vector3.zero)
            return Vector3.zero;

        float angularSpeedDeg = angleInDegrees / deltaTime;
        return rotationAxis.normalized * angularSpeedDeg;
    }
}