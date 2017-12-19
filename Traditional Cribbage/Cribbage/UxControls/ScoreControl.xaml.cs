using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;
using LongShotHelpers;

namespace Cribbage
{
    
    public delegate void AcceptScoreHandler(object sender, EventArgs e);
    public sealed partial class ScoreControl : UserControl, INotifyPropertyChanged
    {
        private bool _mouseCaptured = false;
        private Point _pointMouseDown;
        private Open _open = new Open();
        private Orientation _orientation = Orientation.Horizontal;
        DispatcherTimer _timer = new DispatcherTimer();
        bool _closeWithTimer = true;
        Owner _owner = Owner.Shared;
        double _hiddenMargin = 0;
        public event AcceptScoreHandler OnAcceptScore;
        public event PropertyChangedEventHandler PropertyChanged;

        ObservableCollection<ScoreInstance> _testScores = new ObservableCollection<ScoreInstance>();

        public double HiddenMargin
        {
            get { return _hiddenMargin; }
            set 
            { 
                _hiddenMargin = value;

                UpdateMargin();
            }
        }

        private void UpdateMargin()
        {
            switch (_open)
            {
                case Open.Right:
                    _rectBackground.Margin = new Thickness(-_hiddenMargin, 0, 0, 0);
                    break;
                case Open.Left:
                    _rectBackground.Margin = new Thickness(0, 0, -_hiddenMargin, 0);
                    break;
                case Open.Down:
                    _rectBackground.Margin = new Thickness(0, -_hiddenMargin, 0, 0);
                    break;
                case Open.Up:
                    _rectBackground.Margin = new Thickness(0, 0, 0, -_hiddenMargin);
                    break;
            }

            Debug.WriteLine("Name:{0} Open:{1} Margin:{2}", this.Name, _open, _rectBackground.Margin);
        }


        public Owner Owner
        {
            get { return _owner; }
            set
            {
                _owner = value;

                // TODO: PORT
                //_circle1.Owner = _owner;
                //_circle2.Owner = _owner;
                //_circle3.Owner = _owner;
            }
        }
        bool _isOpen = false;

        public bool IsOpen
        {
            get
            {
                return _isOpen;
            }

        }


         
        public ScoreControl()
        {
            this.InitializeComponent();


            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {

                _timer.Tick += OnTimer_Tick;
                _timer.Interval = new TimeSpan(0, 0, 5);
                _timer.Stop();

            }

            ScoreInstance score = new ScoreInstance(StatName.Hand3CardRun, 1, 3, null);
            _testScores.Add(score);
            score = new ScoreInstance(StatName.Hand15s, 2, 4, null);
            _testScores.Add(score);
            score = new ScoreInstance(StatName.HandPairs, 2, 4, null);
            _testScores.Add(score);

            _listScores.ItemsSource = _testScores;

            _open = Open.Left;


        }

        async void OnTimer_Tick(object sender, object e)
        {
            if (!ShowOK)
                await Show(false);

            _timer.Stop();

        }


        public Open Open
        {
            get
            {
                return _open;
            }
            set
            {
                _open = value;
                UpdateGrabBar();
                UpdateMargin();
            }
        }


        public string Caption
        {
            get
            {
                return "";
            }
            set
            {

            }
        }

        public double ShowWidth
        {
            get
            {
                double width = 0.0;
                int count = LayoutRoot.ColumnDefinitions.Count;
                width = this.ActualWidth - OuterGrid.ColumnDefinitions[0].ActualWidth;
                return width;
            }

        }

        public double GrabWidth
        {
            get
            {
                double width = LayoutRoot.ColumnDefinitions[0].ActualWidth;
                return width;
            }
        }

        public bool ShowOK
        {
            get
            {
                return _btnOK.Visibility == Visibility.Visible;
            }
            set
            {
                if (value == true)
                {
                    _btnOK.Visibility = Visibility.Visible;
                    _closeWithTimer = false;

                }
                else
                {
                    _btnOK.Visibility = Visibility.Collapsed;
                    _closeWithTimer = true;
                }
            }
        }

        /// <summary>
        ///  this can open up (grab bars on top), down (grab bars on bottom), left (grab bars on right), and right (grab bas on left)
        ///  
        /// </summary>

        private void UpdateGrabBar()
        {

            if (_open == Cribbage.Open.Left)
            {
                _grabBarBottom.Visibility = Visibility.Collapsed;
                _grabBarLeft.Visibility = Visibility.Visible;
                _grabBarTop.Visibility = Visibility.Collapsed;
                _grabBarRight.Visibility = Visibility.Collapsed;

                _borderBottom.Visibility = Visibility.Collapsed;
                _borderLeft.Visibility = Visibility.Visible;
                _borderRight.Visibility = Visibility.Collapsed;
                _borderTop.Visibility = Visibility.Collapsed;




            }
            else if (_open == Open.Right)
            {
                _grabBarBottom.Visibility = Visibility.Collapsed;
                _grabBarLeft.Visibility = Visibility.Collapsed;
                _grabBarTop.Visibility = Visibility.Collapsed;
                _grabBarRight.Visibility = Visibility.Visible;

                _borderBottom.Visibility = Visibility.Collapsed;
                _borderLeft.Visibility = Visibility.Collapsed;
                _borderRight.Visibility = Visibility.Visible;
                _borderTop.Visibility = Visibility.Collapsed;
            }
            else if (_open == Open.Up)
            {
                _grabBarBottom.Visibility = Visibility.Collapsed;
                _grabBarLeft.Visibility = Visibility.Collapsed;
                _grabBarTop.Visibility = Visibility.Visible;
                _grabBarRight.Visibility = Visibility.Collapsed;

                _borderBottom.Visibility = Visibility.Collapsed;
                _borderLeft.Visibility = Visibility.Collapsed;
                _borderRight.Visibility = Visibility.Collapsed;
                _borderTop.Visibility = Visibility.Visible;


            }
            else if (_open == Open.Down)
            {
                _grabBarBottom.Visibility = Visibility.Visible;
                _grabBarLeft.Visibility = Visibility.Collapsed;
                _grabBarTop.Visibility = Visibility.Collapsed;
                _grabBarRight.Visibility = Visibility.Collapsed;

                _borderBottom.Visibility = Visibility.Visible;
                _borderLeft.Visibility = Visibility.Collapsed;
                _borderRight.Visibility = Visibility.Collapsed;
                _borderTop.Visibility = Visibility.Collapsed;

            }

        }
        
     

        private void OnControlSizeChanged(object sender, SizeChangedEventArgs e)
        {

            if (e.NewSize.Width < 175)
            {
                _fontSize = 12.0;

            }
            else if (e.NewSize.Width > 499)
            {
                _fontSize = 20.0;
            }
            else
            {
                _fontSize = 18.0;
            }

            OnPropertyChanged("DynamicFontSize");
        }
        double _fontSize = 20.0;
        public double DynamicFontSize
        {
            get
            {
                return _fontSize;
            }
            set
            {
                _fontSize = value;
                OnPropertyChanged("DynamicFontSize");
            }
        }

        public double Radius
        {
            get
            {
                return _rectBackground.RadiusX;
            }
            set
            {
                _rectBackground.RadiusX = value;
                _rectBackground.RadiusY = value;
               
            }
        }



        public async Task Show(bool show)
        {

            _timer.Stop();

            if (show)
            {
                double width = Window.Current.Bounds.Width;
                double height = Window.Current.Bounds.Height;
                double myLeft = 0;
                double myTop = 0;
                
                if (Parent.GetType() == typeof(Canvas))
                {
                    myLeft = Canvas.GetLeft(this);
                    myTop = Canvas.GetTop(this);

                    switch (_open)
                    {

                        case Open.Left:
                            _xAnimation.Value = Math.Max(-ShowWidth, -myLeft);
                            break;
                        case Open.Right:
                            double myRight = myLeft + this.ActualWidth;
                            _xAnimation.Value = Math.Min(width - myRight, ShowWidth);
                            break;
                        case Open.Down:
                            _xAnimation.Value = Math.Min(height - myTop, (this.ActualWidth - 135));
                            break;
                        case Open.Up:
                            _xAnimation.Value = Math.Max(-(myTop), 135 - this.ActualWidth);
                            break;
                    }
                }
                else if (Parent.GetType() == typeof(Grid))
                {
                    int col = Grid.GetColumn(this);
                    Grid grid = (Grid)this.Parent;
                    if (_open == Open.Left)
                    {
                        for (int i = 0; i < col; i++)
                            myLeft += grid.ColumnDefinitions[i].ActualWidth;

                        _xAnimation.Value = Math.Max(-ShowWidth, -myLeft);
                    }
                    else if (_open == Open.Right)
                    {
                        int colSpan = Grid.GetColumnSpan(this);
                        for (int i = col+colSpan; i< grid.ColumnDefinitions.Count; i++)
                            myLeft += grid.ColumnDefinitions[i].ActualWidth;

                        _xAnimation.Value = Math.Min(myLeft, ShowWidth);
                    }
                }

                
                

                _isOpen = true;
                await StaticHelpers.RunStoryBoard(ScoreWindowAnimatePosition, false, 500, false);
                if (!ShowOK)
                {
                    _timer.Start();
                }


            }
            else
            {
                //   if (!ShowOK) // don't close it programatically if the ok button is shown
                {

                    _xAnimation.Value = 0;
                    _isOpen = false;
                    await StaticHelpers.RunStoryBoard(ScoreWindowAnimatePosition, false, 500, false);
                }
            }


        }


        private void LayoutRoot_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //if (!ShowOK)
            {
                _timer.Stop();
                _pointMouseDown = e.GetCurrentPoint(this).Position;
                _mouseCaptured = ((UIElement)sender).CapturePointer(e.Pointer);
                e.Handled = true;
            }
        }

        private void LayoutRoot_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_mouseCaptured)
                return;


        }

        private async void LayoutRoot_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;

            //if (ShowOK)
            //    return;

            if (_mouseCaptured)
            {
                _mouseCaptured = false;
                this.ReleasePointerCapture(e.Pointer);
                // if (!ShowOK)
                {
                    _closeWithTimer = _isOpen;
                }
                await Show(!_isOpen);


            }
        }

        internal void RotateAsync()
        {
            double angle = -90;

            if (Orientation == Orientation.Horizontal)
            {
                angle = 0;

            }

            _rotateAnimation.To = angle;
            _rotateAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(1000));
            _rotate.Begin();

        }

        public Orientation Orientation
        {
            get
            {
                return _orientation;
            }
            set
            {
                _orientation = value;
                if (_orientation == Orientation.Horizontal)
                {
                    if (_open == Open.Right || _open == Open.Down)
                    {
                        _open = Open.Right;
                    }
                    else
                    {
                        _open = Open.Left;
                    }
                }
                else // orientation is vertical
                {
                    if (_open == Open.Left || _open == Open.Up)
                    {
                        _open = Open.Up;
                    }
                    else
                    {
                        _open = Open.Down;
                    }
                }


            }
        }

        private async void OnContinue(object sender, RoutedEventArgs e)
        {
            if (OnAcceptScore != null)
            {

                OnAcceptScore(this, null);
                _timer.Stop();
                ShowOK = false;

            }

            await Show(false);
        }

        public static async Task WhenClosed(ScoreControl ctrl)
        {
            var tcs = new TaskCompletionSource<object>();

            void OnCompletion(object _, EventArgs args)
            {
                tcs.SetResult(null);
            }

            try
            {
                ctrl.OnAcceptScore += OnCompletion;
                await tcs.Task;
            }
            finally
            {
                ctrl.OnAcceptScore -= OnCompletion;
            }


        }


        //public static Task WhenClosed(ScoreControl ctrl)
        //{
        //    var tcs = new TaskCompletionSource<object>();

        //    AcceptScoreHandler OnCompletion = null;

        //    OnCompletion += (_, args) =>
        //    {
        //        AcceptScoreHandler handler = ctrl.OnAcceptScore;
        //        ctrl.OnAcceptScore -= OnCompletion;
        //        tcs.SetResult(null);
        //    };

        //    ctrl.OnAcceptScore += OnCompletion;

        //    return tcs.Task;
        //}


        public void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
