using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public sealed partial class ScrollingTextCtrl : UserControl
    {
        public ScrollingTextCtrl()
        {
            InitializeComponent();
        }

        public double TranslateX
        {
            get => _tranform.TranslateX;
            set => _tranform.TranslateX = value;
        }

        public string Message
        {
            get => _textBlock.Text;
            set => _textBlock.Text = value;
        }

        public event EventHandler<object> Phase2Completed;
        public event EventHandler<object> Phase1Completed;


        public void BeginAnimation()
        {
            _textBlock.UpdateLayout();
            UpdateLayout();

            double durationPerChar = 25;
            var duration = durationPerChar * Message.Length;
            duration = Math.Max(3000, duration);

            _daMoveText.BeginTime = TimeSpan.FromMilliseconds(0);
            _daMoveText.Duration = new Duration(TimeSpan.FromMilliseconds(duration));
            _daMoveText.To = TranslateX - ActualWidth;
            _textBlock.Text = _textBlock.Text.Replace('.', ' ');

           

            void AnimationPhase1Completed(object s, object ex) 
            {
                _sbMoveText.Completed -= AnimationPhase1Completed;
                _sbMoveText.Completed += AnimationPhase2Completed;
                _daMoveText.To = -ActualWidth;
                _daMoveText.Duration = new Duration(TimeSpan.FromMilliseconds(duration));
                _sbMoveText.Begin();
                Phase1Completed?.Invoke(this, ex);
            };

             void AnimationPhase2Completed(object s, object ex)
            {
                _sbMoveText.Completed -= AnimationPhase1Completed;                
                Phase2Completed?.Invoke(this, ex);
            }

            

            _sbMoveText.Completed += AnimationPhase1Completed;

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