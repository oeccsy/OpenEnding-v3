using System.Collections.Generic;
using UnityEngine;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 모서리를 보존하면서 손떨림을 줄이는 보정기.
    /// 모서리 사이 구간에 이동 평균을 적용하고, 양 끝점과 모서리는 고정한다(구현 예정).
    /// </summary>
    public class MovingAverageStrokeSmoother : IStrokeSmoother
    {
        public RawStroke Smooth(RawStroke stroke, IReadOnlyList<int> corners, int radius)
        {
            IReadOnlyList<StrokePoint> points = stroke.Points;
            if (radius <= 0 || points.Count < 3) return stroke;

            List<int> anchors = CollectAnchorIndices(corners, points.Count);
            List<StrokePoint> smoothed = new List<StrokePoint>(points);

            for (int i = 1; i < anchors.Count; i++)
            {
                SmoothSegment(points, smoothed, anchors[i - 1], anchors[i], radius);
            }

            return new RawStroke(stroke.Color, stroke.Width, smoothed);
        }

        // 움직이지 않을 점 : 획의 양 끝과 모서리. 이웃한 두 고정점 사이가 따로 평균 내는 구간이 된다
        private List<int> CollectAnchorIndices(IReadOnlyList<int> corners, int pointCount)
        {
            List<int> anchors = new List<int> { 0 };
            anchors.AddRange(corners);
            anchors.Add(pointCount - 1);

            return anchors;
        }

        // 구간 안쪽 점마다 앞뒤로 같은 수의 이웃과 위치를 평균 낸다. 시각은 그대로 둔다
        // 고정점에 가까우면 이웃 수를 줄여, 고정점 너머의 점이 섞이지 않고 한쪽으로 쏠리지도 않게 한다
        private void SmoothSegment(IReadOnlyList<StrokePoint> source, List<StrokePoint> target, int start, int end, int radius)
        {
            for (int i = start + 1; i < end; i++)
            {
                int reach = Mathf.Min(radius, i - start, end - i);

                Vector2 sum = Vector2.zero;
                for (int j = i - reach; j <= i + reach; j++)
                {
                    sum += source[j].Position;
                }

                Vector2 average = sum / (reach * 2 + 1);
                target[i] = new StrokePoint(average, source[i].Time);
            }
        }
    }
}
