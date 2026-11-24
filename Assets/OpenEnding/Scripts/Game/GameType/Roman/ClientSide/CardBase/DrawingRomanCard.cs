using System.Collections;
using DG.Tweening;
using Game.GameType.Roman.Data;
using Oeccsy.StrokeMotion;
using UnityEngine;

namespace Game.GameType.Roman.ClientSide.CardBase
{
    /// <summary>
    /// 폴리곤 대신 StrokeSketch로 그린 드로잉 애니메이션을 재생하는 카드.
    /// 카드마다 그림 경로와 카드 정보 경로만 정하고, 필요하면 그리는 속도를 바꾼다.
    /// </summary>
    public abstract class DrawingRomanCard : RomanCard
    {
        // 그림은 게임 해상도(1080 x 2400) 캔버스에 그렸다. 캔버스 높이를 카메라 높이에 맞춘다
        private const float CanvasHeight = 2400f;
        private const float HideSeconds = 0.2f;

        private Transform drawingRoot;
        private Vector3 fittedScale;
        private StrokeMotionObject drawing;

        protected abstract string DrawingPath { get; }
        protected abstract string CardInfoPath { get; }

        // 한 장이 3~4초 안에 다 그려지는 속도
        protected virtual float PenSpeed => 1600f;
        protected virtual float GapSeconds => 0.12f;

        protected override void Awake()
        {
            base.Awake();

            RomanCardInfo cardInfo = Resources.Load<RomanCardInfo>(CardInfoPath);
            cardInfoUI.RefreshUI(cardInfo);

            drawingRoot = new GameObject("Drawing").transform;
            drawingRoot.SetParent(transform, false);
            float canvasWorldHeight = CanvasHeight / DrawingCoordinates.PixelsPerUnit;
            float cameraWorldHeight = Camera.main.orthographicSize * 2f;
            fittedScale = Vector3.one * (cameraWorldHeight / canvasWorldHeight);
            drawingRoot.localScale = fittedScale;

            StrokeMotionPlayer player = FindFirstObjectByType<StrokeMotionPlayer>();
            drawing = player.Load(DrawingPath, drawingRoot, PenSpeed, GapSeconds);
        }

        // 처음부터 다시 그리고, 다 그려질 때까지 기다린다
        protected override IEnumerator ShowPolygons()
        {
            drawingRoot.DOKill();
            drawingRoot.localScale = fittedScale;
            drawing.Play();

            yield return new WaitForSeconds(drawing.Duration);
        }

        public override IEnumerator Hide()
        {
            Tween hideTween = drawingRoot.DOScale(0f, HideSeconds).SetEase(Ease.InCirc).SetLink(gameObject);

            yield return hideTween.WaitForCompletion();
        }
    }
}
