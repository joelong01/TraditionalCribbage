using Cards;
using CardView;
using Cribbage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CribbagePlayers
{
    class InteractivePlayer : Player
    {
        CardGrid _discardedGrid = null;
        TraditionalBoard _board = null;
        public IGameView GameView { get; set; } = null;

        public InteractivePlayer(CardGrid grid, TraditionalBoard board)
        {
            _discardedGrid = grid;
            _board = board;
            
        }

        public override async Task<Card> GetCountCard(List<Card> playedCards, List<Card> uncountedCards, int currentCount)
        {
            List<CardCtrl> cardList = await WaitForCardsFromUser(1);
            Debug.Assert(cardList.Count == 1);
            return cardList[0].Card;

        }

        public async Task<CardCtrl> GetCountCard()
        {
            List<CardCtrl> cardList = await WaitForCardsFromUser(1);
            Debug.Assert(cardList.Count == 1);
            return cardList[0];
        }

        private async Task<List<CardCtrl>> WaitForCardsFromUser(int count)
        {

            TaskCompletionSource<List<CardCtrl>> tcs = null;
            tcs = new TaskCompletionSource<List<CardCtrl>>();
            List<CardCtrl> totalCards = new List<CardCtrl>();

            Task<bool> OnCardsDropped(List<CardCtrl> droppedCards, int currentMax)
            {
                totalCards.AddRange(droppedCards);
                GameView.PlayerCardsAddedToCrib(totalCards); // reduces the max # of cards that the player can select
                if (totalCards.Count == count)
                {
                    tcs.TrySetResult(totalCards);
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }


            try
            {
                _discardedGrid.OnCardDropped += OnCardsDropped;
                List<CardCtrl> retList = await tcs.Task;
                return retList;
            }
            finally
            {
                _discardedGrid.OnCardDropped -= OnCardsDropped;
            }


        }

        public async Task<List<CardCtrl>> SelectCribUiCards(List<Card> hand, bool myCrib)
        {
            List<CardCtrl> cardCtrlList = await WaitForCardsFromUser(2);            

            return cardCtrlList;
        }

        public override async Task<List<Card>> SelectCribCards(List<Card> hand, bool myCrib)
        {
            List<CardCtrl> cardCtrlList = await WaitForCardsFromUser(2);
            Debug.Assert(cardCtrlList.Count == 2);

            List<Card> cardList = new List<Card>()
            {
                cardCtrlList[0].Card,
                cardCtrlList[1].Card
            };

            return cardList;
        }

        public async Task<int> GetScoreFromUser(int score, bool autoEnter)
        {
            return await _board.ShowAndWaitForContinue(score, autoEnter);
        }

        public override void Init(string parameters)
        {

        }
    }
}
