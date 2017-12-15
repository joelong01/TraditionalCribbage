using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class ScoreMessageEventArgs : EventArgs
    {
        public ScoreMessageEventArgs(bool p)
        {
            this.Checked = p;
        }
        public bool Checked { get; set; }
    }

    public delegate void MessageBoxEventHandler(object sender, ScoreMessageEventArgs e);

   
    
    public sealed partial class FlipMessageControl : UserControl
    {
        public event MessageBoxEventHandler OnCheckedAutoUpdateScore;
        public event MessageBoxEventHandler OnDone;

        public FlipMessageControl()
        {
            this.InitializeComponent();
        }

        public string Message
        {
            get
            {
                return _txtScore.Text;
            }
            set
            {
                _txtScore.Text = value;
            }
        }

        public bool AutoSetScore
        {
            get
            {
                return (_chkAutoSetScore.IsChecked == true);
            }
            set
            {

                _chkAutoSetScore.IsChecked = value;
            }

        }

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            if (OnCheckedAutoUpdateScore != null)
            {
                OnCheckedAutoUpdateScore(this, new ScoreMessageEventArgs(true));

            }
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (OnCheckedAutoUpdateScore != null)
            {
                OnCheckedAutoUpdateScore(this, new ScoreMessageEventArgs(false));

            }
        }

        private void OnFinish(object sender, RoutedEventArgs e)
        {
            if (OnDone != null)
            {
                OnDone(this, null);
            }

        }
        public bool ShowCheckBox
        {
            get
            {
                return (_chkAutoSetScore.Visibility == Visibility.Visible);
            }
            set
            {
                if (value)
                    _chkAutoSetScore.Visibility = Visibility.Visible;
                else
                    _chkAutoSetScore.Visibility = Visibility.Collapsed;

            }


        }

       
    }
}
