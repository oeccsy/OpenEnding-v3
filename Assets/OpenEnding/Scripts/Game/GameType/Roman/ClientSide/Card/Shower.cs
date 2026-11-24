using System.Collections;
using DG.Tweening;
using Game.GameType.Roman.ClientSide.CardBase;
using Game.GameType.Roman.Data;
using Oeccsy.StrokeMotion;
using UnityEngine;

namespace Game.GameType.Roman.ClientSide.Card
{
    /// <summary>
    /// 소나기 카드의 연출. 폴리곤 대신 StrokeSketch로 그린 드로잉 애니메이션을 재생한다.
    /// </summary>
    public class Shower : RomanCard
    {
        private const string DrawingPath = "Drawings/card_shower";
        // 그림은 게임 해상도(1080 x 2400) 캔버스에 그렸다. 캔버스 높이를 카메라 높이에 맞춘다
        private const float CanvasHeight = 2400f;
        // 뒤집은 뒤 확인 시간(3초) 안에 다 그려지도록 카드용으로 빠르게 그린다
        private const float PenSpeed = 3000f;
        private const float GapSeconds = 0.07f;
        private const float HideSeconds = 0.2f;

        private Transform drawingRoot;
        private Vector3 fittedScale;
        private StrokeMotionObject drawing;

        protected override void Awake()
        {
            base.Awake();
            cardType = CardType.Shower;

            RomanCardInfo cardInfo = Resources.Load<RomanCardInfo>("ScriptableObject/Roman/CardInfoSO_Shower");
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
