using System.Collections.Generic;
using UnityEngine;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 획의 점을 목표 간격에 맞춰 다시 배치하는 리샘플러.
    /// 원본 획을 따라 목표 간격마다 점을 찍고, 마지막 점을 교체하거나 추가해 끝점을 포함한다.
    /// </summary>
    public class FixedSpacingStrokeResampler : IStrokeResampler
    {
        public RawStroke Resample(RawStroke stroke, float targetSpacing)
        {
            IReadOnlyList<StrokePoint> points = stroke.Points;
            if (points.Count < 2 || targetSpacing <= 0f) return stroke;

            List<StrokePoint> resampled = new List<StrokePoint> { points[0] };
            float carry = 0f;

            for (int i = 1; i < points.Count; i++)
            {
                StrokePoint from = points[i - 1];
                StrokePoint to = points[i];
                float segment = Vector2.Distance(from.Position, to.Position);

                // 선분을 넘어가도 남은 거리를 이어 센다.
                while (carry + segment >= targetSpacing && segment > 0f)
                {
                    from = Interpolate(from, to, (targetSpacing - carry) / segment);
                    resampled.Add(from);
                    segment = Vector2.Distance(from.Position, to.Position);
                    carry = 0f;
                }

                carry += segment;
            }

            EnsureEndPoint(resampled, points[points.Count - 1], targetSpacing);

            return new RawStroke(stroke.Color, stroke.Width, stroke.Layer, resampled);
        }

        // 끝점까지의 직선거리가 목표 간격의 절반보다 짧으면 마지막 점을 교체해 짧은 조각을 줄인다.
        private void EnsureEndPoint(List<StrokePoint> resampled, StrokePoint endPoint, float targetSpacing)
        {
            Vector2 lastPosition = resampled[resampled.Count - 1].Position;
            float remaining = Vector2.Distance(lastPosition, endPoint.Position);

            if (remaining < targetSpacing * 0.5f && resampled.Count > 1) resampled[resampled.Count - 1] = endPoint;
            else resampled.Add(endPoint);
        }

        // 두 점 사이의 이동 속도가 일정하다고 보고 위치와 시각을 함께 보간한다.
        private StrokePoint Interpolate(StrokePoint from, StrokePoint to, float ratio)
        {
            Vector2 position = Vector2.Lerp(from.Position, to.Position, ratio);
            float time = Mathf.Lerp(from.Time, to.Time, ratio);

            return new StrokePoint(position, time);
        }
    }
}
