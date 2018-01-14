using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using CardView;

namespace Cribbage
{
    public sealed partial class MainPage : Page
    {
        public const double FlipAnimationDuration = 250;

        public const double
            MOVE_CARDS_ANIMATION_DURATION = 250; // how long for the cards to get to the player and the computer

        private bool _firstDeal = true;

        /// <summary>
        ///     Deal does the following
        ///     1. moves cards from the deck to the computer and player
        ///     2. flips the players cards faceup
        ///     3. moves 2 cards from the computer to the discard grid
        ///     Assume we start with the cards in the deck
        ///     One problem we have is that we can't animate (X,Y) in one TimeLine -- so we have to compose them.
        /// </summary>
        public async Task AnimateDeal(List<CardCtrl> playerCards, List<CardCtrl> computerCards,
            List<CardCtrl> discardedComputerCards, PlayerType dealer)
        {
            playerCards.Sort(CardCtrl.CompareCardsByRank); // sort from lowest to highest, ignoring suit

            var taskList = new List<Task>();

            //
            //  assume dealer is player and then set it if it is not
            var dealersCards = playerCards;
            var nonDealerCards = computerCards;


            if (dealer == PlayerType.Computer)
            {
                dealersCards = computerCards;
                nonDealerCards = playerCards;

            }


            Task t = null;
            double beginTime = 0;

            var zIndex = 53; // this number is important -- the layout engine for cards sets Zindex too...
            for (var z = 0; z < nonDealerCards.Count; z++)
            {
                nonDealerCards[z].ZIndex = zIndex;
                dealersCards[z].ZIndex = zIndex - 1;
                zIndex -= 2;
            }


            _cgDeck.Cards.Sort((c1, c2) => Canvas.GetZIndex(c2) - Canvas.GetZIndex(c1));

            double targetIndex = 0; // just being a bit silly here -- by incrementing by 0.5 and casting to an int, we can increate the target index by 1 after 2 iterations through the loop
            foreach (var card in _cgDeck.Cards)
            {
                switch (card.Owner)
                {
                    case Owner.Player:
                        t = CardGrid.AnimateMoveOneCard(_cgDeck, _cgPlayer, card, (int)targetIndex, true,
                            MOVE_CARDS_ANIMATION_DURATION, beginTime);
                        targetIndex += 0.5;
                        card.Location = CardView.Location.Player;
                        break;
                    case Owner.Computer:
                        t = CardGrid.AnimateMoveOneCard(_cgDeck, _cgComputer, card, (int)targetIndex, true,
                            MOVE_CARDS_ANIMATION_DURATION, beginTime);
                        targetIndex += 0.5;
                        card.Location = CardView.Location.Computer;
                        break;
                    case Owner.Shared:
                        continue;
                    default:
                        throw new InvalidOperationException("Card owner not set");
                }

                taskList.Add(t);
                beginTime += MOVE_CARDS_ANIMATION_DURATION;
            }


            //
            //  Now flip the players cards
            taskList.AddRange(CardGrid.SetCardsToOrientationTask(playerCards, CardOrientation.FaceUp,
                FlipAnimationDuration, beginTime));
            await Task.WhenAll(taskList);

            taskList.Clear();
            //
            //  move computer cards to the crib.  do it slow the first time so that the user can learn where to place the cards
            //
            // don't Transfer the cards because they all still belong to the deck -- we'll transfer below
            await AnimateMoveComputerCardstoCrib(discardedComputerCards, false);
            //
            //  Now put the cards where they belong - they are all currently owned by the deck...
            CardGrid.TransferCards(_cgDeck, _cgComputer, computerCards);
            CardGrid.TransferCards(_cgDeck, _cgPlayer, playerCards);
            CardGrid.TransferCards(_cgComputer, _cgCrib, discardedComputerCards);
        }

        public async Task AnimateMoveComputerCardstoCrib(List<CardCtrl> computerCribCards, bool moveCards = true)
        {
            double beginTime = 0;
            var taskList = new List<Task>();
            var animationDuration = FlipAnimationDuration;
            if (_firstDeal)
            {
                animationDuration *= 4;
                _firstDeal = false;
            }

            var tList = CardGrid.MoveListOfCards(_cgComputer, _cgCrib, computerCribCards, animationDuration,
                beginTime);
            if (tList != null)
            {
                taskList.AddRange(tList);
            }

            await Task.WhenAll(taskList);
            if (moveCards)
            {
                CardGrid.TransferCards(_cgComputer, _cgCrib, computerCribCards);
            }
        }

        /// <summary>
        ///     This is after the player has dropped 2 cards. To the Crib Do 2 things
        ///     1. Flip the cards to face down in the crib grid
        ///     2. flip the deck card to faceup
        /// </summary>
        /// <returns></returns>
        public async Task AnimateMoveToCribAndFlipDeckCard()
        {
            double beginTime = 0;
            var taskList = new List<Task>();

            var cards = _cgCrib.Cards;



            //
            //  when that is done flip the shared card
            beginTime = 0;
            taskList.AddRange(CardGrid.SetCardsToOrientationTask(_cgDeck.Cards, CardOrientation.FaceUp,
                FlipAnimationDuration, beginTime));

            //
            // run the animation
            await Task.WhenAll(taskList);
        }

        private void SetZIndex(List<CardCtrl> cards, int zIndex)
        {
            foreach (var card in cards)
            {
                Canvas.SetZIndex(card, zIndex);
            }
        }


        private async Task AnimateSendCardsBackToOwner()
        {
            var tList = new List<Task>();
            var targetIndexForComputer = 0;
            var targetIndexForPlayer = 0;
            var playerCards = new List<CardCtrl>();
            var computerCards = new List<CardCtrl>();
            foreach (var card in _cgDiscarded.Cards)
            {
                card.Opacity = 1.0;
                Task task;
                if (card.Owner == Owner.Computer)
                {
                    task = CardGrid.AnimateMoveOneCard(_cgDiscarded, _cgComputer, card, targetIndexForComputer, false,
                        MOVE_CARDS_ANIMATION_DURATION, targetIndexForComputer * MOVE_CARDS_ANIMATION_DURATION);
                    computerCards.Add(card);
                    targetIndexForComputer++;
                }
                else
                {
                    task = CardGrid.AnimateMoveOneCard(_cgDiscarded, _cgPlayer, card, targetIndexForPlayer, false,
                        MOVE_CARDS_ANIMATION_DURATION, MOVE_CARDS_ANIMATION_DURATION * targetIndexForPlayer);
                    targetIndexForPlayer++;
                    playerCards.Add(card);
                }

                tList.Add(task);
            }

            playerCards.Sort(CardCtrl.CompareCardsByRank); // sort from lowest to highest, ignoring suit

            await Task.WhenAll(tList);

            CardGrid.TransferCards(_cgDiscarded, _cgPlayer, playerCards);
            CardGrid.TransferCards(_cgDiscarded, _cgComputer, computerCards);
        }

        private void SetGridZIndex(int computer = 0, int player = 0, int crib = 0, int discard = 0, int deck = 0)
        {
            Canvas.SetZIndex(_cgComputer, computer);
            Canvas.SetZIndex(_cgPlayer, player);
            Canvas.SetZIndex(_cgDeck, deck);
            Canvas.SetZIndex(_cgCrib, crib);
            Canvas.SetZIndex(_cgDiscarded, discard);
        }

        public async Task AnimateMoveCribCardsBackToOwner(PlayerType dealer)
        {
            //
            //  when we open a game in the state GameState.ScorePlayerCrib, we will have already
            //  moved the crib back to the player -- so there are no cards in the crib.
            //  check for this and abort if there are no cards in the crib

            if (_cgCrib.Cards.Count == 0) return;

            var taskList = new List<Task>();
            taskList.AddRange(AnimateFlipAllCards(_cgComputer.Cards, CardOrientation.FaceDown, true));
            taskList.AddRange(AnimateFlipAllCards(_cgPlayer.Cards, CardOrientation.FaceDown, true));
            await Task.WhenAll(taskList);
            taskList.Clear();
            var newTaskList = CardGrid.AnimateMoveAllCards(_cgComputer, _cgDeck, MOVE_CARDS_ANIMATION_DURATION, 0,
                AnimateMoveOptions.StackAtZero, false);
            if (newTaskList != null)
            {
                taskList.AddRange(newTaskList);
            }
            newTaskList = CardGrid.AnimateMoveAllCards(_cgPlayer, _cgDeck, MOVE_CARDS_ANIMATION_DURATION, 0, AnimateMoveOptions.StackAtZero, false);
            if (newTaskList != null)
            {
                taskList.AddRange(newTaskList);
            }

            if (taskList.Count > 0)
            {
                await Task.WhenAll(taskList);
                taskList.Clear();
            }

            var cribOwnerGrid = dealer == PlayerType.Computer ? _cgComputer : _cgPlayer;
            taskList = CardGrid.AnimateMoveAllCards(_cgCrib, cribOwnerGrid, MOVE_CARDS_ANIMATION_DURATION, 0,
                AnimateMoveOptions.MoveSequentlyEndingAtZero, false);
            if (taskList != null)
            {
                taskList.AddRange(newTaskList);
                await Task.WhenAll(taskList);
                taskList.Clear();
            }

            taskList = AnimateFlipAllCards(_cgCrib.Cards, CardOrientation.FaceUp,
                true);
            if (taskList != null)
            {
                await Task.WhenAll(taskList);
                taskList.Clear();
            }

            CardGrid.TransferAllCards(_cgComputer, _cgDeck, true);
            CardGrid.TransferAllCards(_cgPlayer, _cgDeck, true);
            CardGrid.TransferAllCards(_cgCrib, cribOwnerGrid, false);
        }

        private async Task AnimationEndHand(PlayerType dealer)
        {
            var taskList = new List<Task>();

            var cribOwnerGrid = dealer == PlayerType.Computer ? _cgComputer : _cgPlayer;
            taskList.AddRange(AnimateFlipAllCards(cribOwnerGrid.Cards, CardOrientation.FaceDown, true));
            taskList.AddRange(CardGrid.AnimateMoveAllCards(cribOwnerGrid, _cgDeck, MOVE_CARDS_ANIMATION_DURATION, 0,
                AnimateMoveOptions.MoveSequentlyEndingAtZero, false));
            await Task.WhenAll(taskList);
        }

        private List<Task> AnimateFlipAllCards(List<CardCtrl> cards, CardOrientation orientation, bool parallel)
        {
            var taskList = new List<Task>();
            double beginTime = 0;
            foreach (var card in cards)
            {
                var t = card.SetOrientationTask(orientation, FlipAnimationDuration, beginTime);
                if (!parallel)
                {
                    beginTime += FlipAnimationDuration;
                }

                if (t != null)
                {
                    taskList.Add(t);
                }
            }

            return taskList;
        }
    }
}