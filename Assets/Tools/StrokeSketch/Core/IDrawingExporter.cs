using Oeccsy.StrokeMotion;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 그림 데이터를 파일로 내보내는 인터페이스.
    /// 그림과 저장 경로를 받아 구현체가 정한 형식으로 저장한다.
    /// </summary>
    public interface IDrawingExporter
    {
        void Export(Drawing drawing, string path);
    }
}
