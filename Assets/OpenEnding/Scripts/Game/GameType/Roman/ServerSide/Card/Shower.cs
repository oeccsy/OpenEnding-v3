using Game.GameType.Roman.ServerSide.CardBase;
using Game.Manager.GameManage;

namespace Game.GameType.Roman.ServerSide.Card
{
    /// <summary>
    /// 소나기 카드의 규칙. 뒤집으면 필드의 모든 카드의 성장을 한 단계 되돌린다.
    /// 작품과 우상은 성장 단계를, 새싹은 뒤집힌 횟수를 하나 줄인다. 0 아래로는 내려가지 않는다.
    /// </summary>
    public class Shower : RomanCard, IFlipAbility
    {
        public Shower()
        {
            cardType = CardType.Shower;
        }

        public void FlipAbility()
        {
            RomanGameMode gameMode = GameManager.Instance.GameMode as RomanGameMode;
            if (gameMode == null) return;

            foreach (IGrowable growableCard in gameMode.cardContainer.GetCards<IGrowable>())
            {
                growableCard.Regress();
            }

            foreach (Sprout sprout in gameMode.cardContainer.GetCards<Sprout>())
            {
                sprout.Regress();
            }
        }

        public override void OnEnterField() {}
        public override void OnExitField() {}
    }
}
