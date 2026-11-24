using Game.GameType.Roman.ClientSide;
using Game.GameType.Roman.ServerSide.CardBase;
using Game.Manager.GameManage;


namespace Game.GameType.Roman.ServerSide.Card
{
    public class Sprout : RomanCard, IFlipAbility
    {
        public Sprout()
        {
            cardType = CardType.Sprout;
        }

        private ColorPalette.ColorName _firstPlayer = ColorPalette.ColorName.DeviceDefault;
        private int _flipCount = 0;
        
        public void FlipAbility()
        {
            if (_firstPlayer == ColorPalette.ColorName.DeviceDefault)
            {
                var gameState = GameManager.Instance.GameState as RomanGameState;
                _firstPlayer = gameState.curPlayer;
            }
            
            _flipCount++;

            if (_flipCount >= 10)
            {
                var gameMode = GameManager.Instance.GameMode as RomanGameMode;
                gameMode.Victory(_firstPlayer);
            }
        }

        // 소나기가 부른다. 뒤집힌 횟수를 하나 줄이고, 0 아래로는 내려가지 않는다. 처음 뒤집은 사람은 그대로 둔다
        public void Regress()
        {
            if (_flipCount > 0) _flipCount--;
        }

        public override void OnEnterField() {}
        public override void OnExitField() {}
    }
}