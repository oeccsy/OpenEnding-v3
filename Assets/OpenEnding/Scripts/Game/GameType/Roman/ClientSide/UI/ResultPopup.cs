using System.Collections;
using DG.Tweening;
using Oeccsy.StrokeMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GameType.Roman.ClientSide.UI
{
    /// <summary>
    /// 승리 화면. 배경을 칠하는 그림과 웃는 얼굴 그림을 차례로 그린 뒤 글자를 서서히 보여 준다.
    /// </summary>
    public class ResultPopup : MonoBehaviour, IUIElement
    {
        private const string BackgroundDrawingPath = "Drawings/background";
        private const string FaceDrawingPath = "Drawings/face";

        // 그림은 게임 해상도(1080 x 2400) 캔버스에 그렸다. 캔버스 높이를 카메라 높이에 맞춘다
        private const float CanvasHeight = 2400f;
        // 카드(z = 0)보다 Main Camera(z = -5) 쪽에 두어 카드 위에 그린다. 얼굴은 배경보다 앞에 둔다
        private const float BackgroundDepth = -1f;
        private const float FaceDepth = -1.1f;

        // 배경은 화면을 가득 칠하는 아주 긴 한 획이라 빠르게, 얼굴은 손으로 그리는 속도로 그린다
        private const float BackgroundPenSpeed = 15000f;
        private const float FacePenSpeed = 3000f;
        private const float FaceGapSeconds = 0.12f;

        private const float TextFadeSeconds = 0.5f;
        private const float PressTextDelaySeconds = 1f;

        [SerializeField]
        private TextMeshProUGUI titleText;
        [SerializeField]
        private TextMeshProUGUI resultText;
        [SerializeField]
        private TextMeshProUGUI pressText;

        public Button button;

        private void Awake()
        {
            titleText.alpha = 0;
            resultText.alpha = 0;
            pressText.alpha = 0;
        }

        public void RefreshResultText(ColorPalette.ColorName winPlayer)
        {
            resultText.text = $"{winPlayer.ToString()} 승리";
        }

        public void Show()
        {
            StartCoroutine(ShowRoutine());
        }

        public void Hide()
        {
            throw new System.NotImplementedException();
        }

        private IEnumerator ShowRoutine()
        {
            StrokeMotionPlayer player = FindFirstObjectByType<StrokeMotionPlayer>();
            Transform backgroundRoot = CreateDrawingRoot("ResultBackground", BackgroundDepth);
            Transform faceRoot = CreateDrawingRoot("ResultFace", FaceDepth);
            StrokeMotionObject background = player.Load(BackgroundDrawingPath, backgroundRoot, BackgroundPenSpeed, 0f);
            StrokeMotionObject face = player.Load(FaceDrawingPath, faceRoot, FacePenSpeed, FaceGapSeconds);

            background.Play();
            yield return new WaitForSeconds(background.Duration);

            face.Play();
            yield return new WaitForSeconds(face.Duration);

            resultText.DOFade(1f, TextFadeSeconds);
            Tween titleFade = titleText.DOFade(1f, TextFadeSeconds);
            yield return titleFade.WaitForCompletion();

            yield return new WaitForSeconds(PressTextDelaySeconds);
            pressText.DOFade(1f, TextFadeSeconds);
            button.interactable = true;
        }

        // 그림은 UI가 아니라 Main Camera가 그리는 월드에 둔다. 씬을 떠나면 함께 지워진다
        private Transform CreateDrawingRoot(string rootName, float depth)
        {
            Transform root = new GameObject(rootName).transform;
            root.position = new Vector3(0f, 0f, depth);

            float canvasWorldHeight = CanvasHeight / DrawingCoordinates.PixelsPerUnit;
            float cameraWorldHeight = Camera.main.orthographicSize * 2f;
            root.localScale = Vector3.one * (cameraWorldHeight / canvasWorldHeight);
            return root;
        }
    }
}
