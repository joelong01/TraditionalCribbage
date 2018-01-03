using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Cards;
using Cribbage;

namespace CardView
{
    public class ListChangedEventArgs : EventArgs
    {
        public ListChangedEventArgs(ListChangedAction action, CardCtrl card)
        {
            Action = action;
            Card = card;
        }

        public ListChangedAction Action { get; }
        public CardCtrl Card { get; }
    }


    public enum CardLayout
    {
        Overlapped,
        Stacked,
        Normal
    }

    public enum AnimateMoveOptions
    {
        MoveSequentlyStartingAtZero,
        MoveSequentlyEndingAtZero,
        StackAtZero
    }

    public delegate Task<bool> CardDroppedDelegate(List<CardCtrl> cards, int currentMax);

    public enum ListChangedAction
    {
        Added,
        Removed,
        Inserted,
        Cleared
    }

    /// <summary>
    ///     we need this class because we want to hook into all of the calls to add or remove something so that we can
    ///     subscibe/unsubsribe
    ///     to the mouse events
    /// </summary>
    public class CardList : List<CardCtrl>
    {
        public delegate void ListChanged(object sender, ListChangedEventArgs e);

        private readonly CardGrid _parent;

        public CardList(CardGrid parent)
        {
            _parent = parent;
        }

        private bool Selectable
        {
            get
            {
                var selectable = _parent.MaxSelectedCards > 0;
                return selectable;
            }
        }

        public event ListChanged OnListChanged;


        public new void Add(CardCtrl card)
        {
            if (Selectable) card.PointerPressed += _parent.CardGrid_PointerPressed;

            base.Add(card);
            OnListChanged?.Invoke(this, new ListChangedEventArgs(ListChangedAction.Added, card));
        }

        public void ClearPointerPressed()
        {
            foreach (var c in this) c.PointerPressed -= _parent.CardGrid_PointerPressed;
        }

        public void AddPointerPressed()
        {
            foreach (var c in this) c.PointerPressed += _parent.CardGrid_PointerPressed;
        }

        public new void Insert(int index, CardCtrl card)
        {
            if (Selectable) card.PointerPressed += _parent.CardGrid_PointerPressed;
            base.Insert(index, card);
            OnListChanged?.Invoke(this, new ListChangedEventArgs(ListChangedAction.Inserted, card));
        }

        public new void Clear()
        {
            if (Selectable)
                foreach (var c in this)
                    c.PointerPressed -= _parent.CardGrid_PointerPressed;

            foreach (var card in this) card.DisconnectCardCanvas();

            base.Clear();
            OnListChanged?.Invoke(this, new ListChangedEventArgs(ListChangedAction.Cleared, null));
        }

        public new bool Remove(CardCtrl card)
        {
            if (Selectable) card.PointerPressed -= _parent.CardGrid_PointerPressed;

            var ret = base.Remove(card);
            if (ret) OnListChanged?.Invoke(this, new ListChangedEventArgs(ListChangedAction.Removed, card));

            return ret;
        }
    }

    /// <summary>
    ///     ok.  for the record.  Why is this a Grid?  and not a GridView? Why are the cards in the LayoutRoot?
    ///     1. We don't use GridView because the drag and drop semantics and the select semantics are incorrect for what we
    ///     want.
    ///     2. we don't put children in here because we then have to add and remove them when we move cards around, and that
    ///     makes them blink
    ///     3. we *do* use the Grid to control the layout, but all movement in the surface is done via animation
    ///     4.
    /// </summary>
    public class CardGrid : Grid, INotifyPropertyChanged
    {
        private const int MOUSE_MOVE_SENSITIVITY = 5;

        public static readonly DependencyProperty DropTargetProperty =
            DependencyProperty.Register("DropTarget", typeof(CardGrid), typeof(CardGrid), null);

        public static readonly DependencyProperty ParentGridProperty =
            DependencyProperty.Register("ParentGrid", typeof(Grid), typeof(Grid), null);

        private readonly Thickness _highlightThickness = new Thickness(6.0);

        private readonly TextBlock _tbDescription;

        private Rect _bounds;
        private int _maxSelectedCards = 1;
        private CardCtrl _pushedCard;
        private Thickness _regularBorderThickness = new Thickness(1.0);

        //
        //  highlight support
        private Brush _regularBrush;


        public CardGrid()
        {
            Cards = new CardList(this);
            Cards.OnListChanged += OnListChanged;
            DataContext = this;
            LayoutUpdated += CardGrid_LayoutUpdated;
            UpdateLayout();
            Loaded += CardGrid_Loaded;
            _tbDescription = new TextBlock
            {
                Text = "Uninitialized",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 36,
                Foreground = new SolidColorBrush(Colors.White)
            };
            Canvas.SetZIndex(_tbDescription, 1);
            SetColumnSpan(_tbDescription, 99);
            SetRowSpan(_tbDescription, 99);

            Children.Add(_tbDescription);
        }

        public Owner Owner { get; set; } = Owner.Uninitialized;


        public Location Location { get; set; } = Location.Unintialized;

        public CardGrid DropTarget
        {
            get => (CardGrid) GetValue(DropTargetProperty);
            set => SetValue(DropTargetProperty, value);
        }

        public Grid ParentGrid
        {
            get => (Grid) GetValue(ParentGridProperty);
            set => SetValue(ParentGridProperty, value);
        }

        public string Description
        {
            get => _tbDescription.Text;
            set
            {
                if (value != Description) _tbDescription.Text = value;
            }
        }

        public CardList Cards { get; }

        public CardLayout CardLayout { get; set; } = CardLayout.Normal;

        public int MaxSelectedCards
        {
            get => _maxSelectedCards;

            set
            {
                _maxSelectedCards = value;
                //
                //  this changes with the state -- the problem is that when you transfer cards (in say GameState.Deal), the max selected
                //  is 0, but when you are doine, the MaxSelected is 2...but the cards have already transferred, so we don't have the chance
                //  to update the mouse events.  this will do it correctly every time MaxSelectedCards is called. The reason I remove the event
                //  before adding it back is that I don't want it there twice.
                Cards.ClearPointerPressed();
                if (value > 0) Cards.AddPointerPressed();
            }
        }

        public List<CardCtrl> SelectedCards { get; set; } = new List<CardCtrl>();

        public bool AcceptsDroppedCards { get; set; } = false;

        public Rect Bounds
        {
            get => _bounds;

            set => _bounds = value;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event CardDroppedDelegate OnBeginCardDropped;
        public event CardDroppedDelegate OnEndCardDropped;

        private void OnListChanged(object sender, ListChangedEventArgs e)
        {
            var list = sender as CardList;
            _tbDescription.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public void Reset()
        {
            Cards.Clear();
            Children.Clear();
            Children.Add(_tbDescription);
        }

        private void CardGrid_Loaded(object sender, RoutedEventArgs e)
        {
            _regularBorderThickness = BorderThickness;
            _regularBrush = BorderBrush;
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return string.Format($"Name:{Name} Count:{Cards.Count} Layout:{CardLayout}");
        }

        public string Serialize(string sep = ",")
        {
            if (Cards.Count == 0)
                return "";

            var sb = new StringBuilder();
            foreach (var card in Cards)
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

        public (bool ret, string badToken) Deserialize(string saveString, string sep)
        {
            if (saveString == "")
                return (true, "");

            var tokens = saveString.Split(sep.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var cards = new List<Card>();
            foreach (var token in tokens)
            {
                var subTokens = token.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (subTokens.Count() != 3) return (false, token);

                if (Enum.TryParse(subTokens[0], out CardNames cardName))
                    if (Enum.TryParse(subTokens[1], out Owner owner))
                        if (bool.TryParse(subTokens[2], out var enabled))
                        {
                            var card = new Card(cardName)
                            {
                                Owner = owner,
                                IsEnabled = enabled
                            };
                            cards.Add(card);
                        }
                        else
                        {
                            return (false, "IsEnabled incorrectly set");
                        }
                    else
                        return (false, subTokens[1]);
                else
                    return (false, subTokens[0]);
            }

            Cards.Clear();
            var uiCards = CardCtrl.CreateCardCtrlFromListOfCards(cards);
            foreach (var card in uiCards) Cards.Add(card);

            return (true, "");
        }

        private void CardGrid_LayoutUpdated(object sender, object e)
        {
            _bounds.X = 0;
            _bounds.Y = 0;
            _bounds.Width = ActualWidth;
            _bounds.Height = ActualHeight;
        }


        public void Highlight(bool bHighlight)
        {
            if (bHighlight)
            {
                BorderThickness = _highlightThickness;
                // var brush = Application.Current.Resources["SelectColor"] as SolidColorBrush;
                var brush = Application.Current.Resources["LineBrush"] as SolidColorBrush;
                BorderBrush = brush;
            }
            else
            {
                BorderThickness = _regularBorderThickness;
                BorderBrush = _regularBrush;
            }
        }


        public async void CardGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_maxSelectedCards == 0)
                return;

            var draggedCards =
                new List<CardCtrl>(); // we don't drag the selected cards, because you can click and drag something that isn't selected

            _pushedCard = sender as CardCtrl;
            if (_pushedCard != null)
            {
                var pt = await DragAsync(this, _pushedCard, e, draggedCards);
            }
        }

        /// <summary>
        ///     true of the mouse is in the specified grid, othewise false
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static bool MouseInGrid(CardGrid grid, PointerRoutedEventArgs e)
        {
            if (grid != null)
            {
                var pt = e.GetCurrentPoint(grid)
                    .Position; // get the mouse position in the context of the drop target                
                return grid.Bounds.Contains(pt);
            }

            return false;
        }

        /// <summary>
        ///     Transfer cards from the "fromGrid" to the "toGrid" grid
        ///     leavs the cards list alone in case the caller is passing in a CardGrid.Cards collection
        /// </summary>
        public static void TransferCards(CardGrid fromGrid, CardGrid toGrid, List<CardCtrl> cards,
            bool pushCards = true)
        {
            for (var i = cards.Count - 1; i >= 0; i--)
            {
                var card = cards[i];
                if (fromGrid.Cards.Contains(card))
                {
                    // card.TraceMessage($"Moving {card.ToString()} from {fromGrid} to {toGrid}");
                    fromGrid.Cards.Remove(card);
                    if (toGrid.CardLayout == CardLayout.Normal)
                        if (pushCards)
                            toGrid.Cards.Insert(0, card); // preserves the order of Cards as they may be sorted    
                        else
                            toGrid.Cards.Add(card);
                    else
                        toGrid.Cards.Add(card);
                    card.Location = toGrid.Location;
                    card.Selected = false;
                }
                else
                {
                    throw new InvalidOperationException(string.Format(
                        $"You asked me to move {card} from {fromGrid} to {toGrid} and it isn't in {fromGrid}"));
                }
            }


            toGrid.SelectedCards.Clear();
            fromGrid.SelectedCards.Clear();


            toGrid.UpdateCardLayoutAsync();
            fromGrid.UpdateCardLayoutAsync();
        }

        public static void TransferAllCards(CardGrid from, CardGrid to, bool pushCards)
        {
            TransferCards(from, to, from.Cards, pushCards);
        }

        private void ToggleSelect(CardCtrl card)
        {
            if (card == null)
                return;


            if (card.Selected)
            {
                card.Selected = false;
                SelectedCards.Remove(card);
                return;
            }

            if (_maxSelectedCards == 0)
                return;

            if (SelectedCards.Count == _maxSelectedCards)
            {
                SelectedCards[0].Selected = false;
                SelectedCards.RemoveAt(0);
            }

            SelectedCards.Add(card);
            card.Selected = true;
        }

        public void SelectCard(CardCtrl card)
        {
            if (!card.Selected) ToggleSelect(card);
        }

        private void ReleaseCard(CardCtrl card)
        {
            card.PushCard(false);
        }

        //
        //  in screen coordinates, where will the next card land?
        //  need to pass the card in because it gives the position reletive to the card
        //
        //  the index parameter is "Where would you place me if this was my position in the Cards list?"
        //
        public Point GetNextCardPosition(CardCtrl card, int index = -1)
        {
            int nCards;
            if (index == -1)
                nCards = Cards.Count;
            else
                nCards = index;

            if (Cards.Contains(card)) // one of mine -- move it back where it belongs!
                nCards = Cards.IndexOf(card);


            var pt = new Point(0, 0);

            if (ColumnDefinitions.Count > 1)
            {
                double firstColWidth = 0;
                if (CardLayout != CardLayout.Stacked)
                {
                    firstColWidth = ColumnDefinitions[0].Width.Value;
                    pt.X = firstColWidth + nCards * ColumnDefinitions[1].Width.Value;
                    pt.X += BorderThickness.Left;
                }
            }


            var gt = TransformToVisual(card);

            var position = gt.TransformPoint(pt);
            // this.TraceMessage($"Pos: {position} for {card.CardName} at location:{card.Location} as index {index}");

            return position;
        }

        public Task<Point> DragAsync(CardGrid container, CardCtrl cardClickedOn, PointerRoutedEventArgs origE,
            List<CardCtrl> dragList)
        {
            //
            //  this is called while the pointer is pressed!
            var taskCompletionSource = new TaskCompletionSource<Point>();
            var pointMouseDown = origE.GetCurrentPoint(cardClickedOn).Position;
            var totalCardMovement = new Point(0, 0);

            PointerEventHandler pointerMovedHandler = null;
            PointerEventHandler pointerReleasedHandler = null;
            var myZIndex = Canvas.GetZIndex(this);
            cardClickedOn.PushCard(true);

            var dragging = false;

            if (dragList.Contains(cardClickedOn) == false)
                dragList.Insert(0, cardClickedOn); // card you clicked is always the first one

            #region Pointer_moved

            pointerMovedHandler = (s, e) =>
            {
                //
                //  now pointer moved

                var pt = e.GetCurrentPoint(cardClickedOn).Position;
                var delta = new Point
                {
                    X = pt.X - pointMouseDown.X,
                    Y = pt.Y - pointMouseDown.Y
                };
                totalCardMovement.X += Math.Abs(delta.X);
                totalCardMovement.Y += Math.Abs(delta.Y);

                var localCard = (CardCtrl) s;
                var reorderCards = false;
                foreach (var c in SelectedCards)
                    if (dragList.Contains(c) == false)
                        dragList.Add(c);

                if (dragList.Contains(localCard) == false) dragList.Add(localCard);

                if (dragList.Count > MaxSelectedCards)
                {
                    var c = SelectedCards[0];
                    c.Selected = false;
                    dragList.Remove(c);
                    //   this.TraceMessage($"Removing {c.CardName} from dragList MaxSelected: {MaxSelectedCards}");
                }

                if (totalCardMovement.X > MOUSE_MOVE_SENSITIVITY || totalCardMovement.Y > MOUSE_MOVE_SENSITIVITY)
                    dragging = true;

                if (MouseInGrid(container, e)) //still inside the host container
                    reorderCards = true;

                if (dragList.Count > 1)
                {
                    reorderCards = false;
                    var otherCard = dragList[0];
                    var cardWidth = cardClickedOn.ActualWidth;
                    if (cardClickedOn.Index == otherCard.Index)
                        otherCard = dragList[1];

                    //
                    //  this moves the card to make space for reordering
                    var left = (int) (cardClickedOn.AnimationPosition.X - otherCard.AnimationPosition.X);

                    if (left > cardWidth)
                    {
                        otherCard.AnimateToReletiveAsync(new Point(left - cardWidth, 0), 0);
                        return;
                    }

                    if (left < -cardClickedOn.ActualWidth)
                    {
                        otherCard.AnimateToReletiveAsync(new Point(left + cardWidth, 0), 0);
                        return;
                    }
                }


                foreach (var c in dragList)
                {
                    c.BoostZindex();
                    c.AnimateToReletiveAsync(delta);
                }

                if (reorderCards)
                {
                    var indexOfDraggedCard = container.Cards.IndexOf(cardClickedOn);

                    if (delta.X > 0)
                    {
                        if (indexOfDraggedCard < container.Cards.Count - 1)
                        {
                            var cardToMove = container.Cards[indexOfDraggedCard + 1];
                            if (cardClickedOn.AnimationPosition.X + cardClickedOn.ActualWidth * 0.5 >
                                cardToMove.AnimationPosition.X)
                            {
                                cardToMove.AnimateToReletiveAsync(new Point(-cardClickedOn.ActualWidth, 0), 250);
                                container.Cards.Remove(cardClickedOn);
                                container.Cards.Insert(container.Cards.IndexOf(cardToMove) + 1, cardClickedOn);
                            }
                        }
                    }
                    else //moving left
                    {
                        if (indexOfDraggedCard > 0)
                        {
                            var cardToMove = container.Cards[indexOfDraggedCard - 1];
                            if (cardClickedOn.AnimationPosition.X - cardClickedOn.ActualWidth * 0.5 <
                                cardToMove.AnimationPosition.X)
                            {
                                cardToMove.AnimateToReletiveAsync(new Point(cardClickedOn.ActualWidth, 0), 250);
                                container.Cards.Remove(cardClickedOn);
                                container.Cards.Insert(container.Cards.IndexOf(cardToMove), cardClickedOn);
                            }
                        }
                    }
                }


                if (DropTarget != null)
                    if (MouseInGrid(DropTarget, e))
                        DropTarget.Highlight(true);
                    else
                        DropTarget.Highlight(false);


                pointMouseDown = pt;
            };

            #endregion

            #region Pointer_Released

            pointerReleasedHandler = async (s, e) =>
            {
                var localCard = (CardCtrl) s;
                localCard.PointerMoved -= pointerMovedHandler;
                localCard.PointerReleased -= pointerReleasedHandler;
                localCard.ReleasePointerCapture(origE.Pointer);
                cardClickedOn.PushCard(false);

                if (DropTarget != null)
                    DropTarget.Highlight(false);


                if (!dragging)
                {
                    ToggleSelect(cardClickedOn);
                    dragList.Clear();
                    //  this.TraceMessage("Clearling dragList");
                }

                var exitPoint = e.GetCurrentPoint(cardClickedOn).Position;

                if (!MouseInGrid(this, e)) // if we've moved the mouse out of the grid -- send it on its way.  no undo.
                {
                    if (DropTarget.AcceptDrop(dragList)
                    ) // there might be a bug here where the view accepts the drop, but something fails...
                    {
                        await DropTarget.BeginDropCards(dragList);

                        foreach (var card in dragList)
                        {
                            var to = DropTarget.GetNextCardPosition(card);
                            await card.AnimateTo(to, false, false, 250,
                                0); // need to await it so that it ends in the right spot before moving to crib
                        }

                        TransferCards(this, DropTarget,
                            dragList); // this fixes up the Zindex                           
                        await DropTarget.EndDropCards(dragList);
                        dragList.Clear();
                        //  this.TraceMessage("Clearling dragList");
                    }
                }
                else
                {
                    foreach (var card in dragList)
                    {
                        var to = GetNextCardPosition(card);
                        card.AnimateToAsync(to, false, 50);
                        Canvas.SetZIndex(card, myZIndex);
                    }

                    dragList.Clear();
                    // this.TraceMessage("Clearling dragList");
                }

                Canvas.SetZIndex(this, myZIndex);

                //
                //  returns the point that the mouse was released.  the _selectedCards list
                //  will have the cards that were selected.  if dragging occurred, the card(s)e
                //  will be in the _draggingList

                container.UpdateCardLayoutAsync();


                taskCompletionSource.SetResult(exitPoint);
            };

            #endregion

            if (dragList.Count > 0)
            {
                dragList.Last().CapturePointer(origE.Pointer);
                dragList.Last().PointerMoved += pointerMovedHandler;
                dragList.Last().PointerReleased += pointerReleasedHandler;
            }

            return taskCompletionSource.Task;
        }

        //
        //  updates the cards as part of an asyc animation
        public void UpdateCardLayoutAsync()
        {
            for (var i = 0; i < Cards.Count; i++)
            {
                var card = Cards[i];
                UpdateOneCardLayout(i, card);
                var p = GetNextCardPosition(card, i);
                card.AnimateToAsync(p, false, 250);
            }
        }

        private void UpdateOneCardLayout(int i, CardCtrl card)
        {
            card.Visibility = Visibility.Visible;
            switch (CardLayout)
            {
                case CardLayout.Overlapped:
                    Canvas.SetZIndex(card, i + 1);
                    break;
                case CardLayout.Stacked:
                    Canvas.SetZIndex(card, 53 - i); // in case I ever put a full deck in there...
                    break;
                default:
                    Canvas.SetZIndex(card, 1);
                    break;
            }
        }

        public void SetCardPositionsNoAnimation()
        {
            for (var i = 0; i < Cards.Count; i++)
            {
                var card = Cards[i];
                UpdateOneCardLayout(i, card);
                var p = GetNextCardPosition(card, i);
                card.SetCardPositionAndDoAnimationFixups(p);
            }
        }


        public static Task AnimateMoveOneCard(CardGrid from, CardGrid to, CardCtrl card, int index, bool rotate,
            double msDuration, double msBeginTime)
        {
            card.Location = to.Location;
            var pTo = to.GetNextCardPosition(card, index);
            return card.AnimateToTask(pTo, rotate, msDuration, msBeginTime);
        }

        public static List<Task> AnimateMoveAllCards(CardGrid from, CardGrid to, double msDuration, double msBeginTime,
            AnimateMoveOptions layoutOptions, bool parallel)
        {
            var taskList = new List<Task>();

            var beginTime = msBeginTime;
            var index = 0;
            if (layoutOptions == AnimateMoveOptions.MoveSequentlyEndingAtZero) index = from.Cards.Count - 1;
            foreach (var card in from.Cards)
            {
                var task = AnimateMoveOneCard(from, to, card, index, false, msDuration, beginTime);

                if (layoutOptions == AnimateMoveOptions.MoveSequentlyEndingAtZero)
                    index--;
                else if (layoutOptions == AnimateMoveOptions.MoveSequentlyStartingAtZero)
                    index++;


                if (task != null)
                    taskList.Add(task);

                if (!parallel)
                    beginTime += msDuration;
            }

            if (taskList.Count == 0) return null;


            return taskList;
        }

        public static async Task AnimateMoveAllCards(CardGrid from, CardGrid to, double msDuration, double msBeginTime)
        {
            var zIndex = Canvas.GetZIndex(from);
            Canvas.SetZIndex(from, 10 + zIndex);
            var cards = new List<CardCtrl>();
            var i = 0;

            var taskList = new List<Task>();
            Task t = null;

            try
            {
                foreach (var c in from.Cards)
                {
                    c.BoostZindex();
                    var pTo = to.GetNextCardPosition(c, i++);
                    t = c.AnimateToTask(pTo, false, msDuration, msBeginTime);
                    if (t != null)
                        taskList.Add(t);
                    cards.Add(c);
                }

                await Task.WhenAll(taskList);


                TransferCards(from, to, cards);
            }
            finally
            {
                foreach (var c in from.Cards) c.ResetZIndex();
                Canvas.SetZIndex(from, zIndex);
            }
        }

        public static List<Task> MoveListOfCards(CardGrid from, CardGrid to, List<CardCtrl> cards, double msDuration,
            double msBeginTime)
        {
            var i = 0;

            if (cards == null)
                return null;

            Task t = null;
            var taskList = new List<Task>();


            foreach (var c in cards)
            {
                var pTo = to.GetNextCardPosition(c, i++);
                t = c.AnimateToTask(pTo, false, msDuration, msBeginTime);
                if (t != null)
                    taskList.Add(t);
            }

            return taskList;
        }

        public static void SetCardsToOrientation(IEnumerable<CardCtrl> cardList, CardOrientation orientation,
            double msDuration, double msBeginTime)
        {
            var taskList = new List<Task>();

            foreach (var card in cardList)
            {
                var t = card.SetOrientationTask(orientation, msDuration, msBeginTime);
                if (t != null)
                    taskList.Add(t);
            }


            Task.WaitAll(taskList.ToArray());
        }


        public static List<Task> SetCardsToOrientationTask(IEnumerable<CardCtrl> cardList, CardOrientation orientation,
            double msDuration, double msBeginTime)
        {
            var taskList = new List<Task>();

            foreach (var card in cardList)
            {
                var t = card.SetOrientationTask(orientation, msDuration, msBeginTime);
                if (t != null)
                    taskList.Add(t);
            }

            return taskList;
        }


        private bool AcceptDrop(List<CardCtrl> dragList)
        {
            return true;
        }

        private async Task<bool> BeginDropCards(List<CardCtrl> dragList)
        {
            var ret = false;

            if (OnBeginCardDropped != null) ret = await OnBeginCardDropped(dragList, _maxSelectedCards);


            return ret;
        }

        private async Task<bool> EndDropCards(List<CardCtrl> dragList)
        {
            var ret = false;

            if (OnEndCardDropped != null) ret = await OnEndCardDropped(dragList, _maxSelectedCards);


            return ret;
        }
    }
}