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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public sealed partial class ScrollingTextCtrl : UserControl
    {
        public event System.EventHandler<object> Phase2Completed;
        public event System.EventHandler<object> Phase1Completed;



        public ScrollingTextCtrl()
        {
            this.InitializeComponent();
        }

        public double TranslateX
        {
            get
            {
                return _tranform.TranslateX;
            }
            set
            {
                _tranform.TranslateX = value;
            }
        }

        public void BeginAnimation(string msg, double duration, double width)
        {

            _daMoveText.BeginTime = TimeSpan.FromMilliseconds(0);
            _textBlock.Text = msg;
            _daMoveText.Duration = new Duration(TimeSpan.FromMilliseconds(duration * .6));
            UpdateLayout();
           

            _daMoveText.To = TranslateX -this.ActualWidth - 25; // 25 pixel gap guaranteed

            EventHandler<object> Animation_Phase1_Completed = null;
            EventHandler<object> Animation_Phase2_Completed = null;

            Animation_Phase1_Completed = (s, ex) =>
            {
                _sbMoveText.Completed -= Animation_Phase1_Completed;
                _sbMoveText.Completed += Animation_Phase2_Completed;
                _daMoveText.To =  -this.ActualWidth;
                _daMoveText.Duration = new Duration(TimeSpan.FromMilliseconds(duration * .8));
                _sbMoveText.Begin();
                Phase1Completed?.Invoke(this, ex);
            };

            Animation_Phase2_Completed = (s, ex) =>
            {
                _sbMoveText.Completed -= Animation_Phase1_Completed;
                Phase2Completed?.Invoke(this, ex);

            };

            _sbMoveText.Completed += Animation_Phase1_Completed;

            _sbMoveText.Begin();
        }

        public void Pause()
        {
            _sbMoveText.Pause();
        }

        public void Resume()
        {
            _sbMoveText.Resume();
        }

        public void Stop()
        {
            _sbMoveText.Stop();
        }

       
    }
}
