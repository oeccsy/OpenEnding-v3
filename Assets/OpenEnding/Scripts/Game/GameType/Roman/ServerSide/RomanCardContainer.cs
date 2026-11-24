using System;
using System.Collections.Generic;
using System.Linq;
using Game.GameType.Roman.ServerSide.Card;
using Game.GameType.Roman.ServerSide.CardBase;
using Random = UnityEngine.Random;

namespace Game.GameType.Roman.ServerSide
{
    [Serializable]
    public class RomanCardContainer
    {
        private Dictionary<CardType, RomanCard> availableCards = new Dictionary<CardType, RomanCard>();
        private Dictionary<CardType, RomanCard> usedCards = new Dictionary<CardType, RomanCard>();
        
        public RomanCardContainer()
        {
            // 테스트용으로 4장만 쓴다. 망원경, 작품, 새싹은 잠시 뺐다
            availableCards.Add(CardType.Star, new Star());
            availableCards.Add(CardType.RoleModel, new RoleModel());
            availableCards.Add(CardType.Shower, new Shower());
            availableCards.Add(CardType.PaperPlane, new PaperPlane());
        }

        public void UseCard(CardType cardType, ColorPalette.ColorName deviceColor)
        {
            if (!availableCards.ContainsKey(cardType)) return;
            
            RomanCard cardData = availableCards[cardType];
            availableCards.Remove(cardType);

            cardData.deviceColor = deviceColor;
            usedCards.Add(cardType, cardData);
            
            cardData.OnEnterField();
        }

        // 아직 필드에 나오지 않은 카드 종류. 처음 나눠 줄 카드를 고를 때 쓴다
        public List<CardType> GetAvailableCardTypes()
        {
            return availableCards.Keys.ToList();
        }

        public RomanCard ReplaceCard(CardType cardType)
        {
            if (!usedCards.ContainsKey(cardType)) return null;

            List<RomanCard> availableCardList = availableCards.Values.ToList();
            RomanCard newCard = availableCardList[Random.Range(0, availableCardList.Count)];
            availableCards.Remove(newCard.cardType);
            
            RomanCard prevCard = usedCards[cardType];
            usedCards.Remove(cardType);
            
            newCard.deviceColor = prevCard.deviceColor;
            prevCard.deviceColor = ColorPalette.ColorName.DeviceDefault;
            
            usedCards.Add(newCard.cardType, newCard);
            availableCards.Add(cardType, prevCard);
            
            prevCard.OnExitField();
            newCard.OnEnterField();

            return newCard;
        }

        // 필드의 두 카드가 디바이스를 맞바꾼다. 보이는 면은 디바이스의 상태이므로 디바이스를 따라간다
        public void SwapCards(CardType firstType, CardType secondType)
        {
            if (!usedCards.ContainsKey(firstType)) return;
            if (!usedCards.ContainsKey(secondType)) return;

            RomanCard firstCard = usedCards[firstType];
            RomanCard secondCard = usedCards[secondType];

            ColorPalette.ColorName firstColor = firstCard.deviceColor;
            firstCard.deviceColor = secondCard.deviceColor;
            secondCard.deviceColor = firstColor;

            Define.DisplayedFace firstFace = firstCard.displayedFace;
            firstCard.displayedFace = secondCard.displayedFace;
            secondCard.displayedFace = firstFace;
        }

        public RomanCard GetCard(CardType cardType)
        {
            if (!usedCards.ContainsKey(cardType)) return null;
            
            return usedCards[cardType];
        }
        
        public List<T> GetCards<T>()
        {
            List<T> cards = new List<T>();

            foreach (var cardData in usedCards)
            {
                if (cardData.Value is T card)
                {
                    cards.Add(card);   
                }
            }

            return cards;
        }

        public void SetCardFace(CardType cardType, Define.DisplayedFace face)
        {
            if (!usedCards.ContainsKey(cardType)) return;

            var cardData = usedCards[cardType];
            cardData.displayedFace = face;
        }

        public bool IsAllHide()
        {
            int cardCount = usedCards.Count;
            int hideCount = usedCards.Count(cardData => cardData.Value.displayedFace == Define.DisplayedFace.Tail);
                
            return hideCount == cardCount;
        }
    }
}