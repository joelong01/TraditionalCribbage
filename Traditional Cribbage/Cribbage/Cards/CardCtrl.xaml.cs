using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Cards;
using Cribbage;
using LongShotHelpers;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace CardView
{
    public class CardSelectEventArgs : EventArgs
    {
        public CardSelectEventArgs(bool selected)
        {
            Selected = selected;
        }

        public bool Selected { get; set; }
    }

    public enum CardOrientation
    {
        FaceDown,
        FaceUp
    }

    public delegate void CardSelectionChangedDelegate(object sender, CardSelectEventArgs e);

    public enum Location
    {
        Unintialized,
        Deck,
        Discarded,
        Computer,
        Player,
        Crib
    }


    public sealed partial class CardCtrl : INotifyPropertyChanged
    {
        private static readonly Dictionary<CardNames, Canvas> CardCache = new Dictionary<CardNames, Canvas>();

        public static readonly DependencyProperty CardNameProperty = DependencyProperty.Register("CardName",
            typeof(CardNames), typeof(Card), new PropertyMetadata(CardNames.AceOfSpades, CardNameChanged));

        public bool UseImageCache { get; set; } = true;

        //
        //  these are the properties that we save/load
        private readonly List<string> _savedProperties = new List<string> { "FaceDown", "Suit", "Rank" };

        private Location _location = Location.Unintialized;
        private CardOrientation _myOrientation = CardOrientation.FaceUp;

        public CardCtrl()
        {
            InitializeComponent();
            DataContext = this;
            Selected = false;
        }

        public CardCtrl(Card card, bool useImageCache=true)
        {
            InitializeComponent();
            UseImageCache = useImageCache;
            DataContext = this;
            Card = card;
            CardName = Card.CardName;
            Selected = false;
        }


        public Card Card { get; }


        public CardNames CardName
        {
            get
            {
                if (Card != null)
                {
                    if ((CardNames)GetValue(CardNameProperty) != Card.CardName)
                    {
                        CardName = Card.CardName;
                    }
                }

                return (CardNames)GetValue(CardNameProperty);
            }
            set => SetValue(CardNameProperty, value);
        }


        public int Rank
        {
            get
            {
                if (Card == null)
                {
                    return 0;
                }

                return Card.Rank;
            }
        }

        public int Index
        {
            get
            {
                if (Card == null)
                {
                    return 0;
                }

                return Card.Index;
            }
        }

        public int Value
        {
            get
            {
                if (Card == null)
                {
                    return 0;
                }

                return Card.Value;
            }
        }


        public Location Location
        {
            get => _location;
            set
            {
                if (value != _location)
                {
                    _location = value;
                    NotifyPropertyChanged("Info");
                    NotifyPropertyChanged();
                }
            }
        }

        public string Info
        {
            get { return ToString(); }
            set { }
        }

        public new bool IsEnabled
        {
            get => base.IsEnabled;
            set
            {
                Opacity = value ? 1.0 : .5;
                base.IsEnabled = value;
            }
        }

        public Point AnimationPosition
        {
            get
            {
                var x = (double)MoveCardDoubleAnimationX.To;
                var y = (double)MoveCardDoubleAnimationY.To;
                return new Point(x, y);
            }
            set
            {
                MoveCardDoubleAnimationX.To = value.X;
                MoveCardDoubleAnimationY.To = value.Y;
            }
        }

        public double AnimateRotation
        {
            get => (double)MoveCardDoubleAnimationAngle.To;
            set => MoveCardDoubleAnimationAngle.To = value;
        }

        public bool Selected
        {
            get => SelectedGrid.Visibility == Visibility.Visible;
            set
            {
                Highlight = value;

                SelectedGrid.Visibility = value ? Visibility.Visible : Visibility.Collapsed;

                CardSelectionChanged?.Invoke(this, new CardSelectEventArgs(value));
            }
        }

        public bool HighlightCards { get; set; } = false;


        public Owner Owner
        {
            get => Card.Owner;
            set
            {
                if (value != Card.Owner)
                {
                    Card.Owner = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool CurrentRun => IsEnabled;

        public bool Counted { get; set; }

        //
        //  events
        public event PropertyChangedEventHandler PropertyChanged;
        public event CardSelectionChangedDelegate CardSelectionChanged;


        public static void InitCardCache()
        {
            foreach (CardNames cardName in Enum.GetValues(typeof(CardNames)))
            {
                if (cardName == CardNames.Uninitialized)
                {
                    continue;
                }

                if (cardName == CardNames.BackOfCard)
                {
                    continue;
                }

                var s = $"ms-appx:///Assets/Cards/xaml/{cardName}.xaml";
                var uri = new Uri(s);
                var file = StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().Result;
                var xaml = FileIO.ReadTextAsync(file).AsTask().Result;
                CardCache[cardName] = (Canvas)XamlReader.Load(xaml);
            }
        }

        public void SetImageForCard(CardNames cardName)
        {
            if (UseImageCache == false || CardCache.TryGetValue(cardName, out var cardCanvas) == false )
            {
                var s = $"ms-appx:///Assets/Cards/xaml/{cardName}.xaml";
                var uri = new Uri(s);
                var file = StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().Result;
                var xaml = FileIO.ReadTextAsync(file).AsTask().Result;
                cardCanvas = (Canvas)XamlReader.Load(xaml);
                if (UseImageCache)
                    CardCache[cardName] = cardCanvas;
            }
            //
            //  if this thows an "Value does not fall within the expected range." that means you didn't remove the Canvas from the CardCtrl
            //  

            _vbCard.Child = cardCanvas;
        }

        /// <summary>
        ///     this needs to be called whenever a CardCtrl is removed from a Parent
        /// </summary>
        public void DisconnectCardCanvas()
        {
            _vbCard.Child = null;
        }

        private static void CardNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var cardCtrl = d as CardCtrl;
            var cardName = (CardNames)e.NewValue;

            cardCtrl?.SetImageForCard(cardName);
        }


        /// <summary>
        ///     This is where the cards are created...
        /// </summary>
        /// <param name="number"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public static List<CardCtrl> GetCards(int number, Owner owner)
        {
            var deck = new Deck(Environment.TickCount);
            var cards = deck.GetCards(number, owner);
            return CreateCardCtrlFromListOfCards(cards);
        }

        public static List<CardCtrl> CreateCardCtrlFromListOfCards(List<Card> cards)
        {
            var cardUi = new List<CardCtrl>();
            foreach (var card in cards)
            {
                var c = new CardCtrl(card)
                {
                    Width = 125,
                    Height = 175,
                    Margin = new Thickness(0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Orientation = CardOrientation.FaceDown,
                    ShowDebugInfo = false,
                    IsEnabled = card.IsEnabled,
                    Selected = false
                };

                card.Tag = c;

                Grid.SetColumnSpan(c, 99); // should be enough...
                Grid.SetRowSpan(c, 99); // should be enough...
                cardUi.Add(c);
            }

            return cardUi;
        }


        public override string ToString()
        {
            if (Card == null)
            {
                return "";
            }

            return string.Format($"{Card} Owner: {Owner}\n ZIndex: {Canvas.GetZIndex(this)}\n Location: {Location}");
        }

        public string Serialize()
        {
            return this.SerializeObject(_savedProperties);
        }

        public bool Deserialize(string s)
        {
            this.DeserializeObject(s);
            return true;
        }

        public void PushCard(bool shrink)
        {
            if (shrink)
            {
                _daScaleCardX.To = .98;
                _daScaleCardY.To = .98;
            }
            else
            {
                _daScaleCardX.To = 1.0;
                _daScaleCardY.To = 1.0;
            }

            _sbScaleCard.Begin();
        }

        public async Task Rotate(double angle, bool callStop)
        {
            _daRotate.To = angle;

            await StaticHelpers.RunStoryBoard(_sbRotateCard, false);
        }

        public Task RotateTask(double angle, double duration)
        {
            _daRotate.To = angle;
            _daRotate.Duration = TimeSpan.FromMilliseconds(duration);
            return _sbRotateCard.ToTask();
        }


        private void SetupMoveCardAnimation(Point to, bool rotate, double msDuration, double beginTime)
        {
            MoveCardDoubleAnimationX.To = to.X;
            MoveCardDoubleAnimationY.To = to.Y;
            MoveCardDoubleAnimationX.Duration = TimeSpan.FromMilliseconds(msDuration);
            MoveCardDoubleAnimationY.Duration = TimeSpan.FromMilliseconds(msDuration);
            MoveCardDoubleAnimationAngle.Duration = TimeSpan.FromMilliseconds(msDuration);

            MoveCardDoubleAnimationX.BeginTime = TimeSpan.FromMilliseconds(beginTime);
            MoveCardDoubleAnimationY.BeginTime = TimeSpan.FromMilliseconds(beginTime);
            MoveCardDoubleAnimationAngle.BeginTime = TimeSpan.FromMilliseconds(beginTime);

            if (rotate)
            {
                MoveCardDoubleAnimationAngle.To += 360;
            }
        }

        /// <summary>
        ///     This puts the card in the desired position directly.
        ///     it also sets up the animation positions so that any delta animations will work correctly.
        /// </summary>
        /// <param name="to"></param>
        public void SetCardPositionAndDoAnimationFixups(Point to)
        {
            SetupMoveCardAnimation(to, false, 0, 0);
            _tranformLayoutRoot.TranslateX = to.X;
            _tranformLayoutRoot.TranslateY = to.Y;
        }

        public void AnimateToAsync(Point to, bool rotate, double msDuration, double beginTime = 0)
        {
            SetupMoveCardAnimation(to, rotate, msDuration, beginTime);
            MoveCardStoryboard.Begin();
        }

        public Task AnimateToTask(Point to, bool rotate, double msDuration, double msBeginTime)
        {
            SetupMoveCardAnimation(to, rotate, msDuration, msBeginTime);
            return MoveCardStoryboard.ToTask();
        }

        public async Task AnimateTo(Point to, bool rotate, bool callStop, double msDuration, double msBeginTime)
        {
            SetupMoveCardAnimation(to, rotate, msDuration, msBeginTime);
            await StaticHelpers.RunStoryBoard(MoveCardStoryboard, callStop, msDuration);
        }

        public void MoveCardToReletivePosition(Point delta)
        {
            var to = new Point
            {
                X = (double)MoveCardDoubleAnimationX.To + delta.X,
                Y = (double)MoveCardDoubleAnimationY.To + delta.Y
            };
            SetCardPositionAndDoAnimationFixups(to);
        }

        public void AnimateToReletiveAsync(Point to, double milliseconds = 0)
        {
            MoveCardDoubleAnimationX.To += to.X;
            MoveCardDoubleAnimationY.To += to.Y;
            MoveCardDoubleAnimationX.Duration = TimeSpan.FromMilliseconds(milliseconds);
            MoveCardDoubleAnimationY.Duration = TimeSpan.FromMilliseconds(milliseconds);
            MoveCardStoryboard.Begin();
        }


        internal void ResetZIndex()
        {
            var zIndex = Canvas.GetZIndex(this);


            while (zIndex > 1000)
            {
                zIndex -= 1000;
                //Debug.WriteLine("Resetting ZIndex.  Card: {0} ZIndex:{1}", this.CardName, ZIndex - 500);
                Canvas.SetZIndex(this, zIndex);
            }

            //Debug.WriteLine($"Card:{this.CardName} New ZIndex:{Canvas.GetZIndex(this)}");
        }


        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SetupFlipAnimation(CardOrientation toOrientation, double msDuration, double msBeginTime)
        {
            var flipToFaceUp = _myOrientation == CardOrientation.FaceDown;

            _myOrientation = toOrientation;

            if (flipToFaceUp)
            {
                daFlipBack.To = -90;
                daFlipFront.To = 0;
                daFlipBack.BeginTime = TimeSpan.FromMilliseconds(msBeginTime);
                daFlipFront.BeginTime = TimeSpan.FromMilliseconds(msDuration + msBeginTime);

                daFlipFront.Duration = TimeSpan.FromMilliseconds(msDuration);
                daFlipBack.Duration = TimeSpan.FromMilliseconds(msDuration);
            }
            else
            {
                daFlipBack.To = 0;
                daFlipFront.To = 90;
                daFlipBack.BeginTime = TimeSpan.FromMilliseconds(msDuration + msBeginTime);
                daFlipBack.Duration = TimeSpan.FromMilliseconds(msDuration);
                daFlipFront.BeginTime = TimeSpan.FromSeconds(msBeginTime);
                daFlipFront.Duration = TimeSpan.FromMilliseconds(msDuration);
            }
        }

        public void SetOrientationAsync(CardOrientation orientation, double msDuration, double msBeginTime)
        {
            if (orientation == _myOrientation)
            {
                return;
            }

            SetupFlipAnimation(orientation, msDuration, msBeginTime);
            sbFlip.Begin();
        }

        public Task SetOrientationTask(CardOrientation orientation, double msDuration, double msBeginTime)
        {
            if (orientation == _myOrientation)
            {
                return null;
            }

            SetupFlipAnimation(orientation, msDuration, msBeginTime);
            return sbFlip.ToTask();
        }

        private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Highlight = true;
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            Highlight = false;
        }

        public static int CompareCardsByRank(CardCtrl x, CardCtrl y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }

            // If x is not null... 
            // 
            if (y == null)
            {
                // ...and y is null, x is greater.
                return 1;
            }

            return x.Rank - y.Rank;
        }

        #region PROPERTIES

        public bool Highlight
        {
            get => _highlightBorder.Visibility == Visibility.Visible;
            set
            {
                if (value && HighlightCards)
                {
                    _highlightBorder.Visibility = Visibility.Visible;
                }
                else
                {
                    _highlightBorder.Visibility = Visibility.Collapsed;
                }
            }
        }

        public bool ShowDebugInfo
        {
            get => DebugGrid.Visibility == Visibility.Visible;
            set
            {
                var vis = Visibility.Visible;
                if (!value)
                {
                    vis = Visibility.Collapsed;
                }

                DebugGrid.Visibility = vis;
            }
        }


        public bool FaceDown
        {
            get => Orientation == CardOrientation.FaceDown;
            set
            {
                Orientation = value ? CardOrientation.FaceDown : CardOrientation.FaceUp;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        ///     sets the orientation -- NOT animated!
        /// </summary>
        public CardOrientation Orientation
        {
            get => _myOrientation;
            set
            {
                _myOrientation = value;
                if (_myOrientation == CardOrientation.FaceUp)
                {
                    _ppFrontGrid.RotationY = 0;
                    _ppBackGrid.RotationY = -90;
                }
                else
                {
                    _ppFrontGrid.RotationY = 90;
                    _ppBackGrid.RotationY = 0;
                }

                NotifyPropertyChanged();
            }
        }

        internal void BoostZindex()
        {
            var zIndex = Canvas.GetZIndex(this);
            zIndex += 1000;

            if (zIndex > 2000)
            {
                zIndex -= 1000;
            }

            //   Debug.WriteLine($"Card:{this.CardName} New ZIndex:{ZIndex}" );
            Canvas.SetZIndex(this, zIndex);
        }


#pragma warning disable IDE1006 // Naming Styles
        public int ZIndex
#pragma warning restore IDE1006 // Naming Styles
        {
            get { return Canvas.GetZIndex(this); }
            set
            {
                Canvas.SetZIndex(this, value);
                NotifyPropertyChanged();
            }
        }


        internal void Reset()
        {
            _daScaleCardX.To = 1.0;
            _daScaleCardY.To = 1.0;
            _sbScaleCard.Duration = new Duration(TimeSpan.FromMilliseconds(0));
            _sbScaleCard.Begin();
            AnimateToAsync(new Point(0, 0), false, 0);

            Selected = false;
        }

        #endregion
    }
}