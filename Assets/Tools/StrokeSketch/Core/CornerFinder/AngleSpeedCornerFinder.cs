using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 획에서 뾰족하게 보존할 모서리를 찾는 판별기.
    /// 앞뒤 점의 방향으로 꺾임 각도를 재고, 입력 속도를 보조 기준으로 후보를 고른다.
    /// </summary>
    public class AngleSpeedCornerFinder : ICornerFinder
    {
        // 손떨림을 모서리로 잡지 않도록 앞뒤로 떨어져 볼 점 수.
        private const int Reach = 4;

        // 같은 모서리로 묶을 후보 사이의 인덱스 거리.
        private const int MergeRange = Reach * 2;

        public IReadOnlyList<int> FindCorners(RawStroke stroke, float angleThreshold, float speedRatio)
        {
            IReadOnlyList<StrokePoint> points = stroke.Points;

            // 180도 초과는 모서리 판단을 끄는 값이다.
            if (angleThreshold > 180f) return Array.Empty<int>();
            if (points.Count < Reach * 2 + 1) return Array.Empty<int>();

            float[] angles = MeasureTurnAngles(points);
            float[] speeds = MeasureSpeeds(points);
            float slowSpeed = Median(speeds) * speedRatio;

            List<int> candidates = new List<int>();
            for (int i = Reach; i < points.Count - Reach; i++)
            {
                bool isSharp = angles[i] >= angleThreshold;
                bool isSlowTurn = angles[i] >= angleThreshold * 0.5f && speeds[i] <= slowSpeed;

                if (isSharp || isSlowTurn) candidates.Add(i);
            }

            return SelectStrongestCorners(candidates, angles);
        }

        // Reach만큼 떨어진 앞뒤 점으로 꺾임을 잰다. 0도는 직진, 180도는 되돌아감.
        private float[] MeasureTurnAngles(IReadOnlyList<StrokePoint> points)
        {
            float[] angles = new float[points.Count];
            for (int i = Reach; i < points.Count - Reach; i++)
            {
                Vector2 incoming = points[i].Position - points[i - Reach].Position;
                Vector2 outgoing = points[i + Reach].Position - points[i].Position;
                angles[i] = Vector2.Angle(incoming, outgoing);
            }

            return angles;
        }

        // 시간이 유효하지 않은 점은 느린 점으로 잡히지 않도록 속도를 무한대로 둔다.
        private float[] MeasureSpeeds(IReadOnlyList<StrokePoint> points)
        {
            float[] speeds = new float[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                StrokePoint before = points[Mathf.Max(0, i - 1)];
                StrokePoint after = points[Mathf.Min(points.Count - 1, i + 1)];
                float distance = Vector2.Distance(before.Position, after.Position);
                float duration = after.Time - before.Time;

                speeds[i] = duration > 0f ? distance / duration : float.PositiveInfinity;
            }

            return speeds;
        }

        // 획마다 다른 입력 속도를 비교할 기준. 짝수 개이면 가운데 두 값 중 뒤의 값을 쓴다.
        private float Median(float[] values)
        {
            float[] sorted = (float[])values.Clone();
            Array.Sort(sorted);
            float median = sorted[sorted.Length / 2];

            return float.IsInfinity(median) ? 0f : median;
        }

        // 같은 꺾임이 중복되지 않도록 각도가 큰 후보부터 고른 뒤, 획 순서로 정렬한다.
        private List<int> SelectStrongestCorners(List<int> candidates, float[] angles)
        {
            candidates.Sort((a, b) => angles[b].CompareTo(angles[a]));

            List<int> corners = new List<int>();
            foreach (int candidate in candidates)
            {
                bool isNearKept = corners.Exists(corner => Mathf.Abs(corner - candidate) <= MergeRange);
                if (!isNearKept) corners.Add(candidate);
            }

            corners.Sort();

            return corners;
        }
    }
}
