using System.Collections.Generic;

namespace Oeccsy.StrokeMotion
{
    public interface IStrokeRenderer
    {
        void BuildMesh(IReadOnlyList<TrimmedStroke> strokes);
    }
}
