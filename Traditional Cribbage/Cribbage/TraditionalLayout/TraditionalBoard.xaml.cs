using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation;
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
        
        public TraditionalBoard()
        {
            this.InitializeComponent();            
            this.DataContext = this;                     
        
          //  ShowButtons();
        }



        
       public async void ResetAsync()
        {
            await _board.Reset();

        }


        public async Task Reset()
        {
           await _board.Reset();
        }

       

        void Animate(PegControl peg, Point to, double duration, List<Task<object>> taskList, bool reletive, double angle)
        {

            
        }

    


      

        private void ButtonDownScore_Click(object sender, RoutedEventArgs e)
        {


            int scoreDelta = Convert.ToInt32(_tbScoreToAdd.Text);
            if (scoreDelta < 0) scoreDelta = 0;
            if (scoreDelta > 0)
            {
                _board.HighlightPeg(PlayerType.Player, _board.PlayerFrontScore + scoreDelta, false);              
                scoreDelta -= 1;
                _tbScoreToAdd.Text = scoreDelta.ToString();
            }


        }
        //
        //   TODO: PORT

        //private Brush _scoreBrush = (Brush)App.Current.Resources["SelectColor"];
        private Brush _scoreBrush = (Brush)new SolidColorBrush(Colors.Red);
        private void ButtonUpScore_Click(object sender, RoutedEventArgs e)
        {
            int scoreDelta = Convert.ToInt32(_tbScoreToAdd.Text);
            scoreDelta += 1;
            if (scoreDelta > 29) scoreDelta = 29;
            _tbScoreToAdd.Text = scoreDelta.ToString();
            _board.HighlightPeg(PlayerType.Player, _board.PlayerFrontScore + scoreDelta, true);            

        }

       

        private void DumpScores(PlayerType playerType, int scoreDelta, [CallerMemberName] String caller = "")
        {
            //PegScore pegScore = _scorePlayer;
            //if (playerType == PlayerType.Computer)
            //{
            //    pegScore = _scoreComputer;
            //}

          //  MainPage.LogTrace.TraceMessageAsync(String.Format("[{0}] {1}: Front={2} Back={3} Add={4}", caller, playerType, pegScore.Score2, pegScore.Score1, scoreDelta));

        }

        private void HighlightScore(int score, int count, bool highlight)
        {
            int start = score + 1;
            for (int i=start; i<start+count; i++)
            {
                _board.HighlightPeg(PlayerType.Player, i, highlight);    
            }
        }

        public async Task<int> ShowAndWaitForContinue(int actualScore)
        {
            //
            //  PORT TODO
            
          //  if (MainPage.Current.Settings.AutoSetScore)
          if (true)
            {
                HighlightScore(_board.PlayerFrontScore + actualScore, Convert.ToInt32(_tbScoreToAdd.Text), false);  //if the player guessed too high, need to reset those back to normal
                _tbScoreToAdd.Text = actualScore.ToString();
                HighlightScore(_board.PlayerFrontScore, actualScore, true);               
            }
            //else
            //{
            //    HighlightScore(_board.PlayerFrontScore, Convert.ToInt32(_tbScoreToAdd.Text), true);               
            //}

            var tcs = new TaskCompletionSource<object>();

            void OnCompletion(object _, RoutedEventArgs args)
            {
                int scoreDelta = Convert.ToInt32(_tbScoreToAdd.Text);
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

            }
        }

        public void  ShowButtons()
        {
            foreach (DoubleAnimation da in _sbAnimateSetScore.Children)
            {
                da.Duration = TimeSpan.FromMilliseconds(500);
                da.To = 1;
            }

             _sbAnimateSetScore.Begin();
        }

        public async Task Hide()
        {
            foreach (DoubleAnimation da in _sbAnimateSetScore.Children)
            {
                da.Duration = TimeSpan.FromMilliseconds(500);
                da.To = 0;
            }

            await _sbAnimateSetScore.ToTask();
        }

        public void HideAsync()
        {
            foreach (DoubleAnimation da in _sbAnimateSetScore.Children)
            {
                da.Duration = TimeSpan.FromMilliseconds(500);
                da.To = 0;
            }

            _sbAnimateSetScore.Begin();
        }



        public async Task UpdatePegsForResolution()
        {
            int pBackScore = _board.PlayerBackScore;
            if (pBackScore == -1) pBackScore = 0;
            int pDelta = _board.PlayerFrontScore - pBackScore;

            int cBackScore = _board.ComputerBackScore;
            if (cBackScore == -1) cBackScore = 0;
            int cDelta = _board.ComputerFrontScore - cBackScore; ;

            await _board.Reset();

            List<Task<object>> taskList = new List<Task<object>>();

             
            if (pBackScore > 0)
                taskList.AddRange(_board.AnimateScore(PlayerType.Player, pBackScore, false));

            if (pDelta > 0)
                taskList.AddRange(_board.AnimateScore(PlayerType.Player, pDelta, false));

            if (cBackScore > 0)
                taskList.AddRange(_board.AnimateScore(PlayerType.Computer, cBackScore, false));

            if (cDelta > 0)
                taskList.AddRange(_board.AnimateScore(PlayerType.Computer, cDelta, false));

            await Task.WhenAll(taskList);
        }

    
        
        

        internal void AnimateScore(PlayerType playerType, int p)
        {
            AnimateScoreAsync(playerType, p);
        }


        public void AnimateScoreAsync(PlayerType player, int scoreToAdd)
        {
            _board.AnimateScore(player, scoreToAdd, true);            
        }

      
    }


   
}
