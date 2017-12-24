using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using System.Threading.Tasks;
using Windows.UI.Popups;
using CardView;
using Cards;
using LongShotHelpers;
using System.Diagnostics;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Cribbage
{

    public enum CardType { Player, Computer, Deck, Crib, Counted}; // all the card lists

    public interface IGameView
    {

        Task<PlayerType> ChooseDealer();

        CardList GetCards(CardType cardType);

        //
        // Remove any cards that are in the grids  
        // add the cards to the grid and then run the Deal Animation
        Task Deal(List<CardCtrl> playerCards, List<CardCtrl> computerCards, List<CardCtrl> sharedCard, List<CardCtrl> computerGribCards, PlayerType dealer);

        //
        //  Move cards to Crib and flip the shared card
        //  called in Count state
        Task MoveCardsToCrib();

        //
        //  Count Computer Card - animate the right card and update the current count
        Task CountCard(CardCtrl card, int newCount);

        //
        // Animate the cards back to the player and the computer so that scoring can take place
        // hide the Count control
        Task SendCardsBackToOwner();
        int AddScore(List<Score> scores, PlayerType playerTurn);
        void PlayerCardDroppedToCrib(List<CardCtrl> cards);
        void SetState(GameState state);
        Task RestartCounting(PlayerType player);
        Task<int> ScoreHand(List<Score> scores, PlayerType playerType, HandType handType);
        Task ReturnCribCards(PlayerType dealer);
        Task EndHand(PlayerType dealer);

        List<CardCtrl> DiscardedCards { get; }// needed after counting is over

        void AddMessage(string message);
        void SetPlayableCards(int currentCount);
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IGameView
    {
        public List<CardCtrl> DiscardedCards
        {
            get
            {
                return _cgDiscarded.Cards;
            }
        }

        public Task ComputerGiveToCrib(List<CardCtrl> cardList)
        {
            throw new NotImplementedException();
        }

        public async Task CountCard(CardCtrl card, int newCount)
        {
            if (newCount == 31) newCount = 0;

            _ctrlCount.Count = newCount;
            
            if (_game.PlayerTurn == PlayerType.Computer)
            {
                List<Task> tList = new List<Task>();
                Task task = CardGrid.AnimateMoveOneCard(_cgComputer, _cgDiscarded, card, _cgDiscarded.Cards.Count, false, MOVE_CARDS_ANIMATION_DURATION, 0);
                tList.Add(task);
                task = card.SetOrientationTask(CardOrientation.FaceUp, FLIP_ANIMATION_DURATION, 0);
                if (task != null)
                    tList.Add(task);


                CardGrid.TransferCards(_cgComputer, _cgDiscarded, new List<CardCtrl> { card });

                await Task.WhenAll(tList);
            }


            _game.SetPlayableCards(); // both enables playable cards and disables non-playable ones
            _cgPlayer.MaxSelectedCards = 1;

        }

        public async Task Deal(List<CardCtrl> playerCards, List<CardCtrl> computerCards, List<CardCtrl> sharedCard, List<CardCtrl> computerGribCards, PlayerType dealer)
        {
            AddCardsToDeckVisually(sharedCard);
            sharedCard[0].zIndex = 40;
            AddCardsToDeckVisually(playerCards);
            AddCardsToDeckVisually(computerCards);
            _cgDeck.SetCardPositionsNoAnimation();
            await MoveCrib(dealer);
            await AnimateDeal(playerCards, computerCards, computerGribCards, dealer);

            _txtCribOwner.Text = dealer.ToString() + "'s Crib";
        }

        private Task MoveCrib(PlayerType player)
        {
            if (player == PlayerType.Computer)
            {
                _daMoveCribY.To = -(_cgCrib.ActualHeight + 10); // 10 is a row that is used as empty space
            }
            else
            {
                _daMoveCribY.To = (_cgCrib.ActualHeight + 10);
            }

            return _sbMoveCrib.ToTask();
        }

        public async Task MoveCardsToCrib()
        {
            await AnimateMoveToCribAndFlipDeckCard();
            if (_game != null) // can be null if we are testing animations
                _cgPlayer.MaxSelectedCards = (_game.PlayerTurn == PlayerType.Player) ? 1 : 0;


        }

        public async Task SendCardsBackToOwner()
        {
            await AnimateSendCardsBackToOwner();

        }



        private void AddCardsToDeckVisually(List<CardCtrl> cardList)
        {
            foreach (CardCtrl c in cardList)
            {
                LayoutRoot.Children.Add(c);
                _cgDeck.Cards.Insert(0, c);
                //   c.Tapped += Card_DebugTapped;
            }
        }

        private void Card_DebugTapped(object sender, TappedRoutedEventArgs e)
        {
            CardCtrl card = sender as CardCtrl;
            _textCardInfo.Text = card.ToString();


        }

        public async Task<PlayerType> ChooseDealer()
        {

            List<CardCtrl> cards = null;
            Task t = null;
            List<Task> taskList = new List<Task>();
            const double waitToReset = 0;

            while (true)
            {
                cards = CardCtrl.GetCards(2, Owner.Uninitialized);
                cards[0].Owner = Owner.Player;
                cards[1].Owner = Owner.Computer;
                AddCardsToDeckVisually(cards);
                _cgDeck.SetCardPositionsNoAnimation();
                await DealOneCardEach();
                await Reset();
                ResetCards();
                if (cards[0].Card.CardOrdinal == cards[1].Card.CardOrdinal)
                {
                    AddMessage("Tie!  Try again.");
                    await Task.Delay(250);
                    continue;
                }

                break;

            }

            PlayerType playerType = PlayerType.Computer;
            if (cards[0].Card.CardOrdinal < cards[1].Card.CardOrdinal)
            {

                playerType = PlayerType.Player;
            }
            string user = playerType == PlayerType.Player ? "You" : "The Computer";
            string message = String.Format($"{user} got low card and will deal.");
            AddMessage(message);

            return playerType;

            //
            //  wow - local functions!
            async Task DealOneCardEach()
            {
                taskList.Clear();
                t = CardGrid.AnimateMoveOneCard(_cgDeck, _cgPlayer, cards[0], 0, true, MOVE_CARDS_ANIMATION_DURATION, 0);
                taskList.Add(t);
                t = CardGrid.AnimateMoveOneCard(_cgDeck, _cgComputer, cards[1], 0, true, MOVE_CARDS_ANIMATION_DURATION, 0);
                taskList.Add(t);
                t = cards[1].SetOrientationTask(CardOrientation.FaceUp, FLIP_ANIMATION_DURATION, 500);
                taskList.Add(t);
                t = cards[0].SetOrientationTask(CardOrientation.FaceUp, FLIP_ANIMATION_DURATION, 1500);
                taskList.Add(t);
                await Task.WhenAll(taskList);
                await Task.Delay(1000); // let the user see it for a second
            }

            async Task Reset()
            {
                taskList.Clear();
                t = cards[1].SetOrientationTask(CardOrientation.FaceDown, FLIP_ANIMATION_DURATION, waitToReset);
                taskList.Add(t);
                t = cards[0].SetOrientationTask(CardOrientation.FaceDown, FLIP_ANIMATION_DURATION, waitToReset);
                taskList.Add(t);
                t = CardGrid.AnimateMoveOneCard(_cgPlayer, _cgDeck, cards[0], 0, true, MOVE_CARDS_ANIMATION_DURATION, FLIP_ANIMATION_DURATION);
                taskList.Add(t);
                t = CardGrid.AnimateMoveOneCard(_cgComputer, _cgDeck, cards[1], 0, true, MOVE_CARDS_ANIMATION_DURATION, FLIP_ANIMATION_DURATION);
                taskList.Add(t);
                await Task.WhenAll(taskList);
            }

        }







        public void PlayerCardDroppedToCrib(List<CardCtrl> cards)
        {
            _cgPlayer.MaxSelectedCards = 4 - _cgDiscarded.Cards.Count;

        }



        public void SetState(GameState state)
        {
            //
            //  this is the default state of the UI -- modify it below for different states
            //  I do it here instead of the default of the case because some states only 
            //  change one of the items and I found that putting it in the default made
            //  the code more complicated (e.g. look at what would happen for GameState.ScorePlayerHand)

            _cgPlayer.MaxSelectedCards = 0;
            _txtInstructions.Text = "";
            _ctrlCount.Visibility = Visibility.Collapsed;

            switch (state)
            {
                case GameState.PlayerSelectsCribCards:
                    _cgPlayer.MaxSelectedCards = 2;
                    _cgPlayer.DropTarget = _cgCrib;
                    _txtInstructions.Text = "Drop two cards on the crib";
                    break;
                case GameState.CountPlayer:
                    _cgPlayer.MaxSelectedCards = 1;
                    _cgPlayer.DropTarget = _cgDiscarded;
                    _ctrlCount.Visibility = Visibility.Visible;
                    _txtInstructions.Text = "Drop the card to be counted";
                    break;

                case GameState.CountComputer:
                case GameState.Count:
                    _ctrlCount.Visibility = Visibility.Visible;
                    _txtCribOwner.Text = "";
                    break;
                case GameState.ScorePlayerHand:
                    _txtInstructions.Text = "use the buttons on the board to set a score for your hand";
                    break;
                case GameState.ScorePlayerCrib:
                    _txtInstructions.Text = "use the buttons on the board to set a score for your crib";
                    break;
                default:
                    break;
            }
        }

        public async Task RestartCounting(PlayerType playerType)
        {
            _ctrlCount.Count = 0;
            foreach (CardCtrl card in _cgDiscarded.Cards)
            {
                card.Opacity = 0.8;
                card.Counted = true;
            }

            await WaitForContinue(playerType);
            
            
        }

        private async Task WaitForContinue(PlayerType playerType)
        {
            if (playerType == PlayerType.Computer)
            {
                _btnContinue.Visibility = Visibility.Visible;
                await _btnContinue.WhenClicked();
                _btnContinue.Visibility = Visibility.Collapsed;
            }            
        }

        public int AddScore(List<Score> scores, PlayerType playerTurn)
        {
            int scoreDelta = 0;
            if (scores == null)
                return 0;
            foreach (Score score in scores)
            {
                string s = playerTurn == PlayerType.Player ? "You" : "The Computer";

                scoreDelta += score.Value;
                AddMessage(String.Format($"{s} scored: {score.ToString()}"));

            }

            _board.AnimateScoreAsync(playerTurn, scoreDelta);
            return scoreDelta;
        }


        public async Task<int> ScoreHand(List<Score> scores, PlayerType playerType, HandType handType)
        {
            int ret = AddScore(scores, playerType);
            string message = String.Format($"{playerType} scores {ret} for their {handType}");
            
            AddMessage(message);
            await Task.Delay(1000);
            if (playerType == PlayerType.Computer)
            {
                _btnContinue.Visibility = Visibility.Visible;
                await _btnContinue.WhenClicked();
                _btnContinue.Visibility = Visibility.Collapsed;
            }
            return ret;
        }

        public async Task ReturnCribCards(PlayerType dealer)
        {
            await AnimateMoveCribCardsBackToOwner(dealer);
        }

        public async Task EndHand(PlayerType dealer)
        {
            await AnimationEndHand(dealer);
            ResetCards();
            await Task.Delay(0);
        }
        public void AddMessage(string msg)
        {
            _scoreViewCtrl.AddMessage(msg);

        }

        public CardList GetCards(CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Player:
                    return _cgPlayer.Cards;                    
                case CardType.Computer:
                    return _cgComputer.Cards;
                case CardType.Deck:
                    return _cgDeck.Cards;
                case CardType.Crib:
                    return _cgCrib.Cards;
                case CardType.Counted:
                    return _cgDiscarded.Cards;
                default:
                    throw new Exception("bad CardType enum");
            }
        }

        public void SetPlayableCards(int count)
        {
            
            foreach (var card in _cgPlayer.Cards)
            {
                if (card.Value + count <= 31)
                    card.IsEnabled = true;
                else
                    card.IsEnabled = false;
            }
        }
    }
}
