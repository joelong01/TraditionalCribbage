using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using LongShotHelpers;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public sealed partial class TraditionalBoard : UserControl
    {
        //private Brush _scoreBrush = (Brush)App.Current.Resources["SelectColor"];
        private Brush _scoreBrush = new SolidColorBrush(Colors.Red);

        public TraditionalBoard()
        {
            InitializeComponent();
            DataContext = this;
            //if (StaticHelpers.IsInVisualStudioDesignMode)
            //     HideButtonsAsync();
        }


        public async void ResetAsync()
        {
            await _board.Reset();
        }


        public async Task Reset()
        {
            await _board.Reset();
        }


        private void ButtonDownScore_Click(object sender, RoutedEventArgs e)
        {
            var scoreDelta = Convert.ToInt32(_tbScoreToAdd.Text);
            if (scoreDelta < 0) scoreDelta = 0;
            if (scoreDelta > 0)
            {
                _board.HighlightPeg(PlayerType.Player, _board.PlayerFrontScore + scoreDelta, false);
                scoreDelta -= 1;
                _tbScoreToAdd.Text = scoreDelta.ToString();
            }
        }

        private void ButtonUpScore_Click(object sender, RoutedEventArgs e)
        {
            var scoreDelta = Convert.ToInt32(_tbScoreToAdd.Text);
            scoreDelta += 1;
            if (scoreDelta > 29) scoreDelta = 29;
            _tbScoreToAdd.Text = scoreDelta.ToString();
            _board.HighlightPeg(PlayerType.Player, _board.PlayerFrontScore + scoreDelta, true);
        }


        private void DumpScores(PlayerType playerType, int scoreDelta, [CallerMemberName] string caller = "")
        {
            //PegScore pegScore = _scorePlayer;
            //if (playerType == PlayerType.Computer)
            //{
            //    pegScore = _scoreComputer;
            //}

            //  MainPage.LogTrace.TraceMessageAsync(String.Format("[{0}] {1}: Front={2} Back={3} Add={4}", caller, playerType, pegScore.Score2, pegScore.Score1, scoreDelta));
        }

        public void HighlightScore(PlayerType player, int score, int count, bool highlight)
        {
            var start = score + 1;
            for (var i = start; i < start + 30; i++)
                if (i < start + count)
                    _board.HighlightPeg(player, i, highlight);
                else
                    _board.HighlightPeg(player, i,
                        false); // this way if you went down, we always turn off the highlight
        }


        public async Task<int> HighlightScoreAndWaitForContinue(int actualScore, bool autosetScore)
        {
            var maxHighlight = 0;

            if (autosetScore)
            {
                HighlightScore(PlayerType.Player, _board.PlayerFrontScore + actualScore,
                    Convert.ToInt32(_tbScoreToAdd.Text),
                    false); //if the player guessed too high, need to reset those back to normal
                _tbScoreToAdd.Text = actualScore.ToString();
                maxHighlight = actualScore;
                HighlightScore(PlayerType.Player, _board.PlayerFrontScore, actualScore, true);
            }
            else
            {
                maxHighlight = Convert.ToInt32(_tbScoreToAdd.Text);
                HighlightScore(PlayerType.Player, _board.PlayerFrontScore, maxHighlight, true);
            }

            var tcs = new TaskCompletionSource<object>();

            void OnCompletion(object _, RoutedEventArgs args)
            {
                var scoreDelta = Convert.ToInt32(_tbScoreToAdd.Text);
                DumpScores(PlayerType.Player, scoreDelta);
                tcs.SetResult(null);
            }

            try
            {
                _btnAccept.Click += OnCompletion;

                ShowButtons();
                await tcs.Task;
                return Convert.ToInt32(_tbScoreToAdd.Text);
            }
            finally
            {
                _btnAccept.Click -= OnCompletion;
                HideButtonsAsync();
                HighlightScore(PlayerType.Player, _board.PlayerFrontScore, maxHighlight, false);
            }
        }

        public void ShowButtons()
        {
            foreach (DoubleAnimation da in _sbAnimateSetScore.Children)
            {
                da.Duration = TimeSpan.FromMilliseconds(50);
                da.To = 1;
            }

            _sbAnimateSetScore.Begin();
        }

        public async Task HideButtons()
        {
            foreach (DoubleAnimation da in _sbAnimateSetScore.Children)
            {
                da.Duration = TimeSpan.FromMilliseconds(500);
                da.To = 0;
            }

            await _sbAnimateSetScore.ToTask();
        }

        public void HideButtonsAsync()
        {
            foreach (DoubleAnimation da in _sbAnimateSetScore.Children)
            {
                da.Duration = TimeSpan.FromMilliseconds(50);
                da.To = 0;
            }

            _sbAnimateSetScore.Begin();
        }


        public List<Task> AnimateScore(PlayerType playerType, int score)
        {
            return _board.AnimateScore(playerType, score, false);
        }

        public void TraceBackPegPosition()
        {
            _board.TraceBackPegPosition();
        }


        public void AnimateScoreAsync(PlayerType player, int scoreToAdd)
        {
            _board.AnimateScore(player, scoreToAdd, true);
        }


        public (int computerBackScore, int computerScore, int playerBackScore, int playerScore) GetScores()
        {
            return (_board.ComputerBackScore, _board.ComputerFrontScore, _board.PlayerBackScore, _board.PlayerFrontScore
                );
        }
    }
}