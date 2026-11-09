using UnityEngine;

namespace Oeccsy.StrokeMotion
{
    /// <summary>
    /// 획 애니메이션 객체를 씬에 만드는 클래스.
    /// 게임 오브젝트를 부모 아래에 놓고, 모든 객체가 공유하는 획 머티리얼을 입혀 돌려준다.
    /// </summary>
    public class StrokeMotionObjectSpawner : IObjectSpawner
    {
        // Resources 폴더 기준 머티리얼 경로
        private const string StrokeMaterialPath = "Oeccsy.StrokeMotion/Material/StrokeLine";

        public StrokeMotionObject Spawn(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);

            StrokeMotionObject motionObject = gameObject.AddComponent<StrokeMotionObject>();

            Material strokeMaterial = Resources.Load<Material>(StrokeMaterialPath);
            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = strokeMaterial;

            return motionObject;
        }
    }
}
