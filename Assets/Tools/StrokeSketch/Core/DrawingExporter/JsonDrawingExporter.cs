using System;
using Oeccsy.StrokeMotion;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// Drawing을 StrokeMotion의 JsonDrawingParser가 읽는 JSON 형식으로 바꿔 파일로 내보내는 클래스.
    /// 아직 구현하지 않았다.
    /// </summary>
    public class JsonDrawingExporter : IDrawingExporter
    {
        public void Export(Drawing drawing, string path)
        {
            throw new NotImplementedException("그림 내보내기는 아직 구현하지 않았음");
        }
    }
}
