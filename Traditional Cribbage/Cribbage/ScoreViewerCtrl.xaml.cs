﻿using System;
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
    public sealed partial class ScoreViewerCtrl : UserControl
    {

        List<string> _scores = new List<string>();
        DispatcherTimer _timer = new DispatcherTimer();
        DateTime _lastDispatchedMessage = DateTime.Now;

        public ScoreViewerCtrl()
        {
            this.InitializeComponent();
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

                TimeSpan ts = DateTime.Now - _lastDispatchedMessage;
                if (ts.TotalMilliseconds < 1000 && _scores.Count > 1)
                {
                    return;
                }

                string s = _scores[0];
                _scores.RemoveAt(0);
                BeginAnimation(s);

            }

            finally
            {


                _lastDispatchedMessage = DateTime.Now;
            }
        }

        public void AddScore(string message)
        {
            _scores.Add(message);
            if (_scores.Count == 1)
                 BeginAnimation(message);
          //  _timer.Start();
        }

        private void BeginAnimation(string message)
        {
           
            ScrollingTextCtrl ctrl = new ScrollingTextCtrl();
            ctrl.HorizontalAlignment = HorizontalAlignment.Left;
            ctrl.VerticalAlignment = VerticalAlignment.Center;
            ctrl.TranslateX = this.ActualWidth;
            this.UpdateLayout();
            EventHandler< object > Phase2AnmiationComplete = null;
            EventHandler<object> Phase1AnmiationComplete = null;
            Phase1AnmiationComplete = (s, ex) =>
            {
                //
                // the text has scrolled its position and now we can send the next one
                _scores.RemoveAt(0);
               if (_scores.Count > 0)
                {
                    BeginAnimation(_scores[0]);
                } 

            };

            Phase2AnmiationComplete = (s, ex) =>
            {
                LayoutRoot.Children.Remove(ctrl);

            };

            ctrl.Phase1Completed += Phase1AnmiationComplete;
            ctrl.Phase2Completed += Phase2AnmiationComplete;
            LayoutRoot.Children.Add(ctrl);
            ctrl.BeginAnimation(message, 5000, this.ActualWidth);
        }

       

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_scoreGridClip == null) return;
            _scoreGridClip.Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);            
        }
    }

   
}