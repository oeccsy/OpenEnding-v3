using System;
using System.Collections.Generic;
using System.Linq;
using Oeccsy.StrokeMotion;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 완성된 획과 입력 중인 선을 표시하는 메시 미리보기.
    /// QuadStrokeRenderer로 메시를 만들고, PreviewRenderUtility의 직교 카메라로 렌더링한다.
    /// </summary>
    public class StrokeSketchPreview : IDisposable
    {
        private const string StrokeMaterialPath = "Oeccsy.StrokeMotion/Material/StrokeLine";

        private const float CameraDistance = 10f;

        private static readonly Color PaperColor = new Color32(253, 250, 243, 255);

        private readonly IStrokeSampler strokeSampler = new BezierStrokeSampler();
        private readonly PreviewRenderUtility previewUtility = new PreviewRenderUtility();
        private readonly QuadStrokeRenderer finishedStrokeRenderer;
        private readonly QuadStrokeRenderer drawingStrokeRenderer;

        public StrokeSketchPreview()
        {
            Camera camera = previewUtility.camera;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = PaperColor;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.allowHDR = false;

            Material strokeMaterial = Resources.Load<Material>(StrokeMaterialPath);
            GameObject finishedObject = new GameObject("Finished Strokes") { hideFlags = HideFlags.HideAndDontSave };
            finishedStrokeRenderer = finishedObject.AddComponent<QuadStrokeRenderer>();
            finishedObject.GetComponent<MeshRenderer>().sharedMaterial = strokeMaterial;
            previewUtility.AddSingleGO(finishedObject);

            GameObject drawingObject = new GameObject("Drawing Stroke") { hideFlags = HideFlags.HideAndDontSave };
            drawingStrokeRenderer = drawingObject.AddComponent<QuadStrokeRenderer>();
            drawingObject.GetComponent<MeshRenderer>().sharedMaterial = strokeMaterial;
            previewUtility.AddSingleGO(drawingObject);
        }

        // 완성된 획은 샘플링한다.
        public void RebuildFinishedStrokes(IReadOnlyList<Stroke> strokes)
        {
            List<TrimmedStroke> renderStrokes = new List<TrimmedStroke>();
            foreach (SampledStroke sampledStroke in strokeSampler.Sample(strokes))
            {
                renderStrokes.Add(new TrimmedStroke(sampledStroke.Color, sampledStroke.Width, sampledStroke.Points.ToArray()));
            }

            finishedStrokeRenderer.BuildMesh(renderStrokes);
        }

        // IMGUI의 Repaint 단계에서만 렌더링한다. 입력 중인 획은 원본 점을 사용한다.
        public void Draw(Rect rect, IReadOnlyList<StrokePoint> drawingPoints, SketchSettings settings)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (rect.width <= 0f || rect.height <= 0f) return;

            List<TrimmedStroke> drawingStrokes = new List<TrimmedStroke>();
            if (drawingPoints.Count >= 2)
            {
                Vector2[] positions = drawingPoints.Select(point => point.Position).ToArray();
                drawingStrokes.Add(new TrimmedStroke(settings.penColor, settings.penWidth, positions));
            }

            drawingStrokeRenderer.BuildMesh(drawingStrokes);
            
            FitCamera(settings.canvasSize);
            previewUtility.BeginPreview(rect, GUIStyle.none);
            previewUtility.Render(allowScriptableRenderPipeline: true, updatefov: false);
            Texture image = previewUtility.EndPreview();
            GUI.DrawTexture(rect, image, ScaleMode.StretchToFill, false);
        }

        // Cleanup은 씬 오브젝트를 정리하므로, 생성한 메시는 별도로 해제한다.
        public void Dispose()
        {
            MeshFilter finishedMeshFilter = finishedStrokeRenderer.GetComponent<MeshFilter>();
            if (finishedMeshFilter.sharedMesh != null) Object.DestroyImmediate(finishedMeshFilter.sharedMesh);

            MeshFilter drawingMeshFilter = drawingStrokeRenderer.GetComponent<MeshFilter>();
            if (drawingMeshFilter.sharedMesh != null) Object.DestroyImmediate(drawingMeshFilter.sharedMesh);

            previewUtility.Cleanup();
        }

        // 카메라를 그림 중심의 로컬 위치에 둔다.
        private void FitCamera(Vector2 canvasSize)
        {
            Vector2 worldSize = canvasSize / DrawingCoordinates.PixelsPerUnit;
            Vector3 localCenter = DrawingCoordinates.ToLocal(canvasSize * 0.5f);
            Vector3 cameraPosition = new Vector3(localCenter.x, localCenter.y, -CameraDistance);

            Camera camera = previewUtility.camera;
            camera.transform.SetPositionAndRotation(cameraPosition, Quaternion.identity);
            camera.orthographicSize = worldSize.y * 0.5f;
            camera.aspect = worldSize.x / worldSize.y;
        }
    }
}
