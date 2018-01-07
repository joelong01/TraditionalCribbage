using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public sealed partial class ScoreViewerCtrl : UserControl
    {
        private DateTime _lastDispatchedMessage = DateTime.Now;

        private int _maxLength = 25;

        private readonly List<string> _scores = new List<string>();
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public ScoreViewerCtrl()
        {
            InitializeComponent();
            _timer.Interval = TimeSpan.FromMilliseconds(1000);
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, object e)
        {
            try
            {
                if (_scores.Count == 0)
                {
                    _timer.Stop();
                    return;
                }

                var ts = DateTime.Now - _lastDispatchedMessage;
                if (ts.TotalMilliseconds < 1000 && _scores.Count > 1)
                {
                    return;
                }

                var s = _scores[0];
                _scores.RemoveAt(0);
                BeginAnimation(s);
            }

            finally
            {
                _lastDispatchedMessage = DateTime.Now;
            }
        }

        public void AddMessage(string message)
        {
            _scores.Add(message);
            if (_scores.Count == 1)
            {
                BeginAnimation(message);
            }
            //  _timer.Start();
        }

        private void BeginAnimation(string message)
        {
            var len = message.Length;
            _maxLength = Math.Max(len, _maxLength);
            var sb = new StringBuilder(message);
            for (var i = len; i < _maxLength; i++)
            {
                sb.Append(".");
            }

            UpdateLayout();
            var ctrl = new ScrollingTextCtrl
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                TranslateX = ActualWidth,
                Message = sb.ToString()
            };


            EventHandler<object> Phase2AnmiationComplete = null;

            void Phase1AnmiationComplete(object s, object ex)
            {
                //
                // the text has scrolled its position and now we can send the next one
                _scores.RemoveAt(0);
                if (_scores.Count > 0)
                {
                    BeginAnimation(_scores[0]);
                }
            }

            Phase2AnmiationComplete = (s, ex) => { LayoutRoot.Children.Remove(ctrl); };

            ctrl.Phase1Completed += Phase1AnmiationComplete;
            ctrl.Phase2Completed += Phase2AnmiationComplete;
            LayoutRoot.Children.Add(ctrl);
            ctrl.BeginAnimation();
        }


        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_scoreGridClip == null)
            {
                return;
            }

            _scoreGridClip.Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);
        }
    }
}