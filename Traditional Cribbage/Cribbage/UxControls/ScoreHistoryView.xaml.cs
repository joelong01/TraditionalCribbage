using Cards;
using CardView;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;


namespace Cribbage
{
    public sealed partial class ScoreHistoryView : UserControl
    {
        List<Rectangle> _rectangles = new List<Rectangle>();
        List<TextBlock> _textBlocks = new List<TextBlock>();
        List<CardCtrl> _cards = new List<CardCtrl>();
        
        //
        //  state
        PlayerType _player;
        ScoreType _scoreType;
        ScoreInstance _score;
        int _index;
        int _total;
        int _actualScore;
        string _gameScore;


        public string Save()
        {

            //
            //  Score Instance and Cards use "," as a seperator.  This is level 2, use "|"

            //string s = String.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|", 
            //                        _player, _scoreType, _score.Save(), _index, _total, _actualScore, _gameScore, StaticHelpers.SerializeFromList(_cards));

            // return s;
            return "";
        }


        public ScoreHistoryView()
        {
            this.InitializeComponent();

            _rectangles.Add(_card0);
            _rectangles.Add(_card1);
            _rectangles.Add(_card2);
            _rectangles.Add(_card3);
            _rectangles.Add(_card4);
            _rectangles.Add(_card5);
            _rectangles.Add(_card6);
            _rectangles.Add(_card7);

            foreach (FrameworkElement el in LayoutRoot.Children)
            {
                try
                {
                    Type t = el.GetType();
                    if (t == typeof(Viewbox))
                    {
                        TextBlock tb = ((Viewbox)el).Child as TextBlock;                    
                        _textBlocks.Add((TextBlock)tb);


                    }
                }
                catch (Exception)
                {
                    
                }
            }

            this.DataContext = this;

        }

        // top text to look like: Computer  Counting Run of 3 
        // bottom text like 3 of 9 points

        public async Task PopulateGrid(List<CardCtrl> cards, ScoreInstance score, PlayerType player, ScoreType scoreType, int index, int total, int actualScore, string gameScore)
        {
            _cards = cards;
            _score = score;
            _player = player;
            _scoreType = scoreType;
            _index = index;
            _total = total;
            _actualScore = actualScore;
            _gameScore = gameScore;
            await PopulateGrid();
           
        }

      

        private async Task PopulateGrid()
        {
            string gameScore = _gameScore.Replace("&", "\n");
            _txtScore.Text = String.Format("{0} Points of {1}", _score.Score, _total);
            _txtScoreType.Text = _scoreType.ToString();
            _txtPlayer.Text = _player.ToString();
            _txtScoreDescription.Text = _score.Description;
            _txtGameScore.Text = gameScore;
            if (_player == PlayerType.Player)
            {
                _rectBackground.Fill = (ImageBrush)Application.Current.Resources["bmBurledMaple"];
                SetTextColor(Colors.Black);

            }
            else
            {
                _rectBackground.Fill = (ImageBrush)Application.Current.Resources["bmWalnut"];
                SetTextColor(Colors.White);
            }


            await AddCardImages(_cards, _scoreType);
            ShowScore(_cards, _score.Cards);
        }

        private void SetTextColor(Color color)
        {
            SolidColorBrush br = new SolidColorBrush(color);
            foreach (TextBlock tb in _textBlocks)
            {
                tb.Foreground = br;
            }
        }

        private void ShowScore(List<CardCtrl> cards, List<int> scoreCards)
        {

            foreach (Rectangle rect in _rectangles)
            {
                if (rect.Tag != null)
                {
                    if (scoreCards.Contains((int)rect.Tag))
                    {
                        Grid.SetRow(rect, 2);
                    }
                }
            }           
        }

        private async Task AddCardImages(List<CardCtrl> cards, ScoreType scoreType)
        {

            if (scoreType == ScoreType.Count)
            {
                for (int i = 0; i < _rectangles.Count; i++)
                {
                    if (i < cards.Count)
                    {
                        await SetCardToBitmap(cards[i], _rectangles[i]);
                    }
                    else
                    {
                        LayoutRoot.Children.Remove(_rectangles[i]);
                    }
                }

                return;
            }
            else if (scoreType == ScoreType.Hand || scoreType == ScoreType.Crib || scoreType == ScoreType.Cut)
            {
                for (int i = 0; i < _rectangles.Count; i++)
                {
                    if (i < 4)
                    {
                        await SetCardToBitmap(cards[i], _rectangles[i]);
                    }
                    else if (i == 6)
                    {
                        await SetCardToBitmap(cards[4], _rectangles[i]);
                    }
                    else
                    {
                        LayoutRoot.Children.Remove(_rectangles[i]);
                    }
                }

                return;
            }

            throw new NotSupportedException();

        }

        private async Task SetCardToBitmap(CardCtrl card, Rectangle rect)
        {
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(card, (int)rect.Width, (int)rect.Height);
            ImageBrush imageBrush = new ImageBrush();
            imageBrush.ImageSource = renderTargetBitmap;
            rect.Fill = imageBrush;
            rect.Tag = card.Index;
        }

         public async Task Animate(Point point)
         {
             _daX.To = point.X;
             _daY.To = point.Y;
             _daX.Duration = TimeSpan.FromMilliseconds(500);
             _daY.Duration = TimeSpan.FromMilliseconds(500 * point.Y / point.X);
             _daY.BeginTime = _daX.Duration.TimeSpan;

             await _sbAnimateAcrossScreen.ToTask();
         }

        public CompositeTransform Transform
        { 
            get
            {
                return _transform;
            }
        }

        public void EndAnimation()
        {
            _sbAnimateAcrossScreen.Stop();
            Transform.TranslateX = 0;
        }
    }
}
