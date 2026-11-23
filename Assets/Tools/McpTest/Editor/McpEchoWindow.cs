using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Oeccsy.McpTest
{
    /// <summary>
    /// MCP로 전달받은 텍스트를 띄우는 테스트용 창.
    /// </summary>
    public class McpEchoWindow : EditorWindow
    {
        private Label messageLabel;

        [MenuItem("Tools/MCP Test/Echo Window")]
        public static McpEchoWindow Open()
        {
            McpEchoWindow window = GetWindow<McpEchoWindow>();
            window.titleContent = new GUIContent("MCP Echo");

            return window;
        }

        public void ShowMessage(string message)
        {
            messageLabel.text = message;
            Repaint();
        }

        private void CreateGUI()
        {
            messageLabel = new Label("아직 받은 텍스트가 없습니다.");
            messageLabel.style.fontSize = 18;
            messageLabel.style.whiteSpace = WhiteSpace.Normal;
            messageLabel.style.paddingLeft = 8;
            messageLabel.style.paddingTop = 8;

            rootVisualElement.Add(messageLabel);
        }
    }
}
