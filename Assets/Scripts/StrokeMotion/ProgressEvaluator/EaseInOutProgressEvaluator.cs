using UnityEngine;

namespace Oeccsy.StrokeMotion
{
    /// <summary>
    /// 지금 시각에 획 하나가 얼마나 그려졌는지(0~1)를 구하는 클래스.
    /// 천천히 시작해 빨라졌다가 천천히 끝나도록 ease-in-out을 적용한다.
    /// </summary>

    public class EaseInOutProgressEvaluator : IProgressEvaluator
    {
        public float Evaluate(ScheduledStroke stroke, float time)
        {
            float elapsed = time - stroke.StartTime;
            float linearProgress = Mathf.Clamp01(elapsed / stroke.Duration);

            return EaseInOut(linearProgress);
        }

        // 3차 ease-in-out. 앞 절반은 t^3으로 가속하고, 뒤 절반은 그와 대칭으로 감속한다
        private float EaseInOut(float t)
        {
            if (t < 0.5f)
            {
                return 4f * t * t * t;
            }
            else
            {
                float mirrored = -2f * t + 2f;
                return 1f - mirrored * mirrored * mirrored / 2f;    
            }
        }
    }
}
