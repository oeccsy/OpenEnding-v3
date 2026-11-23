using System.Collections.Generic;
using Oeccsy.StrokeMotion;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 입력된 획을 다듬어 그림으로 쌓는 파이프라인.
    /// 리샘플링부터 곡선 피팅까지 순서대로 연결하고, 모은 획을 내보낸다.
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

        public StrokeSketcher(IStrokeInputHandler strokeInputHandler, SketchSettings settings)
        {
            this.strokeInputHandler = strokeInputHandler;
            Settings = settings;
        }

        public SketchSettings Settings { get; set; }
        public IReadOnlyList<Stroke> Strokes => strokes;

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
