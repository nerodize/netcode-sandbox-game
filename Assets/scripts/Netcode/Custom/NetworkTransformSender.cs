using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Sendet regelmäßig die eigene Position/Rotation an den Server,
/// der sie an alle Clients verteilt.
/// Clients interpolieren die empfangene Position.
/// </summary>
public class NetworkTransformSender : NetworkBehaviour
{
    [Header("Sende-Frequenz")]
    public float sendRate = 1f / 20f; // 20 Hz
    private float _sendTimer;

    [Header("Interpolation")]
    public bool interpolate = true;
    public float lerpSpeed = 10f;

    private Vector3 _targetPosition;
    private Quaternion _targetRotation;

    void Update()
    {
        if (IsOwner)
        {
            // Nur Owner sendet seine Transform-Daten regelmäßig
            _sendTimer += Time.deltaTime;
            if (_sendTimer >= sendRate)
            {
                _sendTimer = 0f;
                SendTransformServerRpc(transform.position, transform.rotation);
            }
        }
        else
        {
            // Nicht-Owner interpolieren zur Zielposition
            if (interpolate)
            {
                transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * lerpSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * lerpSpeed);
            }
        }
    }

    [ServerRpc]
    void SendTransformServerRpc(Vector3 pos, Quaternion rot)
    {
        BroadcastTransformClientRpc(pos, rot);
    }

    [ClientRpc]
    void BroadcastTransformClientRpc(Vector3 pos, Quaternion rot)
    {
        if (!IsOwner)
        {
            _targetPosition = pos;
            _targetRotation = rot;
        }
    }
}