namespace Oeccsy.StrokeSketch
{
    public interface IStrokeResampler
    {
        RawStroke Resample(RawStroke stroke, float targetSpacing);
    }
}
