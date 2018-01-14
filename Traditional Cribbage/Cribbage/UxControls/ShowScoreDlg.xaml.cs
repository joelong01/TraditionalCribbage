using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Cards;
using CardView;
using LongShotHelpers;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage.UxControls
{
    public sealed partial class ShowScoreDlg
    {
        private const double ANIMATION_SPEED = 250;
        private readonly ObservableCollection<CardCtrl> _cardList = new ObservableCollection<CardCtrl>();
        private readonly List<Score> _scores;
        private int _scoreCount;
        public ShowScoreDlg()
        {
            this.InitializeComponent();
        }

        public ShowScoreDlg(IEnumerable<Card> hand, Card sharedCard, int total, List<Score> scores)
        {
            this.InitializeComponent();
            foreach (var card in hand)
            {
                var newCard = new CardCtrl(card, false);
                _cardList.Add(newCard);

            }
            _cardList.Add(new CardCtrl(sharedCard, false));

            _scores = scores;
            var ignored = HighlightCardForScore(true);
            _tbTotalScore.Text = $"Total Score: {total}";
        }

        public async Task WaitForClose()
        {
            await _btnClose.WhenClicked();
        }

        private async void OnPreviousScore(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;
                _scoreCount = Math.Max(0, _scoreCount - 1);
                await HighlightCardForScore(false);
            }
            finally
            {
                EnableOrDisableButtons();
            }
        }

        private async void OnNextScore(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;
                _scoreCount = Math.Min(_scores.Count - 1, _scoreCount + 1);
                await HighlightCardForScore(true);
            }
            finally
            {
                EnableOrDisableButtons();
            }
        }

        private void EnableOrDisableButtons()
        {
            _btnPrev.IsEnabled = (_scoreCount != 0);
            _btnNext.IsEnabled = (_scoreCount != _scores.Count - 1);
        }

        private async Task HighlightCardForScore(bool next)
        {
            if (_scores.Count > 0)
            {
                foreach (var c in _cardList)
                {
                    c.HighlightCard(.8, .5, ANIMATION_SPEED);
                }

                var score = _scores[_scoreCount];

                foreach (var card in score.Cards)
                {
                    CardNameToCard(card.CardName).HighlightCard(1.0, 1.0, ANIMATION_SPEED);
                }

                await SetScoreMessage(score, next);
                _btnPrev.IsEnabled = (_scoreCount != 0);
                _btnNext.IsEnabled = (_scoreCount != _scores.Count - 1);
            }
            else
            {
                _tbScore.Text = "No Score";
                _btnPrev.IsEnabled = false;
                _btnNext.IsEnabled = false;
            }

        }
        private CardCtrl CardNameToCard(CardName name)
        {
            foreach (var card in _cardList)
            {
                if (card.CardName == name)
                    return card;
            }

            throw new Exception("why isn't this card here?");
        }


        private Score _lastScore;
        private int _runningScore;

        private async Task SetScoreMessage(Score score, bool next)
        {
            if (next)
            {
                _runningScore += score.Value;
            }
            else
            {
                _runningScore -= _lastScore.Value;

            }

            _daOpacity.To = 0;
            _daOpacity.Duration = TimeSpan.FromMilliseconds(ANIMATION_SPEED);
            await _sbOpacity.ToTask();
            switch (score.ScoreName)
            {
                case ScoreName.Run:
                case ScoreName.Flush:
                    _tbScore.Text = $"{CardScoring.ScoreDescription[(int)score.ScoreName]} of {score.Value} for {_runningScore}";
                    break;
                default:
                    _tbScore.Text = $"{CardScoring.ScoreDescription[(int)score.ScoreName]} for {_runningScore}";
                    break;
            }

            _daOpacity.To = 1.0;
            _sbOpacity.Begin();
            _lastScore = score;
        }
    }
}
