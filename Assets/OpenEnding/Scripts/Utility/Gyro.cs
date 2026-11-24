using UnityEngine;
using UnityEngine.InputSystem;

namespace Utility
{
    public class Gyro : MonoBehaviour
    {
        public Vector3 gravity = Vector3.zero;
        
        private void Awake()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (GravitySensor.current != null) InputSystem.EnableDevice(GravitySensor.current);
#endif
        }

        private void Update()
        {
            if (GravitySensor.current == null || !GravitySensor.current.enabled) return;

            gravity = GravitySensor.current.gravity.ReadValue().normalized;
        }
    }
}