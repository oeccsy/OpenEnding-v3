using System.Collections.Generic;
using UnityEngine;

namespace Oeccsy.StrokeMotion
{
    /// <summary>
    /// 획의 베지어 곡선들을 곡선마다 같은 개수의 점으로 쪼개는 클래스.
    /// 점 사이 거리를 더해 가며 누적 길이도 함께 구한다.
    /// </summary>
    public class BezierStrokeSampler : IStrokeSampler
    {
        public IReadOnlyList<SampledStroke> Sample(IReadOnlyList<Stroke> strokes, int samplesPerCurve = 24)
        {
            List<SampledStroke> sampledStrokes = new List<SampledStroke>();
            foreach (Stroke stroke in strokes)
            {
                SampledStroke sampledStroke = Sample(stroke, samplesPerCurve);
                sampledStrokes.Add(sampledStroke);
            }

            return sampledStrokes;
        }

        private SampledStroke Sample(Stroke stroke, int samplesPerCurve)
        {
            List<Vector2> points = new List<Vector2>();
            points.Add(stroke.Curves[0].Start);

            foreach (Curve curve in stroke.Curves)
            {
                // 시작점은 앞 곡선의 끝점과 같으므로, 시작점을 제외한 [1/n, n/n] 지점만 추가
                for (int i = 1; i <= samplesPerCurve; i++)
                {
                    float t = i / (float)samplesPerCurve;
                    Vector2 point = EvaluateBezier(curve, t);
                    points.Add(point);
                }
            }

            float[] cumulativeLengths = CalculateCumulativeLengths(points);

            return new SampledStroke(stroke.Color, stroke.Width, stroke.Layer, points.ToArray(), cumulativeLengths);
        }
        
        private Vector2 EvaluateBezier(Curve curve, float t)
        {
            Vector2 p0 = curve.Start;
            Vector2 p1 = curve.Control1;
            Vector2 p2 = curve.Control2;
            Vector2 p3 = curve.End;
            float u = 1f - t;

            // B(t) = (1-t)^3 P0 + 3(1-t)^2 t P1 + 3(1-t) t^2 P2 + t^3 P3
            return (u * u * u * p0) + (3f * u * u * t * p1) + (3f * u * t * t * p2) + (t * t * t * p3);
        }

        private float[] CalculateCumulativeLengths(List<Vector2> points)
        {
            float[] cumulativeLengths = new float[points.Count];
            cumulativeLengths[0] = 0f;

            for (int i = 1; i < points.Count; i++)
            {
                float segmentLength = Vector2.Distance(points[i - 1], points[i]);
                cumulativeLengths[i] = cumulativeLengths[i - 1] + segmentLength;
            }

            return cumulativeLengths;
        }
    }
}
