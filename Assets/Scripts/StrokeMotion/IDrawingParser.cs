namespace Oeccsy.StrokeMotion
{
    public interface IDrawingParser
    {
        string Load(string path);
        Drawing Parse(string source);
    }
}
