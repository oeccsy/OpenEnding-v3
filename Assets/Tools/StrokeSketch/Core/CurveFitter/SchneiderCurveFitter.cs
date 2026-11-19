using System;
using System.Collections.Generic;
using Oeccsy.StrokeMotion;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 점 목록을 이어진 3차 베지어 곡선들로 맞추는 클래스.
    /// Schneider(Graphics Gems, 1990) 방식이며, 모서리에서는 접선을 공유하지 않는다. 아직 구현하지 않았다.
    /// </summary>
    public class SchneiderCurveFitter : ICurveFitter
    {
        public Stroke Fit(RawStroke stroke, IReadOnlyList<int> corners, float tolerance)
        {
            throw new NotImplementedException("곡선 피팅은 아직 구현하지 않았음");
        }
    }
}
