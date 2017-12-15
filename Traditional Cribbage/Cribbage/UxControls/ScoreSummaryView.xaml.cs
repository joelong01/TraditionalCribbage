using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public sealed partial class ScoreSummaryView : UserControl
    {
        public ScoreSummaryView()
        {
            this.InitializeComponent();
        }

        internal void Initialize(ScoreType scoreType, int playerScore, int computerScore)
        {
            _tbPlayer.Text = String.Format("Player Score: {0}", playerScore);
            _tbScoreType.Text = scoreType.ToString();
            _tbComputer.Text = String.Format("Computer Score: {0}", computerScore);
        }
    }
}
