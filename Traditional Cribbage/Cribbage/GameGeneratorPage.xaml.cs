using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Cards;
using CardView;
using LongShotHelpers;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Cribbage
{

    //
    //  meta data about the List/Grid Views used for drand and drop
    public class DropTargetTag
    {
        public Location Location { get; set; }
        public ObservableCollection<CardCtrl> CardList { get; set; }
        public Owner Owner { get; set; } = Owner.Uninitialized;

        public DropTargetTag(Location loc, ObservableCollection<CardCtrl> cards)
        {
            Location = loc;
            CardList = cards;
        }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GameGeneratorPage : Page
    {

        private readonly GameState[] _validStates = new GameState[] { GameState.PlayerSelectsCribCards, GameState.CountPlayer, GameState.ScorePlayerHand, GameState.ScorePlayerCrib };
        public ObservableCollection<CardCtrl> DeckCards { get; } = new ObservableCollection<CardCtrl>();
        public ObservableCollection<CardCtrl> ComputerCards { get; } = new ObservableCollection<CardCtrl>();
        public ObservableCollection<CardCtrl> PlayerCards { get; } = new ObservableCollection<CardCtrl>();
        public ObservableCollection<CardCtrl> CountedCards { get; } = new ObservableCollection<CardCtrl>();
        public ObservableCollection<CardCtrl> CribCards { get; } = new ObservableCollection<CardCtrl>();
        public ObservableCollection<CardCtrl> SharedCard { get; } = new ObservableCollection<CardCtrl>();
        public ObservableCollection<GameState> GameStates { get; } = new ObservableCollection<GameState>();

        public static readonly DependencyProperty CurrentCountProperty = DependencyProperty.Register("CurrentCount", typeof(int), typeof(GameGeneratorPage), new PropertyMetadata(0));
        public static readonly DependencyProperty StateProperty = DependencyProperty.Register("State", typeof(GameState), typeof(GameGeneratorPage), new PropertyMetadata(GameState.ScorePlayerHand));
        public static readonly DependencyProperty PlayerBackScoreProperty = DependencyProperty.Register("PlayerBackScore", typeof(int), typeof(GameGeneratorPage), new PropertyMetadata(0));
        public static readonly DependencyProperty PlayerScoreDeltaProperty = DependencyProperty.Register("PlayerScoreDelta", typeof(int), typeof(GameGeneratorPage), new PropertyMetadata(0));
        public static readonly DependencyProperty ComputerScoreDeltaProperty = DependencyProperty.Register("ComputerScoreDelta", typeof(int), typeof(GameGeneratorPage), new PropertyMetadata(0));
        public static readonly DependencyProperty ComputerBackScoreProperty = DependencyProperty.Register("ComputerBackScore", typeof(int), typeof(GameGeneratorPage), new PropertyMetadata(0));
        public static readonly DependencyProperty DealerProperty = DependencyProperty.Register("Dealer", typeof(PlayerType), typeof(GameGeneratorPage), new PropertyMetadata(PlayerType.Player));
        public static readonly DependencyProperty AutoSetScoreProperty = DependencyProperty.Register("AutoSetScore", typeof(bool), typeof(GameGeneratorPage), new PropertyMetadata(true));
        public bool AutoSetScore
        {
            get => (bool)GetValue(AutoSetScoreProperty);
            set => SetValue(AutoSetScoreProperty, value);
        }
        public PlayerType Dealer
        {
            get => (PlayerType)GetValue(DealerProperty);
            set => SetValue(DealerProperty, value);
        }
        public int ComputerBackScore
        {
            get => (int)GetValue(ComputerBackScoreProperty);
            set => SetValue(ComputerBackScoreProperty, value);
        }
        public int ComputerScoreDelta
        {
            get => (int)GetValue(ComputerScoreDeltaProperty);
            set => SetValue(ComputerScoreDeltaProperty, value);
        }
        public int PlayerScoreDelta
        {
            get => (int)GetValue(PlayerScoreDeltaProperty);
            set => SetValue(PlayerScoreDeltaProperty, value);
        }
        public int PlayerBackScore
        {
            get => (int)GetValue(PlayerBackScoreProperty);
            set => SetValue(PlayerBackScoreProperty, value);
        }
        public GameState State
        {
            get => (GameState)GetValue(StateProperty);
            set => SetValue(StateProperty, value);
        }

        public int CurrentCount
        {
            get => (int)GetValue(CurrentCountProperty);
            set => SetValue(CurrentCountProperty, value);
        }


        public GameGeneratorPage()
        {
            this.InitializeComponent();

            var deck = new Deck(0, false);
            var cardList = deck.GetCards(52, Owner.Uninitialized);

            //
            //  this puts the cards into the list in the right order for the columns.
            for (var i = 0; i < 13; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    var cardCtrl = new CardCtrl(cardList[i + j * 13], true)
                    {
                        Location = Location.Deck,
                        CanDrag = true,
                    };
                    cardCtrl.DragStarting += Card_DragStarting;
                    DeckCards.Add(cardCtrl);
                }

            }

            CurrentCount = 20;

            _lvDeck.Tag = new DropTargetTag(Location.Deck, DeckCards);
            _lvComputer.Tag = new DropTargetTag(Location.Computer, ComputerCards ) {Owner = Owner.Computer};
            _lvCountedCards.Tag = new DropTargetTag(Location.Counted, CountedCards);
            _lvCrib.Tag = new DropTargetTag(Location.Crib, CribCards);
            _lvPlayer.Tag = new DropTargetTag(Location.Player, PlayerCards) {Owner=Owner.Player};
            _lvSharedCard.Tag = new DropTargetTag(Location.Shared, SharedCard){Owner = Owner.Shared};

            GameStates.AddRange(_validStates);
            //_cmbDealer.ItemsSource = Enum.GetValues(typeof(PlayerType)).Cast<PlayerType>();
        }

        private void Card_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var card = (CardCtrl)sender;
            var index = 0;
            switch (card.Location)
            {
                case Location.Player:
                    index = PlayerCards.IndexOf(card);
                    break;
                case Location.Computer:
                    index = ComputerCards.IndexOf(card);
                    break;
                case Location.Shared:
                    index = SharedCard.IndexOf(card);
                    break;
                case Location.Crib:
                    index = CribCards.IndexOf(card);
                    break;
                case Location.Deck:
                    index = DeckCards.IndexOf(card);
                    break;
                case Location.Counted:
                    index = CountedCards.IndexOf(card);
                    break;
                case Location.Unintialized:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (index == -1)
            {
                throw new Exception("Card Owner set incorrectly");
            }

            args.Data.SetText($"{card.CardName}.{card.Location}.{index}");
            this.TraceMessage($"DragStarting: {args.Data}");
        }

        private void OnBack(object sender, RoutedEventArgs e)
        {
            if (this.Frame == null || !this.Frame.CanGoBack) return;

            this.Frame.GoBack();
        }

        private void ListView_DragEnter(object target, DragEventArgs e)
        {
            SetThickness(target, 3);
        }
        private void ListView_DragLeave(object target, DragEventArgs e)
        {
            SetThickness(target, 1);

        }
        private void SetThickness(object target, double thickness)
        {
            ((Control)target).BorderThickness = new Thickness(thickness);

            //if (target.GetType() == typeof(ListView))
            //{
            //    ((Control)target).BorderThickness = new Thickness(thickness);
            //}
            //else if (target.GetType() == typeof(GridView))
            //{
            //    ((GridView)target).BorderThickness = new Thickness(thickness);
            //}

        }

        private async void ListView_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.Text))
            {
                var tag = await e.DataView.GetTextAsync();
                var tokens = tag.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Count() != 3)
                {
                    throw new Exception($"bad marshalled data in drag and drop: {tag}");
                }

                var sourceLocation = StaticHelpers.ParseEnum<Location>(tokens[1]);
                var sourceIndex = int.Parse(tokens[2]);
                var dropTargetTag = (DropTargetTag)((Control)sender).Tag;
                CardCtrl card = null;
                switch (sourceLocation)
                {
                    case Location.Player:
                        card = PlayerCards[sourceIndex];
                        PlayerCards.Remove(card);
                        break;
                    case Location.Computer:
                        card = ComputerCards[sourceIndex];
                        ComputerCards.Remove(card);
                        break;
                    case Location.Shared:
                        card = SharedCard[sourceIndex];
                        SharedCard.Remove(card);
                        break;
                    case Location.Crib:
                        card = CribCards[sourceIndex];
                        CribCards.Remove(card);
                        break;
                    case Location.Deck:
                        card = DeckCards[sourceIndex];
                        DeckCards.Remove(card);
                        break;
                    case Location.Unintialized:
                        break;
                    case Location.Counted:
                        card = CountedCards[sourceIndex];
                        CountedCards.Remove(card);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                dropTargetTag.CardList.Add(card);
                Debug.Assert(card != null, nameof(card) + " != null");
                card.Location = dropTargetTag.Location;
                card.Owner = dropTargetTag.Owner;


            }
        }

        private void ListView_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
            if (e.DragUIOverride != null)
            {
                e.DragUIOverride.IsGlyphVisible = false;
                e.DragUIOverride.IsCaptionVisible = false;
            }
        }

        private async void OnSave(object sender, RoutedEventArgs e)
        {

            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Crib File", new List<string> { ".crib" });
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
        private string GetSaveString()
        {
            var s = new StringBuilder();

            s.Append("[Game]\r\n");
            s.Append(StaticHelpers.SetValue("Version", "1.0"));
            s.Append(StaticHelpers.SetValue("CurrentCount", CurrentCount));
            s.Append(StaticHelpers.SetValue("State", _cmbGameState.SelectedItem));
            s.Append(StaticHelpers.SetValue("AutoEnterScore", AutoSetScore));



            s.Append(StaticHelpers.SetValue("PlayerScoreDelta", PlayerScoreDelta));
            s.Append(StaticHelpers.SetValue("PlayerBackScore", PlayerBackScore));
            s.Append(StaticHelpers.SetValue("ComputerScoreDelta", ComputerScoreDelta));
            s.Append(StaticHelpers.SetValue("ComputerBackScore", ComputerBackScore));


            s.Append(StaticHelpers.SetValue("Dealer", Dealer));
            s.Append("[Cards]\r\n");
            s.Append(StaticHelpers.SetValue("Computer", SerializeList(ComputerCards)));
            s.Append(StaticHelpers.SetValue("Player", SerializeList(PlayerCards)));
            s.Append(StaticHelpers.SetValue("Counted", SerializeList(CountedCards)));
            s.Append(StaticHelpers.SetValue("Crib", SerializeList(CribCards)));
            s.Append(StaticHelpers.SetValue("SharedCard", SerializeList(SharedCard)));
            return s.ToString();
        }

        private string SerializeList(ObservableCollection<CardCtrl> cards)
        {
            const string sep = ",";

            if (cards.Count == 0)
            {
                return "";
            }

            var sb = new StringBuilder();
            foreach (var card in cards)
            {
                sb.Append(card.CardName);
                sb.Append(".");
                sb.Append(card.Owner);
                sb.Append(".");
                sb.Append(card.IsEnabled);
                sb.Append(sep);
            }

            var sepLen = sep.Length;
            sb.Remove(sb.Length - sepLen, sepLen);
            return sb.ToString();

        }
    }
}

