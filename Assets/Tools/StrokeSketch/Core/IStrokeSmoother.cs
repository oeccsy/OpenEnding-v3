using System.Collections.Generic;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 획의 모서리를 보존하면서 손떨림을 줄이는 인터페이스.
    /// 획과 모서리 인덱스, 보정 반경을 받아 점의 위치를 보정한다.
    /// </summary>
    public interface IStrokeSmoother
    {
        // 양 끝점과 모서리는 고정한다. radius는 이웃 점의 반경(점 개수)이다.
        RawStroke Smooth(RawStroke stroke, IReadOnlyList<int> corners, int radius);
    }
}
