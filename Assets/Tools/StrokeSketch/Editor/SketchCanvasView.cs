using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 스케치 창의 캔버스 배치와 입력 전달을 담당하는 UI.
    /// UI Toolkit으로 캔버스 비율을 유지하고, 포인터 입력을 그림 좌표와 시각으로 변환한다.
    /// </summary>
    public class SketchCanvasView : VisualElement
    {
        private const float CanvasMargin = 16f;

        private static readonly Color BackdropColor = new Color32(58, 58, 58, 255);

        private readonly IMGUIContainer canvas;
        private Vector2 canvasSize;

        private int activePointerId = PointerId.invalidPointerId;

        public SketchCanvasView(Vector2 canvasSize)
        {
            this.canvasSize = canvasSize;
            canvas = new IMGUIContainer(() => CanvasDrawRequested?.Invoke(new Rect(Vector2.zero, canvas.contentRect.size)));

            style.flexGrow = 1;
            style.backgroundColor = BackdropColor;
            style.justifyContent = Justify.Center;
            style.alignItems = Align.Center;
            RegisterCallback<GeometryChangedEvent>(_ => FitCanvas());

            canvas.RegisterCallback<PointerDownEvent>(OnPointerDown);
            canvas.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            canvas.RegisterCallback<PointerUpEvent>(OnPointerUp);
            canvas.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            
            Add(canvas);
        }

        public event Action<Rect> CanvasDrawRequested;
        public event Action<Vector2, double> CanvasPressed;
        public event Action<Vector2, double> CanvasDragged;
        public event Action<Vector2, double> CanvasReleased;
        public event Action CanvasCanceled;

        public void ResizeCanvas(Vector2 canvasSize)
        {
            this.canvasSize = canvasSize;
            FitCanvas();
            canvas.MarkDirtyRepaint();
        }

        private void FitCanvas()
        {
            Vector2 available = contentRect.size - Vector2.one * (CanvasMargin * 2f);
            if (available.x <= 0f || available.y <= 0f) return;

            float scale = Mathf.Min(available.x / canvasSize.x, available.y / canvasSize.y);
            canvas.style.width = canvasSize.x * scale;
            canvas.style.height = canvasSize.y * scale;
        }

        // 로컬 좌표와 그림 좌표는 모두 아래쪽이 +Y이므로 뒤집지 않는다.
        private Vector2 ToCanvasPosition(Vector3 localPosition)
        {
            Vector2 normalized = Rect.PointToNormalized(canvas.contentRect, localPosition);
            return Vector2.Scale(normalized, canvasSize);
        }

        // 입력 시각은 밀리초에서 초로 변환해 전달한다.
        private void OnPointerDown(PointerDownEvent pointerEvent)
        {
            if (pointerEvent.button != (int)MouseButton.LeftMouse) return;
            if (activePointerId != PointerId.invalidPointerId) return;

            activePointerId = pointerEvent.pointerId;
            canvas.CapturePointer(activePointerId);

            Vector2 position = ToCanvasPosition(pointerEvent.localPosition);
            CanvasPressed?.Invoke(position, pointerEvent.timestamp / 1000.0);
        }

        private void OnPointerMove(PointerMoveEvent pointerEvent)
        {
            if (pointerEvent.pointerId != activePointerId) return;

            Vector2 position = ToCanvasPosition(pointerEvent.localPosition);
            CanvasDragged?.Invoke(position, pointerEvent.timestamp / 1000.0);
            canvas.MarkDirtyRepaint();
        }

        private void OnPointerUp(PointerUpEvent pointerEvent)
        {
            if (pointerEvent.pointerId != activePointerId) return;

            // 캡처 해제를 취소로 처리하지 않도록 활성 포인터를 먼저 비운다.
            activePointerId = PointerId.invalidPointerId;
            canvas.ReleasePointer(pointerEvent.pointerId);

            Vector2 position = ToCanvasPosition(pointerEvent.localPosition);
            CanvasReleased?.Invoke(position, pointerEvent.timestamp / 1000.0);
            canvas.MarkDirtyRepaint();
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent pointerEvent)
        {
            if (activePointerId == PointerId.invalidPointerId) return;

            activePointerId = PointerId.invalidPointerId;
            CanvasCanceled?.Invoke();
            canvas.MarkDirtyRepaint();
        }
    }
}
