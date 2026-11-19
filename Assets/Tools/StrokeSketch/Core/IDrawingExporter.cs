using Oeccsy.StrokeMotion;

namespace Oeccsy.StrokeSketch
{
    public interface IDrawingExporter
    {
        void Export(Drawing drawing, string path);
    }
}
