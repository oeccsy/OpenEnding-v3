using System.Collections.Generic;
using UnityEngine;

namespace Oeccsy.StrokeMotion
{
    /// <summary>
    /// 손을 떼지 않는 것을 기준으로 획 하나를 나타내는 클래스.
    /// 하나의 획은 여러 베지어 곡선을 이어서 이루어진다. 좌표는 그림 좌표(y는 아래가 +)다.
    /// </summary>
    public class Stroke
    {
        private readonly List<Curve> curves;

        public Stroke(Color color, float width, int layer, IEnumerable<Curve> curves)
        {
            Color = color;
            Width = width;
            Layer = layer;
            this.curves = new List<Curve>(curves);
        }

        public Color Color { get; }
        public float Width { get; }
        // 겹침 순서. 클수록 위에 그려지고, 같으면 나중 획이 위다. 그려지는 시간 순서와는 따로다.
        public int Layer { get; }
        public IReadOnlyList<Curve> Curves => curves;
    }
}
