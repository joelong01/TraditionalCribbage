﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Cards
{
    public enum CardLayout { Overlapped, Stacked, Normal };
    public enum AnimateMoveOptions { MoveSequentlyStartingAtZero, MoveSequentlyEndingAtZero, StackAtZero };
    public delegate Task<bool> CardDroppedDelegate(List<CardCtrl> cards, int currentMax);

    /// <summary>
    ///  we need this class because we want to hook into all of the calls to add or remove something so that we can subscibe/unsubsribe
    ///  to the mouse events 
    /// </summary>

    public class CardList : List<CardCtrl>
    {
        CardGrid _parent = null;

        public CardList(CardGrid parent)
        {
            _parent = parent;
        }

        new public void Add(CardCtrl card)
        {
            if (Selectable) card.PointerPressed += _parent.CardGrid_PointerPressed;
            base.Add(card);
        }

        private bool Selectable
        {
            get
            {
                return _parent.MaxSelectedCards > 0;
            }
        }

        new public void Insert(int index, CardCtrl card)
        {
            if (Selectable) card.PointerPressed += _parent.CardGrid_PointerPressed;
            base.Insert(index, card);
        }

        new public void Clear()
        {
            if (Selectable)
            {
                foreach (var c in this)
                {
                    c.PointerPressed -= _parent.CardGrid_PointerPressed;
                }
            }

            base.Clear();
        }

        new public bool Remove(CardCtrl card)
        {
            if (Selectable) card.PointerPressed -= _parent.CardGrid_PointerPressed;
            return base.Remove(card);
        }
    }

    /// <summary>
    ///     ok.  for the record.  Why is this a Grid?  and not a GridView? Why are the cards in the LayoutRoot?
    ///     1. We don't use GridView because the drag and drop semantics and the select semantics are incorrect for what we want.
    ///     2. we don't put children in here because we then have to add and remove them when we move cards around, and that makes them blink
    ///     3. we *do* use the Grid to control the layout, but all movement in the surface is done via animation
    ///     4. 
    /// </summary>
    public class CardGrid : Grid, INotifyPropertyChanged
    {

        List<CardCtrl> _selectedCards = new List<CardCtrl>();
        CardLayout _cardLayout = CardLayout.Normal;
        int _maxSelectedCards = 1;
        CardCtrl _pushedCard = null;
        bool _acceptsDroppedCards = false;
        Rect _bounds = new Rect();
        CardList _myCards = null;
        public event CardDroppedDelegate OnCardDropped;
        //
        //  highlight support
        Brush _regularBrush;
        Thickness _regularBorderThickness = new Thickness(1.0);
        Thickness _highlightThickness = new Thickness(6.0);

        public static readonly DependencyProperty DropTargetProperty = DependencyProperty.Register("DropTarget", typeof(CardGrid), typeof(CardGrid), null);
        public static readonly DependencyProperty ParentGridProperty = DependencyProperty.Register("ParentGrid", typeof(Grid), typeof(Grid), null);

        public Location Location { get; set; } = Location.Unintialized;
        public CardGrid DropTarget
        {
            get
            {
                return (CardGrid)GetValue(DropTargetProperty);
            }
            set
            {
                SetValue(DropTargetProperty, value);
            }
        }

        public Grid ParentGrid
        {
            get
            {
                return (Grid)GetValue(ParentGridProperty);
            }
            set
            {
                SetValue(ParentGridProperty, value);
            }
        }




        //new public CardCtrl Children
        //{
        //    get
        //    {
        //        throw new InvalidOperationException("You don't add Children to this kind of grid.");
        //    }
        //}
        public CardGrid()
        {

            _myCards = new CardList(this);

            this.DataContext = this;
            this.LayoutUpdated += CardGrid_LayoutUpdated;
            this.UpdateLayout();
            this.Loaded += CardGrid_Loaded;


        }

        private void CardGrid_Loaded(object sender, RoutedEventArgs e)
        {
            _regularBorderThickness = this.BorderThickness;
            _regularBrush = this.BorderBrush;
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return string.Format($"Name:{this.Name} Count:{_myCards.Count} Layout:{this.CardLayout}");
        }

        private void CardGrid_LayoutUpdated(object sender, object e)
        {
            _bounds.X = 0;
            _bounds.Y = 0;
            _bounds.Width = this.ActualWidth;
            _bounds.Height = this.ActualHeight;
        }



        public void Highlight(bool bHighlight)
        {
            if (bHighlight)
            {

                this.BorderThickness = _highlightThickness;
                // var brush = Application.Current.Resources["SelectColor"] as SolidColorBrush;
                var brush = Application.Current.Resources["LineBrush"] as SolidColorBrush;
                this.BorderBrush = brush;
            }
            else
            {
                this.BorderThickness = _regularBorderThickness;
                this.BorderBrush = _regularBrush;
            }
        }

        public CardList Cards
        {
            get
            {
                return _myCards;
            }
        }



        public async void CardGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_maxSelectedCards == 0)
                return;

            List<CardCtrl> draggedCards = new List<CardCtrl>(); // we don't drag the selected cards, because you can click and drag something that isn't selected

            _pushedCard = sender as CardCtrl;
            if (_pushedCard != null)
            {
                Point pt = await DragAsync(this, _pushedCard, e, draggedCards);

            }




        }

        public CardLayout CardLayout
        {
            get
            {
                return _cardLayout;
            }

            set
            {
                _cardLayout = value;
            }
        }

        public int MaxSelectedCards
        {
            get
            {
                return _maxSelectedCards;
            }

            set
            {
                _maxSelectedCards = value;
            }
        }

        public List<CardCtrl> SelectedCards
        {
            get
            {
                return _selectedCards;
            }

            set
            {
                _selectedCards = value;
            }
        }

        public bool AcceptsDroppedCards
        {
            get
            {
                return _acceptsDroppedCards;
            }

            set
            {
                _acceptsDroppedCards = value;
            }
        }

        public Rect Bounds
        {
            get
            {
                return _bounds;
            }

            set
            {
                _bounds = value;
            }
        }


        private bool MouseInTarget(PointerRoutedEventArgs e)
        {
            if (DropTarget != null)
            {

                Point pt = e.GetCurrentPoint(DropTarget).Position; // get the mouse position in the context of the drop target                
                return DropTarget.Bounds.Contains(pt);
            }

            return false;
        }

        private static bool MouseInGrid(CardGrid grid, PointerRoutedEventArgs e)
        {
            if (grid != null)
            {

                Point pt = e.GetCurrentPoint(grid).Position; // get the mouse position in the context of the drop target                
                return grid.Bounds.Contains(pt);
            }

            return false;
        }

        /// <summary>
        ///  Transfer cards from the "fromGrid" to the "toGrid" grid
        ///  leavs the cards list alone in case the caller is passing in a CardGrid.Cards collection
        /// </summary>

        public static void TransferCards(CardGrid fromGrid, CardGrid toGrid, List<CardCtrl> cards, bool pushCards = true)
        {

            for (int i = cards.Count - 1; i >= 0; i--)
            {
                CardCtrl card = cards[i];
                if (fromGrid.Cards.Contains(card))
                {
                    fromGrid.Cards.Remove(card);
                    if (toGrid.CardLayout == CardLayout.Normal)
                    {
                        if (pushCards)
                            toGrid.Cards.Insert(0, card); // preserves the order of Cars as they may be sorted    
                        else
                            toGrid.Cards.Add(card);
                    }
                    else
                    {
                        toGrid.Cards.Add(card);
                    }
                    card.Location = toGrid.Location;
                    card.Selected = false;
                    continue;
                }
                else
                {
                    throw new InvalidOperationException(String.Format($"You asked me to move {card} from {fromGrid} to {toGrid} and it isn't in {fromGrid}"));
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
                _selectedCards.Remove(card);
                return;
            }

            if (_maxSelectedCards == 0)
                return;

            if (_selectedCards.Count == _maxSelectedCards)
            {
                _selectedCards[0].Selected = false;
                _selectedCards.RemoveAt(0);
            }

            _selectedCards.Add(card);
            card.Selected = true;
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
                nCards = this.Cards.Count;
            else
                nCards = index;

            if (Cards.Contains(card)) // one of mine -- move it back where it belongs!
            {
                nCards = Cards.IndexOf(card);
            }

            double firstColWidth = 0;
            Point pt = new Point(0, 0);

            if (this.ColumnDefinitions.Count > 1)
            {

                if (this.CardLayout != CardLayout.Stacked)
                {
                    firstColWidth = this.ColumnDefinitions[0].Width.Value;
                    pt.X = firstColWidth + (nCards) * this.ColumnDefinitions[1].Width.Value;
                    pt.X += this.BorderThickness.Left;
                }
            }




            GeneralTransform gt = this.TransformToVisual(card);

            Point position = gt.TransformPoint(pt);

            return position;
        }

        private const int MOUSE_MOVE_SENSITIVITY = 5;

        public event PropertyChangedEventHandler PropertyChanged;

        public Task<Point> DragAsync(CardGrid container, CardCtrl cardClickedOn, PointerRoutedEventArgs origE, List<CardCtrl> dragList)
        {
            //
            //  this is called while the pointer is pressed!
            TaskCompletionSource<Point> taskCompletionSource = new TaskCompletionSource<Point>();
            Point pointMouseDown = origE.GetCurrentPoint(cardClickedOn).Position;
            PointerEventHandler pointerMovedHandler = null;
            PointerEventHandler pointerReleasedHandler = null;
            int myZIndex = Canvas.GetZIndex(this);
            cardClickedOn.PushCard(true);

            bool dragging = false;

            if (dragList.Contains(cardClickedOn) == false)
                dragList.Insert(0, cardClickedOn); // card you clicked is always the first one
            #region Pointer_moved
            pointerMovedHandler = (Object s, PointerRoutedEventArgs e) =>
            {
                //
                //  now pointer moved

                Point pt = e.GetCurrentPoint(cardClickedOn).Position;
                Point delta = new Point();
                delta.X = pt.X - pointMouseDown.X;
                delta.Y = pt.Y - pointMouseDown.Y;
                CardCtrl localCard = (CardCtrl)s;
                bool reorderCards = false;
                foreach (var c in this.SelectedCards)
                {
                    if (dragList.Contains(c) == false)
                    {
                        dragList.Add(c);

                    }
                }

                if (dragList.Contains(localCard) == false)
                    dragList.Add(localCard);

                if (dragList.Count > MaxSelectedCards)
                {
                    CardCtrl c = this.SelectedCards[0];
                    c.Selected = false;
                    dragList.Remove(c);

                }

                if (Math.Abs(delta.X - MOUSE_MOVE_SENSITIVITY) > 0 || Math.Abs(delta.Y - MOUSE_MOVE_SENSITIVITY) > 0)
                {
                    dragging = true;
                }


                if (CardGrid.MouseInGrid(container, e)) //still inside the host container
                {
                    reorderCards = true;


                }

                if (dragList.Count > 1)
                {
                    reorderCards = false;
                    CardCtrl otherCard = dragList[0];
                    double cardWidth = cardClickedOn.ActualWidth;
                    if (cardClickedOn.Index == otherCard.Index)
                        otherCard = dragList[1];

                    //
                    //  this moves the card to make space for reordering
                    int left = (int)(cardClickedOn.AnimationPosition.X - otherCard.AnimationPosition.X);

                    if (left > cardWidth)
                    {
                        otherCard.AnimateToReletiveAsync(new Point(left - cardWidth, 0), 0);
                        return;
                    }
                    else if (left < -cardClickedOn.ActualWidth)
                    {
                        otherCard.AnimateToReletiveAsync(new Point(left + cardWidth, 0), 0);
                        return;
                    }

                }


                foreach (CardCtrl c in dragList)
                {
                    c.BoostZindex();
                    c.AnimateToReletiveAsync(delta);



                }

                if (reorderCards)
                {
                    int indexOfDraggedCard = container.Cards.IndexOf(cardClickedOn);

                    if (delta.X > 0)
                    {
                        if (indexOfDraggedCard < container.Cards.Count - 1)
                        {
                            CardCtrl cardToMove = container.Cards[indexOfDraggedCard + 1];
                            if (cardClickedOn.AnimationPosition.X + cardClickedOn.ActualWidth * 0.5 > cardToMove.AnimationPosition.X)
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
                            CardCtrl cardToMove = container.Cards[indexOfDraggedCard - 1];
                            if (cardClickedOn.AnimationPosition.X - cardClickedOn.ActualWidth * 0.5 < cardToMove.AnimationPosition.X)
                            {
                                cardToMove.AnimateToReletiveAsync(new Point(cardClickedOn.ActualWidth, 0), 250);
                                container.Cards.Remove(cardClickedOn);
                                container.Cards.Insert(container.Cards.IndexOf(cardToMove), cardClickedOn);
                            }
                        }


                    }
                }


                if (DropTarget != null)
                {

                    if (MouseInTarget(e))
                    {
                        DropTarget.Highlight(true);
                    }
                    else
                    {
                        DropTarget.Highlight(false);
                    }
                }





                pointMouseDown = pt;

            };
            #endregion
            #region Pointer_Released
            pointerReleasedHandler = async (Object s, PointerRoutedEventArgs e) =>
               {
                   CardCtrl localCard = (CardCtrl)s;
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
                   }

                   Point exitPoint = e.GetCurrentPoint(cardClickedOn).Position;

                   if (MouseInTarget(e))
                   {
                       if (DropTarget.AcceptDrop(dragList) == true)  // there might be a bug here where the view accepts the drop, but something fails...
                       {
                           foreach (CardCtrl card in dragList)
                           {
                               Point to = DropTarget.GetNextCardPosition(card);
                               await card.AnimateTo(to, false, false, 50, 0); // need to await it so that it ends in the right spot before moving to crib
                           }

                           CardGrid.TransferCards(this, DropTarget, dragList); // this fixes up the Zindex                           
                           await DropTarget.DroppedCard(dragList);
                           dragList.Clear();

                       }

                   }
                   else
                   {
                       foreach (CardCtrl card in dragList)
                       {
                           Point to = this.GetNextCardPosition(card);
                           card.AnimateToAsync(to, false, 50);
                           Canvas.SetZIndex(card, myZIndex);
                       }

                       dragList.Clear();

                   }

                   Canvas.SetZIndex(this, myZIndex);

                   //
                   //  returns the point that the mouse was released.  the _selectedCards list
                   //  will have the cards that were selected.  if dragging occurred, the card(s)e
                   //  will be in the _draggingList



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

            for (int i = 0; i < this.Cards.Count; i++)
            {
                CardCtrl card = this.Cards[i];
                UpdateOneCardLayout(i, card);
                Point p = GetNextCardPosition(card, i);
                card.AnimateToAsync(p, false, 250);
            }

        }

        private void UpdateOneCardLayout(int i, CardCtrl card)
        {
            card.Visibility = Visibility.Visible;
            switch (this.CardLayout)
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

            for (int i = 0; i < this.Cards.Count; i++)
            {
                CardCtrl card = this.Cards[i];
                UpdateOneCardLayout(i, card);
                Point p = GetNextCardPosition(card, i);
                card.SetCardPositionAndDoAnimationFixups(p);
            }

        }


        public static Task AnimateMoveOneCard(CardGrid from, CardGrid to, CardCtrl card, int index, bool rotate, double msDuration, double msBeginTime)
        {
            card.Location = to.Location;
            Point pTo = to.GetNextCardPosition(card, index);
            return card.AnimateToTask(pTo, rotate, msDuration, msBeginTime);


        }

        public static List<Task> AnimateMoveAllCards(CardGrid from, CardGrid to, double msDuration, double msBeginTime, AnimateMoveOptions layoutOptions, bool parallel)
        {
            List<Task> taskList = new List<Task>();
            Task task = null;
            double beginTime = msBeginTime;
            int index = 0;
            if (layoutOptions == AnimateMoveOptions.MoveSequentlyEndingAtZero) index = from.Cards.Count - 1;
            foreach (CardCtrl card in from.Cards)
            {
                task = CardGrid.AnimateMoveOneCard(from, to, card, index, false, msDuration, beginTime);

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
            int zIndex = Canvas.GetZIndex(from);
            Canvas.SetZIndex(from, 10 + zIndex);
            List<CardCtrl> cards = new List<CardCtrl>();
            int i = 0;

            List<Task> taskList = new List<Task>();
            Task t = null;

            try
            {

                foreach (CardCtrl c in from.Cards)
                {
                    c.BoostZindex();
                    Point pTo = to.GetNextCardPosition(c, i++);
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
                foreach (CardCtrl c in from.Cards)
                {
                    c.ResetZIndex();
                }
                Canvas.SetZIndex(from, zIndex);
            }

        }

        public static List<Task> MoveListOfCards(CardGrid from, CardGrid to, List<CardCtrl> cards, double msDuration, double msBeginTime)
        {
            int i = 0;


            Task t = null;
            List<Task> taskList = new List<Task>();


            foreach (CardCtrl c in cards)
            {
                Point pTo = to.GetNextCardPosition(c, i++);
                t = c.AnimateToTask(pTo, false, msDuration, msBeginTime);
                if (t != null)
                    taskList.Add(t);
            }

            return taskList;
        }

        public static void SetCardsToOrientation(IEnumerable<CardCtrl> cardList, CardOrientation orientation, double msDuration, double msBeginTime)
        {
            List<Task> taskList = new List<Task>();

            foreach (CardCtrl card in cardList)
            {
                Task t = card.SetOrientationTask(orientation, msDuration, msBeginTime);
                if (t != null)
                    taskList.Add(t);
            }


            Task.WaitAll(taskList.ToArray());
        }


        public static List<Task> SetCardsToOrientationTask(IEnumerable<CardCtrl> cardList, CardOrientation orientation, double msDuration, double msBeginTime)
        {
            List<Task> taskList = new List<Task>();

            foreach (CardCtrl card in cardList)
            {
                Task t = card.SetOrientationTask(orientation, msDuration, msBeginTime);
                if (t != null)
                    taskList.Add(t);
            }

            return taskList;
        }




        private bool AcceptDrop(List<CardCtrl> dragList)
        {
            return true;
        }

        private async Task<bool> DroppedCard(List<CardCtrl> dragList)
        {
            bool ret = false;
            if (OnCardDropped != null)
            {
                ret = await OnCardDropped(dragList, _maxSelectedCards);
            }


            return ret;
        }
    }


}