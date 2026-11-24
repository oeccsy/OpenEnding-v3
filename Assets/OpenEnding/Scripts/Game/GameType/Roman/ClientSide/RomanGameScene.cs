using System.Collections;
using System.Collections.Generic;
using Game.GameType.Roman.ClientSide.CardBase;
using Game.GameType.Roman.ClientSide.UI;
using Game.Manager.GameManage;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utility;
using Utility.Hierarchy;

namespace Game.GameType.Roman.ClientSide
{
    public class RomanGameScene : GameScene
    {
        public RomanCard card;
        public RomanCard discoveryCard;

        // 결과 화면은 승리를 만든 카드의 애니메이션이 끝난 뒤에 띄운다
        private const float ResultCheckDelaySeconds = 0.5f;
        private const float ResultDelaySeconds = 1f;

        private bool isCardShowing;
        
        protected override void Awake()
        {
            base.Awake();
        }

        public void ShowStartPlayerPopup()
        {
            IEnumerator ShowRoutine()
            {
                StartPlayerPopup popup = UIManager.Instance.ShowPopup("Prefabs/Roman/StartPlayerPopup", 9).GetComponentInChildren<StartPlayerPopup>();
                yield return new WaitForSeconds(4f);
                popup.Hide();    
            }

            StartCoroutine(ShowRoutine());
        }

        public void ShowRequestFlipToTailPopup()
        {
            RequestFlipToTailPopup popup = UIManager.Instance.ShowPopup("Prefabs/Roman/RequestFlipToTailPopup", 9).GetComponentInChildren<RequestFlipToTailPopup>();
            
            (GameManager.Instance.PlayerController as RomanPlayerController).flip.OnNextTail += popup.Hide;
        }

        public void CreateCard(CardType cardType)
        {
            GameObject prefab = Resources.Load<GameObject>($"Prefabs/Roman/{cardType.ToString()}");
            card = Instantiate(prefab, GameObjectRoot.Transform).GetComponent<RomanCard>();
        }
        
        public void ReplaceCard(CardType cardType)
        {
            $"Replace to {cardType}".Log();
            if (card == null) return;

            IEnumerator ReplaceRoutine()
            {
                yield return card.Hide();
                yield return card.cardInfoUI.Hide();
                Destroy(card.gameObject);
                
                yield return new WaitForSeconds(0.5f);
                
                CreateCard(cardType);
                yield return card.Show();
            }

            StartCoroutine(ReplaceRoutine());
        }

        // 엎어져 있는 디바이스의 카드를 연출 없이 바꾼다. 다음에 세웠을 때 바뀐 카드가 보인다
        public void SwapCardSilently(CardType cardType)
        {
            $"Swap silently to {cardType}".Log();
            if (card != null) Destroy(card.gameObject);

            CreateCard(cardType);
        }

        public void DiscoverCard(CardType cardType)
        {
            $"Discover {cardType}".Log();
            if (card == null) return;
            
            IEnumerator DiscoverRoutine()
            {
                isCardShowing = true;
                yield return card.Hide();
                yield return card.cardInfoUI.Hide();
                
                yield return new WaitForSeconds(0.5f);
                
                var prefab = Resources.Load<GameObject>($"Prefabs/Roman/{cardType.ToString()}");
                discoveryCard = Instantiate(prefab, GameObjectRoot.Transform).GetComponent<RomanCard>();
                yield return discoveryCard.Show();
                isCardShowing = false;
                
                RomanGameState gameState = GameManager.Instance.GameState as RomanGameState;
                yield return new WaitUntil(() => gameState?.curStep == GameStep.HideCard);
                
                // 발견한 카드는 이번 턴에만 보여 주므로 숨긴 뒤 지운다. 남겨 두면 다음 턴의 카드와 겹친다
                HideOnTail(discoveryCard, true);
            }
            
            StartCoroutine(DiscoverRoutine());
        }

        public void ShowCard()
        {
            "ShowCard".Log();
            IEnumerator ShowRoutine()
            {
                isCardShowing = true;
                yield return card.Show();
                isCardShowing = false;
                
                RomanGameState gameState = GameManager.Instance.GameState as RomanGameState;
                yield return new WaitUntil(() => gameState?.curStep == GameStep.HideCard);
                
                HideOnTail(card, false);
            }

            StartCoroutine(ShowRoutine());
        }

        // 이미 뒷면이면 바로 숨기고, 아니면 다음에 뒷면으로 뒤집을 때 숨긴다.
        // 확인 시간 안에 먼저 엎으면 뒷면으로 바뀌는 순간이 다시 오지 않아 카드가 계속 남기 때문이다.
        private void HideOnTail(RomanCard target, bool destroyAfterHide)
        {
            Flip flip = GameManager.Instance.PlayerController.flip;
            if (flip.CurFace == Define.DisplayedFace.Tail)
            {
                StartCoroutine(HideRoutine(target, destroyAfterHide));
                return;
            }

            flip.OnNextTail += () => StartCoroutine(HideRoutine(target, destroyAfterHide));
        }

        private IEnumerator HideRoutine(RomanCard target, bool destroyAfterHide)
        {
            if (target == null) yield break;

            Coroutine infoHide = StartCoroutine(target.cardInfoUI.Hide());
            yield return target.Hide();
            yield return infoHide;

            if (destroyAfterHide) Destroy(target.gameObject);
        }

        public void ShowResultPopup()
        {
            IEnumerator ShowRoutine()
            {
                // 카드를 보여 달라는 메시지가 결과보다 늦게 올 수 있어 잠시 기다린 뒤 확인한다
                yield return new WaitForSeconds(ResultCheckDelaySeconds);
                yield return new WaitUntil(() => !isCardShowing);
                yield return new WaitForSeconds(ResultDelaySeconds);

                var instance = UIManager.Instance.ShowPopup("Prefabs/Roman/ResultPopup", 9);
                var resultPopup = instance.GetComponent<ResultPopup>();
                var gameState = GameManager.Instance.GameState as RomanGameState;
                
                resultPopup.RefreshResultText(gameState.curPlayer);
                resultPopup.Show();
                
                resultPopup.button.onClick.AddListener(()=> SceneManager.LoadScene(Define.SceneType.ConnectScene.ToString()));
                resultPopup.button.onClick.AddListener(()=> resultPopup.button.onClick.RemoveAllListeners());
            }

            StartCoroutine(ShowRoutine());
        }
    }
}