using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
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

        // 명령 글자 하나, 또는 숫자 하나(부호, 소수, 지수 포함). 쉼표와 공백은 구분자로 건너뛴다
        private static readonly Regex TokenPattern = new Regex(@"[A-Za-z]|[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?");

        public event Action<IReadOnlyList<StrokePoint>> StrokeFinished;

        // 경로를 읽어 획마다 완료를 알리고, 알린 획 수를 반환한다.
        // 경로 전체를 먼저 해석하므로 문법이 틀리면 획을 하나도 알리지 않고 FormatException을 던진다
        public int ReadPath(string pathData)
        {
            SvgPathParser parser = new SvgPathParser(pathData);
            List<List<Vector2>> parsedStrokes = parser.Parse();

            int finishedCount = 0;
            foreach (List<Vector2> positions in parsedStrokes)
            {
                if (positions.Count < MinPointCount) continue;

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

            private void MoveTo(Vector2 point)
            {
                currentStroke = new List<Vector2> { point };
                strokes.Add(currentStroke);

                currentPoint = point;
                subpathStart = point;
            }

            private void LineTo(Vector2 end)
            {
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
                if (currentStroke == null) throw new FormatException("경로가 M으로 시작하지 않습니다.");
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
