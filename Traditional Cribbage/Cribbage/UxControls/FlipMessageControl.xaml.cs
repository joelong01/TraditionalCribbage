using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public class ScoreMessageEventArgs : EventArgs
    {
        public ScoreMessageEventArgs(bool p)
        {
            Checked = p;
        }

        public bool Checked { get; set; }
    }

    public delegate void MessageBoxEventHandler(object sender, ScoreMessageEventArgs e);


    public sealed partial class FlipMessageControl : UserControl
    {
        public FlipMessageControl()
        {
            InitializeComponent();
        }

        public string Message
        {
            get => _txtScore.Text;
            set => _txtScore.Text = value;
        }

        public bool AutoSetScore
        {
            get => _chkAutoSetScore.IsChecked == true;
            set => _chkAutoSetScore.IsChecked = value;
        }

        public bool ShowCheckBox
        {
            get => _chkAutoSetScore.Visibility == Visibility.Visible;
            set
            {
                if (value)
                    _chkAutoSetScore.Visibility = Visibility.Visible;
                else
                    _chkAutoSetScore.Visibility = Visibility.Collapsed;
            }
        }

        public event MessageBoxEventHandler OnCheckedAutoUpdateScore;
        public event MessageBoxEventHandler OnDone;

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            if (OnCheckedAutoUpdateScore != null) OnCheckedAutoUpdateScore(this, new ScoreMessageEventArgs(true));
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (OnCheckedAutoUpdateScore != null) OnCheckedAutoUpdateScore(this, new ScoreMessageEventArgs(false));
        }

        private void OnFinish(object sender, RoutedEventArgs e)
        {
            if (OnDone != null) OnDone(this, null);
        }
    }
}