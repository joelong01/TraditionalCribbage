using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Cards;
using CardView;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public enum UserChoice
    {
        Continue,
        Muggins
    }

    public interface IShowInstructionsAndHistoryController
    {
        void SetMessage(string message);

        Task<UserChoice> ShowAndWait(string message, Visibility showMuggins);
        void ShowAsync(bool show, bool closeWithTimerm, string message);
        void InsertScoreSummary(ScoreType scoreType, int playerScore, int computerScore);

        Task Show(bool show, bool closeWithTimer, string message);

        Task InsertEndOfHandSummary(PlayerType dealer, int cribScore, List<CardCtrl> list, int nComputerCountingPoint,
            int nPlayerCountingPoint, int ComputerPointsThisTurn, int PlayerPointsThisTurn, HandsFromServer Hfs);

        void ResetScoreHistory();

        Task AddToHistory(List<CardCtrl> fullHand, ScoreCollection scores, PlayerType player, Deck Deck, string score);

        string Save();

        Task<bool> Load(string s);
    }


    public sealed partial class HintWindow : UserControl
    {
        private const int SCROLLBAR_WIDTH = 35;
        private const double HEIGHT_WIDTH_RATIO = 140.0 / 240.0;
        private const double HEIGHT_WIDTH_RATIO_HAND_SUMMARY = 400.0 / 310.0;

        public HintWindow()
        {
            InitializeComponent();


            IsOpen = true;
            if (!DesignMode.DesignModeEnabled) _listHistory.Items.Clear();

            _listHistory.ItemsSource = HistoryList;
        }


        public bool IsOpen { get; set; }


        public string Message
        {
            get => _tbMessage.Text;
            set => _tbMessage.Text = value;
        }


        private double HistoryViewWidth => _listHistory.ActualWidth - SCROLLBAR_WIDTH;


        private ObservableCollection<UserControl> HistoryList { get; } = new ObservableCollection<UserControl>();

        //
        //  returns the top left point that we want to animate the score views to -- in this case the (0,0) point of the listView
        //  in the coordinates of the parent...
        private UIElement AnimationPointTo => _listHistory;


        private bool IsRightSide => true;


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
        //          //  Debug.WriteLine("_listHistory is too small! ActualWidth:{0} Width:{1}", _listHistory.ActualWidth, _listHistory.Width);
        //        }
        //    }
        //}


        public void RemoveScoreDetails()
        {
            // TODO: PORT

            //for (int i = _scoreHistoryList.Count - 1; i >= 0; i-- )
            //{
            //    Control view = _scoreHistoryList[i];
            //    if (view.GetType() != typeof(OneHandHistoryCtrl))
            //    {
            //        _scoreHistoryList.RemoveAt(i);
            //    }
            //}
        }


        private void SetMessage(string message)
        {
            _tbMessage.Text = message;
        }


        private void ShowAsync(bool show, bool closeWithTimerm, string message)
        {
            Message = message;
        }

        private void InsertScoreSummary(ScoreType scoreType, int playerScore, int computerScore)
        {
            var view = new ScoreSummaryView();
            view.Initialize(scoreType, playerScore, computerScore);
            view.Width = _listHistory.ActualWidth - SCROLLBAR_WIDTH;
            view.Height = view.Width * HEIGHT_WIDTH_RATIO * .50;
            HistoryList.Insert(0, view);
        }

        private async Task Show(bool show, bool closeWithTimer, string message)
        {
            Message = message;
            await Task.Delay(0);
        }

        // TODO: PORT
        //async Task InsertEndOfHandSummary(PlayerType dealer, int cribScore, List<CardCtrl> crib, 
        //                                int nComputerCountingPoint, int nPlayerCountingPoint, int ComputerPointsThisTurn, int PlayerPointsThisTurn, HandsFromServer hfs)
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

        private void ResetScoreHistory()
        {
            HistoryList.Clear();
        }

        private void AddToHistory(ScoreHistoryView shv)
        {
            HistoryList.Insert(0, shv);
        }


        private void Bounce()
        {
            //
            //    do nothing 
        }


        private Storyboard BounceAnimation()
        {
            return null;
        }
    }
}