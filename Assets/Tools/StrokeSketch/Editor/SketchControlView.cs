using System;
using UnityEngine.UIElements;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 그림을 다루는 버튼을 모은 패널.
    /// 버튼이 눌리면 요청만 알리고, 실제 처리는 받는 쪽이 한다.
    /// </summary>
    public class SketchControlView : VisualElement
    {
        private const float PanelPadding = 8f;
        private const float StrokeButtonHeight = 36f;
        private const float HelpButtonHeight = 28f;
        private const float ButtonGap = 4f;
        private const float HelpButtonGap = 16f;

        public SketchControlView()
        {
            style.paddingLeft = PanelPadding;
            style.paddingRight = PanelPadding;

            // 설명하는 대상인 설정 바로 아래에 둔다.
            Button helpButton = new Button();
            helpButton.text = "파라미터 안내";
            helpButton.clicked += () => HelpRequested?.Invoke();
            helpButton.style.height = HelpButtonHeight;
            Add(helpButton);

            Button undoButton = new Button();
            undoButton.text = "이전 획 지우기 (Ctrl+Z)";
            undoButton.clicked += () => UndoRequested?.Invoke();
            undoButton.style.height = StrokeButtonHeight;
            // 성격이 다른 도움말과 간격을 두어 지우기 버튼과 구분한다.
            undoButton.style.marginTop = HelpButtonGap;
            undoButton.style.marginBottom = ButtonGap;
            Add(undoButton);

            Button clearButton = new Button();
            clearButton.text = "전체 지우기 (Ctrl+Del)";
            clearButton.clicked += () => ClearRequested?.Invoke();
            clearButton.style.height = StrokeButtonHeight;
            clearButton.style.marginBottom = ButtonGap;
            Add(clearButton);

            Button exportButton = new Button();
            exportButton.text = "내보내기 (Ctrl+S)";
            exportButton.clicked += () => ExportRequested?.Invoke();
            exportButton.style.height = StrokeButtonHeight;
            Add(exportButton);
        }

        public event Action UndoRequested;
        public event Action ClearRequested;
        public event Action HelpRequested;
        public event Action ExportRequested;
    }
}
