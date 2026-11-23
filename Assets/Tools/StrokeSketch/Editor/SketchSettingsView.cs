using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 그림 설정을 편집하는 패널.
    /// 설정의 입력칸을 만들고, 값이 바뀌면 바뀐 설정을 알린다.
    /// </summary>
    public class SketchSettingsView : VisualElement
    {
        private const float PanelWidth = 300f;
        private const float PanelPadding = 8f;
        private const float SectionPadding = 6f;
        private const float SectionRadius = 4f;

        // 반투명 회색이라 라이트, 다크 테마 모두에서 묶음이 구분된다.
        private static readonly Color SectionColor = new Color(0.5f, 0.5f, 0.5f, 0.12f);

        private SketchSettings settings;

        public SketchSettingsView(SketchSettings initialSettings)
        {
            settings = initialSettings;

            style.width = PanelWidth;
            style.flexShrink = 0f;
            style.paddingLeft = PanelPadding;
            style.paddingRight = PanelPadding;
            style.paddingTop = PanelPadding;

            // 라벨을 빼서 X, Y 입력칸이 패널 폭을 모두 쓰게 한다.
            VisualElement canvasSection = AddSection("캔버스 크기");
            AddField(canvasSection, new Vector2Field(), settings.canvasSize, (value) => settings.canvasSize = value);

            VisualElement penSection = AddSection("펜");
            AddField(penSection, new ColorField("색"), settings.penColor, (value) => settings.penColor = value);
            AddField(penSection, new Slider("두께", 0.5f, 100f) { showInputField = true }, settings.penWidth, (value) => settings.penWidth = value);

            VisualElement refineSection = AddSection("다듬기");
            AddField(refineSection, new Slider("점 간격", 1f, 12f) { showInputField = true }, settings.targetSpacing, (value) => settings.targetSpacing = value);
            AddField(refineSection, new Slider("꺾임 각도", 10f, 181f) { showInputField = true }, settings.cornerAngle, (value) => settings.cornerAngle = value);
            AddField(refineSection, new Slider("꺾임 속도 비율", 0f, 1f) { showInputField = true }, settings.cornerSpeedRatio, (value) => settings.cornerSpeedRatio = value);
            AddField(refineSection, new SliderInt("다듬기 반경", 0, 10) { showInputField = true }, settings.smoothingRadius, (value) => settings.smoothingRadius = value);
            AddField(refineSection, new Slider("피팅 허용 오차", 0.5f, 12f) { showInputField = true }, settings.tolerance, (value) => settings.tolerance = value);

            // 자주 쓰지 않는 도움말은 패널 바닥에 붙인다.
            Button helpButton = new Button(() => HelpRequested?.Invoke()) { text = "파라미터 안내" };
            helpButton.style.marginTop = StyleKeyword.Auto;
            helpButton.style.marginBottom = PanelPadding;
            Add(helpButton);
        }

        public event Action<SketchSettings> SettingsChanged;
        public event Action HelpRequested;

        private VisualElement AddSection(string title)
        {
            VisualElement section = new VisualElement();
            section.style.backgroundColor = SectionColor;
            section.style.borderTopLeftRadius = SectionRadius;
            section.style.borderTopRightRadius = SectionRadius;
            section.style.borderBottomLeftRadius = SectionRadius;
            section.style.borderBottomRightRadius = SectionRadius;
            section.style.paddingLeft = SectionPadding;
            section.style.paddingRight = SectionPadding;
            section.style.paddingTop = SectionPadding;
            section.style.paddingBottom = SectionPadding;
            section.style.marginBottom = PanelPadding;

            Label titleLabel = new Label(title);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = SectionPadding;

            section.Add(titleLabel);
            Add(section);
            return section;
        }

        private void AddField<T>(VisualElement section, BaseField<T> field, T initialValue, Action<T> applyValue)
        {
            field.SetValueWithoutNotify(initialValue);
            field.AddToClassList(BaseField<T>.alignedFieldUssClassName);
            field.RegisterValueChangedCallback((changeEvent) =>
            {
                applyValue(changeEvent.newValue);
                SettingsChanged?.Invoke(settings);
            });

            section.Add(field);
        }
    }
}
