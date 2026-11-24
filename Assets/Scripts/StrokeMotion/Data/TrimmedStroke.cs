using System.Collections.Generic;
using UnityEngine;

namespace Oeccsy.StrokeMotion
{
    /// <summary>
    /// 진행도만큼 앞에서부터 잘린 획을 나타내는 클래스.
    /// 그리기 단계가 받는 입력으로, 색과 굵기와 겹침 순서와 잘린 점 목록을 가진다.
    /// </summary>
    public class TrimmedStroke
    {
        private readonly Vector2[] points;

        public TrimmedStroke(Color color, float width, int layer, Vector2[] points)
        {
            Color = color;
            Width = width;
            Layer = layer;
            this.points = points;
        }

        public Color Color { get; }
        public float Width { get; }
        public int Layer { get; }
        public IReadOnlyList<Vector2> Points => points;
    }
}
