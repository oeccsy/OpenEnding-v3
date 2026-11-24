using Game.GameType.Roman.ServerSide.CardBase;
using Game.Manager.GameManage;

namespace Game.GameType.Roman.ServerSide.Card
{
    /// <summary>
    /// 종이비행기 카드의 규칙.
    /// 흔들면 덱 대신 필드의 무작위 다른 디바이스 카드와 자리를 바꾼다(맞바꾸기는 RomanGameMode.ShakeCard에서 한다).
    /// 다른 카드 없이 3번 연속 뒤집히면 3번째로 뒤집은 사람이 승리한다.
    /// </summary>
    public class PaperPlane : RomanCard, IFlipAbility
    {
        private const int VictoryFlipCount = 3;

        private int _consecutiveFlipCount = 0;

        public PaperPlane()
        {
            cardType = CardType.PaperPlane;
        }

        public void FlipAbility()
        {
            _consecutiveFlipCount++;
            if (_consecutiveFlipCount < VictoryFlipCount) return;

            RomanGameMode gameMode = GameManager.Instance.GameMode as RomanGameMode;
            gameMode?.Victory();
        }

        public override void OnEnterField()
        {
            _consecutiveFlipCount = 0;

            RomanGameMode gameMode = GameManager.Instance.GameMode as RomanGameMode;
            gameMode.OnCardFlipped += ResetOnOtherFlip;
        }

        public override void OnExitField()
        {
            RomanGameMode gameMode = GameManager.Instance.GameMode as RomanGameMode;
            gameMode.OnCardFlipped -= ResetOnOtherFlip;
        }

        // 다른 카드가 뒤집히면 연속이 끊긴다
        private void ResetOnOtherFlip(CardType cardType)
        {
            if (cardType == CardType.PaperPlane) return;

            _consecutiveFlipCount = 0;
        }
    }
}
