using System.Collections.Generic;
using UnityEngine;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 모든 점 간격을 같게 맞추는 리샘플러.
    /// 전체 길이를 정확히 나누는 간격을 목표 간격 근처에서 골라 찍는다.
    /// 이후 단계가 점 개수로 범위를 정하므로, 간격이 같아야 범위가 어디서나 같은 거리가 된다.
    /// </summary>
    public class UniformStrokeResampler : IStrokeResampler
    {
        public RawStroke Resample(RawStroke stroke, float targetSpacing)
        {
            IReadOnlyList<StrokePoint> points = stroke.Points;
            if (points.Count < 2 || targetSpacing <= 0f) return stroke;

            float[] distances = MeasureDistances(points);
            float totalLength = distances[distances.Length - 1];
            if (totalLength <= 0f) return stroke;

            // 목표 간격에 가장 가까우면서 전체 길이를 정확히 나누는 간격을 쓴다
            int intervalCount = Mathf.Max(1, Mathf.RoundToInt(totalLength / targetSpacing));
            float evenSpacing = totalLength / intervalCount;

            List<StrokePoint> resampled = new List<StrokePoint>(intervalCount + 1);
            int segmentEnd = 1;
            for (int i = 0; i <= intervalCount; i++)
            {
                float targetDistance = evenSpacing * i;

                // 목표 거리를 품은 선분까지 앞으로만 나아간다
                while (segmentEnd < points.Count - 1 && distances[segmentEnd] < targetDistance) segmentEnd++;

                resampled.Add(PointAtDistance(points, distances, segmentEnd, targetDistance));
            }

            return new RawStroke(stroke.Color, stroke.Width, resampled);
        }

        // 시작점부터 각 점까지 선을 따라 잰 거리. 마지막 값이 전체 길이다
        private float[] MeasureDistances(IReadOnlyList<StrokePoint> points)
        {
            float[] distances = new float[points.Count];
            for (int i = 1; i < points.Count; i++)
            {
                float segmentLength = Vector2.Distance(points[i - 1].Position, points[i].Position);
                distances[i] = distances[i - 1] + segmentLength;
            }

            return distances;
        }

        // segmentEnd 앞 선분 위에서 목표 거리에 놓이는 점. 위치와 시각을 같은 비율로 섞는다
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
