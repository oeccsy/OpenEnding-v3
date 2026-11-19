using System;
using System.Collections.Generic;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 모서리 사이 구간마다 이동 평균으로 손떨림을 줄이는 클래스.
    /// 획의 양 끝과 모서리는 움직이지 않는다. 아직 구현하지 않았다.
    /// </summary>
    public class MovingAverageStrokeSmoother : IStrokeSmoother
    {
        public RawStroke Smooth(RawStroke stroke, IReadOnlyList<int> corners, int radius)
        {
            throw new NotImplementedException("손떨림 보정은 아직 구현하지 않았음");
        }
    }
}
