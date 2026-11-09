using System.Collections.Generic;

namespace Oeccsy.StrokeMotion
{
    /// <summary>
    /// 펜이 일정한 속도로 움직인다고 보고 획마다 시작 시각과 걸리는 시간을 정하는 클래스.
    /// 획은 순서대로 그려지고, 한 획이 끝나면 잠깐 쉬었다가 다음 획을 시작한다.
    /// </summary>
    public class StrokeScheduler : IStrokeScheduler
    {
        private readonly float penSpeed;
        private readonly float gapSeconds;

        // penSpeed는 초당 그림 좌표 단위, gapSeconds는 획 사이 쉬는 시간
        public StrokeScheduler(float penSpeed = 900f, float gapSeconds = 0.1f)
        {
            this.penSpeed = penSpeed;
            this.gapSeconds = gapSeconds;
        }

        public IReadOnlyList<ScheduledStroke> Schedule(IReadOnlyList<SampledStroke> strokes)
        {
            List<ScheduledStroke> scheduledStrokes = new List<ScheduledStroke>();
            float startTime = 0f;
            
            foreach (SampledStroke stroke in strokes)
            {
                float duration = stroke.TotalLength / penSpeed;
                scheduledStrokes.Add(new ScheduledStroke(stroke, startTime, duration));

                startTime += duration + gapSeconds;
            }

            return scheduledStrokes;
        }
    }
}
