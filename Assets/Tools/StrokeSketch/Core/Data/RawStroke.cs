using System.Collections.Generic;
using UnityEngine;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 곡선으로 정리하기 전의 획 하나를 점 목록으로 나타내는 클래스.
    /// 입력 수집, 리샘플링, 손떨림 보정 단계가 이 형식으로 주고받고, 곡선 피팅을 거쳐 StrokeMotion의 Stroke가 된다.
    /// </summary>
    public class RawStroke
    {
        private readonly List<StrokePoint> points;

        public RawStroke(Color color, float width, IEnumerable<StrokePoint> points)
        {
            Color = color;
            Width = width;
            this.points = new List<StrokePoint>(points);
        }

        public Color Color { get; }
        public float Width { get; }
        public IReadOnlyList<StrokePoint> Points => points;
    }
}
