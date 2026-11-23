using System.Globalization;
using System.IO;
using System.Text;
using Oeccsy.StrokeMotion;
using UnityEngine;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 그림 데이터를 JSON 파일로 저장하는 내보내기 담당.
    /// StrokeMotion의 JsonDrawingParser가 읽는 형식으로 직렬화한다(구현 예정).
    /// </summary>
    public class JsonDrawingExporter : IDrawingExporter
    {
        // 그림 좌표는 소수 둘째 자리까지만 남긴다. 화면에서 구분되지 않는 자릿수로 파일이 커지지 않게 한다
        private const string NumberFormat = "0.##";

        public void Export(Drawing drawing, string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            File.WriteAllText(path, ToJson(drawing), new UTF8Encoding(false));
        }

        // 획 하나를 한 줄에 쓴다. 사람과 AI가 파일을 열어 획 단위로 읽고 고칠 수 있게 한다
        private string ToJson(Drawing drawing)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{\n  \"strokes\": [\n");

            for (int i = 0; i < drawing.Strokes.Count; i++)
            {
                builder.Append("    ");
                AppendStroke(builder, drawing.Strokes[i]);
                builder.Append(i < drawing.Strokes.Count - 1 ? ",\n" : "\n");
            }

            builder.Append("  ]\n}\n");

            return builder.ToString();
        }

        private void AppendStroke(StringBuilder builder, Stroke stroke)
        {
            builder.Append("{\"color\": \"").Append(ToHtmlColor(stroke.Color)).Append("\", ");
            builder.Append("\"width\": ").Append(ToNumber(stroke.Width)).Append(", ");
            builder.Append("\"curves\": [");

            for (int i = 0; i < stroke.Curves.Count; i++)
            {
                Curve curve = stroke.Curves[i];
                if (i > 0) builder.Append(", ");

                builder.Append('[');
                AppendPoint(builder, curve.Start);
                builder.Append(", ");
                AppendPoint(builder, curve.Control1);
                builder.Append(", ");
                AppendPoint(builder, curve.Control2);
                builder.Append(", ");
                AppendPoint(builder, curve.End);
                builder.Append(']');
            }

            builder.Append("]}");
        }

        private void AppendPoint(StringBuilder builder, Vector2 point)
        {
            builder.Append('[').Append(ToNumber(point.x)).Append(", ").Append(ToNumber(point.y)).Append(']');
        }

        // 불투명하면 #RRGGBB, 투명도가 있으면 #RRGGBBAA. 파서의 ColorUtility.TryParseHtmlString이 둘 다 읽는다
        private string ToHtmlColor(Color color)
        {
            string hex = color.a < 1f ? ColorUtility.ToHtmlStringRGBA(color) : ColorUtility.ToHtmlStringRGB(color);

            return "#" + hex;
        }

        // 컴퓨터의 지역 설정과 상관없이 소수점을 항상 점(.)으로 쓴다
        private string ToNumber(float value)
        {
            return value.ToString(NumberFormat, CultureInfo.InvariantCulture);
        }
    }
}
