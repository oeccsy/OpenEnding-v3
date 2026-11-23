using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Oeccsy.StrokeMotion
{
    /// <summary>
    /// 잘린 획들을 월드 메시 하나로 만드는 클래스.
    /// 선분마다 사각형을 하나씩 만들고, 가장자리는 StrokeLine 셰이더가 SDF로 매끈하게 칠한다.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class QuadStrokeRenderer : MonoBehaviour, IStrokeRenderer
    {
        // 셰이더가 가장자리를 부드럽게 칠할 여유 공간 (그림 좌표 단위)
        private const float AntialiasMargin = 2f;

        private Mesh mesh;

        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<int> triangles = new List<int>();
        private readonly List<Vector4> uvs = new List<Vector4>();
        private readonly List<Color> colors = new List<Color>();

        private void Awake()
        {
            EnsureMesh();
        }

        private void OnDestroy()
        {
            Destroy(mesh);
        }

        public void BuildMesh(IReadOnlyList<TrimmedStroke> strokes)
        {
            EnsureMesh();
            ClearBuffers();
            
            foreach (TrimmedStroke stroke in strokes)
            {
                for (int i = 0; i + 1 < stroke.Points.Count; i++)
                {
                    AddSegmentQuad(stroke.Points[i], stroke.Points[i + 1], stroke.Width, stroke.Color);
                }
            }

            ApplyToMesh();
        }

        private void EnsureMesh()
        {
           if (mesh != null) return;

            mesh = new Mesh();
            mesh.MarkDynamic();

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
        }

        private void ClearBuffers()
        {
            vertices.Clear();
            triangles.Clear();
            uvs.Clear();
            colors.Clear();
        }

        private void AddSegmentQuad(Vector2 start, Vector2 end, float width, Color color)
        {
            float length = Vector2.Distance(start, end);
            if (length < 0.01f) return;

            //  topLeft                                   topRight
            //     ●━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━●  ┬
            //     ┃                                         ┃  │ acrossExtent
            //     ┃ start ●━━━━━━━━━━━━━━━━━━━━━━━━>● end   ┃  ┴
            //     ┃                                         ┃
            //     ●━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━●
            //  bottomLeft                               bottomRight
            //                          ├────────────────────┤
            //                               alongExtent

            Vector2 dir = (end - start) / length;
            Vector2 normal = new Vector2(dir.y, -dir.x);
            Vector2 center = (start + end) * 0.5f;

            float halfLength = length * 0.5f;
            float halfWidth = width * 0.5f;

            float alongExtent = halfLength + halfWidth + AntialiasMargin;
            float acrossExtent = halfWidth + AntialiasMargin;

            Vector2 alongOffset = dir * alongExtent;
            Vector2 acrossOffset = normal * acrossExtent;

            Vector2 bottomLeft = center - alongOffset - acrossOffset;
            Vector2 topLeft = center - alongOffset + acrossOffset;
            Vector2 topRight = center + alongOffset + acrossOffset;
            Vector2 bottomRight = center + alongOffset - acrossOffset;

            int baseIndex = vertices.Count;

            vertices.Add(DrawingCoordinates.ToLocal(bottomLeft));
            vertices.Add(DrawingCoordinates.ToLocal(topLeft));
            vertices.Add(DrawingCoordinates.ToLocal(topRight));
            vertices.Add(DrawingCoordinates.ToLocal(bottomRight));

            triangles.Add(baseIndex + 0);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 3);
            triangles.Add(baseIndex + 0);

            // (dir 좌표, normal 좌표, 선분 반길이, 선 반폭). 좌표는 선분 중점이 원점이고, 모서리 위치의 부호와 같다
            uvs.Add(new Vector4(-alongExtent, -acrossExtent, halfLength, halfWidth));
            uvs.Add(new Vector4(-alongExtent, acrossExtent, halfLength, halfWidth));
            uvs.Add(new Vector4(alongExtent, acrossExtent, halfLength, halfWidth));
            uvs.Add(new Vector4(alongExtent, -acrossExtent, halfLength, halfWidth));

            for (int i = 0; i < 4; i++)
            {
                colors.Add(color);
            }
        }

        private void ApplyToMesh()
        {
            mesh.Clear();

            // 정점이 16비트 인덱스 한도를 넘으면 32비트 인덱스로 바꾼다
            bool needsWideIndex = vertices.Count > ushort.MaxValue;
            mesh.indexFormat = (needsWideIndex) ? IndexFormat.UInt32 : IndexFormat.UInt16;

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.RecalculateBounds();
        }
    }
}
