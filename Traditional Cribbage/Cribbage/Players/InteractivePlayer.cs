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
        CardGrid _cribGrid = null;
        public IGameView GameView { get; set; } = null;

        public InteractivePlayer(CardGrid grid, CardGrid cribGrid, TraditionalBoard board)
        {
            _discardedGrid = grid;
            _board = board;
            _cribGrid = cribGrid;

        }

        public override async Task<Card> GetCountCard(List<Card> playedCards, List<Card> uncountedCards, int currentCount)
        {
            List<CardCtrl> cardList = await WaitForCardsFromUser(_discardedGrid, 1, false);
            Debug.Assert(cardList.Count == 1);
            return cardList[0].Card;

        }

        private async Task<List<CardCtrl>> WaitForCardsFromUser(CardGrid dropTarget, int count, bool flipCards)
        {

            TaskCompletionSource<List<CardCtrl>> tcs = null;
            tcs = new TaskCompletionSource<List<CardCtrl>>();
            List<CardCtrl> totalCards = new List<CardCtrl>();

            async Task<bool> OnBeginCardsDropped(List<CardCtrl> droppedCards, int currentMax)
            {

                List<Task> taskList = new List<Task>();
                foreach (var card in droppedCards)
                {
                    if (card.Orientation == CardOrientation.FaceUp) // if you drop two cards, these are already face down
                    {
                        card.Selected = false;
                        taskList.Add(card.SetOrientationTask(CardOrientation.FaceDown, 250, 0));
                    }
                }
                if (taskList.Count > 0)
                {
                    await Task.WhenAll(taskList);
                }
                return true;
            }

            Task<bool> OnEndCardsDropped(List<CardCtrl> droppedCards, int currentMax)
            {
                totalCards.AddRange(droppedCards);
                if (count > 1) GameView.PlayerCardDroppedToCrib(totalCards); // reduces the max # of cards that the player can select
                if (totalCards.Count == count)
                {
                    tcs.TrySetResult(totalCards);
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }



            try
            {
                if (flipCards)
                {
                    dropTarget.OnBeginCardDropped += OnBeginCardsDropped;
                }
                dropTarget.OnEndCardDropped += OnEndCardsDropped;                
                List<CardCtrl> retList = await tcs.Task;
                return retList;
            }
            finally
            {
                dropTarget.OnEndCardDropped -= OnEndCardsDropped;
                if (flipCards)
                {
                    dropTarget.OnBeginCardDropped -= OnBeginCardsDropped;
                }
            }


        }

        public async Task<List<CardCtrl>> SelectCribUiCards(List<Card> hand, bool myCrib)
        {
            List<CardCtrl> cardCtrlList = await WaitForCardsFromUser(_cribGrid, 2, true);

            return cardCtrlList;
        }

        public override async Task<List<Card>> SelectCribCards(List<Card> hand, bool myCrib)
        {
            List<CardCtrl> cardCtrlList = await WaitForCardsFromUser(_cribGrid, 2, true);
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
