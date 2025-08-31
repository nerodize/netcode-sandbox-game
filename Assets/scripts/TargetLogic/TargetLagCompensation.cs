using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

// TODO: safe delete

public class TargetLagCompensation : NetworkBehaviour
{
    struct BufferState
    {
        public Vector3 Position;
        public double Timestamp;
    }

    private readonly List<BufferState> _buffer = new();
    private const float BufferTime = 1.0f; // Sekunden zurück haltbar

    private void Update()
    {
        // Nur Server speichert BufferStates
        if (!IsServer) return;

        _buffer.Add(new BufferState
        {
            Position = transform.position,
            Timestamp = NetworkManager.Singleton.ServerTime.Time
        });

        // Nur aktuelle Buffer halten
        _buffer.RemoveAll(state => NetworkManager.Singleton.ServerTime.Time - state.Timestamp > BufferTime);
    }

    public Vector3? GetRewindPosition(double timestamp)
    {
        // Suche die zwei Buffer-Einträge um den gesuchten Zeitpunkt
        BufferState? older = null, newer = null;

        foreach (var state in _buffer)
        {
            if (state.Timestamp <= timestamp)
                older = state;
            else if (state.Timestamp > timestamp)
            {
                newer = state;
                break;
            }
        }

        // Wenn beide da, interpolieren
        if (older.HasValue && newer.HasValue)
        {
            float t = (float)((timestamp - older.Value.Timestamp) / (newer.Value.Timestamp - older.Value.Timestamp));
            return Vector3.Lerp(older.Value.Position, newer.Value.Position, t);
        }

        // Fallback auf letzten bekannten Stand
        return older?.Position;
    }
}