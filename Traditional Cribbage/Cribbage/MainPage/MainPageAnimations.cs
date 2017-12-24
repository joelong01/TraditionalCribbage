using Cards;
using Cribbage;
using System;
using System.Collections.Generic;
using CardView;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Cribbage
{
    public sealed partial class MainPage : Page
    {
        public const double FLIP_ANIMATION_DURATION = 250;
        public const double MOVE_CARDS_ANIMATION_DURATION = 250; // how long for the cards to get to the player and the computer
        bool _firstDeal = true;
        /// <summary>
        ///     Deal does the following
        ///     1. moves cards from the deck to the computer and player
        ///     2. flips the players cards faceup
        ///     3. moves 2 cards from the computer to the discard grid
        ///     
        ///     Assume we start with the cards in the deck
        ///     
        ///     One problem we have is that we can't animate (X,Y) in one TimeLine -- so we have to compose them.
        ///             
        ///     
        /// </summary>
        public async Task AnimateDeal(List<CardCtrl> playerCards, List<CardCtrl> computerCards, List<CardCtrl> discardedComputerCards, PlayerType dealer)
        {


            playerCards.Sort(CardCtrl.CompareCardsByRank);  // sort from lowest to highest, ignoring suit

            List<Task> taskList = new List<Task>();

            //
            //  assume dealer is player and then set it if it is not
            List<CardCtrl> dealersCards = playerCards;
            List<CardCtrl> nonDealerCards = computerCards;
            CardGrid dealerGrid = _cgPlayer;
            CardGrid nonDealerGrid = _cgComputer;

            if (dealer == PlayerType.Computer)
            {
                dealersCards = computerCards;
                nonDealerCards = playerCards;
                dealerGrid = _cgComputer;
                nonDealerGrid = _cgPlayer;

            }


            Task t = null;
            double beginTime = 0;

            int zIndex = 53; // this number is important -- the layout engine for cards sets Zindex too...
            for (int z = 0; z < nonDealerCards.Count; z++)
            {
                nonDealerCards[z].zIndex = zIndex;
                dealersCards[z].zIndex = zIndex - 1;
                zIndex -= 2;

            }


            _cgDeck.Cards.Sort(delegate (CardCtrl c1, CardCtrl c2)
           {
               return Canvas.GetZIndex(c2) - Canvas.GetZIndex(c1);

           });

            double targetIndex = 0; // just being a bit silly here -- by incrementing by 0.5 and casting to an int, we can increate the target index by 1 after 2 iterations through the loop
            foreach (CardCtrl card in _cgDeck.Cards)
            {

                switch (card.Owner)
                {
                    case Owner.Player:
                        t = CardGrid.AnimateMoveOneCard(_cgDeck, _cgPlayer, card, (int)targetIndex, true, MOVE_CARDS_ANIMATION_DURATION, beginTime);
                        targetIndex += 0.5;
                        card.Location = Location.Player;
                        break;
                    case Owner.Computer:
                        t = CardGrid.AnimateMoveOneCard(_cgDeck, _cgComputer, card, (int)targetIndex, true, MOVE_CARDS_ANIMATION_DURATION, beginTime);
                        targetIndex += 0.5;
                        card.Location = Location.Computer;
                        break;
                    case Owner.Shared:
                        continue;
                    case Owner.Crib:
                    case Owner.Uninitialized:
                    default:
                        throw new InvalidOperationException("Card owner not set");
                }

                taskList.Add(t);
                beginTime += MOVE_CARDS_ANIMATION_DURATION;
            }





            //
            //  Now flip the players cards
            taskList.AddRange(CardGrid.SetCardsToOrientationTask(playerCards, CardOrientation.FaceUp, FLIP_ANIMATION_DURATION, beginTime));
            await Task.WhenAll(taskList);

            taskList.Clear();
            //
            //  move computer cards to the crib.  do it slow the first time so that the user can learn where to place the cards
            //
            beginTime = 0;
            double animationDuration = FLIP_ANIMATION_DURATION;
            if (_firstDeal)
            {
                animationDuration *= 4;
                _firstDeal = false;
            }
            List<Task> tList = CardGrid.MoveListOfCards(_cgComputer, _cgCrib, discardedComputerCards, animationDuration, beginTime);
            if (tList != null) taskList.AddRange(tList);
            await Task.WhenAll(taskList);

            //
            //  Now put the cards where they belong - they are all currently owned by the deck...
            CardGrid.TransferCards(_cgDeck, _cgComputer, computerCards);
            CardGrid.TransferCards(_cgDeck, _cgPlayer, playerCards);
            CardGrid.TransferCards(_cgComputer, _cgCrib, discardedComputerCards);




        }
        /// <summary>
        ///     This is after the player has dropped 2 cards. To the Crib Do 2 things
        /// 
        ///     1. Flip the cards to face down in the crib grid        
        ///     2. flip the deck card to faceup
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task AnimateMoveToCribAndFlipDeckCard()
        {


            double beginTime = 0;
            List<Task> taskList = new List<Task>();

            CardList cards = _cgCrib.Cards;

            //foreach (CardCtrl card in cards)
            //{
            //    if (card.Orientation == CardOrientation.FaceUp)
            //        card.BoostZindex();
            //}
            ////
            ////  flip the cards -- note that the 2 computer cards are face down
            //taskList = CardGrid.SetCardsToOrientationTask(cards, CardOrientation.FaceDown, FLIP_ANIMATION_DURATION, beginTime);
            //await Task.WhenAll(taskList);
            //taskList.Clear();


            //
            //  when that is done flip the shared card
            beginTime = 0;
            taskList.AddRange(CardGrid.SetCardsToOrientationTask(_cgDeck.Cards, CardOrientation.FaceUp, FLIP_ANIMATION_DURATION, beginTime));

            //
            // run the animation
            await Task.WhenAll(taskList);


        }

        private void SetZIndex(List<CardCtrl> cards, int zIndex)
        {
            foreach (CardCtrl card in cards)
            {
                Canvas.SetZIndex(card, zIndex);
            }
        }


        private async Task AnimateSendCardsBackToOwner()
        {
            List<Task> tList = new List<Task>();
            int targetIndexForComputer = 0;
            int targetIndexForPlayer = 0;
            List<CardCtrl> playerCards = new List<CardCtrl>();
            List<CardCtrl> computerCards = new List<CardCtrl>();
            foreach (CardCtrl card in _cgDiscarded.Cards)
            {
                card.Opacity = 1.0;
                Task task;
                if (card.Owner == Owner.Computer)
                {
                    task = CardGrid.AnimateMoveOneCard(_cgDiscarded, _cgComputer, card, targetIndexForComputer, false, MOVE_CARDS_ANIMATION_DURATION, targetIndexForComputer * MOVE_CARDS_ANIMATION_DURATION);
                    computerCards.Add(card);
                    targetIndexForComputer++;
                }
                else
                {
                    task = CardGrid.AnimateMoveOneCard(_cgDiscarded, _cgPlayer, card, targetIndexForPlayer, false, MOVE_CARDS_ANIMATION_DURATION, MOVE_CARDS_ANIMATION_DURATION * targetIndexForPlayer);
                    targetIndexForPlayer++;
                    playerCards.Add(card);
                }

                tList.Add(task);
            }

            playerCards.Sort(CardCtrl.CompareCardsByRank);  // sort from lowest to highest, ignoring suit

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
            List<Task> taskList = new List<Task>();
            taskList.AddRange(AnimateFlipAllCards(_cgComputer.Cards, CardOrientation.FaceDown, true));
            taskList.AddRange(AnimateFlipAllCards(_cgPlayer.Cards, CardOrientation.FaceDown, true));
            await Task.WhenAll(taskList);
            taskList.Clear();
            taskList.AddRange(CardGrid.AnimateMoveAllCards(_cgComputer, _cgDeck, MOVE_CARDS_ANIMATION_DURATION, 0, AnimateMoveOptions.StackAtZero, false));
            var ret = CardGrid.AnimateMoveAllCards(_cgPlayer, _cgDeck, MOVE_CARDS_ANIMATION_DURATION, 0, AnimateMoveOptions.StackAtZero, false);
            taskList.AddRange(ret);
            await Task.WhenAll(taskList);
            taskList.Clear();
            CardGrid cribOwnerGrid = (dealer == PlayerType.Computer) ? _cgComputer : _cgPlayer;
            taskList.AddRange(CardGrid.AnimateMoveAllCards(_cgCrib, cribOwnerGrid, MOVE_CARDS_ANIMATION_DURATION, 0, AnimateMoveOptions.MoveSequentlyEndingAtZero, false));
            await Task.WhenAll(taskList);
            taskList.Clear();
            taskList.AddRange(AnimateFlipAllCards(_cgCrib.Cards, CardOrientation.FaceUp, true)); // moved in space, but not from grids...that happens right below
            await Task.WhenAll(taskList);

            CardGrid.TransferAllCards(_cgComputer, _cgDeck, true);
            CardGrid.TransferAllCards(_cgPlayer, _cgDeck, true);
            CardGrid.TransferAllCards(_cgCrib, cribOwnerGrid, false);



        }

        private async Task AnimationEndHand(PlayerType dealer)
        {
            List<Task> taskList = new List<Task>();

            CardGrid cribOwnerGrid = (dealer == PlayerType.Computer) ? _cgComputer : _cgPlayer;
            taskList.AddRange(AnimateFlipAllCards(cribOwnerGrid.Cards, CardOrientation.FaceDown, true));
            taskList.AddRange(CardGrid.AnimateMoveAllCards(cribOwnerGrid, _cgDeck, MOVE_CARDS_ANIMATION_DURATION, 0, AnimateMoveOptions.MoveSequentlyEndingAtZero, false));
            await Task.WhenAll(taskList);

        }

        private List<Task> AnimateFlipAllCards(List<CardCtrl> cards, CardOrientation orientation, bool parallel)
        {
            List<Task> taskList = new List<Task>();
            double beginTime = 0;
            foreach (var card in cards)
            {
                Task t = card.SetOrientationTask(orientation, FLIP_ANIMATION_DURATION, beginTime);
                if (!parallel)
                    beginTime += FLIP_ANIMATION_DURATION;
                if (t != null)
                    taskList.Add(t);
            }

            return taskList;
        }
    }

}
