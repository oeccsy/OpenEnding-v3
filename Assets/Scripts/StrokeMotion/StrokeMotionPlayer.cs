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
        // 펜 속도는 초당 그림 좌표 단위, 획 사이 쉬는 시간은 초
        public const float DefaultPenSpeed = 900f;
        public const float DefaultGapSeconds = 0.1f;

        private readonly IDrawingParser drawingParser = new JsonDrawingParser();
        private readonly IStrokeSampler strokeSampler = new BezierStrokeSampler();
        private readonly IObjectSpawner objectSpawner = new StrokeMotionObjectSpawner();
        private readonly IProgressEvaluator progressEvaluator = new EaseInOutProgressEvaluator();
        private readonly IStrokeTrimmer strokeTrimmer = new LengthBasedStrokeTrimmer();

        private readonly List<StrokeMotionObject> playList = new List<StrokeMotionObject>();

        private void Update()
        {
            // 부모와 함께 지워진 객체는 목록에서 뺀다
            playList.RemoveAll((motionObject) => motionObject == null);

            foreach (StrokeMotionObject motionObject in playList)
            {
                float elapsed = Time.time - motionObject.StartTime;
                Draw(motionObject, elapsed);
            }
        }

        // 그림을 parent 아래에 불러온다. Play()를 부르기 전까지는 아무것도 그려지지 않는다.
        public StrokeMotionObject Load(string drawingPath, Transform parent, float penSpeed = DefaultPenSpeed, float gapSeconds = DefaultGapSeconds)
        {
            string source = drawingParser.Load(drawingPath);
            Drawing drawing = drawingParser.Parse(source);

            IStrokeScheduler strokeScheduler = new StrokeScheduler(penSpeed, gapSeconds);
            IReadOnlyList<SampledStroke> sampledStrokes = strokeSampler.Sample(drawing.Strokes);
            IReadOnlyList<ScheduledStroke> scheduledStrokes = strokeScheduler.Schedule(sampledStrokes);

            Vector3 centeredPosition = DrawingCoordinates.CalculateCenteredPosition(drawing.CanvasSize);
            StrokeMotionObject motionObject = objectSpawner.Spawn(drawingPath, parent, centeredPosition);
            motionObject.ScheduledStrokes = scheduledStrokes;

            playList.Add(motionObject);
            return motionObject;
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
