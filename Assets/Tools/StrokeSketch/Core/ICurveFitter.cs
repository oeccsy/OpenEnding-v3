using System.Collections.Generic;
using Oeccsy.StrokeMotion;

namespace Oeccsy.StrokeSketch
{
    public interface ICurveFitter
    {
        Stroke Fit(RawStroke stroke, IReadOnlyList<int> corners, float tolerance);
    }
}
