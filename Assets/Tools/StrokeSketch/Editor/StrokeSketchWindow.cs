using System;
using System.IO;
using UnityEditor;
using UnityEditor.ShortcutManagement;
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
        private const string DrawingFolderPath = "Assets/Resources/Drawings";

        private const float SidePanelWidth = 300f;

        private static readonly Vector2 MinWindowSize = new Vector2(640f, 400f);

        private HandSketchInputHandler strokeInputHandler;
        private StrokeSketcher strokeSketcher;
        private StrokeSketchPreview preview;
        private SketchControlView controlView;
        private SketchSettingsView settingsView;
        private SketchCanvasView canvasView;

        [MenuItem("Window/Stroke Sketch")]
        private static void Open()
        {
            StrokeSketchWindow window = GetWindow<StrokeSketchWindow>("Stroke Sketch");
            window.minSize = MinWindowSize;
        }

        [Shortcut("Stroke Sketch/Remove Last Stroke", typeof(StrokeSketchWindow), KeyCode.Z, ShortcutModifiers.Action)]
        private static void RemoveLastStrokeShortcut(ShortcutArguments arguments)
        {
            StrokeSketchWindow window = (StrokeSketchWindow)arguments.context;
            window.OnUndoRequested();
        }

        [Shortcut("Stroke Sketch/Clear Strokes", typeof(StrokeSketchWindow), KeyCode.Delete, ShortcutModifiers.Action)]
        private static void ClearStrokesShortcut(ShortcutArguments arguments)
        {
            StrokeSketchWindow window = (StrokeSketchWindow)arguments.context;
            window.OnClearRequested();
        }

        [Shortcut("Stroke Sketch/Export Drawing", typeof(StrokeSketchWindow), KeyCode.S, ShortcutModifiers.Action)]
        private static void ExportDrawingShortcut(ShortcutArguments arguments)
        {
            StrokeSketchWindow window = (StrokeSketchWindow)arguments.context;
            window.OnExportRequested();
        }

        private void OnEnable()
        {
            strokeInputHandler = new HandSketchInputHandler();
            strokeSketcher = new StrokeSketcher(strokeInputHandler, SketchSettings.Default);
            preview = new StrokeSketchPreview();

            strokeInputHandler.StrokeFinished += OnStrokeFinished;
        }

        private void CreateGUI()
        {
            controlView = new SketchControlView();
            controlView.UndoRequested += OnUndoRequested;
            controlView.ClearRequested += OnClearRequested;
            controlView.HelpRequested += OnHelpRequested;
            controlView.ExportRequested += OnExportRequested;

            settingsView = new SketchSettingsView(strokeSketcher.Settings);
            settingsView.SettingsChanged += OnSettingsChanged;

            canvasView = new SketchCanvasView(strokeSketcher.Settings.canvasSize);
            canvasView.CanvasDrawRequested += OnCanvasDrawRequested;
            canvasView.CanvasPressed += strokeInputHandler.BeginStroke;
            canvasView.CanvasDragged += strokeInputHandler.AddPoint;
            canvasView.CanvasReleased += strokeInputHandler.EndStroke;
            canvasView.CanvasCanceled += strokeInputHandler.CancelStroke;

            VisualElement sidePanel = new VisualElement();
            sidePanel.style.width = SidePanelWidth;
            sidePanel.style.flexShrink = 0f;
            sidePanel.Add(settingsView);
            sidePanel.Add(controlView);

            rootVisualElement.style.flexDirection = FlexDirection.Row;
            rootVisualElement.Add(sidePanel);
            rootVisualElement.Add(canvasView);
        }

        private void OnDisable()
        {
            preview.Dispose();
        }

        private void OnStrokeFinished()
        {
            strokeSketcher.RefineStroke();
            preview.RebuildFinishedStrokes(strokeSketcher.Strokes);
        }

        private void OnUndoRequested()
        {
            strokeSketcher.RemoveLastStroke();
            preview.RebuildFinishedStrokes(strokeSketcher.Strokes);
            canvasView.RepaintCanvas();
        }

        private void OnClearRequested()
        {
            // 되돌릴 수 없으므로 지울 획이 있을 때만 한 번 더 묻는다.
            if (strokeSketcher.Strokes.Count == 0) return;
            if (!EditorUtility.DisplayDialog("전체 지우기", "그린 획을 모두 지울까요? 되돌릴 수 없습니다.", "지우기", "취소")) return;

            strokeSketcher.ClearStrokes();
            preview.RebuildFinishedStrokes(strokeSketcher.Strokes);
            canvasView.RepaintCanvas();
        }

        private void OnSettingsChanged(SketchSettings changedSettings)
        {
            strokeSketcher.Settings = changedSettings;
            canvasView.ResizeCanvas(changedSettings.canvasSize);
        }

        // 프로젝트 기준 경로를 브라우저가 여는 파일 주소로 바꾼다.
        private void OnHelpRequested()
        {
            string guideFullPath = Path.GetFullPath(GuidePath);
            string guideUrl = new Uri(guideFullPath).AbsoluteUri;
            Application.OpenURL(guideUrl);
        }

        // Resources 아래에 저장해 StrokeMotion이 "Drawings/이름"으로 불러올 수 있게 한다.
        private void OnExportRequested()
        {
            if (strokeSketcher.Strokes.Count == 0) return;

            // 저장 창이 이 폴더에서 열리도록, 없으면 상위 폴더까지 만들고 Unity에 알린다.
            if (!AssetDatabase.IsValidFolder(DrawingFolderPath))
            {
                Directory.CreateDirectory(DrawingFolderPath);
                AssetDatabase.Refresh();
            }

            string path = EditorUtility.SaveFilePanelInProject("그림 내보내기", "Drawing", "json", "저장할 위치를 고르세요.", DrawingFolderPath);
            if (string.IsNullOrEmpty(path)) return;

            strokeSketcher.Export(path);
            AssetDatabase.ImportAsset(path);
        }

        private void OnCanvasDrawRequested(Rect rect)
        {
            preview.Draw(rect, strokeInputHandler.DrawingPoints, strokeSketcher.Settings);
        }
    }
}
