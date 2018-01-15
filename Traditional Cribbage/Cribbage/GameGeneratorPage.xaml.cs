using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
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

        private readonly GameState[] _validStates = { GameState.PlayerSelectsCribCards, GameState.CountPlayer, GameState.ScorePlayerHand, GameState.ScorePlayerCrib };
        public ObservableCollection<CardCtrl> DeckCards { get; } = new ObservableCollection<CardCtrl>();
        public ObservableCollection<CardCtrl> ComputerCards { get; } = new ObservableCollection<CardCtrl>();
        public ObservableCollection<CardCtrl> PlayerCards { get; } = new ObservableCollection<CardCtrl>();
        public ObservableCollection<CardCtrl> CountedCards { get; } = new ObservableCollection<CardCtrl>();
        public ObservableCollection<CardCtrl> CribCards { get; } = new ObservableCollection<CardCtrl>();
        public ObservableCollection<CardCtrl> SharedCard { get; } = new ObservableCollection<CardCtrl>();
        public ObservableCollection<GameState> GameStates { get; } = new ObservableCollection<GameState>();

        private List<ObservableCollection<CardCtrl>> _droppedCards = new List<ObservableCollection<CardCtrl>>();
        private List<CardCtrl> _sortedCards = new List<CardCtrl>();

        public GameGeneratorPage()
        {
            InitializeComponent();

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
                        Tag = DeckCards.Count,
                        Location = Location.Deck,
                        CanDrag = true
                    };
                    cardCtrl.DragStarting += Card_DragStarting;
                    cardCtrl.PointerPressed += CardCtrl_PointerPressed;
                    DeckCards.Add(cardCtrl);
                    _sortedCards.Add(cardCtrl);
                }

            }

            CurrentCount = 20;

            _lvDeck.Tag = new DropTargetTag(Location.Deck, DeckCards);
            _lvComputer.Tag = new DropTargetTag(Location.Computer, ComputerCards) { Owner = Owner.Computer };
            _lvCountedCards.Tag = new DropTargetTag(Location.Counted, CountedCards);
            _lvCrib.Tag = new DropTargetTag(Location.Crib, CribCards);
            _lvPlayer.Tag = new DropTargetTag(Location.Player, PlayerCards) { Owner = Owner.Player };
            _lvSharedCard.Tag = new DropTargetTag(Location.Shared, SharedCard) { Owner = Owner.Shared };

            GameStates.AddRange(_validStates);
            _cmbDealer.ItemsSource = Enum.GetValues(typeof(PlayerType)).Cast<PlayerType>();
            _cmbOwner.ItemsSource = Enum.GetValues(typeof(Owner)).Cast<Owner>();
            _cmbLocation.ItemsSource = Enum.GetValues(typeof(Location)).Cast<Location>();

            _droppedCards.Add(ComputerCards);
            _droppedCards.Add(PlayerCards);
            _droppedCards.Add(CountedCards);
            _droppedCards.Add(CribCards);
            _droppedCards.Add(SharedCard);



        }

        #region Properties        
        public static readonly DependencyProperty CurrentCountProperty = DependencyProperty.Register("CurrentCount", typeof(int), typeof(GameGeneratorPage), new PropertyMetadata(0));
        public static readonly DependencyProperty StateProperty = DependencyProperty.Register("State", typeof(GameState), typeof(GameGeneratorPage), new PropertyMetadata(GameState.ScorePlayerHand));
        public static readonly DependencyProperty PlayerBackScoreProperty = DependencyProperty.Register("PlayerBackScore", typeof(int), typeof(GameGeneratorPage), new PropertyMetadata(0));
        public static readonly DependencyProperty PlayerScoreDeltaProperty = DependencyProperty.Register("PlayerScoreDelta", typeof(int), typeof(GameGeneratorPage), new PropertyMetadata(0));
        public static readonly DependencyProperty ComputerScoreDeltaProperty = DependencyProperty.Register("ComputerScoreDelta", typeof(int), typeof(GameGeneratorPage), new PropertyMetadata(0));
        public static readonly DependencyProperty ComputerBackScoreProperty = DependencyProperty.Register("ComputerBackScore", typeof(int), typeof(GameGeneratorPage), new PropertyMetadata(0));
        public static readonly DependencyProperty DealerProperty = DependencyProperty.Register("Dealer", typeof(PlayerType), typeof(GameGeneratorPage), new PropertyMetadata(PlayerType.Player));
        public static readonly DependencyProperty AutoSetScoreProperty = DependencyProperty.Register("AutoSetScore", typeof(bool), typeof(GameGeneratorPage), new PropertyMetadata(true));
        public static readonly DependencyProperty SelectedCardProperty = DependencyProperty.Register("SelectedCard", typeof(CardCtrl), typeof(GameGeneratorPage), new PropertyMetadata(null, SelectedCardChanged));

        public CardCtrl SelectedCard
        {
            get => (CardCtrl)GetValue(SelectedCardProperty);
            set => SetValue(SelectedCardProperty, value);
        }
        private static void SelectedCardChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var depPropClass = d as GameGeneratorPage;
            var newCard = (CardCtrl)e.NewValue;
            var oldCard = (CardCtrl)e.OldValue;
            depPropClass?.ChangeSelectedCard(oldCard, newCard);
        }
        private void ChangeSelectedCard(CardCtrl oldCard, CardCtrl newCard)
        {
            if (oldCard != null) oldCard.Selected = false;
            newCard.Selected = true;
        }


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
            set
            {
                if (value != State)
                {
                    SetValue(StateProperty, value);
                }
            }
        }

        public int CurrentCount
        {
            get => (int)GetValue(CurrentCountProperty);
            set => SetValue(CurrentCountProperty, value);
        }


        #endregion

        private void CardCtrl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            SelectedCard = (CardCtrl)sender;
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
            if (Frame == null || !Frame.CanGoBack) return;

            Frame.GoBack();
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
                var tokens = tag.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != 3)
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
                //
                //  enforce max cards per location
                switch (card.Location)
                {
                    case Location.Unintialized:
                        break;
                    case Location.Deck:
                        break;
                    case Location.Counted:
                        ValidateLocationCount(8, dropTargetTag.CardList);
                        _lvCountedCards.ScrollIntoView(card);
                        break;
                    case Location.Computer:
                        ValidateLocationCount(6, dropTargetTag.CardList);
                        break;
                    case Location.Player:
                        ValidateLocationCount(6, dropTargetTag.CardList);
                        break;
                    case Location.Crib:
                        ValidateLocationCount(4, dropTargetTag.CardList);
                        //
                        //  crib is always owned by the dealer
                        card.Owner = (Dealer == PlayerType.Player) ? Owner.Player : Owner.Computer;
                        break;
                    case Location.Shared:
                        //
                        //  if you drop another card into the shared location,
                        //  put the previous card back into the deck
                        ValidateLocationCount(1, dropTargetTag.CardList);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }


                if (dropTargetTag.Owner != Owner.Uninitialized)
                    card.Owner = dropTargetTag.Owner;


            }
        }

        private void ValidateLocationCount(int count, ObservableCollection<CardCtrl> cardList)
        {
            if (cardList.Count <= count) return;

            var card = cardList[0];
            cardList.Remove(card);
            InsertCardIntoDeck(card);
        }

        private void InsertCardIntoDeck(CardCtrl card)
        {
            var index = (int)card.Tag;

            if (index < DeckCards.Count)
            {
                var cardAtIndex = (int)DeckCards[index].CardName;


                DeckCards.Insert(index, card);
            }
            else
            {
                DeckCards.Add(card);
            }

            card.Location = Location.Deck;
            card.Owner = Owner.Uninitialized;

        }
        private void ListView_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
            if (e.DragUIOverride != null)
            {
                e.DragUIOverride.IsGlyphVisible = false;
                e.DragUIOverride.IsCaptionVisible = false;
            }
        }

        private async void OnSave(object sender, RoutedEventArgs e)
        {
            var rulesPassed = await RulesCheck();
            if (!rulesPassed) return;
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

        private async Task<bool> RulesCheck()
        {
            string msg = "";
            try
            {

                var totalCards = 0;
                foreach (var cardList in _droppedCards)
                {
                    totalCards += cardList.Count;
                    foreach (var card in cardList)
                    {
                        if (card.Owner == Owner.Uninitialized)
                        {
                            msg = $"All cards have to have an Owner.  {card.CardName} does not.\nProTip:  Drop the card on the player or computer first, then move it to the crib or counted list.";
                            return false;
                        }
                    }
                }

                if (totalCards != 13)
                {
                    msg = $"You dropped {totalCards} cards.  A cribbage game requires 13 cards.";
                    return false;
                }

                if (CountedCards.Count > 0)
                {
                    if (CribCards.Count != 4)
                    {
                        msg = $"You need 4 cards in the crib to have any counted cards.";
                        return false;
                    }
                }

                if (SharedCard.Count != 1)
                {
                    msg = $"You need a shared card!";
                    return false;
                }

                //
                //  thie first 2 crib cards are always given by the computer
                switch (CribCards.Count)
                {
                    case 1:
                        msg = $"The first two crib cards always come from the computer.  You can't save with only 1 card in the crib.";
                        return false;
                    case 2:
                        if (ComputerCards.Count > 4)
                        {
                            msg = $"The computer has too many cards";
                            return false;

                        }

                        break;
                    case 3:
                        if (PlayerCards.Count > 5)
                        {
                            msg = $"The player has too many cards";
                            return false;
                        }

                        if (ComputerCards.Count > 4)
                        {
                            msg = $"The computer has too many cards";
                            return false;
                        }

                        break;
                    case 4:
                        if (PlayerCards.Count > 4)
                        {
                            msg = $"The player has too many cards";
                            return false;
                        }

                        if (ComputerCards.Count > 4)
                        {
                            msg = $"The player has too many cards";
                            return false;
                        }

                        break;
                    default:
                        break;

                }

                if ((CribCards.Count == 2 && PlayerCards.Count == 6 && State != GameState.PlayerSelectsCribCards) ||
                    (CribCards.Count == 3 && PlayerCards.Count == 5 && State != GameState.PlayerSelectsCribCards))
                {
                    msg = $"It looks like the state should be PlayerSelectCribCards.\n\nI set it for you.";
                    State = GameState.PlayerSelectsCribCards;
                    return false;
                }

                switch (State)
                {
                    case GameState.PlayerSelectsCribCards:
                        if (PlayerCards.Count + CribCards.Count < 8)
                        {
                            msg =
                                $"Add {8 - PlayerCards.Count - CribCards.Count} card(s) to the crib or to the player.";
                            return false;
                        }

                        if (PlayerCards.Count == 4 && CribCards.Count == 4 && ComputerCards.Count == 4)
                        {
                            msg = $"{State} is invalid.  It can be Count or PlayerScoreHand";
                            return false;
                        }

                        break;
                    case GameState.CountPlayer:
                        if (CountedCards.Count == 0 && Dealer == PlayerType.Player)
                        {
                            msg = $"The state is CountPlayer and the player is the dealer.  One of the computer's card needs to be counted.";
                            return false;
                        }

                        int totalCount = 0;
                        foreach (var card in CountedCards)
                        {
                            totalCount += card.Value;
                            if (totalCount > 31)
                                totalCount = card.Value;
                        }

                        if (CurrentCount != totalCount)
                        {
                            msg = $"{CurrentCount} is incorrect.  The count should be {totalCount}.  I set it for you.";
                            CurrentCount = totalCount;
                            return false;
                        }
                        break;

                    default:
                        break;
                }
            }
            finally
            {

                if (msg != "")
                {
                    await StaticHelpers.ShowErrorText(msg);
                }
            }

            return true;
        }

        private void OnDealerChanged(object sender, SelectionChangedEventArgs e)
        {
            var newDealer = (PlayerType)e.AddedItems[0];
            foreach (var card in CribCards)
            {
                card.Owner = (newDealer == PlayerType.Player) ? Owner.Player : Owner.Computer;
            }
        }

        private void OnReset(object sender, RoutedEventArgs e)
        {
            for (var i= _droppedCards.Count -1; i>=0; i-- )
            {
                _droppedCards[i].Clear();
            }

            DeckCards.Clear();
            foreach (var card in _sortedCards)
            {
                DeckCards.Add(card);
            }
        }
    }
}

