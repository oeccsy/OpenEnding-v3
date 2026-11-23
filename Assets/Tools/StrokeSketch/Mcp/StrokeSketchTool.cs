using System;
using System.Collections.Generic;
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
    [McpForUnityTool("stroke_sketch", Description ="StrokeSketch 에디터 창을 조작한다. action : draw_svg_path (SVG path의 d 속성을 그린다. M마다 한 획이 되고 손그림과 같은 정리 과정을 거친다), capture (완성된 획을 캔버스 크기 PNG로 저장하고 파일 경로를 돌려준다. 그 파일을 읽어 결과를 눈으로 확인한다)")]
    public static class StrokeSketchTool
    {
        private const string DrawSvgPathAction = "draw_svg_path";
        private const string CaptureAction = "capture";

        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString();
            if (string.IsNullOrEmpty(action)) return new ErrorResponse("action 인자가 비어 있습니다.");

            StrokeSketchWindow window = StrokeSketchWindow.Open();

            if (action == DrawSvgPathAction) return DrawSvgPath(window, @params);
            if (action == CaptureAction) return Capture(window);

            return new ErrorResponse($"모르는 action입니다 : {action}");
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

            IReadOnlyList<Stroke> strokes = window.Strokes;
            SketchSettings settings = window.Settings;
            List<object> drawnStrokes = new List<object>();
            for (int i = strokes.Count - drawnCount; i < strokes.Count; i++)
            {
                drawnStrokes.Add(new { index = i, curveCount = strokes[i].Curves.Count });
            }

            object data = new
            {
                drawnCount,
                totalStrokeCount = strokes.Count,
                canvasSize = new { width = settings.canvasSize.x, height = settings.canvasSize.y },
                penColor = "#" + ColorUtility.ToHtmlStringRGBA(settings.penColor),
                penWidth = settings.penWidth,
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

        // 도구 인자 설명. MCP for Unity가 이 중첩 클래스를 읽어 AI에게 인자 목록을 알려 준다
        public class Parameters
        {
            [ToolParameter("할 일. draw_svg_path, capture")]
            public string action { get; set; }

            [ToolParameter("draw_svg_path에서 쓴다. SVG path의 d 속성. 캔버스 좌표(왼쪽 위 원점, y는 아래로). 지원 명령 : M L H V C S Q T Z", Required = false)]
            public string path_data { get; set; }
        }
    }
}
