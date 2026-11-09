using System.Collections.Generic;

namespace Oeccsy.StrokeMotion
{
    public interface IStrokeScheduler
    {
        IReadOnlyList<ScheduledStroke> Schedule(IReadOnlyList<SampledStroke> strokes);
    }
}
