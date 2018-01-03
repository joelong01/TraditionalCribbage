using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public sealed partial class ScoreSummaryView : UserControl
    {
        public ScoreSummaryView()
        {
            InitializeComponent();
        }

        internal void Initialize(ScoreType scoreType, int playerScore, int computerScore)
        {
            _tbPlayer.Text = string.Format("Player Score: {0}", playerScore);
            _tbScoreType.Text = scoreType.ToString();
            _tbComputer.Text = string.Format("Computer Score: {0}", computerScore);
        }
    }
}