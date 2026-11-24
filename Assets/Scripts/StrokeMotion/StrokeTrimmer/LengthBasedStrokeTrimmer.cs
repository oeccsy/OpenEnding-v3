using System.Collections.Generic;
using UnityEngine;

namespace Oeccsy.StrokeMotion
{
    /// <summary>
    /// 획을 앞에서부터 전체 길이 × 진행도만큼만 남기고 자르는 클래스.
    /// 잘리는 지점은 두 점 사이를 보간해 구하며, 이 점이 그리는 중인 펜 끝이 된다.
    /// </summary>
    public class LengthBasedStrokeTrimmer : IStrokeTrimmer
    {
        public TrimmedStroke Trim(SampledStroke stroke, float progress)
        {
            float targetLength = stroke.TotalLength * progress;
            List<Vector2> trimmedPoints = new List<Vector2>();
            
            trimmedPoints.Add(stroke.Points[0]);
            for (int i = 1; i < stroke.Points.Count; i++)
            {
                if (stroke.CumulativeLengths[i] <= targetLength)
                {
                    // 목표 길이보다 가까운 지점의 point는 포함한다.
                    trimmedPoints.Add(stroke.Points[i]);
                }
                else
                {
                    // 마지막 지점은 보간을 통해 실제 펜촉이 있어야 할 자리를 찾아 포함한다.
                    Vector2 penTip = InterpolatePenTip(stroke, i, targetLength);
                    trimmedPoints.Add(penTip);
                    break;
                }                
            }

            return new TrimmedStroke(stroke.Color, stroke.Width, stroke.Layer, trimmedPoints.ToArray());
        }

        // 목표 길이가 (segmentEndIndex - 1)번 점과 segmentEndIndex번 점 사이에 있을 때, 보간한 위치의 점을 구한다
        private Vector2 InterpolatePenTip(SampledStroke stroke, int segmentEndIndex, float targetLength)
        {
            Vector2 segmentStart = stroke.Points[segmentEndIndex - 1];
            Vector2 segmentEnd = stroke.Points[segmentEndIndex];
            float segmentStartLength = stroke.CumulativeLengths[segmentEndIndex - 1];
            float segmentLength = stroke.CumulativeLengths[segmentEndIndex] - segmentStartLength;
            if (segmentLength <= 0f) return segmentStart;

            float t = (targetLength - segmentStartLength) / segmentLength;
            return Vector2.Lerp(segmentStart, segmentEnd, t);
        }
    }
}
