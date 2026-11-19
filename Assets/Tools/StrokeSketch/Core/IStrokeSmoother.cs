using System.Collections.Generic;

namespace Oeccsy.StrokeSketch
{
    public interface IStrokeSmoother
    {
        RawStroke Smooth(RawStroke stroke, IReadOnlyList<int> corners, int radius);
    }
}
