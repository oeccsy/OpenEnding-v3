using System.Collections.Generic;
using UnityEngine;

namespace Oeccsy.StrokeMotion
{
    /// <summary>
    /// 샘플링을 거쳐 곡선을 촘촘한 점으로 변환한 획을 나타내는 클래스.
    /// 점마다 첫 점부터의 누적 길이를 가져, 이후 단계에서 길이를 기준으로 획을 자를 수 있게 한다.
    /// </summary>
    public class SampledStroke
    {
        private readonly Vector2[] points;
        private readonly float[] cumulativeLengths;

        public SampledStroke(Color color, float width, int layer, Vector2[] points, float[] cumulativeLengths)
        {
            Color = color;
            Width = width;
            Layer = layer;
            this.points = points;
            this.cumulativeLengths = cumulativeLengths;
        }

        public Color Color { get; }
        public float Width { get; }
        public int Layer { get; }
        public IReadOnlyList<Vector2> Points => points;
        public IReadOnlyList<float> CumulativeLengths => cumulativeLengths;
        public float TotalLength => cumulativeLengths[cumulativeLengths.Length - 1];
    }
}
