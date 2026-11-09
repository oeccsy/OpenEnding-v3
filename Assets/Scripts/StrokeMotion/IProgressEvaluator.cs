namespace Oeccsy.StrokeMotion
{
    public interface IProgressEvaluator
    {
        float Evaluate(ScheduledStroke stroke, float time);
    }
}
