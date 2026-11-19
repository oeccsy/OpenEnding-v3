using System.Collections.Generic;
using Oeccsy.StrokeMotion;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 손으로 그린 획을 3차 베지어 곡선으로 정리해 그림 데이터로 남기는 클래스.
    /// 입력이 획 하나를 끝낼 때마다 그 획을 읽어 리샘플링, 모서리 판단, 손떨림 보정, 곡선 피팅 순서로 정리해 그림에 더한다.
    /// </summary>
    public class StrokeSketcher
    {
        private readonly IStrokeInputHandler strokeInputHandler;
        private readonly IStrokeResampler strokeResampler = new UniformStrokeResampler();
        private readonly ICornerFinder cornerFinder = new AngleSpeedCornerFinder();
        private readonly IStrokeSmoother strokeSmoother = new MovingAverageStrokeSmoother();
        private readonly ICurveFitter curveFitter = new SchneiderCurveFitter();
        private readonly IDrawingExporter drawingExporter = new JsonDrawingExporter();

        private readonly List<Stroke> strokes = new List<Stroke>();

        // 입력 방식은 환경마다 달라서 각각의 환경에서 넣어 준다
        public StrokeSketcher(IStrokeInputHandler strokeInputHandler, SketchSettings settings)
        {
            this.strokeInputHandler = strokeInputHandler;
            Settings = settings;
        }

        public SketchSettings Settings { get; set; }
        public IReadOnlyList<Stroke> Strokes => strokes;

        // 호스트가 입력의 GetDrawnStroke에 연결해, 획이 끝날 때마다 부르게 한다
        public void RefineStroke()
        {
            RawStroke rawStroke = strokeInputHandler.GetDrawnStroke(Settings.penColor, Settings.penWidth);
            if (rawStroke == null) return;

            RawStroke resampledStroke = strokeResampler.Resample(rawStroke, Settings.targetSpacing);
            IReadOnlyList<int> corners = cornerFinder.FindCorners(resampledStroke, Settings.cornerAngle, Settings.cornerSpeedRatio);
            RawStroke smoothedStroke = strokeSmoother.Smooth(resampledStroke, corners, Settings.smoothingRadius);
            Stroke stroke = curveFitter.Fit(smoothedStroke, corners, Settings.tolerance);

            strokes.Add(stroke);
        }

        public void Export(string path)
        {
            Drawing drawing = new Drawing(strokes);

            drawingExporter.Export(drawing, path);
        }
    }
}
