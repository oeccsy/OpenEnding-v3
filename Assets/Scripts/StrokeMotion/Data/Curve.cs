using UnityEngine;

namespace Oeccsy.StrokeMotion
{
    /// <summary>
    /// 3차 베지어 곡선 한 조각을 나타내는 구조체.
    /// 시작점, 제어점 두 개, 끝점을 가진다.
    /// </summary>
    public readonly struct Curve
    {
        public Curve(Vector2 start, Vector2 control1, Vector2 control2, Vector2 end)
        {
            Start = start;
            Control1 = control1;
            Control2 = control2;
            End = end;
        }

        public Vector2 Start { get; }
        public Vector2 Control1 { get; }
        public Vector2 Control2 { get; }
        public Vector2 End { get; }
    }
}
