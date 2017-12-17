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
using System.Diagnostics;
using Cards;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace CardView
{
    public enum CardOrientation { FaceDown, FaceUp };
    public delegate void CardSelectionChangedDelegate(CardCtrl card, bool selected);
    public enum Location { Unintialized, Deck, Discarded, Computer, Player, Crib };

    public sealed partial class CardCtrl : UserControl, INotifyPropertyChanged
    {



        string _sClub = "§";
        string _sDiamond = "¨";
        string _sHearts = "©";
        string _sSpade = "ª";

        CardOrientation _myOrientation = CardOrientation.FaceUp;
        SolidColorBrush _red = new SolidColorBrush(Colors.DarkRed);
        SolidColorBrush _black = new SolidColorBrush(Colors.Black);
        private bool _highlightCards = false;
        Location _location = Location.Unintialized;

        Card _card = null;

        public Card Card
        {
            get
            {
                return _card;
            }
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
                        SetSuit(_card.Suit);
                        UpdateCardLayout(_card.CardOrdinal);
                    }
                }

                return (CardNames)GetValue(CardNameProperty);


            }
            set { SetValue(CardNameProperty, value); }
        }
        private static void CardNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CardCtrl depPropClass = d as CardCtrl;
            CardNames depPropValue = (CardNames)e.NewValue;
            depPropClass.SetCardName(depPropValue);
        }
        private void SetCardName(CardNames value)
        {
            if (_card == null)
            {
                _card = new Card(value);
                SetSuit(_card.Suit);
                UpdateCardLayout(_card.CardOrdinal);
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

        public CardCtrl(Card card)
        {
            this.InitializeComponent();
            this.DataContext = this;
            _card = card;
            CardName = _card.CardName;
            SetSuit(_card.Suit);
            UpdateCardLayout(_card.CardOrdinal);

        }

        public CardCtrl()
        {
            this.InitializeComponent();
            this.DataContext = this;
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
            List<CardCtrl> CardUI = new List<CardCtrl>();
            Deck deck = new Deck(Environment.TickCount);
            List<Card> Cards = deck.GetCards(number);

            foreach (var card in Cards)
            {
                CardCtrl c = new CardCtrl(card)
                {
                    Width = 125,
                    Height = 175,
                    Margin = new Thickness(0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Owner = owner,
                    VerticalAlignment = VerticalAlignment.Top,
                    Orientation = CardOrientation.FaceDown,
                    ShowDebugInfo = false
                };



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
            return StaticHelpers.SerializeObject<CardCtrl>(this, _savedProperties, true);
        }

        public bool Deserialize(string s)
        {
            StaticHelpers.DeserializeObject<CardCtrl>(this, s, true);
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
        #region CARD_LAYOUT
        private void UpdateCardLayout(CardOrdinal cardOrdinal)
        {
            switch (cardOrdinal)
            {
                case CardOrdinal.Ace:
                    ShowAce();
                    break;
                case CardOrdinal.Two:
                    ShowTwo();
                    break;
                case CardOrdinal.Three:
                    ShowThree();
                    break;
                case CardOrdinal.Four:
                    ShowFour();
                    break;
                case CardOrdinal.Five:
                    ShowFive();
                    break;
                case CardOrdinal.Six:
                    ShowSix();
                    break;
                case CardOrdinal.Seven:
                    ShowSeven();
                    break;
                case CardOrdinal.Eight:
                    ShowEight();
                    break;
                case CardOrdinal.Nine:
                    ShowNine();
                    break;
                case CardOrdinal.Ten:
                    ShowTen();
                    break;
                case CardOrdinal.Jack:
                    ShowFaceCard("J", false);
                    break;
                case CardOrdinal.Queen:
                    ShowFaceCard("Q", true);
                    break;
                case CardOrdinal.King:
                    ShowFaceCard("K", true);
                    break;
                default:
                    break;
            }
        }
        private void SetTextLineBounds(bool tight)
        {
            TextLineBounds tlb = tight ? TextLineBounds.Tight : TextLineBounds.Full;
            _tbCardRankLowerRight.TextLineBounds = tlb;
            _tbCardRankUpperLeft.TextLineBounds = tlb;
        }
        private void ShowFaceCard(string s, bool tight)
        {
            HideAllGlyphs();
            SetTextLineBounds(tight);
            _vbFaceCard.Visibility = Visibility.Visible;
            _tbFaceCard.Text = s;
            _tbCardRankLowerRight.Text = s;
            _tbCardRankUpperLeft.Text = s;
        }
        private void ShowTen()
        {
            HideAllGlyphs();


            _vb1.Visibility = Visibility.Visible;
            _vb3.Visibility = Visibility.Visible;
            _vb4.Visibility = Visibility.Visible;
            _vb5.Visibility = Visibility.Visible;
            _vbThreeTwo.Visibility = Visibility.Visible;

            _vb6.Visibility = Visibility.Visible;
            _vb7.Visibility = Visibility.Visible;
            _vb8.Visibility = Visibility.Visible;
            _vb9.Visibility = Visibility.Visible;
            _vb10.Visibility = Visibility.Visible;




            _tbCardRankLowerRight.Text = "10";
            _tbCardRankUpperLeft.Text = "10";
        }
        private void ShowNine()
        {
            HideAllGlyphs();
            _vb1.Visibility = Visibility.Visible;
            _vb3.Visibility = Visibility.Visible;
            _vb5.Visibility = Visibility.Visible;
            _vbThreeTwo.Visibility = Visibility.Visible;
            _vb6.Visibility = Visibility.Visible;
            _vb7.Visibility = Visibility.Visible;
            _vb8.Visibility = Visibility.Visible;
            _vb9.Visibility = Visibility.Visible;
            _vbCenter.Visibility = Visibility.Visible;



            _tbCardRankLowerRight.Text = "9";
            _tbCardRankUpperLeft.Text = "9";
        }
        private void ShowEight()
        {
            HideAllGlyphs();
            _vb1.Visibility = Visibility.Visible;
            _vb3.Visibility = Visibility.Visible;
            _vb4.Visibility = Visibility.Visible;
            _vb8.Visibility = Visibility.Visible;
            _vb9.Visibility = Visibility.Visible;
            _vb12.Visibility = Visibility.Visible;
            _vb13.Visibility = Visibility.Visible;
            _vb10.Visibility = Visibility.Visible;



            _tbCardRankLowerRight.Text = "8";
            _tbCardRankUpperLeft.Text = "8";
        }
        private void ShowSeven()
        {
            HideAllGlyphs();
            _vb1.Visibility = Visibility.Visible;
            _vb3.Visibility = Visibility.Visible;
            _vb8.Visibility = Visibility.Visible;
            _vb9.Visibility = Visibility.Visible;
            _vb12.Visibility = Visibility.Visible;
            _vb13.Visibility = Visibility.Visible;
            //  _vb10.Visibility = Visibility.Visible;
            _vbCenter.Visibility = Visibility.Visible;

            _tbCardRankLowerRight.Text = "7";
            _tbCardRankUpperLeft.Text = "7";
        }
        private void ShowSix()
        {
            HideAllGlyphs();
            _vb1.Visibility = Visibility.Visible;
            _vb3.Visibility = Visibility.Visible;
            _vb8.Visibility = Visibility.Visible;
            _vb9.Visibility = Visibility.Visible;
            _vb12.Visibility = Visibility.Visible;
            _vb13.Visibility = Visibility.Visible;


            _tbCardRankLowerRight.Text = "6";
            _tbCardRankUpperLeft.Text = "6";
        }
        private void ShowFive()
        {
            HideAllGlyphs();
            _vb1.Visibility = Visibility.Visible;
            _vb3.Visibility = Visibility.Visible;
            _vb8.Visibility = Visibility.Visible;
            _vb9.Visibility = Visibility.Visible;
            _vbCenter.Visibility = Visibility.Visible;
            _tbCardRankLowerRight.Text = "5";
            _tbCardRankUpperLeft.Text = "5";
        }
        private void ShowFour()
        {
            HideAllGlyphs();
            _vb1.Visibility = Visibility.Visible;
            _vb3.Visibility = Visibility.Visible;
            _vb8.Visibility = Visibility.Visible;
            _vb9.Visibility = Visibility.Visible;
            _tbCardRankLowerRight.Text = "4";
            _tbCardRankUpperLeft.Text = "4";
        }
        private void ShowThree()
        {
            HideAllGlyphs();
            _vb2.Visibility = Visibility.Visible;
            _vb11.Visibility = Visibility.Visible;
            _vbCenter.Visibility = Visibility.Visible;
            _tbCardRankLowerRight.Text = "3";
            _tbCardRankUpperLeft.Text = "3";
        }
        private void HideAllGlyphs()
        {
            foreach (var el in GlyphGrid.Children)
            {
                if (el.GetType() == typeof(Viewbox))
                {
                    el.Visibility = Visibility.Collapsed;
                }
            }

            SetTextLineBounds(true);
        }
        private void ShowTwo()
        {
            HideAllGlyphs();

            _vb4.Visibility = Visibility.Visible;
            _vb10.Visibility = Visibility.Visible;
            _tbCardRankLowerRight.Text = "2";
            _tbCardRankUpperLeft.Text = "2";
        }
        private void ShowAce()
        {
            HideAllGlyphs();
            _vbCenter.Visibility = Visibility.Visible;
            _tbCardRankLowerRight.Text = "A";
            _tbCardRankUpperLeft.Text = "A";
        }
        #endregion




        public string GetGlyph(Suit suit)
        {

            string ret = _sClub;
            switch (suit)
            {
                case Suit.Clubs:
                    ret = _sClub;
                    break;
                case Suit.Hearts:
                    ret = _sHearts;
                    break;
                case Suit.Diamonds:
                    ret = _sDiamond;
                    break;
                case Suit.Spades:
                    ret = _sSpade;
                    break;
                default:
                    break;
            }
            return ret;

        }

        private SolidColorBrush SuitBrush
        {
            get
            {
                if (_card?.Suit == Suit.Clubs || _card?.Suit == Suit.Spades)
                    return _black;

                return _red;
            }
        }

        private void SetSuit(Suit suit)
        {
            string glyph = GetGlyph(suit);
            SolidColorBrush br = this.SuitBrush;
            foreach (var el in GlyphGrid.Children)
            {
                if (el.GetType() == typeof(Viewbox))
                {
                    Viewbox vb = (Viewbox)el;
                    if (vb.Child.GetType() == typeof(TextBlock))
                    {
                        TextBlock tb = vb.Child as TextBlock;
                        tb.Text = glyph;
                        tb.Foreground = br;
                    }
                }
            }

            _tbCardRankUpperLeft.Foreground = br;
            _tbSuitUpperLeft.Foreground = br;
            _tbSuitUpperLeft.Text = glyph;

            _tbCardRankLowerRight.Foreground = br;
            _tbSuitLowerRight.Foreground = br;
            _tbSuitLowerRight.Text = glyph;
        }

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

        Owner _owner = Owner.Uninitialized;
        public Owner Owner
        {
            get
            {
                return _owner;
            }
            set
            {
                if (value != _owner)
                {
                    _owner = value;
                    NotifyPropertyChanged();
                }
            }
        }

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
