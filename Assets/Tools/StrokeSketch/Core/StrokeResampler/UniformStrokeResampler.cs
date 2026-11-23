using System.Collections.Generic;
using UnityEngine;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 획의 점을 고른 간격으로 다시 배치하는 리샘플러.
    /// 원본 획을 따라 잰 전체 길이를 등분하고, 각 위치의 좌표와 시각을 보간한다.
    /// </summary>
    public class UniformStrokeResampler : IStrokeResampler
    {
        public RawStroke Resample(RawStroke stroke, float targetSpacing)
        {
            IReadOnlyList<StrokePoint> points = stroke.Points;
            if (points.Count < 2 || targetSpacing <= 0f) return stroke;

            float[] distances = MeasureCumulativeDistances(points);
            float totalLength = distances[distances.Length - 1];
            if (totalLength <= 0f) return stroke;

            // 양 끝점을 포함하도록 구간 수를 정하고 실제 간격을 조정한다.
            int intervalCount = Mathf.Max(1, Mathf.RoundToInt(totalLength / targetSpacing));
            float evenSpacing = totalLength / intervalCount;

            List<StrokePoint> resampled = new List<StrokePoint>(intervalCount + 1);
            int segmentEnd = 1;
            for (int i = 0; i <= intervalCount; i++)
            {
                float targetDistance = evenSpacing * i;

                // 목표 거리가 증가하므로 선분을 처음부터 다시 찾을 필요가 없다.
                while (segmentEnd < points.Count - 1 && distances[segmentEnd] < targetDistance) segmentEnd++;

                resampled.Add(PointAtDistance(points, distances, segmentEnd, targetDistance));
            }

            return new RawStroke(stroke.Color, stroke.Width, resampled);
        }

        // 시작점부터 각 점까지의 누적 거리. 마지막 값이 전체 길이다.
        private float[] MeasureCumulativeDistances(IReadOnlyList<StrokePoint> points)
        {
            float[] distances = new float[points.Count];
            for (int i = 1; i < points.Count; i++)
            {
                float segmentLength = Vector2.Distance(points[i - 1].Position, points[i].Position);
                distances[i] = distances[i - 1] + segmentLength;
            }

            return distances;
        }

        // 목표 거리가 놓인 선분 안에서 위치와 시각을 같은 비율로 보간한다.
        private StrokePoint PointAtDistance(IReadOnlyList<StrokePoint> points, float[] distances, int segmentEnd, float targetDistance)
        {
            StrokePoint from = points[segmentEnd - 1];
            StrokePoint to = points[segmentEnd];
            float segmentLength = distances[segmentEnd] - distances[segmentEnd - 1];
            float ratio = segmentLength > 0f ? Mathf.Clamp01((targetDistance - distances[segmentEnd - 1]) / segmentLength) : 0f;

            Vector2 position = Vector2.Lerp(from.Position, to.Position, ratio);
            float time = Mathf.Lerp(from.Time, to.Time, ratio);

            return new StrokePoint(position, time);
        }
    }
}
