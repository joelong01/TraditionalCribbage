using Cards;
using CardView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

using Windows.UI.Xaml.Media.Animation;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{


    public sealed partial class TraditionalHintWindow : UserControl
    {
       private bool _mouseCaptured = false;
        private Point _pointMouseDown;
        DispatcherTimer _timer = new DispatcherTimer();
        bool _closeWithTimer = true;
        ObservableCollection<UserControl> _scoreHistoryList = new ObservableCollection<UserControl>();

      


        public bool IsOpen { get; set; }

        public TraditionalHintWindow()
        {
            this.InitializeComponent();
            HintWindowAnimatePosition.AutoReverse = false;
            _timer.Tick += OnTimer_Tick;
            _timer.Interval = TimeSpan.FromSeconds(5);
            IsOpen = false;
            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                _listHistory.Items.Clear();
            }

            _listHistory.ItemsSource = _scoreHistoryList;

        }

        public void SetMessage(string message)
        {
            _tbMessage.Text = message;
        }

        public string Message
        {
            get
            {
                return _tbMessage.Text;
            }
            set
            {
                _tbMessage.Text = value;

            }
        }

        public void ShowAsync(bool show, bool closeWithTimer = true)
        {
           
            _closeWithTimer = closeWithTimer;
            if (show)
            {

                _xAnimation.Value = -(this.ActualWidth - GrabBarWidth - 1);
                IsOpen = true;
                //await StaticHelpers.RunStoryBoard(HintWindowAnimatePosition, false, 500, false);
                //  HintWindowAnimatePosition.Begin();
                //if (_closeWithTimer)
                //    _timer.Start();

            }
            else
            {

                _xAnimation.Value = 0;
                IsOpen = false;
                //await StaticHelpers.RunStoryBoard(HintWindowAnimatePosition, false, 500, false);
                //  HintWindowAnimatePosition.Begin();

            }
        }

        public async Task Show(bool show)
        {
            await Task.Delay(0);
            if (show)
            {

                _xAnimation.Value = -(this.ActualWidth - GrabBarWidth - 1);
                IsOpen = true;
                //   await StaticHelpers.RunStoryBoard(HintWindowAnimatePosition, false, 500, false);                
            }
            else
            {

                _xAnimation.Value = 0;
                IsOpen = false;
                //    await StaticHelpers.RunStoryBoard(HintWindowAnimatePosition, false, 500, false);                
            }

        }

        public double GrabBarWidth
        {
            get
            {
                return (LayoutRoot.ColumnDefinitions[4].ActualWidth + LayoutRoot.ColumnDefinitions[5].ActualWidth);
            }
        }
        void OnTimer_Tick(object sender, object e)
        {
          //  Debug.WriteLine("TimerWindow tick. Open is {0}",  IsOpen);
           ShowAsync(false);
                _timer.Stop();
           
        }


        private void LayoutRoot_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _pointMouseDown = e.GetCurrentPoint(this).Position;
            _mouseCaptured = ((Grid)sender).CapturePointer(e.Pointer);
            e.Handled = true;

        }

        private void LayoutRoot_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_mouseCaptured)
                return;
        }

        private void LayoutRoot_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            if (_mouseCaptured)
            {
                _mouseCaptured = false;
                this.ReleasePointerCapture(e.Pointer);
                _closeWithTimer = IsOpen;
                ShowAsync(!IsOpen);


            }
        }

      
        private const int SCROLLBAR_WIDTH = 35;
        private const double HEIGHT_WIDTH_RATIO = 140.0 / 240.0;
        private const double HEIGHT_WIDTH_RATIO_HAND_SUMMARY = 400.0 / 310.0; 
        public async Task AddToHistory(List<CardCtrl> cards, ScoreCollection scores, PlayerType player, Deck deck, string gameScore)
        {
            ShowAsync(true, false);
            foreach (ScoreInstance score in scores.Scores)
            {
                ScoreHistoryView view = new ScoreHistoryView();
                view.Width = _listHistory.ActualWidth - SCROLLBAR_WIDTH;
                view.Height = (view.Width * HEIGHT_WIDTH_RATIO);
                view.UpdateLayout();
                await view.PopulateGrid(cards, score, player, scores.ScoreType, _scoreHistoryList.Count,  scores.Total, scores.ActualScore,  gameScore);
                _scoreHistoryList.Insert(0, view);                

            }

          
        }

        public void ResetScoreHistory()
        {
            _scoreHistoryList.Clear();
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        // TODO: PORT

        //private void ScoreHistoryView_SizeChanged(object sender, SizeChangedEventArgs e)
        //{

        //    foreach (Control view in _scoreHistoryList)
        //    {
        //        if (_listHistory.ActualWidth - SCROLLBAR_WIDTH > 0)
        //        {
        //            view.Width = _listHistory.ActualWidth - SCROLLBAR_WIDTH;

        //            Type type = view.GetType();
        //            if (type == typeof(ScoreHistoryView))
        //            {
        //                view.Height = (view.Width * HEIGHT_WIDTH_RATIO);
        //            }
        //            else if (type == typeof(ScoreSummaryView))
        //            {
        //                view.Height = (view.Width * HEIGHT_WIDTH_RATIO) * 0.5;
        //            }
        //            else if (view.GetType() == typeof(OneHandHistoryCtrl))
        //            {
        //                view.Height = (view.Width * HEIGHT_WIDTH_RATIO_HAND_SUMMARY);
        //            }

        //        }
        //        else
        //        {
        //            Debug.WriteLine("_listHistory is too small! ActualWidth:{0} Width:{1}", _listHistory.ActualWidth, _listHistory.Width);
        //        }
        //    }
        //}


        //public void RemoveScoreDetails()
        //{
        //    for (int i = _scoreHistoryList.Count - 1; i >= 0; i-- )
        //    {
        //        Control view = _scoreHistoryList[i];
        //        if (view.GetType() != typeof(OneHandHistoryCtrl))
        //        {
        //            _scoreHistoryList.RemoveAt(i);
        //        }
        //    }
        //}


        public async Task Show(bool show, string message)
        {
            await Show(show, false, message);
        }

        public void ShowAsync(bool show, bool closeWithTimer, string message)
        {
            this.Message = message;
            ShowAsync(show, closeWithTimer);
        }

        public async Task Show(bool show, bool closeWithTimer, string message)
        {
            this.Message = message;
            _closeWithTimer = closeWithTimer;
            await Show(show);
        }


       

      
      

       
       

        void InsertScoreSummary(ScoreType scoreType, int playerScore, int computerScore)
        {
            ScoreSummaryView view = new ScoreSummaryView();
            view.Initialize(scoreType, playerScore, computerScore);
            view.Width = _listHistory.ActualWidth - SCROLLBAR_WIDTH;
            view.Height = (view.Width * HEIGHT_WIDTH_RATIO) * .50;
            _scoreHistoryList.Insert(0, view);
        }

        // TODO: PORT

        //async Task InsertEndOfHandSummary(  PlayerType dealer, int cribScore, List<CardCtrl> crib, int nComputerCountingPoint, int nPlayerCountingPoint, 
        //                                                        int ComputerPointsThisTurn, int PlayerPointsThisTurn, HandsFromServer hfs)
        //{
        //    RemoveScoreDetails();
        //    OneHandHistoryCtrl view = new OneHandHistoryCtrl();
        //    await view.SetPlayerCards(hfs.PlayerCards);
        //    await view.SetComputerHand(hfs.ComputerCards);
        //    await view.SetSharedCard(hfs.SharedCard);
        //    await view.SetCribHand(crib, dealer);
        //    view.SetCountScores(nPlayerCountingPoint, nComputerCountingPoint);
        //    view.SetCribScore(cribScore);
        //    view.SetComputerHandScore(ComputerPointsThisTurn);
        //    view.SetPlayerHandScore(PlayerPointsThisTurn);
        //    view.Width = _listHistory.ActualWidth - SCROLLBAR_WIDTH;
        //    view.Height = (view.Width * HEIGHT_WIDTH_RATIO_HAND_SUMMARY);
        //    _scoreHistoryList.Insert(0, view);
        //}


        void AddToHistory(ScoreHistoryView shv)
        {
            _scoreHistoryList.Insert(0, shv); 
        }


        double HistoryViewWidth
        {
            get { return _listHistory.Width; }
        }


        ObservableCollection<UserControl> HistoryList
        {
            get { return _scoreHistoryList; }
        }


        UIElement AnimationPointTo
        {
            get
            {
                return _listHistory;
            }
        }


        void Bounce()
        {



            _daGrow.To = 1.05;
            _daGrow.Duration = TimeSpan.FromMilliseconds(250);
            _sbGrowAndShrink.Begin();
        }


        bool IsRightSide
        {
            get
            {
                return false; 
            }            
        }


        Storyboard BounceAnimation()
        {
            _daGrow.To = 1.05;
            return _sbGrowAndShrink;
        }
    }
}
