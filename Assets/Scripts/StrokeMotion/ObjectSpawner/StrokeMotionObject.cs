using System.Collections.Generic;
using UnityEngine;

namespace Oeccsy.StrokeMotion
{
    /// <summary>
    /// 씬에 놓여 획 애니메이션을 보여 주는 객체.
    /// Player가 만들고 전처리 결과(시간표에 놓인 획)를 채우며, 매 프레임 자기 렌더러로 그려진다.
    /// </summary>
    [RequireComponent(typeof(QuadStrokeRenderer))]
    public class StrokeMotionObject : MonoBehaviour
    {
        public IReadOnlyList<ScheduledStroke> ScheduledStrokes { get; set; }

        public IStrokeRenderer Renderer { get; private set; }

        // 재생을 시작한 시각(Time.time). Play()를 부르기 전에는 무한대라 아무것도 그려지지 않는다.
        public float StartTime { get; private set; } = float.PositiveInfinity;

        // 마지막 획이 끝나는 데 걸리는 시간(초)
        public float Duration
        {
            get
            {
                if (ScheduledStrokes == null || ScheduledStrokes.Count == 0) return 0f;

                ScheduledStroke lastStroke = ScheduledStrokes[ScheduledStrokes.Count - 1];
                return lastStroke.EndTime;
            }
        }

        private void Awake()
        {
            Renderer = GetComponent<IStrokeRenderer>();
        }

        // 처음부터 다시 그린다
        public void Play()
        {
            StartTime = Time.time;
        }
    }
}
