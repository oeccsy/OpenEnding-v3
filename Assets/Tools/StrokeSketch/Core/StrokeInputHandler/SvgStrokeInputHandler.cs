using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// SVG 경로를 획 데이터로 만드는 입력 핸들러.
    /// 경로를 M(이동)마다 한 획으로 나누고, 같은 시간 간격의 점으로 바꿔 획마다 완료를 알린다.
    /// </summary>
    public class SvgStrokeInputHandler : IStrokeInputHandler
    {
        // 곡선을 점으로 바꿀 때의 점 간격(그림 좌표 단위). 이후 리샘플링이 간격을 다시 맞추므로 모양만 살리면 된다
        private const float SampleSpacing = 2f;
        // 점 사이 시간 간격(초). 속도가 일정하므로 모서리는 각도 기준으로만 판단된다
        private const float SecondsPerPoint = 0.01f;
        private const int MinPointCount = 2;
        // 점으로 남길 짧은 획의 길이(그림 좌표 단위). 둥근 펜 끝이 붙어 펜 굵기만 한 점이 된다
        private const float DotLength = 1f;

        // 명령 글자 하나, 또는 숫자 하나(부호, 소수, 지수 포함). 쉼표와 공백은 구분자로 건너뛴다
        private static readonly Regex TokenPattern = new Regex(@"[A-Za-z]|[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?");

        public event Action<IReadOnlyList<StrokePoint>> StrokeFinished;

        // 경로를 읽어 획마다 완료를 알리고, 알린 획 수를 반환한다.
        // 경로 전체를 먼저 해석하므로 문법이 틀리면 획을 하나도 알리지 않고 FormatException을 던진다
        public int ReadPath(string pathData)
        {
            SvgPathParser parser = new SvgPathParser(pathData);
            List<List<Vector2>> parsedStrokes = parser.Parse();

            return FinishStrokes(parsedStrokes);
        }

        // SVG 문서를 읽어 viewBox를 캔버스 가운데에 비율을 지켜 맞춰 그리고, 알린 획 수를 반환한다.
        // 선을 그리는 요소(path, line, polyline, polygon, rect, circle, ellipse)의 윤곽을 문서 순서대로 획으로 만든다.
        // 문서 전체를 먼저 해석하므로 읽을 수 없으면 획을 하나도 알리지 않고 FormatException을 던진다
        public int ReadDocument(string svgText, Vector2 canvasSize)
        {
            SvgDocumentParser documentParser = new SvgDocumentParser(svgText);
            Rect viewBox = documentParser.ReadViewBox();

            List<List<Vector2>> parsedStrokes = new List<List<Vector2>>();
            foreach (string pathData in documentParser.ReadPathData())
            {
                SvgPathParser pathParser = new SvgPathParser(pathData);
                parsedStrokes.AddRange(pathParser.Parse());
            }

            // 비율을 지키며 캔버스에 들어가는 가장 큰 배율로 키우고, 남는 쪽은 가운데에 둔다
            float scale = Mathf.Min(canvasSize.x / viewBox.width, canvasSize.y / viewBox.height);
            Vector2 offset = (canvasSize - viewBox.size * scale) * 0.5f;
            foreach (List<Vector2> positions in parsedStrokes)
            {
                for (int i = 0; i < positions.Count; i++)
                {
                    positions[i] = offset + (positions[i] - viewBox.position) * scale;
                }
            }

            return FinishStrokes(parsedStrokes);
        }

        private int FinishStrokes(List<List<Vector2>> parsedStrokes)
        {
            int finishedCount = 0;
            foreach (List<Vector2> positions in parsedStrokes)
            {
                // 길이가 거의 0인 획(둥근 끝으로 점을 찍는 SVG 기법)은 아주 짧은 가로 획으로 바꿔 점으로 남긴다
                if (positions.Count < MinPointCount)
                {
                    Vector2 dotEnd = positions[0] + new Vector2(DotLength, 0f);
                    positions.Add(dotEnd);
                }

                List<StrokePoint> points = new List<StrokePoint>(positions.Count);
                for (int i = 0; i < positions.Count; i++)
                {
                    points.Add(new StrokePoint(positions[i], i * SecondsPerPoint));
                }

                StrokeFinished?.Invoke(points);
                finishedCount++;
            }

            return finishedCount;
        }

        // SVG 문서에서 viewBox와, 선을 그리는 요소마다의 경로 d 문자열을 꺼낸다.
        // transform은 지원하지 않고, 정의용 요소(defs, clipPath, mask, symbol, marker, pattern) 안쪽은 그리지 않는다
        private class SvgDocumentParser
        {
            // 원을 3차 곡선 4개로 나눌 때 제어점까지의 거리 비율. 4(√2 - 1) / 3
            private const float CircleControlRatio = 0.5522848f;

            private static readonly HashSet<string> DefinitionElementNames = new HashSet<string> { "defs", "clipPath", "mask", "symbol", "marker", "pattern" };
            private static readonly Regex NumberPattern = new Regex(@"[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?");

            private readonly XElement root;

            public SvgDocumentParser(string svgText)
            {
                try
                {
                    XDocument document = XDocument.Parse(svgText);
                    root = document.Root;
                }
                catch (XmlException exception)
                {
                    throw new FormatException($"SVG 문서를 읽을 수 없습니다 : {exception.Message}");
                }

                if (root == null || root.Name.LocalName != "svg") throw new FormatException("최상위 요소가 svg가 아닙니다.");
            }

            // viewBox가 없으면 width, height로 (0, 0)부터의 영역을 만든다
            public Rect ReadViewBox()
            {
                string viewBoxText = root.Attribute("viewBox")?.Value;
                if (!string.IsNullOrEmpty(viewBoxText))
                {
                    List<float> values = ReadNumbers(viewBoxText);
                    if (values.Count != 4 || values[2] <= 0f || values[3] <= 0f) throw new FormatException($"viewBox 형식이 아닙니다 : {viewBoxText}");

                    return new Rect(values[0], values[1], values[2], values[3]);
                }

                float width = ReadAttribute(root, "width");
                float height = ReadAttribute(root, "height");
                if (width <= 0f || height <= 0f) throw new FormatException("viewBox나 width, height가 있어야 크기를 알 수 있습니다.");

                return new Rect(0f, 0f, width, height);
            }

            public List<string> ReadPathData()
            {
                List<string> pathDataList = new List<string>();
                foreach (XElement element in root.Descendants())
                {
                    if (IsInsideDefinition(element)) continue;

                    string pathData = ToPathData(element);
                    if (string.IsNullOrEmpty(pathData)) continue;

                    if (element.AncestorsAndSelf().Any((ancestor) => ancestor.Attribute("transform") != null)) throw new FormatException($"transform은 지원하지 않습니다 : <{element.Name.LocalName}>");

                    pathDataList.Add(pathData);
                }

                return pathDataList;
            }

            // 그리지 않는 요소면 null을 반환한다
            private string ToPathData(XElement element)
            {
                string name = element.Name.LocalName;

                if (name == "path") return element.Attribute("d")?.Value;

                if (name == "line")
                {
                    float x1 = ReadAttribute(element, "x1");
                    float y1 = ReadAttribute(element, "y1");
                    float x2 = ReadAttribute(element, "x2");
                    float y2 = ReadAttribute(element, "y2");

                    return Format($"M {x1} {y1} L {x2} {y2}");
                }

                if (name == "polyline" || name == "polygon")
                {
                    string pointsText = element.Attribute("points")?.Value ?? string.Empty;
                    List<float> values = ReadNumbers(pointsText);
                    if (values.Count < 4) return null;

                    StringBuilder builder = new StringBuilder("M");
                    for (int i = 0; i + 1 < values.Count; i += 2)
                    {
                        builder.Append(Format($" {values[i]} {values[i + 1]}"));
                    }

                    if (name == "polygon") builder.Append(" Z");
                    return builder.ToString();
                }

                if (name == "rect")
                {
                    float x = ReadAttribute(element, "x");
                    float y = ReadAttribute(element, "y");
                    float width = ReadAttribute(element, "width");
                    float height = ReadAttribute(element, "height");
                    if (width <= 0f || height <= 0f) return null;

                    // 둥근 모서리(rx, ry)는 무시하고 직각으로 그린다
                    return Format($"M {x} {y} H {x + width} V {y + height} H {x} Z");
                }

                if (name == "circle")
                {
                    float radius = ReadAttribute(element, "r");
                    if (radius <= 0f) return null;

                    float centerX = ReadAttribute(element, "cx");
                    float centerY = ReadAttribute(element, "cy");
                    return ToEllipsePathData(centerX, centerY, radius, radius);
                }

                if (name == "ellipse")
                {
                    float radiusX = ReadAttribute(element, "rx");
                    float radiusY = ReadAttribute(element, "ry");
                    if (radiusX <= 0f || radiusY <= 0f) return null;

                    float centerX = ReadAttribute(element, "cx");
                    float centerY = ReadAttribute(element, "cy");
                    return ToEllipsePathData(centerX, centerY, radiusX, radiusY);
                }

                return null;
            }

            // 오른쪽 끝에서 시작해 시계 방향(y 아래가 +)으로 3차 곡선 4개를 잇는다
            private string ToEllipsePathData(float centerX, float centerY, float radiusX, float radiusY)
            {
                float controlX = radiusX * CircleControlRatio;
                float controlY = radiusY * CircleControlRatio;
                float left = centerX - radiusX;
                float right = centerX + radiusX;
                float top = centerY - radiusY;
                float bottom = centerY + radiusY;

                string start = Format($"M {right} {centerY}");
                string bottomRight = Format($"C {right} {centerY + controlY} {centerX + controlX} {bottom} {centerX} {bottom}");
                string bottomLeft = Format($"C {centerX - controlX} {bottom} {left} {centerY + controlY} {left} {centerY}");
                string topLeft = Format($"C {left} {centerY - controlY} {centerX - controlX} {top} {centerX} {top}");
                string topRight = Format($"C {centerX + controlX} {top} {right} {centerY - controlY} {right} {centerY}");

                return $"{start} {bottomRight} {bottomLeft} {topLeft} {topRight}";
            }

            private bool IsInsideDefinition(XElement element)
            {
                return element.AncestorsAndSelf().Any((ancestor) => DefinitionElementNames.Contains(ancestor.Name.LocalName));
            }

            // 없는 속성은 0으로 본다. "12px"처럼 단위가 붙어도 앞의 숫자만 읽는다
            private float ReadAttribute(XElement element, string attributeName)
            {
                string text = element.Attribute(attributeName)?.Value;
                if (string.IsNullOrEmpty(text)) return 0f;

                List<float> values = ReadNumbers(text);
                if (values.Count == 0) throw new FormatException($"{attributeName} 값이 숫자가 아닙니다 : {text}");

                return values[0];
            }

            private List<float> ReadNumbers(string text)
            {
                List<float> values = new List<float>();
                foreach (Match match in NumberPattern.Matches(text))
                {
                    float value = float.Parse(match.Value, CultureInfo.InvariantCulture);
                    values.Add(value);
                }

                return values;
            }

            // 문화권에 따라 소수점이 쉼표가 되지 않게 고정한다
            private string Format(FormattableString text)
            {
                return text.ToString(CultureInfo.InvariantCulture);
            }
        }

        // SVG 경로 d 속성을 읽어 M마다 나뉜 점 목록으로 바꾼다.
        // 지원 명령 : M L H V C S Q T Z (대문자는 절대 좌표, 소문자는 현재 점 기준 상대 좌표)
        private class SvgPathParser
        {
            // 같은 자리의 점이 겹쳐 쌓이지 않게 둔 최소 거리
            private const float MinPointDistance = 0.01f;

            private readonly List<string> tokens = new List<string>();
            private readonly List<List<Vector2>> strokes = new List<List<Vector2>>();

            private int index;
            private List<Vector2> currentStroke;
            private Vector2 currentPoint;
            private Vector2 subpathStart;
            private Vector2 lastControlPoint;
            private char lastCommand;

            public SvgPathParser(string pathData)
            {
                foreach (Match match in TokenPattern.Matches(pathData))
                {
                    tokens.Add(match.Value);
                }
            }

            public List<List<Vector2>> Parse()
            {
                char command = ' ';
                while (index < tokens.Count)
                {
                    if (IsCommand(tokens[index]))
                    {
                        command = tokens[index][0];
                        index++;
                    }

                    if (command == ' ') throw new FormatException($"명령 없이 숫자가 왔습니다 : {tokens[index]}");

                    ApplyCommand(command);
                    lastCommand = char.ToUpperInvariant(command);

                    // M 뒤에 이어지는 좌표 쌍은 L로 본다. Z 뒤에는 새 명령이 와야 한다
                    if (command == 'M') command = 'L';
                    if (command == 'm') command = 'l';
                    if (lastCommand == 'Z') command = ' ';
                }

                return strokes;
            }

            private void ApplyCommand(char command)
            {
                bool isRelative = char.IsLower(command);
                Vector2 origin = isRelative ? currentPoint : Vector2.zero;
                char upperCommand = char.ToUpperInvariant(command);

                if (upperCommand == 'M')
                {
                    Vector2 point = origin + ReadPoint();
                    MoveTo(point);
                    return;
                }

                if (upperCommand == 'L')
                {
                    Vector2 end = origin + ReadPoint();
                    LineTo(end);
                    return;
                }

                if (upperCommand == 'H')
                {
                    float x = origin.x + ReadNumber();
                    LineTo(new Vector2(x, currentPoint.y));
                    return;
                }

                if (upperCommand == 'V')
                {
                    float y = origin.y + ReadNumber();
                    LineTo(new Vector2(currentPoint.x, y));
                    return;
                }

                if (upperCommand == 'C')
                {
                    Vector2 control1 = origin + ReadPoint();
                    Vector2 control2 = origin + ReadPoint();
                    Vector2 end = origin + ReadPoint();
                    CubicTo(control1, control2, end);
                    return;
                }

                if (upperCommand == 'S')
                {
                    // 앞 명령이 C나 S면 그 두 번째 제어점을 현재 점 기준으로 뒤집어 첫 제어점으로 쓴다
                    bool isSmooth = lastCommand == 'C' || lastCommand == 'S';
                    Vector2 control1 = isSmooth ? 2f * currentPoint - lastControlPoint : currentPoint;
                    Vector2 control2 = origin + ReadPoint();
                    Vector2 end = origin + ReadPoint();
                    CubicTo(control1, control2, end);
                    return;
                }

                if (upperCommand == 'Q')
                {
                    Vector2 control = origin + ReadPoint();
                    Vector2 end = origin + ReadPoint();
                    QuadTo(control, end);
                    return;
                }

                if (upperCommand == 'T')
                {
                    bool isSmooth = lastCommand == 'Q' || lastCommand == 'T';
                    Vector2 control = isSmooth ? 2f * currentPoint - lastControlPoint : currentPoint;
                    Vector2 end = origin + ReadPoint();
                    QuadTo(control, end);
                    return;
                }

                if (upperCommand == 'Z')
                {
                    LineTo(subpathStart);
                    return;
                }

                throw new FormatException($"지원하지 않는 SVG 명령 : {command}");
            }

            // 획 목록에는 그리는 명령이 처음 올 때 넣는다. 이동만 하고 끝난 M은 획이 되지 않는다
            private void MoveTo(Vector2 point)
            {
                currentStroke = new List<Vector2> { point };

                currentPoint = point;
                subpathStart = point;
            }

            private void BeginDrawing()
            {
                if (currentStroke == null) throw new FormatException("경로가 M으로 시작하지 않습니다.");
                if (strokes.Count > 0 && strokes[strokes.Count - 1] == currentStroke) return;

                strokes.Add(currentStroke);
            }

            private void LineTo(Vector2 end)
            {
                BeginDrawing();

                float length = Vector2.Distance(currentPoint, end);
                int segmentCount = CalculateSegmentCount(length);
                for (int i = 1; i <= segmentCount; i++)
                {
                    float t = (float)i / segmentCount;
                    Vector2 point = Vector2.Lerp(currentPoint, end, t);
                    AddPoint(point);
                }

                currentPoint = end;
            }

            private void CubicTo(Vector2 control1, Vector2 control2, Vector2 end)
            {
                BeginDrawing();

                // 제어 다각형 길이는 곡선 길이보다 길거나 같아서, 점이 모자라지 않는 쪽으로 어림한다
                float controlLength = Vector2.Distance(currentPoint, control1) + Vector2.Distance(control1, control2) + Vector2.Distance(control2, end);
                int segmentCount = CalculateSegmentCount(controlLength);
                Vector2 start = currentPoint;
                for (int i = 1; i <= segmentCount; i++)
                {
                    float t = (float)i / segmentCount;
                    Vector2 point = EvaluateCubic(start, control1, control2, end, t);
                    AddPoint(point);
                }

                currentPoint = end;
                lastControlPoint = control2;
            }

            // 2차 곡선은 같은 모양의 3차 곡선으로 바꿔 그린다
            private void QuadTo(Vector2 control, Vector2 end)
            {
                Vector2 control1 = currentPoint + 2f / 3f * (control - currentPoint);
                Vector2 control2 = end + 2f / 3f * (control - end);
                CubicTo(control1, control2, end);

                lastControlPoint = control;
            }

            private void AddPoint(Vector2 point)
            {
                if (Vector2.Distance(currentStroke[currentStroke.Count - 1], point) < MinPointDistance) return;

                currentStroke.Add(point);
            }

            private Vector2 ReadPoint()
            {
                float x = ReadNumber();
                float y = ReadNumber();

                return new Vector2(x, y);
            }

            private float ReadNumber()
            {
                if (index >= tokens.Count) throw new FormatException("명령에 필요한 숫자가 모자랍니다.");
                if (IsCommand(tokens[index])) throw new FormatException($"숫자 자리에 명령이 왔습니다 : {tokens[index]}");

                float value = float.Parse(tokens[index], CultureInfo.InvariantCulture);
                index++;

                return value;
            }

            private bool IsCommand(string token)
            {
                return char.IsLetter(token[0]);
            }

            private int CalculateSegmentCount(float length)
            {
                return Mathf.Max(1, Mathf.CeilToInt(length / SampleSpacing));
            }

            private Vector2 EvaluateCubic(Vector2 start, Vector2 control1, Vector2 control2, Vector2 end, float t)
            {
                float u = 1f - t;

                return u * u * u * start + 3f * u * u * t * control1 + 3f * u * t * t * control2 + t * t * t * end;
            }
        }
    }
}
