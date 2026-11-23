using System;
using System.Collections.Generic;

namespace Oeccsy.StrokeSketch
{
    /// <summary>
    /// 입력을 처리해 완성된 획을 제공하는 인터페이스.
    /// 획이 끝나면 그 점 목록을 이벤트에 실어 알린다. 색과 굵기는 받는 쪽의 설정이 정한다.
    /// </summary>
    public interface IStrokeInputHandler
    {
        // 점 목록은 알림 동안만 유효하다. 받는 쪽에서 보관하려면 복사한다.
        event Action<IReadOnlyList<StrokePoint>> StrokeFinished;
    }
}
