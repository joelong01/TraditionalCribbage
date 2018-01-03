using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using LongShotHelpers;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public sealed partial class PegControl : UserControl
    {
        private Owner _owner;

        public PegControl()
        {
            InitializeComponent();
            var src = new Uri("ms-appx:/Assets/alibaster.png", UriKind.RelativeOrAbsolute);
            var image = new BitmapImage(src);
            PlayerBrush.ImageSource = image;
            src = new Uri("ms-appx:/Assets/ruby.png", UriKind.RelativeOrAbsolute);
            image = new BitmapImage(src);
            ComputerBrush.ImageSource = image;
        }

        public ImageBrush PlayerBrush { get; set; } = new ImageBrush();

        public ImageBrush ComputerBrush { get; set; } = new ImageBrush();

        public int Score { get; set; }

        public Storyboard MovePegStoryBoard { get; set; }


        public Point InitialTranlationValues
        {
            get => new Point(_compositeTransform.TranslateX, _compositeTransform.TranslateY);
            set
            {
                _compositeTransform.TranslateX = value.X;
                _compositeTransform.TranslateY = value.Y;
            }
        }

        public Owner Owner
        {
            get => _owner;

            set
            {
                _owner = value;
                UpdateOwner();
            }
        }

        public Storyboard TraditionalBoardStoryboard { get; set; }

        public Brush PegStroke => _ellipse.Stroke;

        public void SetInitialTranslateValues(double x, double y)
        {
            _compositeTransform.TranslateX = x;
            _compositeTransform.TranslateY = y;
        }


        public void RotateAsync(double angle, double milliseconds)
        {
            _daRotate.Duration = new Duration(TimeSpan.FromMilliseconds(milliseconds));
            _daRotate.To = angle;
            _sbSetScore.Begin();
        }


        public void Reset()
        {
            RotateAsync(0, 1000);
        }

        private void UpdateOwner()
        {
            if (_owner == Owner.Computer)
                _ellipse.Fill = ComputerBrush;
            else
                _ellipse.Fill = PlayerBrush;
        }

        internal void AnimateTo(Point pt, double duration, List<Task> taskList)
        {
            _daTranslateX.To += pt.X;
            _daTranslateY.To += pt.Y;
            _sbTranslate.Duration = TimeSpan.FromMilliseconds(duration);
            taskList.Add(_sbTranslate.ToTask());
        }

        internal async Task AnimateTo(Point pt, double duration)
        {
            _daTranslateX.To += pt.X;
            _daTranslateY.To += pt.Y;
            _sbTranslate.Duration = TimeSpan.FromMilliseconds(duration);
            await _sbTranslate.ToTask();
        }

        internal void AnimateToAsync(Point pt, double duration)
        {
            _daTranslateX.To += pt.X;
            _daTranslateY.To += pt.Y;
            _sbTranslate.Duration = TimeSpan.FromMilliseconds(duration);
            _sbTranslate.Begin();
        }
    }
}