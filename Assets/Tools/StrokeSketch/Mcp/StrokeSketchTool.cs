using System;
using System.Collections.Generic;
using System.IO;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using Oeccsy.StrokeMotion;
using UnityEngine;

namespace Oeccsy.StrokeSketch.Mcp
{
    /// <summary>
    /// MCP 도구 stroke_sketch. action에 따라 StrokeSketch 창에 일을 시킨다.
    /// 실제 일은 창이 하고, 이 클래스는 인자를 꺼내 넘기고 결과를 응답으로 바꾼다.
    /// </summary>
    // 이름을 직접 적는다. 생략하면 도구 목록은 "stroke_sketch", 실행 표는 "stroke_sketch_tool"로 어긋난다(패키지 10.3.0)
    [McpForUnityTool("stroke_sketch", Description = StrokeSketchTool.ToolDescription)]
    public static class StrokeSketchTool
    {
        // 어트리뷰트 인자는 상수여야 하므로, 줄을 나눈 상수 문자열로 만든다
        private const string ToolDescription =
            "StrokeSketch 에디터 창에 손그림 드로잉 모션그래픽의 원본을 그리는 도구다.\n" +
            "그린 획은 export 후 StrokeMotion 플레이어가 획 순서대로 그려 나가는 애니메이션으로 재생된다.\n" +
            "작업 전에 반드시 action=get_guide로 안내 문서(Assets/Tools/StrokeSketch/Editor/Docs/StrokeSketchAiGuide.md)를 읽고, 그 지침대로 작업한다.\n" +
            "action : get_guide, get_settings, set_settings, draw_svg_path, draw_svg, capture, undo, clear, export";

        private const string GetGuideAction = "get_guide";
        private const string GetSettingsAction = "get_settings";
        private const string SetSettingsAction = "set_settings";
        private const string DrawSvgPathAction = "draw_svg_path";
        private const string DrawSvgAction = "draw_svg";
        private const string CaptureAction = "capture";
        private const string UndoAction = "undo";
        private const string ClearAction = "clear";
        private const string ExportAction = "export";
        private const string GuidePath = "Assets/Tools/StrokeSketch/Editor/Docs/StrokeSketchAiGuide.md";
        // StrokeMotion이 Resources.Load로 부르는 경로의 앞부분
        private const string PlayerPathPrefix = "Drawings/";

        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString();
            if (string.IsNullOrEmpty(action)) return new ErrorResponse("action 인자가 비어 있습니다.");

            // 안내 문서는 창이 필요 없으므로 창을 열기 전에 처리한다
            if (action == GetGuideAction) return GetGuide();

            StrokeSketchWindow window = StrokeSketchWindow.Open();

            if (action == GetSettingsAction) return GetSettings(window);
            if (action == SetSettingsAction) return SetSettings(window, @params);
            if (action == DrawSvgPathAction) return DrawSvgPath(window, @params);
            if (action == DrawSvgAction) return DrawSvg(window, @params);
            if (action == CaptureAction) return Capture(window);
            if (action == UndoAction) return Undo(window, @params);
            if (action == ClearAction) return Clear(window);
            if (action == ExportAction) return Export(window, @params);

            return new ErrorResponse($"모르는 action입니다 : {action}");
        }

        private static object GetGuide()
        {
            if (!File.Exists(GuidePath)) return new ErrorResponse($"안내 문서가 없습니다 : {GuidePath}");

            string guide = File.ReadAllText(GuidePath);

            return new SuccessResponse("AI용 안내 문서입니다.", new { guide });
        }

        private static object GetSettings(StrokeSketchWindow window)
        {
            object data = CreateSettingsData(window.Settings);

            return new SuccessResponse("지금 설정입니다.", data);
        }

        // 준 값만 지금 설정에서 바꾸고, 완성된 설정을 통째로 갈아끼운다
        private static object SetSettings(StrokeSketchWindow window, JObject @params)
        {
            SketchSettings changedSettings = window.Settings;

            string colorText = @params["color"]?.ToString();
            if (!string.IsNullOrEmpty(colorText) && !ColorUtility.TryParseHtmlString(colorText, out changedSettings.penColor)) return new ErrorResponse($"color 형식이 아닙니다 : {colorText}");

            changedSettings.penWidth = @params["width"]?.ToObject<float>() ?? changedSettings.penWidth;
            changedSettings.layer = @params["layer"]?.ToObject<int>() ?? changedSettings.layer;
            changedSettings.canvasSize.x = @params["canvas_width"]?.ToObject<float>() ?? changedSettings.canvasSize.x;
            changedSettings.canvasSize.y = @params["canvas_height"]?.ToObject<float>() ?? changedSettings.canvasSize.y;
            changedSettings.targetSpacing = @params["target_spacing"]?.ToObject<float>() ?? changedSettings.targetSpacing;
            changedSettings.cornerAngle = @params["corner_angle"]?.ToObject<float>() ?? changedSettings.cornerAngle;
            changedSettings.cornerSpeedRatio = @params["corner_speed_ratio"]?.ToObject<float>() ?? changedSettings.cornerSpeedRatio;
            changedSettings.smoothingRadius = @params["smoothing_radius"]?.ToObject<int>() ?? changedSettings.smoothingRadius;
            changedSettings.tolerance = @params["tolerance"]?.ToObject<float>() ?? changedSettings.tolerance;

            if (changedSettings.penWidth <= 0f) return new ErrorResponse($"width는 0보다 커야 합니다 : {changedSettings.penWidth}");
            if (changedSettings.canvasSize.x <= 0f || changedSettings.canvasSize.y <= 0f) return new ErrorResponse($"캔버스 크기는 0보다 커야 합니다 : {changedSettings.canvasSize}");
            if (changedSettings.targetSpacing <= 0f) return new ErrorResponse($"target_spacing은 0보다 커야 합니다 : {changedSettings.targetSpacing}");
            if (changedSettings.smoothingRadius < 0) return new ErrorResponse($"smoothing_radius는 0 이상이어야 합니다 : {changedSettings.smoothingRadius}");
            if (changedSettings.tolerance <= 0f) return new ErrorResponse($"tolerance는 0보다 커야 합니다 : {changedSettings.tolerance}");

            window.ChangeSettings(changedSettings);
            object data = CreateSettingsData(window.Settings);

            return new SuccessResponse("설정을 적용했습니다.", data);
        }

        // 다음 호출에서 주지 않은 값은 이대로 유지된다. set_settings 인자 이름과 같게 맞춘다
        private static object CreateSettingsData(SketchSettings settings)
        {
            return new
            {
                color = "#" + ColorUtility.ToHtmlStringRGBA(settings.penColor),
                width = settings.penWidth,
                layer = settings.layer,
                canvas_width = settings.canvasSize.x,
                canvas_height = settings.canvasSize.y,
                target_spacing = settings.targetSpacing,
                corner_angle = settings.cornerAngle,
                corner_speed_ratio = settings.cornerSpeedRatio,
                smoothing_radius = settings.smoothingRadius,
                tolerance = settings.tolerance,
            };
        }

        private static object DrawSvgPath(StrokeSketchWindow window, JObject @params)
        {
            string pathData = @params["path_data"]?.ToString();
            if (string.IsNullOrEmpty(pathData)) return new ErrorResponse("path_data 인자가 비어 있습니다.");

            int drawnCount;
            try
            {
                drawnCount = window.DrawSvgPath(pathData);
            }
            catch (FormatException exception)
            {
                return new ErrorResponse($"SVG 경로 오류 : {exception.Message}");
            }

            return CreateDrawnResponse(window, drawnCount);
        }

        private static object DrawSvg(StrokeSketchWindow window, JObject @params)
        {
            string svgText = @params["svg"]?.ToString();
            if (string.IsNullOrEmpty(svgText)) return new ErrorResponse("svg 인자가 비어 있습니다.");

            int drawnCount;
            try
            {
                drawnCount = window.DrawSvgDocument(svgText);
            }
            catch (FormatException exception)
            {
                return new ErrorResponse($"SVG 문서 오류 : {exception.Message}");
            }

            return CreateDrawnResponse(window, drawnCount);
        }

        // 마지막으로 그린 획들의 번호와 곡선 수를 응답에 담는다
        private static object CreateDrawnResponse(StrokeSketchWindow window, int drawnCount)
        {
            IReadOnlyList<Stroke> strokes = window.Strokes;
            List<object> drawnStrokes = new List<object>();
            for (int i = strokes.Count - drawnCount; i < strokes.Count; i++)
            {
                drawnStrokes.Add(new { index = i, curveCount = strokes[i].Curves.Count });
            }

            object data = new
            {
                drawnCount,
                totalStrokeCount = strokes.Count,
                drawnStrokes,
            };

            return new SuccessResponse($"획 {drawnCount}개를 그렸습니다. 전체 획 : {strokes.Count}", data);
        }

        private static object Capture(StrokeSketchWindow window)
        {
            string path = window.CaptureCanvas();
            SketchSettings settings = window.Settings;

            object data = new
            {
                path,
                canvasSize = new { width = settings.canvasSize.x, height = settings.canvasSize.y },
                totalStrokeCount = window.Strokes.Count,
            };

            return new SuccessResponse($"캡처를 저장했습니다 : {path}", data);
        }

        // 주지 않으면 한 획만 지운다. 남은 획보다 많으면 하나도 지우지 않고 오류를 돌려준다
        private static object Undo(StrokeSketchWindow window, JObject @params)
        {
            int count = @params["count"]?.ToObject<int>() ?? 1;
            if (count < 1) return new ErrorResponse($"count는 1 이상이어야 합니다 : {count}");
            if (count > window.Strokes.Count) return new ErrorResponse($"남은 획({window.Strokes.Count})보다 많이 지울 수 없습니다 : {count}");

            for (int i = 0; i < count; i++)
            {
                window.RemoveLastStroke();
            }

            return new SuccessResponse($"마지막 획 {count}개를 지웠습니다. 남은 획 : {window.Strokes.Count}", new { removedCount = count, totalStrokeCount = window.Strokes.Count });
        }

        private static object Clear(StrokeSketchWindow window)
        {
            int clearedCount = window.Strokes.Count;
            window.ClearStrokes();

            return new SuccessResponse($"획 {clearedCount}개를 모두 지웠습니다.", new { clearedCount, totalStrokeCount = 0 });
        }

        private static object Export(StrokeSketchWindow window, JObject @params)
        {
            string name = @params["name"]?.ToString();
            if (string.IsNullOrEmpty(name)) return new ErrorResponse("name 인자가 비어 있습니다.");
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return new ErrorResponse($"파일 이름으로 쓸 수 없는 글자가 있습니다 : {name}");
            if (window.Strokes.Count == 0) return new ErrorResponse("내보낼 획이 없습니다.");

            string path;
            try
            {
                path = window.ExportDrawing(name);
            }
            catch (IOException exception)
            {
                return new ErrorResponse(exception.Message);
            }

            object data = new
            {
                path,
                playerPath = PlayerPathPrefix + name,
                strokeCount = window.Strokes.Count,
            };

            return new SuccessResponse($"그림을 내보냈습니다 : {path}", data);
        }

        // 도구 인자 설명. MCP for Unity가 이 중첩 클래스를 읽어 AI에게 인자 목록을 알려 준다
        public class Parameters
        {
            [ToolParameter("할 일. get_guide, get_settings, set_settings, draw_svg_path, draw_svg, capture, undo, clear, export")]
            public string action { get; set; }

            // 아래 설정 인자는 모두 set_settings에서 쓴다. 주지 않은 값은 지금 설정을 유지하고, 준 값은 다시 바꿀 때까지 유지된다
            [ToolParameter("펜 색. #RRGGBB 또는 #RRGGBBAA", Required = false)]
            public string color { get; set; }

            [ToolParameter("펜 두께(그림 좌표 단위)", Required = false)]
            public float width { get; set; }

            [ToolParameter("겹침 순서(정수). 클수록 위에 그려지고, 같으면 나중 획이 위다. 재생 순서와는 따로다", Required = false)]
            public int layer { get; set; }

            [ToolParameter("캔버스 너비(px)", Required = false)]
            public float canvas_width { get; set; }

            [ToolParameter("캔버스 높이(px)", Required = false)]
            public float canvas_height { get; set; }

            [ToolParameter("다듬기 : 점 간격(그림 좌표 단위). 입력 점을 이 간격 기준으로 다시 고른다", Required = false)]
            public float target_spacing { get; set; }

            [ToolParameter("다듬기 : 꺾임 각도(도). 모서리로 볼 꺾임 기준이며, 180을 넘기면 모서리 찾기를 끈다", Required = false)]
            public float corner_angle { get; set; }

            [ToolParameter("다듬기 : 꺾임 속도 비율(0~1). 획 속도 중앙값 대비 비율로, 모서리 판단에 쓴다", Required = false)]
            public float corner_speed_ratio { get; set; }

            [ToolParameter("다듬기 : 다듬기 반경(정수, 0 이상). 작을수록 손떨림이 남는다", Required = false)]
            public int smoothing_radius { get; set; }

            [ToolParameter("다듬기 : 피팅 허용 오차. 클수록 곡선이 적고 매끈해진다", Required = false)]
            public float tolerance { get; set; }

            [ToolParameter("draw_svg_path에서 쓴다. SVG path의 d 속성. 캔버스 좌표(왼쪽 위 원점, y는 아래로). 지원 명령 : M L H V C S Q T Z", Required = false)]
            public string path_data { get; set; }

            [ToolParameter("draw_svg에서 쓴다. SVG 문서 전체 텍스트(<svg ...>...</svg>). viewBox나 width, height가 있어야 한다", Required = false)]
            public string svg { get; set; }

            [ToolParameter("undo에서 쓴다. 마지막 획부터 지울 개수(1 이상). 주지 않으면 1", Required = false)]
            public int count { get; set; }

            [ToolParameter("export에서 쓴다. 확장자 없는 파일 이름. Assets/Resources/Drawings/{name}.json으로 저장하고, 같은 이름이 있으면 덮어쓰지 않는다", Required = false)]
            public string name { get; set; }
        }
    }
}
