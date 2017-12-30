using LongShotHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
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

        public string Message
        {
            get
            {
                return _textBlock.Text;
            }
            set
            {
                _textBlock.Text = value;
            }
        }



        public void BeginAnimation()
        {
            
            _textBlock.UpdateLayout();
            UpdateLayout();

            //_textBlock.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            //_textBlock.Arrange(new Rect(0, 0, TranslateX, this.ActualHeight)); 


            double durationPerChar = 25;
            double duration = durationPerChar * Message.Length;
            duration = Math.Max(3000, duration);
//            this.TraceMessage($"Len:{Message.Length} Animation Duration {duration} for {Message} ");
            _daMoveText.BeginTime = TimeSpan.FromMilliseconds(0);            
            _daMoveText.Duration = new Duration(TimeSpan.FromMilliseconds(duration));
            _daMoveText.To = TranslateX - (this.ActualWidth);
            _textBlock.Text = _textBlock.Text.Replace('.', ' ');

            EventHandler<object> Animation_Phase1_Completed = null;
            EventHandler<object> Animation_Phase2_Completed = null;
            
            Animation_Phase1_Completed = (s, ex) =>
            {
                _sbMoveText.Completed -= Animation_Phase1_Completed;
                _sbMoveText.Completed += Animation_Phase2_Completed;
                _daMoveText.To =  -this.ActualWidth;
                _daMoveText.Duration = new Duration(TimeSpan.FromMilliseconds(duration));
                _sbMoveText.Begin();
                Phase1Completed?.Invoke(this, ex);
            };

            Animation_Phase2_Completed = async (s, ex) =>
            {
                _sbMoveText.Completed -= Animation_Phase1_Completed;
               await Task.Delay(0);
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
