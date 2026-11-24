using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Utility
{
    public class Shake : MonoBehaviour
    {
        private bool isShakeable = true;
        public float threshold = 2f;
        public WaitForSecondsRealtime throttle = new WaitForSecondsRealtime(2f);
        public delegate void ShakeHandler();

        public event ShakeHandler OnEveryShake;
        public event ShakeHandler OnNextShake;

        private void Awake()
        {
            if (Accelerometer.current != null) InputSystem.EnableDevice(Accelerometer.current);
        }

        private void Update()
        {
            if (!isShakeable) return;
            if (Accelerometer.current == null || !Accelerometer.current.enabled) return;

            // Android에서 Input System은 가속도를 옛 Input.acceleration과 같은 g 단위로 준다. 기준값 threshold도 g 단위다
            var value = Accelerometer.current.acceleration.ReadValue().magnitude;
            
            if (value > threshold)
            {
                // 흔들기 기준값을 맞추기 위한 기록. 기준을 정하면 지운다
                $"Shake detected : {value:F2}g".Log();
                OnEveryShake?.Invoke();
                OnNextShake?.Invoke();
                OnNextShake = null;

                StartCoroutine(ThrottleRoutine());
            }
        }

        private IEnumerator ThrottleRoutine()
        {
            isShakeable = false;
            yield return throttle;
            isShakeable = true;
        }
    }
}