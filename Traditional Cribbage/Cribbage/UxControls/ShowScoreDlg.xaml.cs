using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Cards;
using CardView;
using LongShotHelpers;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage.UxControls
{
    public sealed partial class ShowScoreDlg : UserControl
    {
        ObservableCollection<CardCtrl> _cardList = new ObservableCollection<CardCtrl>();
        private List<Score> _scores = null;
        private int _scoreCount = 0;
        public ShowScoreDlg()
        {
            this.InitializeComponent();
        }

        public ShowScoreDlg(IEnumerable<Card> hand, Card sharedCard, int total, List<Score> scores)
        {
            this.InitializeComponent();
            foreach (var card in hand)
            {
                _cardList.Add(new CardCtrl(card, false));

            }

            _cardList.Add(new CardCtrl(sharedCard, false));
            _scores = scores;
            HighlightCardForScore();
            _tbTotalScore.Text = $"Total Score: {total}";
        }

        public async Task WaitForClose()
        {
            await _btnClose.WhenClicked();
        }

        private void OnPreviousScore(object sender, RoutedEventArgs e)
        {
            _scoreCount = Math.Max(0, _scoreCount - 1);
            HighlightCardForScore();
        }

        private void HighlightCardForScore()
        {
            if (_scores.Count > 0)
            {
                foreach (var c in _cardList)
                {
                    c.Selected = false;
                }

                var score = _scores[_scoreCount];

                foreach (var card in score.Cards)
                {
                    CardNameToCard(card.CardName).Selected = true;
                }

                SetScoreMessage(score);
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
        private CardCtrl CardNameToCard(CardNames name)
        {
            foreach (var card in _cardList)
            {
                if (card.CardName == name)
                    return card;
            }

            throw new Exception("why isn't this card here?");
        }

        private void OnNextScore(object sender, RoutedEventArgs e)
        {
            _scoreCount = Math.Min(_scores.Count - 1, _scoreCount + 1);
            HighlightCardForScore();
        }

        private void SetScoreMessage(Score score)
        {
            _tbScore.Text = $"{CardScoring.ScoreDescription[(int)score.ScoreName]} for {score.Value}";
        }
    }
}
