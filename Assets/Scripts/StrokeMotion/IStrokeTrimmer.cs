namespace Oeccsy.StrokeMotion
{
    public interface IStrokeTrimmer
    {
        TrimmedStroke Trim(SampledStroke stroke, float progress);
    }
}
