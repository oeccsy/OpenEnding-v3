using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// StrokeSketch를 Play 모드 없이 쓰는 에디터 창.
    /// 창 가운데에 그림 캔버스를 비율을 지켜 가장 크게 놓고, 캔버스 내용은 렌더 텍스처에 그려 보여 준다.
    /// </summary>
    public class StrokeSketchWindow : EditorWindow
    {
        private const float CanvasMargin = 16f;

        private static readonly Vector2 MinWindowSize = new Vector2(640f, 400f);
        private static readonly Color BackdropColor = new Color32(58, 58, 58, 255);
        private static readonly Color PaperColor = new Color32(253, 250, 243, 255);

        private VisualElement canvasArea;
        private Image canvasImage;
        private RenderTexture canvasTexture;

        private HandSketchInputHandler strokeInputHandler;
        private StrokeSketcher strokeSketcher;

        private int activePointerId = PointerId.invalidPointerId;

        [MenuItem("Window/Stroke Sketch")]
        private static void Open()
        {
            StrokeSketchWindow window = GetWindow<StrokeSketchWindow>("Stroke Sketch");
            window.minSize = MinWindowSize;
        }

        private void CreateGUI()
        {
            canvasArea = new VisualElement();
            canvasArea.style.flexGrow = 1;
            canvasArea.style.backgroundColor = BackdropColor;
            canvasArea.style.justifyContent = Justify.Center;
            canvasArea.style.alignItems = Align.Center;
            canvasArea.RegisterCallback<GeometryChangedEvent>(_ => FitCanvas());
            rootVisualElement.Add(canvasArea);

            canvasImage = new Image { scaleMode = ScaleMode.StretchToFill };
            canvasArea.Add(canvasImage);

            canvasImage.RegisterCallback<PointerDownEvent>(OnPointerDown);
            canvasImage.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            canvasImage.RegisterCallback<PointerUpEvent>(OnPointerUp);
            canvasImage.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        private void OnEnable()
        {
            if (strokeSketcher == null)
            {
                strokeInputHandler = new HandSketchInputHandler();
                strokeSketcher = new StrokeSketcher(strokeInputHandler, SketchSettings.Default);
            }

            strokeInputHandler.StrokeFinished += strokeSketcher.RefineStroke;
        }

        private void OnDisable()
        {
            strokeInputHandler.StrokeFinished -= strokeSketcher.RefineStroke;

            ReleaseCanvasTexture();
        }

        // 마우스 왼쪽 버튼, 한 번에 포인터 하나만 그린다.
        private void OnPointerDown(PointerDownEvent pointerEvent)
        {
            if (pointerEvent.button != 0) return;
            if (activePointerId != PointerId.invalidPointerId) return;

            // 캔버스 밖으로 끌고 나가도 이 획이 이벤트를 계속 받게 붙잡는다
            activePointerId = pointerEvent.pointerId;
            canvasImage.CapturePointer(activePointerId);

            Vector2 position = ToCanvasPosition(pointerEvent.localPosition);
            double time = pointerEvent.timestamp / 1000.0;

            strokeInputHandler.BeginStroke(position, time);
        }

        private void OnPointerMove(PointerMoveEvent pointerEvent)
        {
            if (pointerEvent.pointerId != activePointerId) return;

            Vector2 position = ToCanvasPosition(pointerEvent.localPosition);
            double time = pointerEvent.timestamp / 1000.0;

            strokeInputHandler.AddPoint(position, time);
        }

        private void OnPointerUp(PointerUpEvent pointerEvent)
        {
            if (pointerEvent.pointerId != activePointerId) return;

            activePointerId = PointerId.invalidPointerId;
            canvasImage.ReleasePointer(pointerEvent.pointerId);

            Vector2 position = ToCanvasPosition(pointerEvent.localPosition);
            double time = pointerEvent.timestamp / 1000.0;

            strokeInputHandler.EndStroke(position, time);
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent pointerEvent)
        {
            if (activePointerId == PointerId.invalidPointerId) return;

            activePointerId = PointerId.invalidPointerId;
            strokeInputHandler.CancelStroke();
        }

        // 캔버스 요소 안의 좌표를 비율로 바꾼 뒤 그림 좌표로 늘린다. 둘 다 y가 아래로 +라 뒤집지 않는다
        // 캔버스 밖으로 나간 점은 가장자리에 붙인다
        private Vector2 ToCanvasPosition(Vector3 localPosition)
        {
            Vector2 normalized = Rect.PointToNormalized(canvasImage.contentRect, localPosition);

            return Vector2.Scale(normalized, strokeSketcher.Settings.canvasSize);
        }

        // 창 크기가 바뀔 때마다 캔버스를 비율을 지켜 가장 크게 맞추고, 그 크기에 맞는 렌더 텍스처를 다시 만든다
        private void FitCanvas()
        {
            Vector2 available = canvasArea.contentRect.size - Vector2.one * (CanvasMargin * 2f);
            if (available.x <= 0f || available.y <= 0f) return;

            Vector2 canvasSize = strokeSketcher.Settings.canvasSize;
            float scale = Mathf.Min(available.x / canvasSize.x, available.y / canvasSize.y);
            Vector2 displaySize = canvasSize * scale;
            canvasImage.style.width = displaySize.x;
            canvasImage.style.height = displaySize.y;

            // 고해상도 화면에서 흐려지지 않게 화면 픽셀 기준으로 만든다
            Vector2Int textureSize = Vector2Int.Max(Vector2Int.one, Vector2Int.RoundToInt(displaySize * EditorGUIUtility.pixelsPerPoint));
            if (canvasTexture != null && canvasTexture.width == textureSize.x && canvasTexture.height == textureSize.y) return;

            ReleaseCanvasTexture();
            canvasTexture = new RenderTexture(textureSize.x, textureSize.y, 0) { name = "Stroke Sketch Canvas", hideFlags = HideFlags.HideAndDontSave };
            canvasTexture.Create();
            canvasImage.image = canvasTexture;

            DrawCanvas();
        }

        // 지금은 종이 색으로 채우기만 한다. 다음 단계에서 획을 그린다
        private void DrawCanvas()
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = canvasTexture;
            GL.Clear(true, true, PaperColor);
            RenderTexture.active = previous;

            canvasImage.MarkDirtyRepaint();
        }

        private void ReleaseCanvasTexture()
        {
            if (canvasTexture == null) return;

            canvasTexture.Release();
            DestroyImmediate(canvasTexture);
            canvasTexture = null;
        }
    }
}
