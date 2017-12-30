using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Cribbage;
using LongShotHelpers;
using Cards;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace CardView
{
    public enum CardOrientation { FaceDown, FaceUp };
    public delegate void CardSelectionChangedDelegate(CardCtrl card, bool selected);
    public enum Location { Unintialized, Deck, Discarded, Computer, Player, Crib };

    public sealed partial class CardCtrl : UserControl, INotifyPropertyChanged
    {



    

        CardOrientation _myOrientation = CardOrientation.FaceUp;
        SolidColorBrush _red = new SolidColorBrush(Colors.DarkRed);
        SolidColorBrush _black = new SolidColorBrush(Colors.Black);
        private bool _highlightCards = false;
        Location _location = Location.Unintialized;

        public static Dictionary<CardNames, SvgImageSource> _cardNameToSvgImage = new Dictionary<CardNames, SvgImageSource>();


        Card _card = null;


        public CardCtrl()
        {
            this.InitializeComponent();
            this.DataContext = this;            
            InitializeCards();
            
        }

        public CardCtrl(Card card)
        {
            this.InitializeComponent();
            this.DataContext = this;
            _card = card;
            CardName = _card.CardName;

            InitializeCards();

        }

        public double ZoomRatio { get; set; } = 1.0;

        private static void InitializeCards()
        {
            if (_cardNameToSvgImage.Count == 0)
            {
                foreach (CardNames cardName in Enum.GetValues(typeof(CardNames)))
                {
                    if (cardName == CardNames.Uninitialized)
                        continue;

                    string s = $"ms-appx:///Assets/Cards/{cardName}.svg";
                    var uri = new Uri(s);
                    var file = StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().Result;
                    SvgImageSource svgImage = new SvgImageSource();
                    using (var stream = file.OpenStreamForReadAsync().Result)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        var rd = stream.AsRandomAccessStream();
                        svgImage.RasterizePixelHeight = 175; // _cardImage.ActualHeight;
                        svgImage.RasterizePixelWidth = 125; // _cardImage.ActualWidth;
                        svgImage.SetSourceAsync(rd).AsTask();
                    }

                    _cardNameToSvgImage[cardName] = svgImage;

                }
            }
        }

        public Card Card
        {
            get
            {
                return _card;
            }
        }


        public void SetImageForCard(CardNames cardName)
        {
            string s = $"ms-appx:///Assets/Cards/{cardName}.svg";
            var uri = new Uri(s);
            var file = StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().Result;
            SvgImageSource svgImage = new SvgImageSource();

            double cardWidth = FrontGrid.ColumnDefinitions[1].ActualWidth;
            double cardHeight = FrontGrid.RowDefinitions[1].ActualHeight;

            cardWidth =  115 * ZoomRatio;
            cardHeight = 165 * ZoomRatio;
            
            using (var stream = file.OpenStreamForReadAsync().Result)
            {
                stream.Seek(0, SeekOrigin.Begin);
                var rd = stream.AsRandomAccessStream();
                svgImage.RasterizePixelHeight = cardHeight;
                svgImage.RasterizePixelWidth = cardWidth;
                svgImage.SetSourceAsync(rd).AsTask();
            }

            _frontImage.Source = svgImage;
        }

        public static readonly DependencyProperty CardNameProperty = DependencyProperty.Register("CardName", typeof(CardNames), typeof(Card), new PropertyMetadata(CardNames.AceOfSpades, CardNameChanged));
        public CardNames CardName
        {
            get
            {
                if (_card != null)
                {
                    if ((CardNames)GetValue(CardNameProperty) != _card.CardName)
                    {
                        CardName = _card.CardName;
                                                
                    }
                }

                return (CardNames)GetValue(CardNameProperty);


            }
            set { SetValue(CardNameProperty, value); }
        }
        private static void CardNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CardCtrl cardCtrl = d as CardCtrl;
            CardNames cardName = (CardNames)e.NewValue;

            cardCtrl.SetImageForCard(cardName);


        }
        private void SetCardName(CardNames value)
        {
            if (_card == null)
            {
                _card = new Card(value);
                                
            }
            else
            {
                _card.CardName = value;
            }
        }



        public int Rank
        {
            get
            {
                if (_card == null)
                    return 0;

                return _card.Rank;
            }
        }

        public int Index
        {
            get
            {
                if (_card == null)
                    return 0;

                return _card.Index;
            }
        }

        public int Value
        {
            get
            {
                if (_card == null)
                    return 0;

                return _card.Value;
            }
        }

       

      


        public Location Location
        {
            get
            {
                return _location;
            }
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

        /// <summary>
        ///     This is where the cards are created...
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static List<CardCtrl> GetCards(int number, Owner owner)
        {
            
            Deck deck = new Deck(Environment.TickCount);
            List<Card> Cards = deck.GetCards(number, owner);
            return CreateCardCtrlFromListOfCards(Cards);
            
        }

        public static List<CardCtrl> CreateCardCtrlFromListOfCards( List<Card> Cards)
        {
            List<CardCtrl> CardUI = new List<CardCtrl>();
            foreach (var card in Cards)
            {
                CardCtrl c = new CardCtrl(card)
                {
                    Width = 125,
                    Height = 175,
                    Margin = new Thickness(0),
                    HorizontalAlignment = HorizontalAlignment.Left,                    
                    VerticalAlignment = VerticalAlignment.Top,
                    Orientation = CardOrientation.FaceDown,
                    ShowDebugInfo = false,
                    IsEnabled = card.IsEnabled
                };

                card.Tag = (object)c;

                Grid.SetColumnSpan(c, 99); // should be enough...
                Grid.SetRowSpan(c, 99); // should be enough...
                CardUI.Add(c);
            }

            return CardUI;
        }

        //
        //  these are the properties that we save/load
        private List<string> _savedProperties = new List<string> { "FaceDown", "Suit", "Rank" };

        //
        //  events
        public event PropertyChangedEventHandler PropertyChanged;
        public event CardSelectionChangedDelegate CardSelectionChanged;


        public override string ToString()
        {
            if (_card == null) return "";
            return String.Format($"{_card.ToString()} Owner: {Owner}\n zIndex: {Canvas.GetZIndex(this)}\n Location: {Location}");
        }

        public string Info
        {
            get
            {
                return this.ToString();
            }
            set
            {

            }
        }

        public string Serialize()
        {
            
            return this.SerializeObject<CardCtrl>(_savedProperties);
        }

        public bool Deserialize(string s)
        {
            this.DeserializeObject<CardCtrl>(s);
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

        public new bool IsEnabled
        {
            get
            {
                return base.IsEnabled;
            }
            set
            {
                this.Opacity = value ? 1.0 : .5;
                base.IsEnabled = value;
            }
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

        public Point AnimationPosition
        {
            get
            {
                double x = (double)MoveCardDoubleAnimationX.To;
                double y = (double)MoveCardDoubleAnimationY.To;
                return new Point(x, y);
            }
            set
            {
                MoveCardDoubleAnimationX.To = value.X;
                MoveCardDoubleAnimationY.To = value.Y;
            }
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
                MoveCardDoubleAnimationAngle.To += 360;
        }
        /// <summary>
        /// This puts the card in the desired position directly.
        /// it also sets up the animation positions so that any delta animations will work correctly.
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

            Point to = new Point
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
        public double AnimateRotation
        {
            get
            {
                return (double)MoveCardDoubleAnimationAngle.To;
            }
            set
            {
                MoveCardDoubleAnimationAngle.To = value;
            }
        }



        internal void ResetZIndex()
        {
            int zIndex = Canvas.GetZIndex(this);


            while (zIndex > 1000)
            {
                zIndex -= 1000;
                //Debug.WriteLine("Resetting ZIndex.  Card: {0} ZIndex:{1}", this.CardName, zIndex - 500);
                Canvas.SetZIndex(this, zIndex);

            }

            //Debug.WriteLine($"Card:{this.CardName} New zIndex:{Canvas.GetZIndex(this)}");

        }

        #region PROPERTIES

        public bool Highlight
        {
            get
            {
                return (_highlightBorder.Visibility == Visibility.Visible);
            }
            set
            {
                if (value && _highlightCards)
                    _highlightBorder.Visibility = Visibility.Visible;
                else
                    _highlightBorder.Visibility = Visibility.Collapsed;
            }
        }

        public bool ShowDebugInfo
        {
            get
            {
                return (DebugGrid.Visibility == Visibility.Visible) ? true : false;
            }
            set
            {
                Visibility vis = Visibility.Visible;
                if (!value)
                    vis = Visibility.Collapsed;

                DebugGrid.Visibility = vis;
            }

        }



        public bool FaceDown
        {
            get
            {
                return this.Orientation == CardOrientation.FaceDown;
            }
            set
            {
                if (value)
                {
                    this.Orientation = CardOrientation.FaceDown;
                }
                else
                {
                    this.Orientation = CardOrientation.FaceUp;
                }
                NotifyPropertyChanged();
            }
        }
        /// <summary>
        /// sets the orientation -- NOT animated!
        /// </summary>
        public CardOrientation Orientation
        {

            get
            {
                return _myOrientation;
            }
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
            int zIndex = Canvas.GetZIndex(this);
            zIndex += 1000;

            if (zIndex > 2000) zIndex -= 1000;

            //   Debug.WriteLine($"Card:{this.CardName} New zIndex:{zIndex}" );
            Canvas.SetZIndex(this, zIndex);
        }



#pragma warning disable IDE1006 // Naming Styles
        public int zIndex
#pragma warning restore IDE1006 // Naming Styles
        {
            get
            {
                return Canvas.GetZIndex(this);
            }
            set
            {
                Canvas.SetZIndex(this, value);
                NotifyPropertyChanged();
            }
        }




        internal void Reset()
        {
            _daScaleCardX.To = 1.0; ;
            _daScaleCardY.To = 1.0;
            _sbScaleCard.Duration = new Duration(TimeSpan.FromMilliseconds(0));
            _sbScaleCard.Begin();
            AnimateToAsync(new Point(0, 0), false, 0);

            this.Selected = false;



        }

        #endregion
    

       

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SetupFlipAnimation(CardOrientation toOrientation, double msDuration, double msBeginTime)
        {

            bool flipToFaceUp = (_myOrientation == CardOrientation.FaceDown) ? true : false;

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

        public bool Selected
        {

            get
            {

                return (SelectedGrid.Visibility == Visibility.Visible) ? true : false;
            }
            set
            {
                bool currentlySelected = (SelectedGrid.Visibility == Visibility.Visible) ? true : false;


                if (value)
                {
                    SelectedGrid.Visibility = Visibility.Visible;
                }
                else
                {
                    SelectedGrid.Visibility = Visibility.Collapsed;
                }

                CardSelectionChanged?.Invoke(this, value);
            }
        }

        public bool HighlightCards
        {
            get
            {
                return _highlightCards;
            }

            set
            {
                _highlightCards = value;
            }
        }

        
        public Owner Owner
        {
            get
            {
                return _card.Owner;
            }
            set
            {
                if (value != _card.Owner)
                {
                    _card.Owner = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool CurrentRun
        { get
            {
                return (this.IsEnabled);
            }
        }

        public bool Counted { get; set; }

        public void SetOrientationAsync(CardOrientation orientation, double msDuration, double msBeginTime)
        {
            if (orientation == _myOrientation) return;
            SetupFlipAnimation(orientation, msDuration, msBeginTime);
            sbFlip.Begin();
        }

        public Task SetOrientationTask(CardOrientation orientation, double msDuration, double msBeginTime)
        {
            if (orientation == _myOrientation) return null;

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
                    // If x is null and y is null, they're 
                    // equal.  
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y 
                    // is greater.  
                    return -1;
                }
            }
            else
            {
                // If x is not null... 
                // 
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    // ...and y is not null, compare the  
                    // lengths of the two strings. 
                    // 
                    return x.Rank - y.Rank;


                }
            }


        }


    }
}
