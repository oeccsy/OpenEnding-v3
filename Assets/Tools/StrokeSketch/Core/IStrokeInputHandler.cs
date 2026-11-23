using System;
using UnityEngine;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 입력을 처리해 완성된 획을 제공하는 인터페이스.
    /// 획의 완료를 이벤트로 알리고, 지정한 색과 굵기를 적용한 획 데이터를 반환한다.
    /// </summary>
    public interface IStrokeInputHandler
    {
        // 완료 신호만 전달한다. 획은 GetDrawnStroke로 얻는다.
        event Action StrokeFinished;

        // 완성된 획이 없으면 null을 반환한다.
        RawStroke GetDrawnStroke(Color color, float width);
    }
}
