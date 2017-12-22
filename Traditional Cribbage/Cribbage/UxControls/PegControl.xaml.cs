
using Cribbage;
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
        Owner _owner;
        ImageBrush _brushPlayer = new ImageBrush();

        public ImageBrush PlayerBrush
        {
            get { return _brushPlayer; }
            set { _brushPlayer = value; }
        }
        ImageBrush _brushComputer = new ImageBrush();

        public ImageBrush ComputerBrush
        {
            get { return _brushComputer; }
            set { _brushComputer = value; }
        }

        public int Score { get; set; }

        public Storyboard MovePegStoryBoard { get; set; }
      
        
        
        public Point InitialTranlationValues
        {
            get
            {
                return new Point(_compositeTransform.TranslateX, _compositeTransform.TranslateY);
            }
            set
            {
                _compositeTransform.TranslateX = value.X;
                _compositeTransform.TranslateY = value.Y;
            }
        }

        public void SetInitialTranslateValues(double x, double y)
        {
            _compositeTransform.TranslateX = x;
            _compositeTransform.TranslateY = y;

        }

        public PegControl()
        {
            this.InitializeComponent();
            Uri src = new Uri("ms-appx:/Assets/alibaster.png", UriKind.RelativeOrAbsolute);
            BitmapImage image = new BitmapImage(src);
            _brushPlayer.ImageSource = image;
            src = new Uri("ms-appx:/Assets/ruby.png", UriKind.RelativeOrAbsolute);
            image = new BitmapImage(src);
            _brushComputer.ImageSource = image;

           

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

        public Owner Owner
        {
            get
            {
                return _owner;

            }

            set
            {
                _owner = value;
                UpdateOwner();
            }

        }

        private void UpdateOwner()
        {
            if (_owner == Owner.Computer)
            {

                _ellipse.Fill = ComputerBrush;
            }
            else
            {
                _ellipse.Fill = PlayerBrush;

            }
        }
        internal void AnimateTo(Point pt, double duration,  List<Task> taskList)
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

        public Storyboard TraditionalBoardStoryboard { get; set; }

        public Brush PegStroke 
        { 
            get
            {
                return _ellipse.Stroke;
            }
        }
    }


}
