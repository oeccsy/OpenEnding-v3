using System.Collections.Generic;
using UnityEngine;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 점 간격을 목표 간격 그대로 지키는 리샘플러.
    /// 선을 따라 목표 간격마다 찍고, 마지막 구간만 목표 간격의 0.5 ~ 1.5배로 끝점에 맞춘다.
    /// 간격 값과 끝점을 함께 지키려면 한 구간은 양보해야 해서, 그 구간을 범위 안으로 제한했다.
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

                // 직전 선분에서 남은 거리(carry)까지 더해 목표 간격이 찰 때마다 점을 찍는다
                while (carry + segment >= targetSpacing && segment > 0f)
                {
                    from = Interpolate(from, to, (targetSpacing - carry) / segment);
                    resampled.Add(from);
                    segment = Vector2.Distance(from.Position, to.Position);
                    carry = 0f;
                }

                carry += segment;
            }

            AttachEndPoint(resampled, points[points.Count - 1], targetSpacing);

            return new RawStroke(stroke.Color, stroke.Width, resampled);
        }

        // 남은 거리가 목표 간격의 절반보다 짧으면 마지막 점을 끝점으로 바꿔 짧은 조각을 없애고, 아니면 끝점을 더한다
        private void AttachEndPoint(List<StrokePoint> resampled, StrokePoint endPoint, float targetSpacing)
        {
            Vector2 lastPosition = resampled[resampled.Count - 1].Position;
            float remaining = Vector2.Distance(lastPosition, endPoint.Position);

            if (remaining < targetSpacing * 0.5f && resampled.Count > 1) resampled[resampled.Count - 1] = endPoint;
            else resampled.Add(endPoint);
        }

        // 위치와 시각을 같은 비율로 섞는다. 두 원본 점 사이를 손이 일정한 속도로 지났다고 보는 것이다
        private StrokePoint Interpolate(StrokePoint from, StrokePoint to, float ratio)
        {
            Vector2 position = Vector2.Lerp(from.Position, to.Position, ratio);
            float time = Mathf.Lerp(from.Time, to.Time, ratio);

            return new StrokePoint(position, time);
        }
    }
}
