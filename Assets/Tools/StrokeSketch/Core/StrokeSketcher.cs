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
        private readonly IStrokeResampler strokeResampler = new UniformStrokeResampler();
        private readonly ICornerFinder cornerFinder = new AngleSpeedCornerFinder();
        private readonly IStrokeSmoother strokeSmoother = new MovingAverageStrokeSmoother();
        private readonly ICurveFitter curveFitter = new SchneiderCurveFitter();
        private readonly IDrawingExporter drawingExporter = new JsonDrawingExporter();

        private readonly List<Stroke> strokes = new List<Stroke>();

        public StrokeSketcher(SketchSettings settings)
        {
            Settings = settings;
        }

        public SketchSettings Settings { get; set; }
        public IReadOnlyList<Stroke> Strokes => strokes;

        // 입력의 점 목록에 지금 펜 설정의 색과 굵기를 입혀 정리한다.
        public void RefineStroke(IReadOnlyList<StrokePoint> points)
        {
            RawStroke rawStroke = new RawStroke(Settings.penColor, Settings.penWidth, points);

            RawStroke resampledStroke = strokeResampler.Resample(rawStroke, Settings.targetSpacing);
            IReadOnlyList<int> corners = cornerFinder.FindCorners(resampledStroke, Settings.cornerAngle, Settings.cornerSpeedRatio);
            RawStroke smoothedStroke = strokeSmoother.Smooth(resampledStroke, corners, Settings.smoothingRadius);
            Stroke stroke = curveFitter.Fit(smoothedStroke, corners, Settings.tolerance);

            strokes.Add(stroke);
        }

        public void RemoveLastStroke()
        {
            if (strokes.Count == 0) return;

            strokes.RemoveAt(strokes.Count - 1);
        }

        public void ClearStrokes()
        {
            strokes.Clear();
        }

        public void Export(string path)
        {
            Drawing drawing = new Drawing(Settings.canvasSize, strokes);
            drawingExporter.Export(drawing, path);
        }
    }
}
