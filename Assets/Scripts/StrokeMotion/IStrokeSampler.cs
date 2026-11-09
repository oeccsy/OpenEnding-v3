using System.Collections.Generic;

namespace Oeccsy.StrokeMotion
{
    public interface IStrokeSampler
    {
        IReadOnlyList<SampledStroke> Sample(IReadOnlyList<Stroke> strokes, int samplesPerCurve = 24);
    }
}
