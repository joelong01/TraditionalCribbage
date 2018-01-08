using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Cards;
using CardView;
using Cribbage;
using Cribbage.Players;

namespace CribbagePlayers
{
    internal class InteractivePlayer : Player
    {
        private readonly CardGrid _cribGrid;
        private readonly CardGrid _discardedGrid;

        public InteractivePlayer(CardGrid grid, CardGrid cribGrid, int score)
        {
            _discardedGrid = grid;
            _cribGrid = cribGrid;
            Score = score;
        }

        public IGameView GameView { get; set; } = null;

        public override async Task<Card> GetCountCard(List<Card> playedCards, List<Card> uncountedCards,
            int currentCount)
        {
            var cardList = await WaitForCardsFromUser(_discardedGrid, 1, false);
            Debug.Assert(cardList.Count == 1);
            return cardList[0].Card;
        }

        private async Task<List<CardCtrl>> WaitForCardsFromUser(CardGrid dropTarget, int count, bool flipCards)
        {
            TaskCompletionSource<List<CardCtrl>> tcs = null;
            tcs = new TaskCompletionSource<List<CardCtrl>>();
            var totalCards = new List<CardCtrl>();

            async Task<bool> OnBeginCardsDropped(List<CardCtrl> droppedCards, int currentMax)
            {
                var taskList = new List<Task>();
                foreach (var card in droppedCards)
                {
                    if (card.Orientation == CardOrientation.FaceUp
                    ) // if you drop two cards, these are already face down
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
                if (count > 1)
                {
                    GameView.PlayerCardDroppedToCrib(
                        totalCards); // reduces the max # of cards that the player can select                    
                }

                if (droppedCards.Count == 1 && totalCards.Count == 1)
                {
                    GameView.SetInstructions("Drop one more card to the crib");
                }

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
                var retList = await tcs.Task;
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
            var cardCtrlList = await WaitForCardsFromUser(_cribGrid, 2, true);

            return cardCtrlList;
        }

        public override async Task<List<Card>> SelectCribCards(List<Card> hand, bool myCrib)
        {
            var cardCtrlList = await WaitForCardsFromUser(_cribGrid, 2, true);
            Debug.Assert(cardCtrlList.Count == 2);

            var cardList = new List<Card>
            {
                cardCtrlList[0].Card,
                cardCtrlList[1].Card
            };

            return cardList;
        }

        public async Task<int> GetScoreFromPlayer(int score, WrongScoreOption option)
        {
            return await GameView.HighlightScoreAndWaitForContinue(PlayerType.Player, score, option);
        }

        public override void Init(string parameters)
        {
        }
    }
}