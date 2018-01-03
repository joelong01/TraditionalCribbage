using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using LongShotHelpers;

namespace Cribbage
{
    public delegate void AcceptScoreHandler(object sender, EventArgs e);

    public sealed partial class ScoreControl : UserControl, INotifyPropertyChanged
    {
        private bool _closeWithTimer = true;
        private double _fontSize = 20.0;
        private double _hiddenMargin;
        private bool _mouseCaptured;
        private Open _open;
        private Orientation _orientation = Orientation.Horizontal;
        private Point _pointMouseDown;

        private readonly ObservableCollection<ScoreInstance> _testScores = new ObservableCollection<ScoreInstance>();
        private readonly DispatcherTimer _timer = new DispatcherTimer();


        public ScoreControl()
        {
            InitializeComponent();


            if (!DesignMode.DesignModeEnabled)
            {
                _timer.Tick += OnTimer_Tick;
                _timer.Interval = new TimeSpan(0, 0, 5);
                _timer.Stop();
            }

            var score = new ScoreInstance(StatName.Hand3CardRun, 1, 3, null);
            _testScores.Add(score);
            score = new ScoreInstance(StatName.Hand15s, 2, 4, null);
            _testScores.Add(score);
            score = new ScoreInstance(StatName.HandPairs, 2, 4, null);
            _testScores.Add(score);

            _listScores.ItemsSource = _testScores;

            _open = Open.Left;
        }

        public double HiddenMargin
        {
            get => _hiddenMargin;
            set
            {
                _hiddenMargin = value;

                UpdateMargin();
            }
        }


        public Owner Owner { get; set; } = Owner.Shared;

        public bool IsOpen { get; private set; }


        public Open Open
        {
            get => _open;
            set
            {
                _open = value;
                UpdateGrabBar();
                UpdateMargin();
            }
        }


        public string Caption
        {
            get { return ""; }
            set { }
        }

        public double ShowWidth
        {
            get
            {
                var width = 0.0;
                var count = LayoutRoot.ColumnDefinitions.Count;
                width = ActualWidth - OuterGrid.ColumnDefinitions[0].ActualWidth;
                return width;
            }
        }

        public double GrabWidth
        {
            get
            {
                var width = LayoutRoot.ColumnDefinitions[0].ActualWidth;
                return width;
            }
        }

        public bool ShowOK
        {
            get => _btnOK.Visibility == Visibility.Visible;
            set
            {
                if (value)
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

        public double DynamicFontSize
        {
            get => _fontSize;
            set
            {
                _fontSize = value;
                OnPropertyChanged("DynamicFontSize");
            }
        }

        public double Radius
        {
            get => _rectBackground.RadiusX;
            set
            {
                _rectBackground.RadiusX = value;
                _rectBackground.RadiusY = value;
            }
        }

        public Orientation Orientation
        {
            get => _orientation;
            set
            {
                _orientation = value;
                if (_orientation == Orientation.Horizontal)
                {
                    if (_open == Open.Right || _open == Open.Down)
                        _open = Open.Right;
                    else
                        _open = Open.Left;
                }
                else // orientation is vertical
                {
                    if (_open == Open.Left || _open == Open.Up)
                        _open = Open.Up;
                    else
                        _open = Open.Down;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event AcceptScoreHandler OnAcceptScore;

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

            Debug.WriteLine("Name:{0} Open:{1} Margin:{2}", Name, _open, _rectBackground.Margin);
        }

        private async void OnTimer_Tick(object sender, object e)
        {
            if (!ShowOK)
                await Show(false);

            _timer.Stop();
        }

        /// <summary>
        ///     this can open up (grab bars on top), down (grab bars on bottom), left (grab bars on right), and right (grab bas on
        ///     left)
        /// </summary>
        private void UpdateGrabBar()
        {
            if (_open == Open.Left)
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
                _fontSize = 12.0;
            else if (e.NewSize.Width > 499)
                _fontSize = 20.0;
            else
                _fontSize = 18.0;

            OnPropertyChanged("DynamicFontSize");
        }


        public async Task Show(bool show)
        {
            _timer.Stop();

            if (show)
            {
                var width = Window.Current.Bounds.Width;
                var height = Window.Current.Bounds.Height;
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
                            var myRight = myLeft + ActualWidth;
                            _xAnimation.Value = Math.Min(width - myRight, ShowWidth);
                            break;
                        case Open.Down:
                            _xAnimation.Value = Math.Min(height - myTop, ActualWidth - 135);
                            break;
                        case Open.Up:
                            _xAnimation.Value = Math.Max(-myTop, 135 - ActualWidth);
                            break;
                    }
                }
                else if (Parent.GetType() == typeof(Grid))
                {
                    var col = Grid.GetColumn(this);
                    var grid = (Grid) Parent;
                    if (_open == Open.Left)
                    {
                        for (var i = 0; i < col; i++)
                            myLeft += grid.ColumnDefinitions[i].ActualWidth;

                        _xAnimation.Value = Math.Max(-ShowWidth, -myLeft);
                    }
                    else if (_open == Open.Right)
                    {
                        var colSpan = Grid.GetColumnSpan(this);
                        for (var i = col + colSpan; i < grid.ColumnDefinitions.Count; i++)
                            myLeft += grid.ColumnDefinitions[i].ActualWidth;

                        _xAnimation.Value = Math.Min(myLeft, ShowWidth);
                    }
                }


                IsOpen = true;
                await StaticHelpers.RunStoryBoard(ScoreWindowAnimatePosition, false, 500, false);
                if (!ShowOK) _timer.Start();
            }
            else
            {
                //   if (!ShowOK) // don't close it programatically if the ok button is shown
                {
                    _xAnimation.Value = 0;
                    IsOpen = false;
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
                _mouseCaptured = ((UIElement) sender).CapturePointer(e.Pointer);
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
                ReleasePointerCapture(e.Pointer);
                // if (!ShowOK)
                {
                    _closeWithTimer = IsOpen;
                }
                await Show(!IsOpen);
            }
        }

        internal void RotateAsync()
        {
            double angle = -90;

            if (Orientation == Orientation.Horizontal) angle = 0;

            _rotateAnimation.To = angle;
            _rotateAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(1000));
            _rotate.Begin();
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