using System;
using UnityEngine;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 그림 제작에 사용하는 조절값을 모은 설정.
    /// 캔버스 크기와 펜 속성, 각 처리 단계의 기준값을 하나의 구조체로 관리한다.
    /// </summary>
    [Serializable]
    public struct SketchSettings
    {
        // 캔버스 크기(그림 좌표 단위).
        public Vector2 canvasSize;

        // 획이 끝나 다듬을 때 적용한다.
        public Color penColor;
        public float penWidth;

        // 목표 간격(그림 좌표 단위). 실제 간격은 구현체에 따라 다르다.
        public float targetSpacing;

        // 꺾임 각도(도, 180 초과이면 끔)와 획 속도 중앙값 대비 비율.
        public float cornerAngle;
        public float cornerSpeedRatio;

        // 이동 평균 반경(점 개수, 0이면 끔).
        public int smoothingRadius;

        // 피팅 허용 오차(그림 좌표 단위).
        public float tolerance;

        public static SketchSettings Default => new SketchSettings
        {
            canvasSize = new Vector2(1920f, 1080f),
            penColor = Color.black,
            penWidth = 20f,
            targetSpacing = 4f,
            cornerAngle = 60f,
            cornerSpeedRatio = 0.4f,
            smoothingRadius = 3,
            tolerance = 4f,
        };
    }
}
