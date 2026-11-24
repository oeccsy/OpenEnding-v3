using Game.GameType.Roman.ClientSide.CardBase;

namespace Game.GameType.Roman.ClientSide.Card
{
    /// <summary>
    /// 소나기 카드의 연출. 구름에서 비가 내리고 새싹이 자라는 그림을 그린다.
    /// </summary>
    public class Shower : DrawingRomanCard
    {
        protected override string DrawingPath => "Drawings/card_shower";
        protected override string CardInfoPath => "ScriptableObject/Roman/CardInfoSO_Shower";

        protected override void Awake()
        {
            base.Awake();
            cardType = CardType.Shower;
        }
    }
}
