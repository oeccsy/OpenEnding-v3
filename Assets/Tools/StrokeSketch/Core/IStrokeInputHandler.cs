using System;
using UnityEngine;

namespace Oeccsy.StrokeSketch
{
    public interface IStrokeInputHandler
    {
        // 다 그려진 획이 생기면 알린다. 획은 GetDrawnStroke로 얻는다
        event Action StrokeFinished;

        RawStroke GetDrawnStroke(Color color, float width);
    }
}
