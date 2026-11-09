using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Oeccsy.StrokeMotion
{
    /// <summary>
    /// curve-sketch 도구가 내보낸 JSON을 Drawing으로 읽는 클래스.
    /// 손그림 원본(raw)과 피팅 설정(settings) 같은 도구용 필드는 읽지 않는다.
    /// </summary>
    public class JsonDrawingParser : IDrawingParser
    {
        // path는 Resources 폴더 기준 경로이고 확장자를 붙이지 않는다 (예: Oeccsy.StrokeMotion/Drawings/nebula)
        public string Load(string path)
        {
            TextAsset file = Resources.Load<TextAsset>(path);
            if (file == null) throw new FileNotFoundException($"Resources에서 그림 파일을 찾을 수 없습니다 : {path}");

            return file.text;
        }

        public Drawing Parse(string source)
        {
            try
            {
                JObject root = JObject.Parse(source);
                JArray strokeArray = root["strokes"] as JArray;
                if (strokeArray == null) throw new FormatException("strokes 배열이 없습니다.");

                IEnumerable<JToken> drawableStrokeTokens = strokeArray.Where(HasCurves);
                IEnumerable<Stroke> strokes = drawableStrokeTokens.Select(ParseStroke);

                return new Drawing(strokes);
            }
            catch (Exception exception) when (exception is not FormatException)
            {
                // 라이브러리 예외를 감춰, 호출부는 어떤 파서든 FormatException 하나만 잡으면 되게 한다
                throw new FormatException("curve-sketch JSON 형식이 아닙니다.", exception);
            }
        }

        private bool HasCurves(JToken strokeToken)
        {
            JArray curveArray = strokeToken["curves"] as JArray;
            if (curveArray == null) return false;

            return curveArray.Count > 0;
        }

        private Stroke ParseStroke(JToken strokeToken)
        {
            JToken colorToken = strokeToken["color"];
            JToken widthToken = strokeToken["width"];
            JArray curveArray = (JArray)strokeToken["curves"];
            if (colorToken == null) throw new FormatException("획에 color가 없습니다.");
            if (widthToken == null) throw new FormatException("획에 width가 없습니다.");

            string colorText = colorToken.Value<string>();
            Color color = ParseColor(colorText);
            float width = widthToken.Value<float>();
            IEnumerable<Curve> curves = curveArray.Select(ParseCurve);

            return new Stroke(color, width, curves);
        }

        private Color ParseColor(string colorText)
        {
            if (!ColorUtility.TryParseHtmlString(colorText, out Color color)) throw new FormatException($"color 값을 읽을 수 없습니다: {colorText}");

            return color;
        }

        private Curve ParseCurve(JToken curveToken)
        {
            Vector2 start = ParsePoint(curveToken[0]);
            Vector2 control1 = ParsePoint(curveToken[1]);
            Vector2 control2 = ParsePoint(curveToken[2]);
            Vector2 end = ParsePoint(curveToken[3]);

            return new Curve(start, control1, control2, end);
        }

        private Vector2 ParsePoint(JToken pointToken)
        {
            float x = pointToken[0].Value<float>();
            float y = pointToken[1].Value<float>();

            return new Vector2(x, y);
        }
    }
}
