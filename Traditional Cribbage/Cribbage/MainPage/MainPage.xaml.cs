using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Cards;
using CardView;
using Cribbage.UxControls;
using CribbagePlayers;
using LongShotHelpers;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Cribbage
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private CardCtrl _cardPressed;
        private Game _game;

        public MainPage()
        {
            InitializeComponent();
            DataContext = this;
            ResetCards();
            _board.HideButtonsAsync();
            SizeChanged += MainPage_SizeChanged;

            SetState(GameState.GameOver);
            Loaded += MainPage_Loaded;
        }

        public ObservableCollection<CardCtrl> PlayerCards { get; set; } = new ObservableCollection<CardCtrl>();

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, CardCtrl.InitCardCache);
        }

        private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //double ratio = _vbLayoutRoot.GetScaleFactor();
            //foreach (CardCtrl card in _cgPlayer.Cards)

            //{
            //    card.ZoomRatio = ratio;
            //    card.SetImageForCard(card.CardName);
            //}
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var root = Window.Current.Content as FrameworkElement;

            var position = TransformToVisual(root).TransformPoint(e.GetCurrentPoint(this).Position);


            // check items directly under the pointer
            foreach (var element in VisualTreeHelper.FindElementsInHostCoordinates(position, root))
            {
                if (element is CardCtrl)
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
            ((Button) sender).IsEnabled = false;

            ((Button) sender).IsEnabled = true;
        }

        private async void OnFlipCards(object sender, RoutedEventArgs e)
        {
            MyMenu.IsPaneOpen = false;


            var orientation = CardOrientation.FaceUp;

            var taskList = new List<Task>();
            double duration = 1000;
            double startTime = 1000;
            foreach (var card in _cgPlayer.Cards)
            {
                if (card.Orientation == CardOrientation.FaceUp)
                {
                    orientation = CardOrientation.FaceDown;
                }

                var task = card.SetOrientationTask(orientation, duration, startTime);
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
            Debug.WriteLine(
                $"ComputerGrid:{Canvas.GetZIndex(_cgComputer)} PlayerGrid:{Canvas.GetZIndex(_cgPlayer)} Crib:{Canvas.GetZIndex(_cgCrib)} Deck:{Canvas.GetZIndex(_cgDeck)}");
        }

        private void ResetCards()
        {
            for (var i = LayoutRoot.Children.Count - 1; i >= 0; i--)
            {
                var el = LayoutRoot.Children[i];
                if (el is CardCtrl)
                {
                    LayoutRoot.Children.RemoveAt(i);
                }
            }

            _cgComputer.Reset();
            _cgPlayer.Reset();
            _cgCrib.Reset();
            _cgDiscarded.Reset();
            _cgDeck.Reset();
        }


        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            await _board.Reset();
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            MyMenu.IsPaneOpen = !MyMenu.IsPaneOpen;
        }

        private async void OnSetSettings(object sender, RoutedEventArgs e)
        {
            await Task.Delay(0);
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
                if (_cgPlayer.Cards.Count != 6)
                {
                    return;
                }

                foreach (var c in _cgPlayer.Cards)
                {
                    c.Selected = false;
                }

                _cgPlayer.SelectedCards?.Clear();
                _cgPlayer.SelectedCards = _game.ComputerSelectCrib(_cgPlayer.Cards, _game.Dealer == PlayerType.Player);

                if (_cgPlayer.SelectedCards?.Count == 2)
                {
                    _cgPlayer.SelectedCards[0].Selected = true;
                    _cgPlayer.SelectedCards[1].Selected = true;
                }

                return;
            }

            if (_game.State == GameState.CountPlayer)
            {
                var cardPlayed = await _game.GetSuggestionForCount();
                _cgPlayer.SelectCard(cardPlayed);
            }
        }

        private async Task Reset()
        {
            ResetCards();
            await _board.Reset();
            _textCardInfo.Text = "";
            _board.HideButtonsAsync();
        }


        private async void OnNewGame(object sender, RoutedEventArgs e)
        {
            MyMenu.IsPaneOpen = false;
            await Task.Delay(500);

            try
            {
                if (_game != null)
                {
                    if (_game.State != GameState.None)
                    {
                        if (await StaticHelpers.AskUserYesNoQuestion("Cribbage", "Start a new game?", "Yes", "No") ==
                            false)
                        {
                            return;
                        }
                    }

                    _game = null; // what happens if we are in an await???    
                }

                await Reset();
                _txtInstructions.Text = "";
                var player = new InteractivePlayer(_cgDiscarded, _cgCrib, 0);
                var computer = new DefaultPlayer(0);
                computer.Init("-usedroptable");
                _game = new Game(this, computer, player, 0);
                ((Button) sender).IsEnabled = true;
                await StartGame(GameState.Start);
            }
            catch
            {
                // eat this - user won't be able to do anythign anyway
            }
            finally
            {
                ((Button) sender).IsEnabled = true;
            }
        }


        private async void OnShowScore(object sender, RoutedEventArgs e)
        {
            List<Card> hand = null;
            var handType = HandType.Hand;
            var sharedCard = _cgDeck.Cards[0].Card;

            switch (_game.State)
            {
                case GameState.ScoreComputerCrib:
                    handType = HandType.Crib;
                    hand = Game.CardCtrlToCards(_cgComputer.Cards);

                    break;
                case GameState.ScoreComputerHand:
                    handType = HandType.Hand;
                    hand = Game.CardCtrlToCards(_cgComputer.Cards);

                    break;
                case GameState.ScorePlayerCrib:
                    handType = HandType.Crib;
                    hand = Game.CardCtrlToCards(_cgPlayer.Cards);

                    break;
                case GameState.ScorePlayerHand:
                    handType = HandType.Hand;
                    hand = Game.CardCtrlToCards(_cgPlayer.Cards);

                    break;
                default:
                    return;
            }

            var totalScore = CardScoring.ScoreHand(hand, sharedCard, handType, out var scores);
            var dlg = new ShowScoreDlg(hand, sharedCard, totalScore, scores);
            Canvas.SetZIndex(dlg, 99999);
            Grid.SetColumn(dlg, 3);
            Grid.SetColumnSpan(dlg, 3);
            Grid.SetRowSpan(dlg, 8);
            Grid.SetRow(dlg, 1);
            dlg.HorizontalAlignment = HorizontalAlignment.Center;
            dlg.VerticalAlignment = VerticalAlignment.Center;
            LayoutRoot.Children.Add(dlg);


            try
            {
                _btnContinue.IsEnabled = false;
                _btnShowScoreAgain.IsEnabled = false;
                await dlg.WaitForClose();
            }
            finally
            {
                LayoutRoot.Children.Remove(dlg);
                _btnContinue.IsEnabled = true;
                _btnShowScoreAgain.IsEnabled = true;

            }

            //var message = FormatScoreMessage(scores, _game.PlayerTurn, true);
            //var dlg = new MessageDialog(message.message, "Cribbage");
            //await dlg.ShowAsync();
        }


        private async void OnSaveGame(object sender, RoutedEventArgs e)
        {
            MyMenu.IsPaneOpen = false;


            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Crib File", new List<string> {".crib"});
            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = "New Game";

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                // write to file
                await FileIO.WriteTextAsync(file, GetSaveString());
                // Let Windows know that we're finished changing the file so the other app can update the remote version of the file.
                // Completing updates may require Windows to ask for user input.
                var status = await CachedFileManager.CompleteUpdatesAsync(file);
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
            var s = new StringBuilder();

            s.Append("[Game]\r\n");
            s.Append(StaticHelpers.SetValue("Version", "1.0"));
            s.Append(StaticHelpers.SetValue("CurrentCount", _game.CurrentCount));
            s.Append(StaticHelpers.SetValue("State", _game.State));
            s.Append(StaticHelpers.SetValue("AutoEnterScore", _game.AutoEnterScore));

            var (computerBackScore, computerScore, playerBackScore, playerScore) = _board.GetScores();

            s.Append(StaticHelpers.SetValue("PlayerScoreDelta", playerScore - playerBackScore));
            s.Append(StaticHelpers.SetValue("PlayerBackScore", playerBackScore));
            s.Append(StaticHelpers.SetValue("ComputerScoreDelta", computerScore - computerBackScore));
            s.Append(StaticHelpers.SetValue("ComputerBackScore", computerBackScore));


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
            MyMenu.IsPaneOpen = false;

            if (_game != null)
            {
                if (_game.State != GameState.None)
                {
                    if (await StaticHelpers.AskUserYesNoQuestion("Cribbage", "End this game and open an old one?",
                            "yes", "no") == false)
                    {
                        return;
                    }
                }
            }

            var openPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            openPicker.FileTypeFilter.Add(".crib");

            var file = await openPicker.PickSingleFileAsync();
            if (file == null)
            {
                return;
            }

            await Reset();
            _txtInstructions.Text = "";
            var player = new InteractivePlayer(_cgDiscarded, _cgCrib, 0);
            var computer = new DefaultPlayer(0);
            computer.Init("-usedroptable");
            _game = new Game(this, computer, player, 0);


            try
            {
                var contents = await FileIO.ReadTextAsync(file);
                var settings = await StaticHelpers.LoadSettingsFile(contents, file.Name);
                if (settings["Game"]["Version"] != "1.0")
                {
                    await StaticHelpers.ShowErrorText($"Bad Version {settings["Game"]["Version"]}");
                    return;
                }

                _game.CurrentCount = int.Parse(settings["Game"]["CurrentCount"]);
                _game.Player.Score = int.Parse(settings["Game"]["PlayerBackScore"]);
                _game.AutoEnterScore = bool.Parse(settings["Game"]["AutoEnterScore"]);

                _board.AnimateScoreAsync(PlayerType.Player, _game.Player.Score);
                var scoreDelta = int.Parse(settings["Game"]["PlayerScoreDelta"]);
                _board.AnimateScoreAsync(PlayerType.Player, scoreDelta);
                _game.Player.Score += scoreDelta;

                _game.Computer.Score = int.Parse(settings["Game"]["ComputerBackScore"]);
                _board.AnimateScoreAsync(PlayerType.Computer, _game.Computer.Score);
                scoreDelta = int.Parse(settings["Game"]["ComputerScoreDelta"]);
                _board.AnimateScoreAsync(PlayerType.Computer, scoreDelta);
                _game.Computer.Score += scoreDelta;

                _game.Dealer = StaticHelpers.ParseEnum<PlayerType>(settings["Game"]["Dealer"]);
                _game.State = StaticHelpers.ParseEnum<GameState>(settings["Game"]["State"]);
                SetState(_game.State);
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

                var countedCards = new List<CardCtrl>();
                var count = 0;
                foreach (var card in _cgDiscarded.Cards)
                {
                    count += card.Value;
                    if (count <= 31)
                    {
                        countedCards.Add(card);
                    }
                    else
                    {
                        foreach (var cc in countedCards)
                        {
                            cc.Counted = true;
                            cc.Opacity = 0.8;
                        }

                        countedCards.Clear();
                        count = 0;
                    }
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

                var taskList = new List<Task>();
                Task t = null;

                if ((int) _game.State > (int) GameState.GiveToCrib)
                {
                    t = _cgDeck.Cards[0].SetOrientationTask(CardOrientation.FaceUp, 500, 0);
                    taskList.Add(t);
                }

                foreach (var card in _cgPlayer.Cards)
                {
                    t = card.SetOrientationTask(CardOrientation.FaceUp, 0, 0);
                    taskList.Add(t);
                }

                foreach (var card in _cgDiscarded.Cards)
                {
                    t = card.SetOrientationTask(CardOrientation.FaceUp, 0, 0);
                    taskList.Add(t);
                }

                if ((int) _game.State >= (int) GameState.ScoreComputerHand)
                {
                    foreach (var card in _cgComputer.Cards)
                    {
                        t = card.SetOrientationTask(CardOrientation.FaceUp, 0, 0);
                        taskList.Add(t);
                    }
                }

                await Task.WhenAll(taskList);

                await StartGame(_game.State);
            }
            catch (Exception ex)
            {
                await StaticHelpers.ShowErrorText(
                    $"Error loading file {file.Name}\n\nYou should delete the file.\n\nTechnical details:\n{ex}");
            }
        }

        private async Task StartGame(GameState state)
        {
            var winner = await _game.StartGame(state);
            var msg = "";
            msg = winner == PlayerType.Player ? "Congratulations, you won!" : "Oh well. \n\nThe computer won.  Better luck next time!";

            await StaticHelpers.ShowErrorText(msg, "");
            SetState(GameState.GameOver);
        }

        private (bool ret, string badToken) LoadCardsIntoGrid(CardGrid grid, string saveString)
        {
            var ret = grid.Deserialize(saveString, ",");
            if (!ret.ret)
            {
                return ret;
            }

            foreach (var card in grid.Cards)
            {
                LayoutRoot.Children.Add(card);
            }

            grid.SetCardPositionsNoAnimation();


            return (true, "");
        }

        private void Viewbox_SizedChanged(object sender, SizeChangedEventArgs e)
        {
            //this.TraceMessage($"ScaleFactor: {_vbLayoutRoot.GetScaleFactor()}");
        }

        private void ButtonDownScore_Click(object sender, RoutedEventArgs e)
        {
            if (_game?.Player == null)
            {
                return;
            }

            var scoreDelta = Convert.ToInt32(_tbScoreToAdd.Text);
            scoreDelta -= 1;
            if (scoreDelta < 0)
            {
                scoreDelta = 0;
            }

            _board.HighlightScore(PlayerType.Player, _game.Player.Score, scoreDelta, true);
            _tbScoreToAdd.Text = scoreDelta.ToString();
        }

        private void ButtonUpScore_Click(object sender, RoutedEventArgs e)
        {
            if (_game?.Player == null)
            {
                return;
            }

            var scoreDelta = Convert.ToInt32(_tbScoreToAdd.Text);
            scoreDelta += 1;
            if (scoreDelta > 29)
            {
                scoreDelta = 29;
            }

            _tbScoreToAdd.Text = scoreDelta.ToString();
            _board.HighlightScore(PlayerType.Player, _game.Player.Score, scoreDelta, true);
        }

        private void ShowEnterScore(bool show)
        {
            var opacity = 0.0;
            if (show)
            {
                opacity = 1.0;
            }

            _daAnimateShowScoreControls.To = opacity;
            _daAnimateShowScoreControls.Duration = TimeSpan.FromMilliseconds(100);
            _sbAnimateShowScoreControls.Begin();
        }
    }
}