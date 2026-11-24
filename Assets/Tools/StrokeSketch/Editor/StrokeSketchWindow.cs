using System;
using System.Collections.Generic;
using System.IO;
using Oeccsy.StrokeMotion;
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
        // 프로젝트의 Temp 폴더는 에셋으로 가져오지 않고, Unity를 닫으면 지워진다.
        private const string CaptureFolderPath = "Temp/StrokeSketch";

        private const float SidePanelWidth = 300f;

        private static readonly Vector2 MinWindowSize = new Vector2(640f, 400f);

        private HandSketchInputHandler handInputHandler;
        private SvgStrokeInputHandler svgInputHandler;
        private StrokeSketcher strokeSketcher;
        private StrokeSketchPreview preview;
        private SketchControlView controlView;
        private SketchSettingsView settingsView;
        private SketchCanvasView canvasView;

        public SketchSettings Settings => strokeSketcher.Settings;
        public IReadOnlyList<Stroke> Strokes => strokeSketcher.Strokes;

        // 메뉴와 MCP 도구가 함께 쓴다. 이미 열려 있으면 그 창을 돌려준다.
        [MenuItem("Window/Stroke Sketch")]
        public static StrokeSketchWindow Open()
        {
            StrokeSketchWindow window = GetWindow<StrokeSketchWindow>("Stroke Sketch");
            window.minSize = MinWindowSize;

            return window;
        }

        [Shortcut("Stroke Sketch/Remove Last Stroke", typeof(StrokeSketchWindow), KeyCode.Z, ShortcutModifiers.Action)]
        private static void RemoveLastStrokeShortcut(ShortcutArguments arguments)
        {
            StrokeSketchWindow window = (StrokeSketchWindow)arguments.context;
            window.RemoveLastStroke();
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
            handInputHandler = new HandSketchInputHandler();
            svgInputHandler = new SvgStrokeInputHandler();
            strokeSketcher = new StrokeSketcher(SketchSettings.Default);
            preview = new StrokeSketchPreview();

            handInputHandler.StrokeFinished += OnStrokeFinished;
            svgInputHandler.StrokeFinished += OnStrokeFinished;
        }

        private void CreateGUI()
        {
            controlView = new SketchControlView();
            controlView.UndoRequested += RemoveLastStroke;
            controlView.ClearRequested += OnClearRequested;
            controlView.HelpRequested += OnHelpRequested;
            controlView.ExportRequested += OnExportRequested;

            settingsView = new SketchSettingsView(strokeSketcher.Settings);
            settingsView.SettingsChanged += OnSettingsChanged;

            canvasView = new SketchCanvasView(strokeSketcher.Settings.canvasSize);
            canvasView.CanvasDrawRequested += OnCanvasDrawRequested;
            canvasView.CanvasPressed += handInputHandler.BeginStroke;
            canvasView.CanvasDragged += handInputHandler.AddPoint;
            canvasView.CanvasReleased += handInputHandler.EndStroke;
            canvasView.CanvasCanceled += handInputHandler.CancelStroke;

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

        // 그린 획 수를 반환한다. 경로 문법이 틀리면 FormatException.
        // 손 입력과 달리 화면을 다시 그릴 계기가 없어 직접 다시 그린다.
        public int DrawSvgPath(string pathData)
        {
            int drawnCount = svgInputHandler.ReadPath(pathData);
            canvasView.RepaintCanvas();

            return drawnCount;
        }

        // 그린 획 수를 반환한다. 문서를 읽을 수 없으면 FormatException.
        public int DrawSvgDocument(string svgText)
        {
            int drawnCount = svgInputHandler.ReadDocument(svgText, strokeSketcher.Settings.canvasSize);
            canvasView.RepaintCanvas();

            return drawnCount;
        }

        // 완성된 획을 캔버스 크기 PNG로 저장하고 전체 경로를 반환한다. 이전 캡처와 비교할 수 있게 시각을 이름에 붙인다.
        public string CaptureCanvas()
        {
            byte[] png = preview.CapturePng(strokeSketcher.Settings.canvasSize);

            Directory.CreateDirectory(CaptureFolderPath);
            string fileName = $"capture-{DateTime.Now:yyyyMMdd-HHmmss-fff}.png";
            string path = Path.Combine(CaptureFolderPath, fileName);
            File.WriteAllBytes(path, png);

            return Path.GetFullPath(path);
        }

        // 설정을 통째로 갈아끼운다. 다음에 바꿀 때까지 손 입력과 SVG 입력 모두에 쓰이고, 이미 그린 획은 그대로 둔다.
        public void ChangeSettings(SketchSettings settings)
        {
            strokeSketcher.Settings = settings;
            settingsView.ShowSettings(settings);
            canvasView.ResizeCanvas(settings.canvasSize);
        }

        public void RemoveLastStroke()
        {
            strokeSketcher.RemoveLastStroke();
            preview.RebuildFinishedStrokes(strokeSketcher.Strokes);
            canvasView.RepaintCanvas();
        }

        // 확인 창 없이 지운다. 사람이 누르는 버튼은 OnClearRequested에서 먼저 묻는다.
        public void ClearStrokes()
        {
            strokeSketcher.ClearStrokes();
            preview.RebuildFinishedStrokes(strokeSketcher.Strokes);
            canvasView.RepaintCanvas();
        }

        // 저장 창 없이 Drawings 폴더에 저장하고 에셋 경로를 반환한다. 같은 이름이 있으면 덮어쓰지 않고 IOException.
        public string ExportDrawing(string name)
        {
            string path = $"{DrawingFolderPath}/{name}.json";
            if (File.Exists(path)) throw new IOException($"이미 있는 파일입니다 : {path}");

            strokeSketcher.Export(path);
            AssetDatabase.ImportAsset(path);

            return path;
        }

        private void OnStrokeFinished(IReadOnlyList<StrokePoint> points)
        {
            strokeSketcher.RefineStroke(points);
            preview.RebuildFinishedStrokes(strokeSketcher.Strokes);
        }

        private void OnClearRequested()
        {
            // 되돌릴 수 없으므로 지울 획이 있을 때만 한 번 더 묻는다.
            if (strokeSketcher.Strokes.Count == 0) return;
            if (!EditorUtility.DisplayDialog("전체 지우기", "그린 획을 모두 지울까요? 되돌릴 수 없습니다.", "지우기", "취소")) return;

            ClearStrokes();
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
            preview.Draw(rect, handInputHandler.DrawingPoints, strokeSketcher.Settings);
        }
    }
}
