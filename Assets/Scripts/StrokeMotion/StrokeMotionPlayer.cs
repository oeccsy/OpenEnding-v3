using System.Collections.Generic;
using UnityEngine;

namespace Oeccsy.StrokeMotion
{
    /// <summary>
    /// 그림을 불러와 그려지는 애니메이션으로 재생하는 클래스.
    /// 불러올 때 한 번 전처리해 애니메이션 객체를 만들고, 매 프레임 그 시각의 모습을 각 객체의 렌더러로 그린다.
    /// </summary>
    public class StrokeMotionPlayer : MonoBehaviour
    {
        private readonly IDrawingParser drawingParser = new JsonDrawingParser();
        private readonly IStrokeSampler strokeSampler = new BezierStrokeSampler();
        private readonly IStrokeScheduler strokeScheduler = new StrokeScheduler();
        private readonly IObjectSpawner objectSpawner = new StrokeMotionObjectSpawner();
        private readonly IProgressEvaluator progressEvaluator = new EaseInOutProgressEvaluator();
        private readonly IStrokeTrimmer strokeTrimmer = new LengthBasedStrokeTrimmer();

        private readonly List<StrokeMotionObject> playList = new List<StrokeMotionObject>();
        
        private void Start()
        {
            Load("Drawings/test3");
        }

        private void Update()
        {
            foreach (StrokeMotionObject motionObject in playList)
            {
                Draw(motionObject, Time.time);
            }
        }

        public void Load(string drawingPath)
        {
            string source = drawingParser.Load(drawingPath);
            Drawing drawing = drawingParser.Parse(source);

            IReadOnlyList<SampledStroke> sampledStrokes = strokeSampler.Sample(drawing.Strokes);
            IReadOnlyList<ScheduledStroke> scheduledStrokes = strokeScheduler.Schedule(sampledStrokes);

            Vector3 centeredPosition = DrawingCoordinates.CalculateCenteredPosition(drawing.CanvasSize);
            StrokeMotionObject motionObject = objectSpawner.Spawn(drawingPath, transform, centeredPosition);
            motionObject.ScheduledStrokes = scheduledStrokes;
            
            playList.Add(motionObject);
        }

        private void Draw(StrokeMotionObject motionObject, float time)
        {
            List<TrimmedStroke> trimmedStrokes = new List<TrimmedStroke>();

            foreach (ScheduledStroke scheduledStroke in motionObject.ScheduledStrokes)
            {
                float progress = progressEvaluator.Evaluate(scheduledStroke, time);

                TrimmedStroke trimmedStroke = strokeTrimmer.Trim(scheduledStroke.Stroke, progress);
                trimmedStrokes.Add(trimmedStroke);
            }

            IStrokeRenderer strokeRenderer = motionObject.Renderer;
            strokeRenderer.BuildMesh(trimmedStrokes);
        }
    }
}
