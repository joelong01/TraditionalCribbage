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
using System.Text;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.Provider;

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

        private void OnSelectCribCards(object sender, RoutedEventArgs e)
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

            for (int i = LayoutRoot.Children.Count - 1; i >= 0; i--)
            {
                UIElement el = LayoutRoot.Children[i];
                if (el.GetType() == typeof(CardCtrl))
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

     

        private async void OnGetSuggestion(object sender, RoutedEventArgs e)
        {
            if (_game == null)
            {
                await StaticHelpers.ShowErrorText("Hit + to start a game.", "Cribbage");
                return;
            }

            if (_game.State == GameState.PlayerSelectsCribCards)
            {

                foreach (var c in _cgPlayer.Cards)
                {
                    c.Selected = false;
                }
                _cgPlayer.SelectedCards.Clear();
                _cgPlayer.SelectedCards = _game.ComputerSelectCrib(_cgPlayer.Cards, _game.Dealer == PlayerType.Player);
                _cgPlayer.SelectedCards[0].Selected = true;
                _cgPlayer.SelectedCards[1].Selected = true;
                return;

            }

            if (_game.State == GameState.CountPlayer)
            {
                CardCtrl cardPlayed = await _game.GetSuggestionForCount();
                _cgPlayer.SelectCard(cardPlayed);
                    
            }


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
                    if (_game != null)
                    {
                        _game = null; // what happens if we are in an await???    
                    }
                    await Reset();
                    _txtInstructions.Text = "";
                    InteractivePlayer player = new InteractivePlayer(_cgDiscarded, _cgCrib, _board, 0);
                    DefaultPlayer computer = new DefaultPlayer(0);
                    computer.Init("-usedroptable");
                    _game = new Game(this, computer, player, 0);
                    ((Button)sender).IsEnabled = true;
                    await _game.StartGame(GameState.Start);



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

       
        private void OnShowScore(object sender, RoutedEventArgs e)
        {

            if (_game.State == GameState.ScoreComputerCrib || _game.State == GameState.ScoreComputerHand)
            {

                List<Card> hand = Game.CardCtrlToCards(_cgComputer.Cards);
                Card sharedCard = (Card)_cgDeck.Cards[0].Card;
                HandType handType = HandType.Hand;
                if (_game.State == GameState.ScoreComputerCrib) handType = HandType.Crib;


                StringBuilder s = new StringBuilder(1024);
                s.Append((_game.PlayerTurn == PlayerType.Player) ? "You " : "The Computer ");

                CardScoring.ScoreHand(hand, sharedCard, handType, out List<Score> scores);
                ShowScoreMessage(scores, PlayerType.Computer);
            }
                        

        }
        

        private async void OnSaveGame(object sender, RoutedEventArgs e)
        {
            FileSavePicker savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Crib File", new List<string>() { ".crib" });
            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = "New Game";

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                // write to file
                await FileIO.WriteTextAsync(file, GetSaveString());
                // Let Windows know that we're finished changing the file so the other app can update the remote version of the file.
                // Completing updates may require Windows to ask for user input.
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                
            }
            
        }


        /*
         *  Need the following state
         *  1. GameState
         *  2. each of the CardGrid Cards
         *  3. Scores
         *  4. Current Count
         *  5. who dealt
         */

        private string GetSaveString()
        {
            StringBuilder s = new StringBuilder();

            s.Append("[Game]\r\n");
            s.Append(StaticHelpers.SetValue("Version", "1.0"));
            s.Append(StaticHelpers.SetValue("CurrentCount", _game.CurrentCount));
            s.Append(StaticHelpers.SetValue("State", _game.State));
            s.Append(StaticHelpers.SetValue("PlayerScore", _game.Player.Score));
            s.Append(StaticHelpers.SetValue("ComputerScore", _game.Computer.Score));
            s.Append(StaticHelpers.SetValue("Dealer", _game.Dealer));
            s.Append("[Cards]\r\n");
            s.Append(StaticHelpers.SetValue("Computer", _cgComputer.Serialize()));
            s.Append(StaticHelpers.SetValue("Player", _cgPlayer.Serialize()));
            s.Append(StaticHelpers.SetValue("Counted", _cgDiscarded.Serialize()));
            s.Append(StaticHelpers.SetValue("Crib", _cgCrib.Serialize()));
            s.Append(StaticHelpers.SetValue("SharedCard", _cgDeck.Serialize()));
            return s.ToString();
        }

        private async void OnOpenGame(object sender, RoutedEventArgs e)
        {

            if (await StaticHelpers.AskUserYesNoQuestion("Cribbage", "Abondon this game and open an old one?", "yes", "no") == false)
            {
                return;
            }


            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".crib");

            

            StorageFile file = await openPicker.PickSingleFileAsync();

            await Reset();
            _txtInstructions.Text = "";
            InteractivePlayer player = new InteractivePlayer(_cgDiscarded, _cgCrib, _board, 0);
            DefaultPlayer computer = new DefaultPlayer(0);
            computer.Init("-usedroptable");
            _game = new Game(this, computer, player, 0);


            try
            {
                if (file != null)
                {
                    string contents = await FileIO.ReadTextAsync(file);
                    var settings = await StaticHelpers.LoadSettingsFile(contents, file.Name);
                    if (settings["Game"]["Version"] != "1.0")
                    {
                        await StaticHelpers.ShowErrorText($"Bad Version {settings["Game"]["Version"]}");
                        return;
                    }
                    
                    _game.CurrentCount = Int32.Parse(settings["Game"]["CurrentCount"]);                    
                    _game.Player.Score = Int32.Parse(settings["Game"]["PlayerScore"]);
                    _board.AnimateScoreAsync(PlayerType.Player, _game.Player.Score);
                    _game.Computer.Score = Int32.Parse(settings["Game"]["ComputerScore"]);
                    _board.AnimateScoreAsync(PlayerType.Computer, _game.Computer.Score);
                    _game.Dealer = StaticHelpers.ParseEnum<PlayerType>(settings["Game"]["Dealer"]);
                    _game.State = StaticHelpers.ParseEnum<GameState>(settings["Game"]["State"]);
                    await MoveCrib(_game.Dealer);

                    var retTuple = LoadCardsIntoGrid(_cgComputer, settings["Cards"]["Computer"]);
                    if (!retTuple.ret)
                    {
                        throw new Exception(retTuple.badToken);
                    }

                    retTuple = LoadCardsIntoGrid(_cgPlayer, settings["Cards"]["Player"]);
                    if (!retTuple.ret)
                    {
                        throw new Exception(retTuple.badToken);
                    }

                    retTuple = LoadCardsIntoGrid(_cgDiscarded, settings["Cards"]["Counted"]);
                    if (!retTuple.ret)
                    {
                        throw new Exception(retTuple.badToken);
                    }

                    retTuple = LoadCardsIntoGrid(_cgCrib, settings["Cards"]["Crib"]);
                    if (!retTuple.ret)
                    {
                        throw new Exception(retTuple.badToken);
                    }

                    retTuple = LoadCardsIntoGrid(_cgDeck, settings["Cards"]["SharedCard"]);
                    if (!retTuple.ret)
                    {
                        throw new Exception(retTuple.badToken);
                    }

                    if ((int)_game.State > (int)GameState.GiveToCrib)
                    {
                        _cgDeck.Cards[0].SetOrientationAsync(CardOrientation.FaceUp, 500, 0);
                    }

                    foreach (var card in _cgPlayer.Cards)
                    {
                        card.SetOrientationAsync(CardOrientation.FaceUp, 0, 0);
                    }

                    foreach (var card in _cgDiscarded.Cards)
                    {
                        card.SetOrientationAsync(CardOrientation.FaceUp, 0, 0);
                    }

                    

                    await _game.StartGame(_game.State);

                }
            }
            catch(Exception ex)
            {
                await StaticHelpers.ShowErrorText($"Error loading file {file.Name}\n\nYou should delete the file.\n\nTechnical details:\n{ex.ToString()}");
            }
            
        }

        private (bool ret, string badToken) LoadCardsIntoGrid(CardGrid grid, string saveString)
        {
            var ret = grid.Deserialize(saveString, ",");
            if (!ret.ret)
                return ret;
            
            foreach (var card in grid.Cards)
            {
                LayoutRoot.Children.Add(card);
                
            }

            grid.SetCardPositionsNoAnimation();


            return (true, "");

        }
    }
}
