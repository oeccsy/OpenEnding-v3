using System.Collections.Generic;
using UnityEngine;

namespace Oeccsy.StrokeMotion
{
    /// <summary>
    /// 씬에 놓여 획 애니메이션을 보여 주는 객체.
    /// Player가 만들고 전처리 결과(시간표에 놓인 획)를 채우며, 매 프레임 자기 렌더러로 그려진다.
    /// </summary>
    [RequireComponent(typeof(QuadStrokeRenderer))]
    public class StrokeMotionObject : MonoBehaviour
    {
        public IReadOnlyList<ScheduledStroke> ScheduledStrokes { get; set; }

        public IStrokeRenderer Renderer { get; private set; }

        private void Awake()
        {
            Renderer = GetComponent<IStrokeRenderer>();
        }
    }
}
