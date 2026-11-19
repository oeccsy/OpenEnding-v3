using System.Collections.Generic;

namespace Oeccsy.StrokeSketch
{
    public interface ICornerFinder
    {
        IReadOnlyList<int> FindCorners(RawStroke stroke, float angleThreshold, float speedRatio);
    }
}
