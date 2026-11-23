using UnityEngine;

namespace Oeccsy.StrokeMotion
{
    public interface IObjectSpawner
    {
        StrokeMotionObject Spawn(string name, Transform parent, Vector3 localPosition);
    }
}
