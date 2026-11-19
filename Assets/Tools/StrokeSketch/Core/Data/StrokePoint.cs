using UnityEngine;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 시간과 위치를 데이터로 갖는 RawStroke의 일부.
    /// 위치는 그림 좌표(y는 아래가 +), 시각은 초 단위다. 속도는 저장하지 않고 쓰는 단계에서 위치와 시각으로 계산한다.
    /// </summary>
    public readonly struct StrokePoint
    {
        public StrokePoint(Vector2 position, float time)
        {
            Position = position;
            Time = time;
        }

        public Vector2 Position { get; }
        public float Time { get; }
    }
}
