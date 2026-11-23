namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 획의 점 간격을 조절하는 인터페이스.
    /// 획과 목표 간격을 받아 구현체의 배치 방식에 따라 점을 다시 놓는다.
    /// </summary>
    public interface IStrokeResampler
    {
        // targetSpacing은 그림 좌표 단위이며, 실제 간격과 끝점 처리는 구현체에 따라 다르다.
        RawStroke Resample(RawStroke stroke, float targetSpacing);
    }
}
