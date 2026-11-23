using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 스케치의 입력, 획 처리, 미리보기를 연결하는 에디터 창.
    /// View의 입력을 핸들러에 전달하고, 완성된 획을 다듬어 Preview로 표시한다.
    /// </summary>
    public class StrokeSketchWindow : EditorWindow
    {
        private const string GuidePath = "Assets/Tools/StrokeSketch/Editor/Docs/SketchSettingsGuide.html";

        private static readonly Vector2 MinWindowSize = new Vector2(640f, 400f);

        private HandSketchInputHandler strokeInputHandler;
        private StrokeSketcher strokeSketcher;
        private StrokeSketchPreview preview;
        private SketchSettingsView settingsView;
        private SketchCanvasView canvasView;

        [MenuItem("Window/Stroke Sketch")]
        private static void Open()
        {
            StrokeSketchWindow window = GetWindow<StrokeSketchWindow>("Stroke Sketch");
            window.minSize = MinWindowSize;
        }

        private void OnEnable()
        {
            strokeInputHandler = new HandSketchInputHandler();
            strokeSketcher = new StrokeSketcher(strokeInputHandler, SketchSettings.Default);
            preview = new StrokeSketchPreview();

            // 다듬은 뒤에 다시 만들어야 하므로 등록 순서를 지킨다.
            strokeInputHandler.StrokeFinished += strokeSketcher.RefineStroke;
            strokeInputHandler.StrokeFinished += () => preview.RebuildFinishedStrokes(strokeSketcher.Strokes);
        }

        private void CreateGUI()
        {
            settingsView = new SketchSettingsView(strokeSketcher.Settings);
            settingsView.SettingsChanged += (changedSettings) => strokeSketcher.Settings = changedSettings;
            settingsView.SettingsChanged += (changedSettings) => canvasView.ResizeCanvas(changedSettings.canvasSize);

            string guideFullPath = Path.GetFullPath(GuidePath);
            string guideUrl = new Uri(guideFullPath).AbsoluteUri;
            settingsView.HelpRequested += () => Application.OpenURL(guideUrl);

            canvasView = new SketchCanvasView(strokeSketcher.Settings.canvasSize);
            canvasView.CanvasDrawRequested += (rect) => preview.Draw(rect, strokeInputHandler.DrawingPoints, strokeSketcher.Settings);
            canvasView.CanvasPressed += strokeInputHandler.BeginStroke;
            canvasView.CanvasDragged += strokeInputHandler.AddPoint;
            canvasView.CanvasReleased += strokeInputHandler.EndStroke;
            canvasView.CanvasCanceled += strokeInputHandler.CancelStroke;

            rootVisualElement.style.flexDirection = FlexDirection.Row;
            rootVisualElement.Add(settingsView);
            rootVisualElement.Add(canvasView);
        }

        private void OnDisable()
        {
            preview.Dispose();
        }
    }
}
