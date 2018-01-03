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
using CardView;
using LongShotHelpers;

namespace Cribbage
{
    public sealed partial class ScoreHistoryView : UserControl
    {
        private int _actualScore;
        private List<CardCtrl> _cards = new List<CardCtrl>();
        private string _gameScore;
        private int _index;

        //
        //  state
        private PlayerType _player;
        private readonly List<Rectangle> _rectangles = new List<Rectangle>();
        private ScoreInstance _score;
        private ScoreType _scoreType;
        private readonly List<TextBlock> _textBlocks = new List<TextBlock>();
        private int _total;


        public ScoreHistoryView()
        {
            InitializeComponent();

            _rectangles.Add(_card0);
            _rectangles.Add(_card1);
            _rectangles.Add(_card2);
            _rectangles.Add(_card3);
            _rectangles.Add(_card4);
            _rectangles.Add(_card5);
            _rectangles.Add(_card6);
            _rectangles.Add(_card7);

            foreach (FrameworkElement el in LayoutRoot.Children)
                try
                {
                    var t = el.GetType();
                    if (t == typeof(Viewbox))
                    {
                        var tb = ((Viewbox) el).Child as TextBlock;
                        _textBlocks.Add(tb);
                    }
                }
                catch (Exception)
                {
                }

            DataContext = this;
        }

        public CompositeTransform Transform => _transform;


        public string Save()
        {
            //
            //  Score Instance and Cards use "," as a seperator.  This is level 2, use "|"

            //string s = String.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|", 
            //                        _player, _scoreType, _score.Save(), _index, _total, _actualScore, _gameScore, StaticHelpers.SerializeFromList(_cards));

            // return s;
            return "";
        }

        // top text to look like: Computer  Counting Run of 3 
        // bottom text like 3 of 9 points

        public async Task PopulateGrid(List<CardCtrl> cards, ScoreInstance score, PlayerType player,
            ScoreType scoreType, int index, int total, int actualScore, string gameScore)
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
            var gameScore = _gameScore.Replace("&", "\n");
            _txtScore.Text = string.Format("{0} Points of {1}", _score.Score, _total);
            _txtScoreType.Text = _scoreType.ToString();
            _txtPlayer.Text = _player.ToString();
            _txtScoreDescription.Text = _score.Description;
            _txtGameScore.Text = gameScore;
            if (_player == PlayerType.Player)
            {
                _rectBackground.Fill = (ImageBrush) Application.Current.Resources["bmBurledMaple"];
                SetTextColor(Colors.Black);
            }
            else
            {
                _rectBackground.Fill = (ImageBrush) Application.Current.Resources["bmWalnut"];
                SetTextColor(Colors.White);
            }


            await AddCardImages(_cards, _scoreType);
            ShowScore(_cards, _score.Cards);
        }

        private void SetTextColor(Color color)
        {
            var br = new SolidColorBrush(color);
            foreach (var tb in _textBlocks) tb.Foreground = br;
        }

        private void ShowScore(List<CardCtrl> cards, List<int> scoreCards)
        {
            foreach (var rect in _rectangles)
                if (rect.Tag != null)
                    if (scoreCards.Contains((int) rect.Tag))
                        Grid.SetRow(rect, 2);
        }

        private async Task AddCardImages(List<CardCtrl> cards, ScoreType scoreType)
        {
            if (scoreType == ScoreType.Count)
            {
                for (var i = 0; i < _rectangles.Count; i++)
                    if (i < cards.Count)
                        await SetCardToBitmap(cards[i], _rectangles[i]);
                    else
                        LayoutRoot.Children.Remove(_rectangles[i]);

                return;
            }

            if (scoreType == ScoreType.Hand || scoreType == ScoreType.Crib || scoreType == ScoreType.Cut)
            {
                for (var i = 0; i < _rectangles.Count; i++)
                    if (i < 4)
                        await SetCardToBitmap(cards[i], _rectangles[i]);
                    else if (i == 6)
                        await SetCardToBitmap(cards[4], _rectangles[i]);
                    else
                        LayoutRoot.Children.Remove(_rectangles[i]);

                return;
            }

            throw new NotSupportedException();
        }

        private async Task SetCardToBitmap(CardCtrl card, Rectangle rect)
        {
            var renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(card, (int) rect.Width, (int) rect.Height);
            var imageBrush = new ImageBrush
            {
                ImageSource = renderTargetBitmap
            };
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

        public void EndAnimation()
        {
            _sbAnimateAcrossScreen.Stop();
            Transform.TranslateX = 0;
        }
    }
}