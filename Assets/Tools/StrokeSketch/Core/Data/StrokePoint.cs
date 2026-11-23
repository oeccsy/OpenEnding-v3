using UnityEngine;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 획 위의 점 하나를 나타내는 데이터.
    /// 그림 좌표의 위치와 획 시작부터의 경과 시간(초)을 함께 저장한다.
    /// </summary>
    public readonly struct StrokePoint
    {
        public StrokePoint(Vector2 position, float time)
        {
            Position = position;
            Time = time;
        }

        // 그림 좌표: 왼쪽 위 원점, 오른쪽과 아래쪽이 양수.
        public Vector2 Position { get; }
        public float Time { get; }
    }
}
