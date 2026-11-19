using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 손으로 그리는 획을 점마다 위치와 시각을 담아 기록하는 클래스.
    /// 호스트가 포인터 입력을 그림 좌표로 바꿔 시작, 점 추가, 끝을 알려 주면 점을 모으고, 획이 끝나면 알린다.
    /// UI를 모르므로 에디터 창과 앱이 함께 쓴다.
    /// </summary>
    public class HandSketchInputHandler : IStrokeInputHandler
    {
        // 펜을 멈추고 있을 때 같은 자리의 점이 쌓이지 않게 한다 (그림 좌표 단위)
        private const float MinPointDistance = 0.8f;

        private readonly List<StrokePoint> points = new List<StrokePoint>();

        private double strokeStartTime;

        public event Action StrokeFinished;

        public bool IsDrawing { get; private set; }
        // 아직 손을 떼지 않은 획의 점. 그리는 중이 아니면 비어 있다. 그리는 중인 선을 미리 보여 줄 때 쓴다
        public IReadOnlyList<StrokePoint> DrawingPoints => IsDrawing ? points : Array.Empty<StrokePoint>();

        // 손을 뗐고 점이 2개 이상이면 다 그려진 획이다. 점 하나는 클릭이다
        private bool HasDrawnStroke => !IsDrawing && points.Count >= 2;

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

            if (HasDrawnStroke) StrokeFinished?.Invoke();
        }

        public void CancelStroke()
        {
            IsDrawing = false;
            points.Clear();
        }

        // 다 그려진 획을 돌려준다. 그리는 중이거나 아직 그린 적이 없으면 null을 돌려준다
        public RawStroke GetDrawnStroke(Color color, float width)
        {
            if (!HasDrawnStroke) return null;

            return new RawStroke(color, width, points);
        }

        private void RecordPoint(Vector2 position, double time)
        {
            if (points.Count > 0 && Vector2.Distance(points[points.Count - 1].Position, position) < MinPointDistance) return;

            float elapsedTime = (float)(time - strokeStartTime);
            points.Add(new StrokePoint(position, elapsedTime));
        }
    }
}
