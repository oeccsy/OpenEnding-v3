using System.Collections.Generic;
using UnityEngine;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 곡선으로 변환하기 전의 획을 나타내는 데이터.
    /// 획의 색과 굵기, 겹침 순서, 위치와 시각이 기록된 점 목록으로 구성된다.
    /// </summary>
    public class RawStroke
    {
        private readonly List<StrokePoint> points;

        public RawStroke(Color color, float width, int layer, IEnumerable<StrokePoint> points)
        {
            Color = color;
            Width = width;
            Layer = layer;
            this.points = new List<StrokePoint>(points);
        }

        public Color Color { get; }
        public float Width { get; }
        public int Layer { get; }
        public IReadOnlyList<StrokePoint> Points => points;
    }
}
