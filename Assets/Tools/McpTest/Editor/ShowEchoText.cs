using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;

namespace Oeccsy.McpTest
{
    /// <summary>
    /// MCP 도구 show_echo_text. 받은 텍스트를 McpEchoWindow에 띄운다.
    /// </summary>
    [McpForUnityTool("show_echo_text", Description = "Unity 에디터의 MCP Echo 창을 열고 전달한 텍스트를 띄운다")]
    public static class ShowEchoText
    {
        public static object HandleCommand(JObject @params)
        {
            string text = @params["text"]?.ToString();
            if (string.IsNullOrEmpty(text)) return new ErrorResponse("text 인자가 비어 있습니다.");

            McpEchoWindow window = McpEchoWindow.Open();
            window.ShowMessage(text);

            return new SuccessResponse($"표시함 : {text}");
        }

        // 도구 인자 설명. MCP for Unity가 이 중첩 클래스를 읽어 AI에게 인자 목록을 알려 준다
        public class Parameters
        {
            [ToolParameter("창에 띄울 텍스트")]
            public string text { get; set; }
        }
    }
}
