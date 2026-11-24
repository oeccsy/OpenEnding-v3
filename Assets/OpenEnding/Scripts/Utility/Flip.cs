using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Utility
{
    public class Flip : MonoBehaviour
    {
        [SerializeField]
        private Define.DisplayedFace curFace = Define.DisplayedFace.None;
        public Define.DisplayedFace CurFace => curFace;
        public delegate void FlipHandler();
        public event FlipHandler OnFlipToHead;
        public event FlipHandler OnFlipToTail;
        public event FlipHandler OnStand;

        public delegate void NextFlipHandler();
        public event NextFlipHandler OnNextHead;
        public event NextFlipHandler OnNextTail;
        public event NextFlipHandler OnNextStand;

        private void Awake()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (GravitySensor.current != null) InputSystem.EnableDevice(GravitySensor.current);
#endif
        }
    
        private void Update()
        {
            if (GravitySensor.current == null || !GravitySensor.current.enabled) return;

            // 센서 단위와 상관없이 기울어진 방향만 보도록 정규화한다. 기준값은 중력 크기를 1로 본 값이다
            Vector3 gravity = GravitySensor.current.gravity.ReadValue().normalized;

            if (gravity.z < -0.95f)
            {
                if (curFace == Define.DisplayedFace.Head) return;
                
                curFace = Define.DisplayedFace.Head;
                OnFlipToHead?.Invoke();
                OnNextHead?.Invoke();
                OnNextHead = null;
            }

            if (-0.7f < gravity.z && gravity.z < 0.7f)
            {
                if (curFace == Define.DisplayedFace.Stand) return;
                
                curFace = Define.DisplayedFace.Stand;
                OnStand?.Invoke();
                OnNextStand?.Invoke();
                OnNextStand = null;
            }
            
            if (gravity.z > 0.95f)
            {
                if (curFace == Define.DisplayedFace.Tail) return;
                
                curFace = Define.DisplayedFace.Tail;
                OnFlipToTail?.Invoke();
                OnNextTail?.Invoke();
                OnNextTail = null;
            }
        }
    }
}