using Cards;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Cribbage;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.UI.Core;
using CardView;
using LongShotHelpers;
using CribbagePlayers;
using System.Threading;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Cribbage
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        ObservableCollection<CardCtrl> _playerCards = new ObservableCollection<CardCtrl>();
        Game _game;

        public MainPage()
        {


            this.InitializeComponent();
            this.DataContext = this;
            ResetCards();
            _board.HideAsync();
            


        }

        

        


        public ObservableCollection<CardCtrl> PlayerCards
        {
            get
            {
                return _playerCards;
            }

            set
            {
                _playerCards = value;
            }
        }

        CardCtrl _cardPressed = null;
        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            FrameworkElement root = Window.Current.Content as FrameworkElement;

            Point position = this.TransformToVisual(root).TransformPoint(e.GetCurrentPoint(this).Position);


            // check items directly under the pointer
            foreach (var element in VisualTreeHelper.FindElementsInHostCoordinates(position, root))
            {
                if (element.GetType() == typeof(CardCtrl))
                {
                    _cardPressed = element as CardCtrl;
                    _cardPressed.PushCard(true);

                    break;
                }

            }
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_cardPressed != null)
            {
                _cardPressed?.PushCard(false);
                _cardPressed.Selected = !_cardPressed.Selected;
                _cardPressed = null;
            }

        }

        private  void OnSelectCribCards(object sender, RoutedEventArgs e)
        {
            MyMenu.IsPaneOpen = false;
            ((Button)(sender)).IsEnabled = false;
           
            ((Button)(sender)).IsEnabled = true;

        }

        private async void OnFlipCards(object sender, RoutedEventArgs e)
        {
            MyMenu.IsPaneOpen = false;


            CardOrientation orientation = CardOrientation.FaceUp;

            List<Task> taskList = new List<Task>();
            double duration = 1000;
            double startTime = 1000;
            foreach (CardCtrl card in _cgPlayer.Cards)
            {

                if (card.Orientation == CardOrientation.FaceUp) orientation = CardOrientation.FaceDown;
                Task task = card.SetOrientationTask(orientation, duration, startTime);
                taskList.Add(task);
                startTime += duration;
            }

            await Task.WhenAll(taskList);

        }

        private async void OnMoveCardsToCrib(object sender, RoutedEventArgs e)
        {

            MyMenu.IsPaneOpen = false;
            await AnimateMoveToCribAndFlipDeckCard();

        }

        private void DumpZIndex()
        {
            Debug.WriteLine($"ComputerGrid:{Canvas.GetZIndex(_cgComputer)} PlayerGrid:{Canvas.GetZIndex(_cgPlayer)} Crib:{Canvas.GetZIndex(_cgCrib)} Deck:{Canvas.GetZIndex(_cgDeck)}");
        }

        private void ResetCards()
        {

            for(int i = LayoutRoot.Children.Count - 1; i>=0; i--)
            {
                UIElement el = LayoutRoot.Children[i];
                if (el.GetType() == typeof (CardCtrl))
                {
                    LayoutRoot.Children.RemoveAt(i);
                    continue;
                }
            }


            _cgComputer.Cards.Clear();
            _cgPlayer.Cards.Clear();
            _cgDiscarded.Cards.Clear();
            _cgDeck.Cards.Clear();
            _cgCrib.Cards.Clear();

            _cgComputer.Children.Clear();
            _cgPlayer.Children.Clear();
            _cgDiscarded.Children.Clear();
            _cgDeck.Children.Clear();
            _cgCrib.Children.Clear();
            
        }

      

        

        private async Task OnDeal()
        {
            ResetCards();

            var (computerCards, playerCards, sharedCard) = Game.GetHands();

            await this.Deal(playerCards, computerCards, sharedCard, new List<CardCtrl> { computerCards[0], computerCards[2] }, PlayerType.Computer);
            List<CardCtrl> playerCribCards = new List<CardCtrl>() { _cgPlayer.Cards[0], _cgPlayer.Cards[1] };
            int index = 2;
            foreach (CardCtrl card in playerCribCards)
            {
                await CardGrid.AnimateMoveOneCard(_cgPlayer, _cgDiscarded, card, index++, false, MOVE_CARDS_ANIMATION_DURATION, 0);
            }

            CardGrid.TransferCards(_cgPlayer, _cgDiscarded, playerCribCards);
        }

        private async void OnTestDeal(object sender, RoutedEventArgs e)
        {
            try
            {
                MyMenu.IsPaneOpen = false;
                await OnDeal();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in OnDeal: {ex.Message}");
            }
            finally
            {
                ((Button)(sender)).IsEnabled = true;
            }

             ((Button)(sender)).IsEnabled = true;

        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            await _board.Reset();
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            MyMenu.IsPaneOpen = !MyMenu.IsPaneOpen;
        }

        private void OnOpenGame(object sender, RoutedEventArgs e)
        {

        }

        private void OnGetSuggestion(object sender, RoutedEventArgs e)
        {
            if (_game.State != GameState.PlayerSelectsCribCards)
            {
                return;
            }

            foreach (var c in _cgPlayer.Cards)
            {
                c.Selected = false;
            }
            _cgPlayer.SelectedCards.Clear();
            _cgPlayer.SelectedCards =  _game.ComputerSelectCrib(_cgPlayer.Cards, _game.Dealer == PlayerType.Player);
            _cgPlayer.SelectedCards[0].Selected = true;
            _cgPlayer.SelectedCards[1].Selected = true;


        }

        private async Task Reset()
        {
            ResetCards();
            await _board.Reset();          
            _textCardInfo.Text = "";
            _board.HideAsync();

        }

      

        private async void OnNewGame(object sender, RoutedEventArgs e)
        {
            try
            {
                

               // ((Button)sender).IsEnabled = false;
                bool ret = await StaticHelpers.AskUserYesNoQuestion("Cribbage", "Start a new game?", "Yes", "No");
                if (ret)
                {       
                    if (_game !=null)
                    {
                        _game = null; // what happens if we are in an await???    
                    }
                    await Reset();
                    _txtInstructions.Text = "";
                    InteractivePlayer player = new InteractivePlayer(_cgDiscarded, _cgCrib, _board);
                    DefaultPlayer computer = new DefaultPlayer();
                    computer.Init("-usedroptable");
                    _game = new Game(this, computer, player);
                    ((Button)sender).IsEnabled = true;
                     await _game.StartGame();
                    
                    
                                        
                }
            }
            catch
            {
                // eat this - user won't be able to do anythign anyway
            }
            finally
            {
                ((Button)sender).IsEnabled = true;
            }

        }

        private async void OnTestCribToOwner(object sender, RoutedEventArgs e)
        {
            await this.AnimateMoveCribCardsBackToOwner(PlayerType.Computer);
        }

        private async void OnTestEndHandAnimation(object sender, RoutedEventArgs e)
        {
            await AnimationEndHand(PlayerType.Computer);
        }

        private void OnTestMoveToCrib(object sender, RoutedEventArgs e)
        {
            MoveCrib(PlayerType.Computer);
        }
        int _testScore = 0;
        private  void OnTestAddScore(object sender, RoutedEventArgs e)
        {
            int delta = 0;
            if (_testScore < 80)
            {
                
                delta = 37;
            }
            else if (_testScore > 85)
            {
                delta = 7;
                
            }
            else
            {
                delta = 17;
            }

            _testScore += delta;

            _board.AnimateScore(PlayerType.Player, delta);
            _board.AnimateScore(PlayerType.Computer, delta);

        }

        private async void OnTestReset(object sender, RoutedEventArgs e)
        {
            _testScore = 0;
             await _board.Reset();
            

        }

    }
}
