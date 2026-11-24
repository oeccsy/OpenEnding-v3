using Game.GameType.Roman.ClientSide.CardBase;

namespace Game.GameType.Roman.ClientSide.Card
{
    /// <summary>
    /// 종이비행기 카드의 연출. 구름 사이로 날아가는 종이비행기를 그린다.
    /// </summary>
    public class PaperPlane : DrawingRomanCard
    {
        protected override string DrawingPath => "Drawings/card_paperplane";
        protected override string CardInfoPath => "ScriptableObject/Roman/CardInfoSO_PaperPlane";

        // 색칠 획이 길어서 기본 속도로는 5초가 넘는다. 빠르게 그려 4초 안에 맞춘다
        protected override float PenSpeed => 2400f;

        protected override void Awake()
        {
            base.Awake();
            cardType = CardType.PaperPlane;
        }
    }
}
