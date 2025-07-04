using UnityEngine;
using System;

namespace Utilities
{
    public static class Vector2Extensions
    {
        public static Vector2 With(this Vector2 vector, float? x = null, float? y = null)
            => new Vector2(x ?? vector.x, y ?? vector.y);

        public static Vector2 Add(this Vector2 vector, float? x = null, float? y = null)
            => new Vector2(vector.x + (x ?? 0), vector.y + (y ?? 0));

        public static Vector3 ToVector3(this Vector2 v2, float y = 0)
            => new Vector3(v2.x, y, v2.y);
    }

    public static class Vector3Extensions
    {
        public static Vector3 With(this Vector3 vector, float? x = null, float? y = null, float? z = null) {
            return new Vector3(x ?? vector.x, y ?? vector.y, z ?? vector.z);
        }

        public static Vector3 Add(this Vector3 vector, float? x = null, float? y = null, float? z = null)
            => new Vector3(vector.x + (x ?? 0), vector.y + (y ?? 0), vector.z + (z ?? 0));

        public static Vector2 ToVector2(this Vector3 v3)
            => new Vector2(v3.x, v3.z);
    }

    public static class QuaternionExtensions
    {
        // Set individual euler angles
        public static Quaternion WithEuler(this Quaternion quat, float? x = null, float? y = null, float? z = null)
        {
            var euler = quat.eulerAngles;
            return Quaternion.Euler(x ?? euler.x, y ?? euler.y, z ?? euler.z);
        }

        // Add to euler angles
        public static Quaternion AddEuler(this Quaternion quat, float? x = null, float? y = null, float? z = null)
        {
            var euler = quat.eulerAngles;
            return Quaternion.Euler(euler.x + (x ?? 0), euler.y + (y ?? 0), euler.z + (z ?? 0));
        }
    }

    public static class TransformExtensions
    {
        public static void SetLocalPos(this Transform t, float? x = null, float? y = null, float? z = null)
        {
            Vector3 pos = t.localPosition;
            t.localPosition = new Vector3(x ?? pos.x, y ?? pos.y, z ?? pos.z);
        }

        public static void SetWorldPos(this Transform t, float? x = null, float? y = null, float? z = null)
        {
            Vector3 pos = t.position;
            t.position = new Vector3(x ?? pos.x, y ?? pos.y, z ?? pos.z);
        }

        public static void AddLocalPos(this Transform t, float? x = null, float? y = null, float? z = null)
        {
            Vector3 pos = t.localPosition;
            t.localPosition = new Vector3(pos.x + (x ?? 0), pos.y + (y ?? 0), pos.z + (z ?? 0));
        }
    }

    public static class MathfExtensions
    {
        // Remaps a value from one range to another
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
        }

        // Clamp a float between min and max (just for completeness, Mathf already has Clamp)
        public static float Clamp(this float value, float min, float max)
        {
            return Mathf.Clamp(value, min, max);
        }
    }
    public abstract class Timer {
        protected float initialTime;
        protected float Time { get; set; }
        public bool IsRunning { get; protected set; }
        
        public float Progress => Time / initialTime;
        
        public Action OnTimerStart = delegate { };
        public Action OnTimerStop = delegate { };

        protected Timer(float value) {
            initialTime = value;
            IsRunning = false;
        }

        public void Start() {
            Time = initialTime;
            if (!IsRunning) {
                IsRunning = true;
                OnTimerStart.Invoke();
            }
        }

        public void Stop() {
            if (IsRunning) {
                IsRunning = false;
                OnTimerStop.Invoke();
            }
        }
        
        public void Resume() => IsRunning = true;
        public void Pause() => IsRunning = false;
        
        public abstract void Tick(float deltaTime);
    }
    
    public class CountdownTimer : Timer {
        public CountdownTimer(float value) : base(value) { }

        public override void Tick(float deltaTime) {
            if (IsRunning && Time > 0) {
                Time -= deltaTime;
            }
            
            if (IsRunning && Time <= 0) {
                Stop();
            }
        }
        
        public bool IsFinished => Time <= 0;
        
        public void Reset() => Time = initialTime;
        
        public void Reset(float newTime) {
            initialTime = newTime;
            Reset();
        }
    }
    
    public class StopwatchTimer : Timer {
        public StopwatchTimer() : base(0) { }

        public override void Tick(float deltaTime) {
            if (IsRunning) {
                Time += deltaTime;
            }
        }
        
        public void Reset() => Time = 0;
        
        public float GetTime() => Time;
    }
    
}
