using System.Collections.Generic;
using Oeccsy.StrokeMotion;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 획의 점들을 곡선으로 근사하는 인터페이스.
    /// 획과 모서리 인덱스, 허용 오차를 받아 StrokeMotion의 획 데이터로 변환한다.
    /// </summary>
    public interface ICurveFitter
    {
        // tolerance: 그림 좌표 단위의 허용 오차.
        Stroke Fit(RawStroke stroke, IReadOnlyList<int> corners, float tolerance);
    }
}
