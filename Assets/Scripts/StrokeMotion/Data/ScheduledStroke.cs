namespace Oeccsy.StrokeMotion
{
    /// <summary>
    /// SampledStroke를 기반으로 스케줄링을 거친 획을 나타내는 클래스.
    /// 샘플링한 획을 참조하며, 이후 단계에서 시간에 따른 진행도를 계산할 수 있게 한다. 시간 단위는 초다.
    /// </summary>
    public class ScheduledStroke
    {
        public ScheduledStroke(SampledStroke stroke, float startTime, float duration)
        {
            Stroke = stroke;
            StartTime = startTime;
            Duration = duration;
        }

        public SampledStroke Stroke { get; }
        public float StartTime { get; }
        public float Duration { get; }
        public float EndTime => StartTime + Duration;
    }
}
