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

            _cgDiscarded.OnCardDropped += Player_CardDropped;


        }

        

        private async Task<bool> Player_CardDropped(List<CardCtrl> cards, int currentMax)
        {

            return await _game.CardsDropped(cards);
            
          
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

            var hands = Game.GetHands();

            await this.Deal(hands.playerCards, hands.computerCards, hands.sharedCard, new List<CardCtrl> { hands.computerCards[0], hands.computerCards[2] }, PlayerType.Computer);
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

        }

        private async Task Reset()
        {
            ResetCards();
            await _board.Reset();          
            _textCardInfo.Text = "";

        }

        private async void OnNewGame(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;
                bool ret = await StaticHelpers.AskUserYesNoQuestion("Start a new game?", "Yes", "No", "");
                if (ret)
                {
                    await Reset();
                    _txtInstructions.Text = "";
                    _game = new Game(this);
                    await _game.SetState(GameState.Start);                    
                    
                                        
                }
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

        private async void OnTestMoveToCrib(object sender, RoutedEventArgs e)
        {
            await this.MoveCardsToCrib();
        }

        private void OnTestAddScore(object sender, RoutedEventArgs e)
        {

            string msg = "   Fifteen Two";
            AddScoreMessage(msg);

        }

        private void AddScoreMessage(string msg)
        {
            _scoreViewCtrl.AddScore(msg);
         
        }

    }
}
