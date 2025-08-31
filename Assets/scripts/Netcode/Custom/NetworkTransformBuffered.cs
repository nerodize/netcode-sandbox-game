using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class NetworkTransformBuffered : NetworkBehaviour
{
    [SerializeField] private float sendRate = 1f / 20f;
    private float _sendTimer;

    [SerializeField] private float interpolationDelay = 0.1f; 
    [SerializeField] private float positionThreshold = 0.001f;

    [Header("Features")]
    [SerializeField] private bool useInterpolation = true; 

    private struct State
    {
        public float timestamp;
        public Vector3 position;
        public Quaternion rotation;
    }

    private Queue<State> _stateBuffer = new();

    void Update()
    {
        if (IsOwner)
        {
            _sendTimer += Time.deltaTime;
            if (_sendTimer >= sendRate)
            {
                _sendTimer = 0f;
                float time = (float)NetworkManager.Singleton.LocalTime.Time;
                SendTransformServerRpc(transform.position, transform.rotation, time);
            }
        }
        else
        {
            float renderTime = (float)NetworkManager.Singleton.LocalTime.Time - interpolationDelay;
            if (useInterpolation)
                Interpolate(renderTime);
            else
                ApplyLatestState(); 
        }
    }

    [ServerRpc]
    void SendTransformServerRpc(Vector3 pos, Quaternion rot, float timestamp)
    {
        BroadcastTransformClientRpc(pos, rot, timestamp);
    }

    [ClientRpc]
    void BroadcastTransformClientRpc(Vector3 pos, Quaternion rot, float timestamp)
    {
        if (IsOwner) return;

        _stateBuffer.Enqueue(new State
        {
            timestamp = timestamp,
            position = pos,
            rotation = rot
        });

        while (_stateBuffer.Count > 10)
            _stateBuffer.Dequeue();
    }

    private void Interpolate(float renderTime)
    {
        while (_stateBuffer.Count >= 2)
        {
            State prev = _stateBuffer.Peek();
            State next = default;

            foreach (var state in _stateBuffer)
            {
                if (state.timestamp > renderTime)
                {
                    next = state;
                    break;
                }
                prev = state;
            }

            float t = Mathf.InverseLerp(prev.timestamp, next.timestamp, renderTime);

            if (Vector3.Distance(transform.position, next.position) > positionThreshold)
                transform.position = Vector3.Lerp(prev.position, next.position, t);

            transform.rotation = Quaternion.Slerp(prev.rotation, next.rotation, t);
            return;
        }
        
        ApplyLatestState();
    }
    
    private void ApplyLatestState()
    {
        if (_stateBuffer.Count > 0)
        {
            State latest = _stateBuffer.Peek();
            transform.position = latest.position;
            transform.rotation = latest.rotation;
        }
    }
}
