using System;
using UnityEngine;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// StrokeSketch에서 바꿀 수 있는 값을 한곳에 모은 구조체.
    /// 캔버스, 펜, 정리 단계별 파라미터를 담는다. 사람은 UI로, AI는 요청으로 이 값을 바꾼다.
    /// </summary>
    [Serializable]
    public struct SketchSettings
    {
        // 캔버스 : 그림 좌표로 잰 크기
        public Vector2 canvasSize;

        // 펜 : 다음에 그리는 획부터 적용된다
        public Color penColor;
        public float penWidth;

        // ② 리샘플링 : 점 사이 목표 간격 (그림 좌표). 구현에 따라 실제 간격은 조금 달라질 수 있다
        public float targetSpacing;

        // ③ 모서리 판단 : 꺾임 각도 기준(도, 181이면 끔)과 속도 기준(중앙값 대비 비율)
        public float cornerAngle;
        public float cornerSpeedRatio;

        // ④ 손떨림 보정 : 이동 평균 반경 (점 개수, 0이면 끔)
        public int smoothingRadius;

        // ⑤ 곡선 피팅 : 곡선이 점에서 벗어나도 되는 거리 (그림 좌표)
        public float tolerance;

        public static SketchSettings Default => new SketchSettings
        {
            canvasSize = new Vector2(1920f, 1080f),
            penColor = Color.black,
            penWidth = 4f,
            targetSpacing = 4f,
            cornerAngle = 60f,
            cornerSpeedRatio = 0.5f,
            smoothingRadius = 3,
            tolerance = 4f,
        };
    }
}
