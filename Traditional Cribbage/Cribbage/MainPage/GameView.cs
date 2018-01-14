using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Cards;
using CardView;
using Cribbage.UxControls;
using LongShotHelpers;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Cribbage
{
    public enum CardType
    {
        Player,
        Computer,
        Deck,
        Crib,
        Counted
    } // all the card lists

    public enum WrongScoreOption
    {
        DoNothing,
        SetOnce,
        SetThisGame,
        SetAlways,
        NeverPrompt,
        UnSet
    }

    public interface IGameView
    {
        List<CardCtrl> DiscardedCards { get; } // needed after counting is over

        Task<PlayerType> ChooseDealer();

        CardList GetCards(CardType cardType);

        //
        // Remove any cards that are in the grids  
        // add the cards to the grid and then run the Deal Animation
        Task Deal(List<CardCtrl> playerCards, List<CardCtrl> computerCards, List<CardCtrl> sharedCard,
            List<CardCtrl> computerGribCards, PlayerType dealer);

        //
        //  Move cards to Crib and flip the shared card
        //  called in Count state
        Task MoveCardsToCrib();

        Task AnimateMoveComputerCardstoCrib(List<CardCtrl> computerCribCards, bool transferCardsCuzTheyAreOwnedByTheComputer);
        //
        //  Count Computer Card - animate the right card and update the current count
        Task CountCard(PlayerType playerTurn, CardCtrl card, int newCount);

        //
        // Animate the cards back to the player and the computer so that scoring can take place
        // hide the Count control
        Task SendCardsBackToOwner();
        int AddScore(List<Score> scores, PlayerType playerTurn);
        void PlayerCardDroppedToCrib(List<CardCtrl> cards);
        void SetState(GameState state);
        Task RestartCounting(PlayerType player);
        Task<int> ScoreComputerHand(List<Score> scores, HandType handType);
        Task ReturnCribCards(PlayerType dealer);
        Task EndHand(PlayerType dealer);

        void AddMessage(string message);
        void SetPlayableCards(int currentCount);

        void SetCount(int count);

        void SetInstructions(string message);

        Task<int> HighlightScoreAndWaitForContinue(PlayerType player, int score, WrongScoreOption option);

        Task<WrongScoreOption> PromptUserForWrongScore(int wrongScore);

    }

    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IGameView
    {
        public List<CardCtrl> DiscardedCards => _cgDiscarded.Cards;

        public void SetCount(int count)
        {
            if (count == 31)
            {
                this.TraceMessage("break here");
            }

            _ctrlCount.Count = count;
            _ctrlCount.UpdateLayout();
        }

        //
        //  if this is the computer, do the animation to move it to the shared grid and then flip the card.
        //
        //  if this is the player, update which cards can be played.
        //
        public async Task CountCard(PlayerType playerTurn, CardCtrl card, int newCount)
        {
            if (playerTurn == PlayerType.Computer)
            {
                var tList = new List<Task>();
                var task = CardGrid.AnimateMoveOneCard(_cgComputer, _cgDiscarded, card, _cgDiscarded.Cards.Count, false,
                    MOVE_CARDS_ANIMATION_DURATION, 0);
                tList.Add(task);
                task = card.SetOrientationTask(CardOrientation.FaceUp, FlipAnimationDuration, 0);
                if (task != null)
                {
                    tList.Add(task);
                }

                CardGrid.TransferCards(_cgComputer, _cgDiscarded, new List<CardCtrl> { card });

                await Task.WhenAll(tList);
            }


            _game.SetPlayableCards(); // both enables playable cards and disables non-playable ones
        }

        public async Task Deal(List<CardCtrl> playerCards, List<CardCtrl> computerCards, List<CardCtrl> sharedCard,
            List<CardCtrl> computerGribCards, PlayerType dealer)
        {
            AddCardsToDeckVisually(sharedCard);
            sharedCard[0].ZIndex = 40;
            AddCardsToDeckVisually(playerCards);
            AddCardsToDeckVisually(computerCards);
            await Task.Delay(10);
            _cgDeck.SetCardPositionsNoAnimation();
            await MoveCrib(dealer);
            await AnimateDeal(playerCards, computerCards, computerGribCards, dealer);
        }

        public async Task MoveCardsToCrib()
        {
            await AnimateMoveToCribAndFlipDeckCard();
            if (_game != null) // can be null if we are testing animations
            {
                _cgPlayer.MaxSelectedCards = _game.PlayerTurn == PlayerType.Player ? 1 : 0;
            }
        }

        public async Task SendCardsBackToOwner()
        {
            _cgDiscarded.SetCardsOrientation(CardOrientation.FaceUp, 500, 0);
            await AnimateSendCardsBackToOwner();
        }

        public async Task<PlayerType> ChooseDealer()
        {
            List<CardCtrl> cards = null;
            Task t = null;
            var taskList = new List<Task>();
            const double waitToReset = 0;

            while (true)
            {
                cards = CardCtrl.GetCards(2, Owner.Uninitialized);
                cards[0].Owner = Owner.Player;
                cards[1].Owner = Owner.Computer;
                AddCardsToDeckVisually(cards);
                _cgDeck.SetCardPositionsNoAnimation();
                await Task.Delay(10);
                await DealOneCardEach();
                await Task.Delay(10);
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

            var playerType = PlayerType.Computer;
            if (cards[0].Card.CardOrdinal < cards[1].Card.CardOrdinal)
            {
                playerType = PlayerType.Player;
            }

            var user = playerType == PlayerType.Player ? "You" : "The Computer";
            var message = string.Format($"{user} got low card and will deal.");
            AddMessage(message);

            return playerType;

            //
            //  wow - local functions!
            async Task DealOneCardEach()
            {
                taskList.Clear();
                t = CardGrid.AnimateMoveOneCard(_cgDeck, _cgPlayer, cards[0], 0, true, MOVE_CARDS_ANIMATION_DURATION,
                    0);
                taskList.Add(t);
                t = CardGrid.AnimateMoveOneCard(_cgDeck, _cgComputer, cards[1], 0, true, MOVE_CARDS_ANIMATION_DURATION,
                    0);
                taskList.Add(t);
                t = cards[1].SetOrientationTask(CardOrientation.FaceUp, FlipAnimationDuration, 500);
                taskList.Add(t);
                t = cards[0].SetOrientationTask(CardOrientation.FaceUp, FlipAnimationDuration, 1500);
                taskList.Add(t);
                await Task.WhenAll(taskList);
                await Task.Delay(1000); // let the user see it for a second
            }

            async Task Reset()
            {
                taskList.Clear();
                t = cards[1].SetOrientationTask(CardOrientation.FaceDown, FlipAnimationDuration, waitToReset);
                taskList.Add(t);
                t = cards[0].SetOrientationTask(CardOrientation.FaceDown, FlipAnimationDuration, waitToReset);
                taskList.Add(t);
                t = CardGrid.AnimateMoveOneCard(_cgPlayer, _cgDeck, cards[0], 0, true, MOVE_CARDS_ANIMATION_DURATION,
                    FlipAnimationDuration);
                taskList.Add(t);
                t = CardGrid.AnimateMoveOneCard(_cgComputer, _cgDeck, cards[1], 0, true, MOVE_CARDS_ANIMATION_DURATION,
                    FlipAnimationDuration);
                taskList.Add(t);
                await Task.WhenAll(taskList);
            }
        }


        public void PlayerCardDroppedToCrib(List<CardCtrl> cards)
        {
            _cgPlayer.MaxSelectedCards = 2 - _cgDiscarded.Cards.Count;
        }

        public async Task<WrongScoreOption> PromptUserForWrongScore(int wrongScore)
        {
            WrongScoreCtrl ctrl = new WrongScoreCtrl()
            {
                Width = 400,
                Height = 300,
                WrongScore = wrongScore,
                Option = WrongScoreOption.SetOnce
            };

            LayoutRoot.Children.Add(ctrl);
            Grid.SetColumnSpan(ctrl, 99);
            Grid.SetRowSpan(ctrl, 99);
            ctrl.HorizontalAlignment = HorizontalAlignment.Center;
            ctrl.VerticalAlignment = VerticalAlignment.Center;
            Canvas.SetZIndex(ctrl, 99999);

            await ctrl.WaitForClose();
            LayoutRoot.Children.Remove(ctrl);
            return ctrl.Option;
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
            _btnShowScoreAgain.Visibility = Visibility.Collapsed;
            _btnContinue.Visibility = Visibility.Collapsed;
            ShowEnterScore(true);
            _btnDownScore.Visibility = Visibility.Collapsed;
            _btnUpScore.Visibility = Visibility.Collapsed;
            _tbScoreToAdd.Visibility = Visibility.Collapsed;
            _cgDiscarded.Description = "Counted Cards";
            _txtScoreLabel.Text = "";
            _btnSave.IsEnabled = false;
            switch (state)
            {
                case GameState.PlayerSelectsCribCards:
                    _btnSave.IsEnabled = true;
                    _cgPlayer.MaxSelectedCards = 2;
                    _cgPlayer.DropTarget = _cgCrib;
                    _txtInstructions.Text = "Drop two cards on the crib";
                    _cgDiscarded.Description = "Drop Crib Cards here";
                    break;
                case GameState.CountPlayer:
                    _btnSave.IsEnabled = true;
                    _cgPlayer.MaxSelectedCards = 1;
                    _cgPlayer.DropTarget = _cgDiscarded;
                    _ctrlCount.Visibility = Visibility.Visible;
                    _txtInstructions.Text = "Drop the card to be counted";
                    break;
                case GameState.ScoreComputerCrib:
                case GameState.ScoreComputerHand:
                    _btnShowScoreAgain.Visibility = Visibility.Visible;
                    _btnContinue.Visibility = Visibility.Visible;
                    _txtScoreLabel.Text = "Computer Score:";
                    _btnDownScore.Visibility = Visibility.Collapsed;
                    _btnUpScore.Visibility = Visibility.Collapsed;
                    _tbScoreToAdd.Visibility = Visibility.Visible;
                    _txtInstructions.Text = "Press Continue";
                    break;
                case GameState.CountComputer:
                case GameState.Count:
                    _ctrlCount.Visibility = Visibility.Visible;
                    break;
                case GameState.ScorePlayerHand:
                    _btnSave.IsEnabled = true;
                    _txtScoreLabel.Text = "Player Score:";
                    _txtInstructions.Text = "use the buttons set a score and then press Continue";
                    _btnShowScoreAgain.Visibility = Visibility.Visible;
                    _btnContinue.Visibility = Visibility.Visible;
                    _btnDownScore.Visibility = Visibility.Visible;
                    _btnUpScore.Visibility = Visibility.Visible;
                    _tbScoreToAdd.Visibility = Visibility.Visible;
                    ShowEnterScore(true);

                    break;
                case GameState.ScorePlayerCrib:
                    _btnSave.IsEnabled = true;
                    _btnDownScore.Visibility = Visibility.Visible;
                    _btnUpScore.Visibility = Visibility.Visible;
                    _tbScoreToAdd.Visibility = Visibility.Visible;
                    _btnContinue.Visibility = Visibility.Visible;
                    _txtScoreLabel.Text = "Player Score:";
                    _txtInstructions.Text = "use the buttons set a score and then press Continue";
                    _btnShowScoreAgain.Visibility = Visibility.Visible;
                    ShowEnterScore(true);
                    break;
                case GameState.GameOver:
                    _txtInstructions.Text = "use + on the menu to start a new game";
                    break;
                default:
                    break;
            }
        }

        public async Task RestartCounting(PlayerType playerType)
        {
            _cgPlayer.MaxSelectedCards = 0;

            await WaitForContinue(playerType, "Press Continue");


            _cgDiscarded.SetCardsOrientation(CardOrientation.FaceDown, 500, 0);

            _cgPlayer.MaxSelectedCards = 1;

            SetCount(0);
        }



        public int AddScore(List<Score> scores, PlayerType playerTurn)
        {
            var scoreDelta = 0;
            if (scores == null)
            {
                return 0;
            }

            scoreDelta = ShowScoreMessage(scores, playerTurn);
            _board.AnimateScoreAsync(playerTurn, scoreDelta);
            return scoreDelta;
        }

        public async Task<int> ScoreComputerHand(List<Score> scores, HandType handType)
        {
            _btnContinue.Visibility = Visibility.Visible;
            var scoreDelta = ShowScoreMessage(scores, PlayerType.Computer);
            _tbScoreToAdd.Text = scoreDelta.ToString();
            _board.HighlightScore(PlayerType.Computer, _game.Computer.Score, scoreDelta, true);
            await _btnContinue.WhenClicked();
            _board.HighlightScore(PlayerType.Computer, _game.Computer.Score, scoreDelta, false);
            _board.AnimateScoreAsync(PlayerType.Computer, scoreDelta);
            _btnContinue.Visibility = Visibility.Collapsed;
            return scoreDelta;
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

        public void SetInstructions(string message)
        {
            _txtInstructions.Text = message;
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
                card.IsEnabled = card.Value + count <= 31;
            }
        }

        public async Task<int> HighlightScoreAndWaitForContinue(PlayerType player, int actualScore, WrongScoreOption option)
        {
            var maxHighlight = 0;

            if (option == WrongScoreOption.SetOnce || option == WrongScoreOption.SetAlways || option == WrongScoreOption.SetThisGame)
            {
                _board.HighlightScore(PlayerType.Player, _game.Player.Score, actualScore, true);
                _tbScoreToAdd.Text = actualScore.ToString();
                maxHighlight = actualScore;
            }
            else
            {
                maxHighlight = Convert.ToInt32(_tbScoreToAdd.Text);
                _board.HighlightScore(PlayerType.Player, _game.Player.Score, maxHighlight, true);
            }

            var tcs = new TaskCompletionSource<object>();

            void OnCompletion(object _, RoutedEventArgs args)
            {
                var scoreDelta = Convert.ToInt32(_tbScoreToAdd.Text);
                tcs.SetResult(null);
            }

            try
            {
                _btnContinue.Click += OnCompletion;


                await tcs.Task;
                return Convert.ToInt32(_tbScoreToAdd.Text);
            }
            finally
            {
                _btnContinue.Click -= OnCompletion;

                _board.HighlightScore(PlayerType.Player, _game.Player.Score, maxHighlight, false);
            }
        }

        public Task ComputerGiveToCrib(List<CardCtrl> cardList)
        {
            throw new NotImplementedException();
        }

        private Task MoveCrib(PlayerType player)
        {
            if (player == PlayerType.Computer)
            {
                _daMoveCribY.To = -(_cgCrib.ActualHeight + 10); // 10 is a row that is used as empty space
            }
            else
            {
                _daMoveCribY.To = _cgCrib.ActualHeight + 10;
            }

            return _sbMoveCrib.ToTask();
        }


        private void AddCardsToDeckVisually(List<CardCtrl> cardList)
        {
            foreach (var c in cardList)
            {
                LayoutRoot.Children.Add(c);
                _cgDeck.Cards.Insert(0, c);

                //   c.Tapped += Card_DebugTapped; // if you want to debug card info
            }

            _cgDeck.SetCardPositionsNoAnimation();
        }

        private void Card_DebugTapped(object sender, TappedRoutedEventArgs e)
        {
            var card = sender as CardCtrl;
            _textCardInfo.Text = card.ToString();
        }

        private async Task WaitForContinue(PlayerType playerType, string message)
        {
            _txtInstructions.Text = message;
            _btnContinue.Visibility = Visibility.Visible;
            await _btnContinue.WhenClicked();
            _btnContinue.Visibility = Visibility.Collapsed;
        }

        public int ShowScoreMessage(List<Score> scores, PlayerType playerTurn)
        {
            var (message, scoreDelta) = FormatScoreMessage(scores, playerTurn, false);
            AddMessage(message);
            return scoreDelta;
        }

        private (string message, int scoreDelta) FormatScoreMessage(List<Score> scores, PlayerType playerTurn,
            bool multiLine)
        {
            var s = new StringBuilder(1024);
            var sep = ", ";
            s.Append(playerTurn == PlayerType.Player ? "You " : "The Computer ");
            var scoreDelta = 0;
            foreach (var score in scores)
            {
                scoreDelta += score.Value;
            }

            if (scores.Count == 1)
            {
                s.Append(scores[0].ToString(playerTurn));
                s.Append(".");
            }
            else if (scores.Count == 2)
            {
                s.Append(scores[0].ToString(playerTurn));
                s.Append(" and ");
                s.Append(scores[1].ToString(playerTurn));
                s.Append(".");
            }
            else
            {
                for (var i = 0; i < scores.Count; i++)
                {
                    var score = scores[i];
                    s.Append(score.ToString(playerTurn));
                    if (i < scores.Count - 1)
                    {
                        s.Append(sep);
                    }

                    if (i == scores.Count - 2)
                    {
                        s.Append("and ");
                    }
                }

                s.Append(". ");
            }

            if (multiLine)
            {
                s.Append("\n\n");
            }

            s.Append($"Total of {scoreDelta}. ");
            return (s.ToString(), scoreDelta);
        }
    }
}