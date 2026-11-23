using UnityEngine;

namespace Oeccsy.StrokeMotion
{
    /// <summary>
    /// 그림 좌표와 Unity 로컬 좌표 사이의 규칙을 모은 클래스.
    /// 그림 좌표는 왼쪽 위 원점에 y가 아래로 +이고, 로컬 좌표는 y가 위로 +인 월드 유닛이다.
    /// </summary>
    public static class DrawingCoordinates
    {
        // 그림 좌표 몇 단위를 월드 1유닛으로 볼지. 모든 그림이 같은 축척을 쓴다.
        public const float PixelsPerUnit = 100f;

        // y를 뒤집고 PixelsPerUnit으로 나눠 월드 크기로 맞춘다.
        public static Vector3 ToLocal(Vector2 drawingPoint)
        {
            float x = drawingPoint.x / PixelsPerUnit;
            float y = -drawingPoint.y / PixelsPerUnit;

            return new Vector3(x, y, 0f);
        }

        // 메시 원점은 그림의 왼쪽 위이므로, 그림 중심이 부모 위치에 오도록 반대로 옮긴 위치를 돌려준다.
        public static Vector3 CalculateCenteredPosition(Vector2 canvasSize)
        {
            Vector2 drawingCenter = canvasSize * 0.5f;
            Vector3 localCenter = ToLocal(drawingCenter);

            return -localCenter;
        }
    }
}
