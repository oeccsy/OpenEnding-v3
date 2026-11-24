using System.Collections.Generic;
using System.Linq;
using Oeccsy.StrokeMotion;
using UnityEngine;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 점으로 이루어진 획을 3차 베지어 곡선으로 근사하는 피터.
    /// Schneider 알고리즘으로 구간을 맞추고, 오차에 따라 매개변수를 보정하거나 구간을 나눈다.
    /// </summary>
    public class SchneiderCurveFitter : ICurveFitter
    {
        // 손떨림에 접선이 흔들리지 않도록 방향을 평균할 이웃 수.
        private const int TangentReach = 4;
        private const int MaxIterations = 4;

        public Stroke Fit(RawStroke stroke, IReadOnlyList<int> corners, float tolerance)
        {
            Vector2[] points = stroke.Points.Select(point => point.Position).ToArray();
            List<int> anchors = corners.Prepend(0).Append(points.Length - 1).ToList();
            List<Curve> curves = new List<Curve>();

            // 모서리마다 접선을 따로 추정해 꺾임을 보존한다.
            for (int i = 1; i < anchors.Count; i++)
            {
                int first = anchors[i - 1];
                int last = anchors[i];
                Vector2 startTangent = ComputeTangent(points, first, 1, last - first);
                Vector2 endTangent = ComputeTangent(points, last, -1, last - first);

                FitCubic(points, first, last, startTangent, endTangent, tolerance * tolerance, curves);
            }

            return new Stroke(stroke.Color, stroke.Width, stroke.Layer, curves);
        }

        // 거리의 제곱으로 오차를 비교하며, 허용 거리의 2배 안에서만 보정을 시도한다.
        // 오차가 줄지 않으면 가장 멀리 벗어난 점에서 나눠 다시 맞춘다.
        private void FitCubic(Vector2[] points, int first, int last, Vector2 startTangent, Vector2 endTangent, float squaredTolerance, List<Curve> curves)
        {
            float[] parameters = ChordLengthParameterize(points, first, last);
            float refineLimit = squaredTolerance * 4f;
            bool isInOrder = true;
            int split = (first + last) / 2;

            for (int i = 0; i <= MaxIterations; i++)
            {
                Curve curve = GenerateBezier(points, first, last, parameters, startTangent, endTangent);
                FitError error = ComputeMaxError(points, first, parameters, curve);
                if (error.SquaredDistance < squaredTolerance && isInOrder)
                {
                    curves.Add(curve);
                    return;
                }

                split = error.Index;
                if (error.SquaredDistance >= refineLimit) break;

                isInOrder = Reparameterize(points, first, parameters, curve);
                refineLimit = error.SquaredDistance;
            }

            // 분할점의 접선을 양쪽 곡선이 공유해 매끄럽게 잇는다.
            int reach = Mathf.Min(split - first, last - split);
            Vector2 backward = ComputeTangent(points, split, -1, reach);
            Vector2 forward = ComputeTangent(points, split, 1, reach);
            Vector2 centerTangent = (backward - forward).normalized;
            FitCubic(points, first, split, startTangent, centerTangent, squaredTolerance, curves);
            FitCubic(points, split, last, -centerTangent, endTangent, squaredTolerance, curves);
        }

        // 끝점에서 구간 안쪽으로 향하는 이웃 방향을 평균한다.
        private Vector2 ComputeTangent(Vector2[] points, int origin, int step, int available)
        {
            Vector2 sum = Vector2.zero;
            for (int k = 1; k <= Mathf.Min(TangentReach, available); k++)
            {
                sum += (points[origin + step * k] - points[origin]).normalized;
            }

            return sum.normalized;
        }

        // 구간의 누적 거리 비율을 곡선 매개변수의 초기값으로 쓴다.
        private float[] ChordLengthParameterize(Vector2[] points, int first, int last)
        {
            float[] parameters = new float[last - first + 1];
            for (int i = 1; i < parameters.Length; i++)
            {
                parameters[i] = parameters[i - 1] + Vector2.Distance(points[first + i - 1], points[first + i]);
            }

            float totalLength = parameters[parameters.Length - 1];

            return parameters.Select(length => length / totalLength).ToArray();
        }

        // 접선 방향의 제어점 길이를 최소제곱으로 구한다. 해가 정해지지 않으면 양쪽 길이를 같게 둔다.
        // 길이가 너무 짧거나 제어점의 투영 순서가 뒤집히면 끝점 거리의 1/3을 쓴다.
        private Curve GenerateBezier(Vector2[] points, int first, int last, float[] parameters, Vector2 startTangent, Vector2 endTangent)
        {
            Vector2 start = points[first];
            Vector2 end = points[last];

            float c00 = 0f, c01 = 0f, c11 = 0f, x0 = 0f, x1 = 0f;
            for (int i = 0; i < parameters.Length; i++)
            {
                float u = parameters[i];
                Vector2 a0 = startTangent * Bernstein1(u);
                Vector2 a1 = endTangent * Bernstein2(u);
                Vector2 rest = points[first + i] - (start * (Bernstein0(u) + Bernstein1(u)) + end * (Bernstein2(u) + Bernstein3(u)));

                c00 += Vector2.Dot(a0, a0);
                c01 += Vector2.Dot(a0, a1);
                c11 += Vector2.Dot(a1, a1);
                x0 += Vector2.Dot(a0, rest);
                x1 += Vector2.Dot(a1, rest);
            }

            float determinant = c00 * c11 - c01 * c01;
            float startReach, endReach;
            if (Mathf.Abs(determinant) > Mathf.Epsilon)
            {
                startReach = (x0 * c11 - x1 * c01) / determinant;
                endReach = (c00 * x1 - c01 * x0) / determinant;
            }
            else
            {
                float c0 = c00 + c01;
                float c1 = c01 + c11;
                startReach = Mathf.Abs(c0) > Mathf.Epsilon ? x0 / c0 : Mathf.Abs(c1) > Mathf.Epsilon ? x1 / c1 : 0f;
                endReach = startReach;
            }

            Vector2 chord = end - start;
            float minReach = chord.magnitude * 1e-6f;
            bool isTooShort = startReach < minReach || endReach < minReach;
            bool isCrossed = Vector2.Dot(startTangent * startReach, chord) - Vector2.Dot(endTangent * endReach, chord) > chord.sqrMagnitude;
            if (isTooShort || isCrossed)
            {
                startReach = chord.magnitude / 3f;
                endReach = startReach;
            }

            return new Curve(start, start + startTangent * startReach, end + endTangent * endReach, end);
        }

        // 입력 점과 해당 매개변수의 곡선 위치를 비교해, 최대 오차의 제곱과 점 인덱스를 구한다.
        private FitError ComputeMaxError(Vector2[] points, int first, float[] parameters, Curve curve)
        {
            FitError worst = new FitError(0f, first + parameters.Length / 2);
            for (int i = 1; i < parameters.Length - 1; i++)
            {
                float squaredDistance = (Evaluate(curve, parameters[i]) - points[first + i]).sqrMagnitude;
                if (squaredDistance >= worst.SquaredDistance) worst = new FitError(squaredDistance, first + i);
            }

            return worst;
        }

        // 뉴턴법으로 매개변수를 보정한다. 점 순서가 유지되지 않으면 false를 반환한다.
        private bool Reparameterize(Vector2[] points, int first, float[] parameters, Curve curve)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                parameters[i] = NewtonRaphsonRootFind(curve, points[first + i], parameters[i]);
            }

            for (int i = 1; i < parameters.Length; i++)
            {
                if (parameters[i] <= parameters[i - 1]) return false;
            }

            return true;
        }

        private float NewtonRaphsonRootFind(Curve curve, Vector2 point, float u)
        {
            Vector2 offset = Evaluate(curve, u) - point;
            Vector2 velocity = EvaluateVelocity(curve, u);
            float slope = Vector2.Dot(velocity, velocity) + Vector2.Dot(offset, EvaluateAcceleration(curve, u));
            if (Mathf.Abs(slope) < Mathf.Epsilon) return u;

            return Mathf.Clamp01(u - Vector2.Dot(offset, velocity) / slope);
        }

        // 베지어 계산

        private Vector2 Evaluate(Curve curve, float u)
        {
            return curve.Start * Bernstein0(u) + curve.Control1 * Bernstein1(u) + curve.Control2 * Bernstein2(u) + curve.End * Bernstein3(u);
        }

        private Vector2 EvaluateVelocity(Curve curve, float u)
        {
            float v = 1 - u;

            return (curve.Control1 - curve.Start) * (3 * v * v) + (curve.Control2 - curve.Control1) * (6 * u * v) + (curve.End - curve.Control2) * (3 * u * u);
        }

        private Vector2 EvaluateAcceleration(Curve curve, float u)
        {
            Vector2 startBend = curve.Control2 - 2 * curve.Control1 + curve.Start;
            Vector2 endBend = curve.End - 2 * curve.Control2 + curve.Control1;

            return startBend * (6 * (1 - u)) + endBend * (6 * u);
        }

        private float Bernstein0(float u) => (1 - u) * (1 - u) * (1 - u);
        private float Bernstein1(float u) => 3 * u * (1 - u) * (1 - u);
        private float Bernstein2(float u) => 3 * u * u * (1 - u);
        private float Bernstein3(float u) => u * u * u;

        private readonly struct FitError
        {
            public FitError(float squaredDistance, int index)
            {
                SquaredDistance = squaredDistance;
                Index = index;
            }

            public float SquaredDistance { get; }
            public int Index { get; }
        }
    }
}
