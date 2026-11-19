using System;
using System.Collections.Generic;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 꺾임 각도와 손 속도로 모서리를 찾는 클래스.
    /// 각도가 기준 이상이거나, 기준의 절반 이상이면서 손이 크게 느려진 점을 모서리로 본다. 아직 구현하지 않았다.
    /// </summary>
    public class AngleSpeedCornerFinder : ICornerFinder
    {
        public IReadOnlyList<int> FindCorners(RawStroke stroke, float angleThreshold, float speedRatio)
        {
            throw new NotImplementedException("모서리 판단은 아직 구현하지 않았음");
        }
    }
}
