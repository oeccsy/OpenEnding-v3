using System.Collections.Generic;
using UnityEngine;

namespace Oeccsy.StrokeMotion
{
    /// <summary>
    /// 정지된 그림을 나타내는 클래스.
    /// 아직 모션으로 변환되기 전의 원본 데이터이며, 캔버스 크기와 그리는 순서대로 놓인 획 목록을 가진다.
    /// </summary>
    public class Drawing
    {
        private readonly List<Stroke> strokes;

        // canvasSize는 그림을 그린 캔버스 크기(그림 좌표 단위)다.
        public Drawing(Vector2 canvasSize, IEnumerable<Stroke> strokes)
        {
            CanvasSize = canvasSize;
            this.strokes = new List<Stroke>(strokes);
        }

        public Vector2 CanvasSize { get; }
        public IReadOnlyList<Stroke> Strokes => strokes;
    }
}
