using System;
using System.Collections;
using System.Collections.Generic;
using Game.GameType.Roman.ClientSide;
using Game.GameType.Roman.ServerSide.Card;
using Game.GameType.Roman.ServerSide.CardBase;
using Game.Manager.GameManage;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.GameType.Roman.ServerSide
{
    public class RomanGameMode : GameMode
    {
        public RomanCardContainer cardContainer = new RomanCardContainer();

        public delegate void CardEventHandler(CardType cardType);
        public event CardEventHandler OnCardFlipped;
        public event CardEventHandler OnCardDiscovered;
        public event CardEventHandler OnCardShaken;
        
        public delegate void GameEventHandler();
        public event GameEventHandler OnGameOver;
        
        public ColorPalette.ColorName startPlayer;
        public ColorPalette.ColorName curPlayer;
        public int turnCount = 0;
        public GameStep curStep = GameStep.InitGame;

        private int _shakeLimit = 1;
        private int _shakeCount = 0;
        
        private void Start()
        {
            GameManager.Instance.GameState = new RomanGameState();
        }
        
        protected override IEnumerator GameRoutine()
        {
            yield return new WaitForSeconds(1f);
            yield return new WaitUntil(() => curStep == GameStep.InitGame);
            InitDeviceOwnCard();
            yield return SelectStartPlayer();
            RequestFlipToTail();
            yield return new WaitUntil(() => cardContainer.IsAllHide());
            
            while (curStep != GameStep.GameOver)
            {
                yield return NotifyNewPlayerTurn();
                yield return new WaitUntil(() => curStep == GameStep.SelectCard);
                yield return new WaitUntil(() => curStep == GameStep.FlipOrShake);
                _shakeCount = 0;
                
                yield return new WaitUntil(() => curStep == GameStep.ShowCard);
                yield return WaitForCardCheck();
                yield return new WaitUntil(() => curStep == GameStep.HideCard);
                yield return new WaitUntil(() => cardContainer.IsAllHide());

                turnCount++;
            }
        }

        private void InitDeviceOwnCard()
        {
            List<CardType> cardTypes = cardContainer.GetAvailableCardTypes();
            Utils.ShuffleList(cardTypes);

            for (int i = 0; i < NetworkManager.Instance.connectedDeviceList.Count; i++)
            {
                CardType cardType = cardTypes[i];
                ColorPalette.ColorName deviceColor = NetworkManager.Instance.connectedDeviceList[i];
                
                cardContainer.UseCard(cardType, deviceColor);

                NetworkManager.Instance.ClientRpcCall(deviceColor, typeof(RomanGameScene), "CreateCard", cardType);
            }
        }
        
        private IEnumerator SelectStartPlayer()
        {
            ColorPalette.ColorName startPlayerColor = (ColorPalette.ColorName)Random.Range(0, base.playerCount);

            NetworkManager.Instance.ClientRpcCall(startPlayerColor, typeof(RomanGameScene), "ShowStartPlayerPopup", null);
            yield return new WaitForSecondsRealtime(5f);
        }

        private void RequestFlipToTail()
        {
            NetworkManager.Instance.ClientRpcCall(typeof(RomanGameScene), "ShowRequestFlipToTailPopup", null);
        }

        private IEnumerator NotifyNewPlayerTurn()
        {
            yield return new WaitForSeconds(1f);
            
            curPlayer = (ColorPalette.ColorName)(((int)startPlayer + turnCount) % base.playerCount);
            NetworkManager.Instance.ClientRpcCall(typeof(RomanGameState), "SynchronizeCurPlayer", curPlayer);
            NetworkManager.Instance.ClientRpcCall(typeof(DeviceUtils), "Vibrate", null);
            
            curStep = GameStep.SelectCard;
            $"{curStep.ToString()}".Log();
            NetworkManager.Instance.ClientRpcCall(typeof(RomanGameState), "SynchronizeGameStep", curStep);
        }
        
        private IEnumerator WaitForCardCheck()
        {
            yield return new WaitForSecondsRealtime(3f);
            curStep = GameStep.HideCard;
            $"{curStep.ToString()}".Log();
            NetworkManager.Instance.ClientRpcCall(typeof(RomanGameState), "SynchronizeGameStep", curStep);
        }
        
        public void FlipCard(CardType cardType, Define.DisplayedFace face)
        {
            $"Flip {cardType} to {face}".Log();
            cardContainer.SetCardFace(cardType, face);
        
            if (curStep == GameStep.SelectCard && face == Define.DisplayedFace.Stand)
            {
                ColorPalette.ColorName targetDeviceColor = cardContainer.GetCard(cardType).deviceColor;
                NetworkManager.Instance.ClientRpcCall(targetDeviceColor, typeof(RomanGameScene), "ShowCard", null);
                
                curStep = GameStep.FlipOrShake;
                $"{curStep.ToString()}".Log();
                NetworkManager.Instance.ClientRpcCall(targetDeviceColor, typeof(RomanGameState), "SynchronizeGameStep", curStep);
                return;
            }
        
            if (curStep == GameStep.FlipOrShake && face == Define.DisplayedFace.Head)
            {
                var flippedCard = cardContainer.GetCard(cardType);
                if(flippedCard is IFlipAbility card) card.FlipAbility();
                OnCardFlipped?.Invoke(cardType);

                curStep = GameStep.ShowCard;
                $"{curStep.ToString()}".Log();
                NetworkManager.Instance.ClientRpcCall(typeof(RomanGameState), "SynchronizeGameStep", curStep);
                return;
            }
            
            if (curStep == GameStep.HideCard && face == Define.DisplayedFace.Tail)
            {
                curStep = GameStep.SelectCard;
                $"{curStep.ToString()}".Log();
                NetworkManager.Instance.ClientRpcCall(typeof(RomanGameState), "SynchronizeGameStep", curStep);
                return;
            }
        }

        public void ShakeCard(CardType cardType)
        {
            $"Shake {cardType}".Log();
            if (curStep != GameStep.FlipOrShake) return;
            if (_shakeCount >= _shakeLimit) return;

            _shakeCount++;
            var shakenCard = cardContainer.GetCard(cardType);
            
            if(shakenCard is IShakeAbility card) card.ShakeAbility();
            OnCardShaken?.Invoke(cardType);

            // 종이비행기는 덱 대신 다른 디바이스의 카드와 자리를 바꾼다
            if (shakenCard is PaperPlane && TrySwapWithOtherDevice(shakenCard)) return;
            
            var newCard = cardContainer.ReplaceCard(cardType);

            NetworkManager.Instance.ClientRpcCall(newCard.deviceColor, typeof(RomanGameScene), "ReplaceCard", newCard.cardType);
        }
        
        // 흔든 카드와 필드의 무작위 다른 디바이스 카드를 맞바꾼다. 맞바꿀 카드가 없으면 false
        private bool TrySwapWithOtherDevice(RomanCard shakenCard)
        {
            List<RomanCard> otherCards = cardContainer.GetCards<RomanCard>();
            otherCards.Remove(shakenCard);
            if (otherCards.Count == 0) return false;

            RomanCard targetCard = otherCards[Random.Range(0, otherCards.Count)];
            ColorPalette.ColorName shakerColor = shakenCard.deviceColor;
            ColorPalette.ColorName targetColor = targetCard.deviceColor;
            cardContainer.SwapCards(shakenCard.cardType, targetCard.cardType);
            $"PaperPlane {shakerColor} <-> {targetColor} ({targetCard.cardType})".Log();

            // 흔든 사람은 날아온 카드를 보고, 엎어져 있는 상대 디바이스는 화면 연출 없이 카드만 바뀐다
            NetworkManager.Instance.ClientRpcCall(shakerColor, typeof(RomanGameScene), "ReplaceCard", targetCard.cardType);
            NetworkManager.Instance.ClientRpcCall(targetColor, typeof(RomanGameScene), "SwapCardSilently", shakenCard.cardType);
            return true;
        }

        public void DiscoverCard(CardType cardType)
        {
            $"Discover {cardType}".Log();
            var discoveredCard = cardContainer.GetCard(cardType);
            
            if(discoveredCard is IDiscoveredAbility card) card.DiscoveredAbility();
            OnCardDiscovered?.Invoke(cardType);
        }

        public void Victory()
        {
            GameOver();
            OnGameOver?.Invoke();
            
            NetworkManager.Instance.ClientRpcCall(typeof(RomanGameScene), "ShowResultPopup", null);
        }
        
        public void Victory(ColorPalette.ColorName playerColor)
        {
            GameOver();
            OnGameOver?.Invoke();
            
            NetworkManager.Instance.ClientRpcCall(typeof(RomanGameScene), "ShowResultPopup", null); 
        }
    }
}