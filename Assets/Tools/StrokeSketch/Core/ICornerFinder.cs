using System.Collections.Generic;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 획에서 보존할 모서리를 찾는 인터페이스.
    /// 획과 판별 기준을 받아 모서리에 해당하는 점의 인덱스를 반환한다.
    /// </summary>
    public interface ICornerFinder
    {
        // angleThreshold: 꺾임 각도(도), speedRatio: 획 속도 중앙값 대비 비율.
        IReadOnlyList<int> FindCorners(RawStroke stroke, float angleThreshold, float speedRatio);
    }
}
