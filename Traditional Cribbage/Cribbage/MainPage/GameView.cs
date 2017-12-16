using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using System.Threading.Tasks;
using Windows.UI.Popups;
using CardView;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Cribbage
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IGameView
    {
        public Task ComputerGiveToCrib(List<CardCtrl> cardList)
        {
            throw new NotImplementedException();
        }

        public async Task CountCard(CardCtrl card, int newCount)
        {
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
            await AnimateDeal(playerCards, computerCards, computerGribCards, dealer);
            _txtCribOwner.Text = dealer.ToString() + "'s Crib";
        }

        public async Task MoveCardsToCrib()
        {
            await AnimateMoveToCribAndFlipDeckCard();
            _txtInstructions.Text = "Play a card!";
            _ctrlCount.Visibility = Visibility.Visible;
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
            bool ret = await StaticHelpers.AskUserYesNoQuestion("Would you like to deal first?", "Yes", "No");
            if (ret)
                return PlayerType.Player;

            return PlayerType.Computer;
        }

        public void AddScore(List<Score> scores, PlayerType playerTurn)
        {
            int scoreDelta = 0;
            if (scores == null)
                return;
            foreach (Score score in scores)
            {
                scoreDelta += score.Value;
                AddScoreMessage(String.Format($"{_game.PlayerTurn}\n {score.ToString()}"));
                
            }
            _board.AnimateScore(playerTurn, scoreDelta);
        }



        public void PlayerCardsAddedToCrib(List<CardCtrl> cards)
        {
            _cgPlayer.MaxSelectedCards = 4 - _cgDiscarded.Cards.Count;

        }

        public void SetState(GameState state)
        {
            switch (state)
            {
                case GameState.Uninitialized:                  
                case GameState.Start:                    
                case GameState.Deal:
                case GameState.GiveToCrib:
                case GameState.ScoreHand:
                case GameState.CountingEnded:
                case GameState.ScoreCrib:
                case GameState.ShowCrib:
                case GameState.EndOfHand:                    
                case GameState.GameOver:                                        
                case GameState.None:
                    _cgPlayer.MaxSelectedCards = 2;
                    break;
                case GameState.PlayerSelectsCribCards:
                    _cgPlayer.MaxSelectedCards = 2;
                    break;
                case GameState.Count:
                    _cgPlayer.MaxSelectedCards = 1;
                    break;                                
                default:
                    break;
            }
        }

        public async Task OnGo()
        {
            _ctrlCount.Count = 0;
            foreach (CardCtrl card in _cgDiscarded.Cards)
            {
                card.Opacity = 0.8;
            }

            string content = String.Format($"Go!");
            MessageDialog dlg = new MessageDialog(content);
            await dlg.ShowAsync();
        }

      

        public async Task ScoreHand(List<Score> scores, PlayerType playerType, HandType handType)
        {
            if (scores == null)
                return;

            int s = 0;
            foreach (Score score in scores)
            {
                AddScoreMessage(score.ToString());                
                s += score.Value;
                
            }
            _board.AnimateScore(playerType, s);
            string message = String.Format($"{playerType} scores {s} for their {handType}");
            AddScoreMessage(message);
            await Task.Delay(0);
        }

        public async Task ReturnCribCards(PlayerType dealer)
        {
            await AnimateMoveCribCardsBackToOwner(dealer);
        }

        public async Task EndHand(PlayerType dealer)
        {
            await AnimationEndHand(dealer);
            ResetCards();
            _ctrlCount.Visibility = Visibility.Collapsed;
            await Task.Delay(0);
        }
    }
}
