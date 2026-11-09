using System.Collections.Generic;

namespace Oeccsy.StrokeMotion
{
    /// <summary>
    /// 정지된 그림을 나타내는 클래스.
    /// 아직 모션으로 변환되기 전의 원본 데이터이며, 그리는 순서대로 놓인 획 목록을 가진다.
    /// </summary>
    public class Drawing
    {
        private readonly List<Stroke> strokes;

        public Drawing(IEnumerable<Stroke> strokes)
        {
            this.strokes = new List<Stroke>(strokes);
        }

        public IReadOnlyList<Stroke> Strokes => strokes;
    }
}
