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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cards
{

    public delegate void CardSelectionChangedDelegate(CardCtrl card, bool selected);

    public enum Suit { Uninitialized = 0, Clubs = 1, Diamonds = 2, Hearts = 3, Spades = 4 };
    public enum CardOrientation { FaceDown, FaceUp };
    public enum CardNames
    {
        AceOfClubs = 0, TwoOfClubs = 1, ThreeOfClubs = 2, FourOfClubs = 3, FiveOfClubs = 4, SixOfClubs = 5, SevenOfClubs = 6, EightOfClubs = 7, NineOfClubs = 8, TenOfClubs = 9, JackOfClubs = 10, QueenOfClubs = 11, KingOfClubs = 12,
        AceOfDiamonds = 13, TwoOfDiamonds = 14, ThreeOfDiamonds = 15, FourOfDiamonds = 16, FiveOfDiamonds = 17, SixOfDiamonds = 18, SevenOfDiamonds = 19, EightOfDiamonds = 20, NineOfDiamonds = 21, TenOfDiamonds = 22, JackOfDiamonds = 23, QueenOfDiamonds = 24, KingOfDiamonds = 25,
        AceOfHearts = 26, TwoOfHearts = 27, ThreeOfHearts = 28, FourOfHearts = 29, FiveOfHearts = 30, SixOfHearts = 31, SevenOfHearts = 32, EightOfHearts = 33, NineOfHearts = 34, TenOfHearts = 35, JackOfHearts = 36, QueenOfHearts = 37, KingOfHearts = 38,
        AceOfSpades = 39, TwoOfSpades = 40, ThreeOfSpades = 41, FourOfSpades = 42, FiveOfSpades = 43, SixOfSpades = 44, SevenOfSpades = 45, EightOfSpades = 46, NineOfSpades = 47, TenOfSpades = 48, JackOfSpades = 49, QueenOfSpades = 50, KingOfSpades = 51,
        BlackJoker = 52, RedJoker = 53, BackOfCard = 54
    };

    public enum CardOrdinal
    {
        Uninitialized = 0, Ace = 1, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King
    };

    public enum Location { Unintialized, Deck, Discarded, Computer, Player, Crib };

    public sealed partial class CardCtrl : UserControl, INotifyPropertyChanged
    {



        string _sClub = "§";
        string _sDiamond = "¨";
        string _sHearts = "©";
        string _sSpade = "ª";

        CardOrientation _myOrientation = CardOrientation.FaceUp;
        private int _index = 0;         // the index into the array of 52 cars (0...51)        
        private int _rank = 0;          // the number of the card in the suit -- e.g. 1...13 for A...K - used for counting runs and sorting                
        SolidColorBrush _red = new SolidColorBrush(Colors.DarkRed);
        SolidColorBrush _black = new SolidColorBrush(Colors.Black);
        CardOrdinal _cardOrdinal = CardOrdinal.Uninitialized;
        Suit _suit = Suit.Uninitialized;

        //public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register("Suit", typeof(Suit), typeof(CardCtrl), null);
        //public static readonly DependencyProperty CardOrdinalProperty = DependencyProperty.Register("CardOrdinal", typeof(CardOrdinal), typeof(CardCtrl), null);
        private bool _highlightCards = false;

        Location _location = Location.Unintialized;

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

        //
        //  these are the properties that we save/load
        private List<string> _savedProperties = new List<string> { "FaceDown", "Suit", "Rank" };

        //
        //  events
        public event PropertyChangedEventHandler PropertyChanged;
        public event CardSelectionChangedDelegate CardSelectionChanged;

        public CardCtrl()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        public override string ToString()
        {
            return String.Format($"{CardOrdinal} of {Suit}\n {Orientation}\n Rank: {Rank}\n Value: {Value}\n Owner: {Owner}\n zIndex: {Canvas.GetZIndex(this)}\n Location: {Location}");
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

            Point to = new Point();
            to.X = (double)MoveCardDoubleAnimationX.To + delta.X;
            to.Y = (double)MoveCardDoubleAnimationY.To + delta.Y;
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

        /// <summary>
        /// value for countinge (1..10)
        /// </summary>

        public int Value
        {
            get
            {
                if (Rank <= 10)
                    return Rank;

                return 10;
            }
        }
        /// <summary>
        /// same use as Orientation, but easier to save/load
        /// </summary>

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

        /// <summary>
        /// used where runs and order matters (1...13)
        /// </summary>

        public int Rank
        {
            get
            {
                return _rank;

            }
            set
            {
                _rank = value;
                NotifyPropertyChanged();


                _cardOrdinal = (CardOrdinal)value;
                UpdateCardLayout(_cardOrdinal);
                NotifyPropertyChanged();
                NotifyPropertyChanged("CardName");
                NotifyPropertyChanged("Value");
                NotifyPropertyChanged("CardOrdinal");


            }
        }
        /// <summary>
        /// the index into the unordered card deck
        /// </summary>

        public int Index
        {
            get
            {
                return _index;
            }
            set
            {

                _index = value;
                NotifyPropertyChanged();
            }
        }






        public CardNames CardName
        {
            get
            {
                return (CardNames)(((int)(Suit - 1) * 13 + Rank - 1));

            }
            set
            {
                //
                //  given a number of 0-51, set the suit and the rank

                if ((int)value < 0 || (int)value > 51)
                    throw new InvalidDataException($"The value {(int)value} is an invalid CardNumber");

                int val = (int)value;
                Suit = (Suit)((int)(val / 13) + 1);
                Rank = val % 13 + 1;
            }
        }

        public int zIndex
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



        //public string Glyph
        //{
        //    get { return (string)GetValue(GlyphProperty); }
        //    set { SetValue(GlyphProperty, value); NotifyPropertyChanged(); }
        //}

        public CardOrdinal CardOrdinal
        {
            get
            {

                try
                {
                    return _cardOrdinal;
                }
                catch
                {
                    return CardOrdinal.Uninitialized;
                }



            }
            set
            {
                _cardOrdinal = value;
                UpdateCardLayout(value);
                _rank = (int)value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("CardName");
                NotifyPropertyChanged("Rank");
            }
        }
        public Suit Suit
        {
            get
            {
                try
                {
                    return _suit;
                }
                catch
                {
                    return Suit.Diamonds;
                }
            }
            set
            {
                _suit = value;
                SetSuit(value);
                NotifyPropertyChanged();
                NotifyPropertyChanged("CardName");
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
                if (this.Suit == Suit.Clubs || this.Suit == Suit.Spades)
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
        public static int CompareCardsByIndex(CardCtrl x, CardCtrl y)
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

                    //
                    //  sorted largest to smallest for easier destructive iterations
                    return (y.Index - x.Index);


                }
            }


        }
    }
}
