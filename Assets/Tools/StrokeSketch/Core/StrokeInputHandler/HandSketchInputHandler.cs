using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 손 입력을 처리해 획 데이터로 만드는 입력 핸들러.
    /// 호스트가 전달한 위치와 시각을 기록하고, 획 입력이 끝나면 완료를 알린다.
    /// </summary>
    public class HandSketchInputHandler : IStrokeInputHandler
    {
        // 같은 자리의 점이 쌓이지 않도록 둔 최소 거리(그림 좌표 단위).
        private const float MinPointDistance = 0.8f;

        private readonly List<StrokePoint> points = new List<StrokePoint>();

        private double strokeStartTime;

        public event Action<IReadOnlyList<StrokePoint>> StrokeFinished;

        public bool IsDrawing { get; private set; }
        // 입력 중인 획의 미리보기용 점 목록.
        public IReadOnlyList<StrokePoint> DrawingPoints => IsDrawing ? points : Array.Empty<StrokePoint>();

        // 점 하나인 클릭은 완성된 획에서 제외한다.
        private bool HasDrawnStroke => !IsDrawing && points.Count >= 2;

        // time은 초 단위이며, 이후 입력도 같은 시간 기준을 사용한다.
        public void BeginStroke(Vector2 position, double time)
        {
            points.Clear();
            strokeStartTime = time;
            IsDrawing = true;

            RecordPoint(position, time);
        }

        public void AddPoint(Vector2 position, double time)
        {
            if (!IsDrawing) return;
            RecordPoint(position, time);
        }

        public void EndStroke(Vector2 position, double time)
        {
            if (!IsDrawing) return;

            RecordPoint(position, time);
            IsDrawing = false;

            if (HasDrawnStroke) StrokeFinished?.Invoke(points);
        }

        public void CancelStroke()
        {
            IsDrawing = false;
            points.Clear();
        }

        private void RecordPoint(Vector2 position, double time)
        {
            if (points.Count > 0 && Vector2.Distance(points[points.Count - 1].Position, position) < MinPointDistance) return;

            float elapsedTime = (float)(time - strokeStartTime);
            points.Add(new StrokePoint(position, elapsedTime));
        }
    }
}
