using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;
using LongShotHelpers;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public sealed partial class InnerTraditionalBoard : UserControl, INotifyPropertyChanged
    {
        public const int SCORE_BEFORE_FIRST_ROTATION = 37;
        public const int SCORE_END_FIRST_CURVE = 45;
        public const int SCORE_BEFORE_SECOND_CURVE = 81;
        public const int SCORE_END_SECOND_CURVE = 85;

        private readonly Brush _brushHighlight = new SolidColorBrush(Colors.DarkGreen);
        private Point _centerBottom = new Point(130.5, 684); // for 768 pixels high
        private readonly PlayerDataObject _computer;
        private Point _firstPegLocation = new Point(13, 698); // for 768 pixels high
        private readonly int _pixelRoundDigits = 2;
        private readonly PlayerDataObject _player;
        private Point _topPegLocation = new Point(13, 114); // for 768 pixels high

        public InnerTraditionalBoard()
        {
            InitializeComponent();
            DataContext = this;

            _player = new PlayerDataObject(PlayerType.Player, _sbMovePFPeg, _sbMovePBPeg, _playerFrontPeg,
                _playerBackPeg);
            _computer = new PlayerDataObject(PlayerType.Computer, _sbMoveCFPeg, _sbMoveCBPeg, _computerFrontPeg,
                _computerBackPeg);

            BuildPegLists();

            if (DesignMode.DesignModeEnabled)
            {
                _targetCenterBottom.Opacity = 0.5;
                _targetCenterTop.Opacity = 0.5;
            }
        }

        public double CenterBottomY => Math.Round(_centerBottom.Y, _pixelRoundDigits);

        public double CenterBottomX => Math.Round(_centerBottom.X, _pixelRoundDigits);

        public double PegHoleDiameter
        {
            get
            {
                var d = Math.Round(Height / 64.0, _pixelRoundDigits);
                return Math.Min(d, 22.0);
            }

            //NotifyPropertyChanged("PegHoleDiameter") set in Control_SizeChanged
        }

        public double ThirdColumnDistanceBetweenCurves
        {
            get
            {
                var distance = Math.Round(CenterBottomY - ActualWidth / 2.0 + _player.Pegs[0].Width / 2.0,
                    _pixelRoundDigits);
                return distance;
            }
        }

        public double FirstPegLocationX
        {
            get => Math.Round(_firstPegLocation.X, _pixelRoundDigits);
            set
            {
                _firstPegLocation.X = value;
                NotifyPropertyChanged();
            }
        }

        public double FirstPegLocationY
        {
            get => Math.Round(_firstPegLocation.Y, _pixelRoundDigits);
            set
            {
                _firstPegLocation.Y = value;
                NotifyPropertyChanged();
            }
        }

        public double TopCenterY
        {
            get => Math.Round(_topPegLocation.Y, _pixelRoundDigits);
            set
            {
                _topPegLocation.Y = value;
                NotifyPropertyChanged();
            }
        }

        public double ControlWidth
        {
            get => ActualWidth;
            set
            {
                Width = value;
                NotifyPropertyChanged();
            }
        }

        public double ControlHeight
        {
            get => ActualHeight;
            set
            {
                Height = value;
                NotifyPropertyChanged();
            }
        }

        public int PlayerBackScore => _player.BackPeg.Score;

        public int PlayerFrontScore => _player.FrontScore;

        public int ComputerBackScore => _computer.BackPeg.Score;

        public int ComputerFrontScore => _computer.FrontScore;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task Reset()
        {
            _player.ResetScore(Width / 2.0);
            _computer.ResetScore(Width / 2.0);

            var taskList = new List<Task>();
            Animate(_player, 0, taskList, 250, false);
            Animate(_player, -1, taskList, 250, false);
            Animate(_computer, 0, taskList, 250, false);
            Animate(_computer, -1, taskList, 250, false);
            await Task.WhenAll(taskList);
        }


        private void BuildPegLists()
        {
            _player.Pegs.Add(_pStart1);
            _player.Pegs.Add(_pStart0);
            _computer.Pegs.Add(_cStart1);
            _computer.Pegs.Add(_cStart0);

            Ellipse e = null;
            var name = "";
            for (var i = 1; i < 121; i++)
            {
                name = "_p" + i;
                e = (Ellipse) LayoutRoot.FindName(name);
                _player.Pegs.Add(e);
                name = "_c" + i;
                e = (Ellipse) LayoutRoot.FindName(name);
                _computer.Pegs.Add(e);
            }

            _player.Pegs.Add(_ellipseWinningPeg);
            _computer.Pegs.Add(_ellipseWinningPeg);
        }

        public void UpdateControlLayout(Size newSize)
        {
            LayoutRoot.Height = newSize.Height;
            LayoutRoot.Width = newSize.Width;
            LayoutRoot.RowDefinitions[0].Height = new GridLength(newSize.Width * .04);
            LayoutRoot.RowDefinitions[1].Height = new GridLength(newSize.Width * .46);

            ControlWidth = newSize.Width;
            ControlHeight = newSize.Height;

            var gt = _pStart0.TransformToVisual(LayoutRoot);
            var pt = gt.TransformPoint(new Point(0, 0));
            FirstPegLocationX = pt.X;
            FirstPegLocationY = pt.Y;

            _targetCenterBottom.Width = PegHoleDiameter;
            _targetCenterBottom.Height = _targetCenterBottom.Width;
            _targetCenterTop.Width = _targetCenterBottom.Width;
            _targetCenterTop.Height = _targetCenterBottom.Height;


            gt = _targetCenterTop.TransformToVisual(LayoutRoot);
            pt = gt.TransformPoint(new Point(0, 0));
            TopCenterY = pt.Y;


            gt = _targetCenterBottom.TransformToVisual(LayoutRoot);
            var radius = PegHoleDiameter / 2.0;

            //radius = 0;
            //_centerBottom = gt.TransformPoint(new Point(-radius, -radius));

            //_centerBottom.X = Math.Round(Math.Abs(_centerBottom.X), _pixelRoundDigits);
            //_centerBottom.Y = Math.Round(Math.Abs(_centerBottom.Y), _pixelRoundDigits);

            //this.TraceMessage($"Size={newSize} PegDiameter={PegHoleDiameter} CenterBottom={_centerBottom} ");

            //_targetCenterBottom.Visibility = Visibility.Visible;
            //_targetCenterBottom.Fill = new SolidColorBrush(Colors.HotPink);

            NotifyPropertyChanged("PegHoleDiameter");
            NotifyPropertyChanged("CenterBottomY");
            NotifyPropertyChanged("CenterBottomX");
            NotifyPropertyChanged("ThirdColumnDistanceBetweenCurves");
        }


        private void Control_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateControlLayout(e.NewSize);
        }

        public PlayerDataObject GetPlayerData(PlayerType type)
        {
            return type == PlayerType.Player ? _player : _computer;
        }


        public List<Task> AnimateScore(PlayerType type, int scoreDelta, bool async)
        {
            if (scoreDelta == 0)
            {
                return null;
            }

            var data = GetPlayerData(type);
            var newScore = data.FrontScore + scoreDelta;
            List<Task> taskList = null;
            if (async)
            {
                Animate(data, newScore, taskList, 500, true);
            }
            else
            {
                taskList = new List<Task>();
                Animate(data, newScore, taskList, 500, false);
            }

            data.BackPeg.Score = newScore;
            NotifyPropertyChanged("PlayerFrontScore");
            NotifyPropertyChanged("ComputerFrontScore");
            return taskList;
        }

        public void AnimateScoreAsync(PlayerType type, int scoreDelta)
        {
            AnimateScore(type, scoreDelta, true);
        }


        private Point GetAnimationPointDown(PlayerDataObject data, int targetPeg)
        {
            var diameterDiff = data.Score.Diameter - data.Pegs[0].ActualHeight;
            var targetEllipse = data.Pegs[SCORE_BEFORE_FIRST_ROTATION];
            var movePeg = data.Pegs[targetPeg + 1];
            var gt = targetEllipse.TransformToVisual(movePeg);
            var pt = gt.TransformPoint(new Point(-diameterDiff / 2.0, -diameterDiff / 2.0));
            return pt;
        }

        private double GetThirdColumnHeightForScore(PlayerDataObject data, int newScore)
        {
            var backScore = data.BackPeg.Score;
            FrameworkElement from = data.Pegs[newScore + 1];
            FrameworkElement target = data.Pegs[backScore + 1];
            var offset = 0.0;
            if (backScore < 46)
            {
                target = _targetCenterTop;
                offset = data.Pegs[0].Height;
            }

            var gt = from.TransformToVisual(target);
            var pt = gt.TransformPoint(new Point(0, 0));
            return Math.Abs(Math.Round(pt.Y, _pixelRoundDigits)) + offset;
        }

        private Point GetSecondColumnPositionForScore(PlayerDataObject data, int newScore)
        {
            //  you need to worry the issue that when you do these transforms, the Storyboard hasn't run yet,
            //  so the BackPeg can still be in the bottom Ring - so your (x,y) to the right hole will be wrong.
            //  so we use a target ellipse that is in the same location that you exit the ring 
            //  animation

            var backScore = data.BackPeg.Score;
            FrameworkElement from = data.Pegs[newScore + 1];
            FrameworkElement target = _p86Target;

            if (data.BackPeg.Owner == Owner.Computer)
            {
                target = _c86Target;
            }

            var gt = target.TransformToVisual(from);

            var radiusDiff = (data.BackPeg.ActualHeight - data.Pegs[0].ActualHeight) * 0.5;

            var pt = gt.TransformPoint(new Point(-radiusDiff, -radiusDiff));
            pt.Y = Math.Round(pt.Y, 1);
            pt.X = Math.Round(pt.X, 1);
            // this.TraceMessage($"Target:{target.Name} from {backScore} to {newScore} pt: {pt}");
            return pt;
        }

        private Point GetAnimationPoint(PlayerDataObject data, int score)
        {
            var target = GetEllipseForScore(data, score);
            FrameworkElement movePeg = data.BackPeg;
            var targetRadius = movePeg.ActualHeight / 2.0;
            var targetPoint = new Point(target.ActualWidth / 2.0, target.ActualHeight / 2.0);

            if (score == 0)
            {
                target = data.Pegs[1];
                movePeg = LayoutRoot;
            }
            else if (score == -1)
            {
                target = data.Pegs[0];
                movePeg = LayoutRoot;
            }


            var gt = target.TransformToVisual(movePeg);
            var pt = gt.TransformPoint(targetPoint);
            pt.X = pt.X - targetRadius;
            pt.Y = pt.Y - targetRadius;
            pt.X = Math.Round(pt.X, _pixelRoundDigits);
            pt.Y = Math.Round(pt.Y, _pixelRoundDigits);

            // Debug.WriteLine("{0} to {1} = ({2}, {3})", movePeg.Name, target.Name, pt.X, pt.Y);
            return pt;
        }

        private Ellipse GetEllipseForScore(PlayerDataObject data, int score)
        {
            return data.Pegs[score + 1];
        }

        public void SetDurations(Storyboard pegStoryBoard, double xy, double topAngle, double CenterY,
            double bottomAngle, double durationUpSecondCol, double durationToWin)
        {
            xy = Math.Max(xy, 0);
            topAngle = Math.Max(topAngle, 0);
            CenterY = Math.Max(CenterY, 0);

            Debug.Assert(xy.IsPositiveOrZero());
            Debug.Assert(topAngle.IsPositiveOrZero());
            Debug.Assert(CenterY.IsPositiveOrZero());
            Debug.Assert(bottomAngle.IsPositiveOrZero());
            Debug.Assert(durationUpSecondCol.IsPositiveOrZero());
            Debug.Assert(durationToWin.IsPositiveOrZero());


            var animateX = (DoubleAnimation) pegStoryBoard.Children[(int) PegStoryAnimationChildren.XFirstColumn];
            var animateY = (DoubleAnimation) pegStoryBoard.Children[(int) PegStoryAnimationChildren.YFirstColumn];
            var rotateTop = (DoubleAnimation) pegStoryBoard.Children[(int) PegStoryAnimationChildren.RotateTopAngle];
            var animateCenterY = (DoubleAnimation) pegStoryBoard.Children[(int) PegStoryAnimationChildren.YThirdColumn];
            var rotateBottom =
                (DoubleAnimation) pegStoryBoard.Children[(int) PegStoryAnimationChildren.RotateBottomAngle];
            var animateUpSecondCol =
                (DoubleAnimation) pegStoryBoard.Children[(int) PegStoryAnimationChildren.YSecondColumn];
            var animateCorrectX =
                (DoubleAnimation) pegStoryBoard.Children[(int) PegStoryAnimationChildren.XCorrectLayout];
            var animateCorrectY =
                (DoubleAnimation) pegStoryBoard.Children[(int) PegStoryAnimationChildren.YCorrectLayout];
            var animateToWinX = (DoubleAnimation) pegStoryBoard.Children[(int) PegStoryAnimationChildren.XToWinning];
            var animateToWinY = (DoubleAnimation) pegStoryBoard.Children[(int) PegStoryAnimationChildren.YToWinning];


            Duration xyDuration = TimeSpan.FromMilliseconds(xy);

            animateX.Duration = xyDuration;
            animateY.Duration = xyDuration;

            Duration rotateTopDuration = TimeSpan.FromMilliseconds(topAngle);
            rotateTop.BeginTime = xyDuration.TimeSpan;
            rotateTop.Duration = rotateTopDuration;

            Duration centerYDuration = TimeSpan.FromMilliseconds(CenterY);
            var animateCenterYBeginTime = xyDuration.TimeSpan.Add(rotateTopDuration.TimeSpan);
            animateCenterY.BeginTime = animateCenterYBeginTime;
            animateCenterY.Duration = centerYDuration;

            Duration bottomAngleDuration = TimeSpan.FromMilliseconds(bottomAngle);
            rotateBottom.BeginTime = animateCenterYBeginTime.Add(animateCenterY.Duration.TimeSpan);
            rotateBottom.Duration = TimeSpan.FromMilliseconds(bottomAngle);

            Duration thirdYDuration = TimeSpan.FromMilliseconds(durationUpSecondCol);
            animateUpSecondCol.BeginTime = ((TimeSpan) rotateBottom.BeginTime).Add(rotateBottom.Duration.TimeSpan);
            animateUpSecondCol.Duration = thirdYDuration;
            animateCorrectX.Duration = TimeSpan.FromSeconds(0);
            animateCorrectX.BeginTime = animateUpSecondCol.BeginTime;
            animateCorrectY.Duration = thirdYDuration;
            animateCorrectY.BeginTime = animateUpSecondCol.BeginTime;

            Duration winDuration = TimeSpan.FromMilliseconds(durationToWin);
            animateToWinX.Duration = winDuration;
            animateToWinY.Duration = winDuration;
            animateToWinY.BeginTime =
                ((TimeSpan) animateUpSecondCol.BeginTime).Add(animateUpSecondCol.Duration.TimeSpan);
            animateToWinX.BeginTime = animateToWinY.BeginTime;
        }


        private double AnimateUpFirstColumn(PlayerDataObject data, Storyboard storyboard, int newScore,
            double durationPerPoint)
        {
            if (data.BackPeg.Score > SCORE_BEFORE_FIRST_ROTATION)
            {
                return 0;
            }

            var animateScore = newScore;
            if (newScore >= SCORE_BEFORE_FIRST_ROTATION)
            {
                animateScore = SCORE_BEFORE_FIRST_ROTATION - 1;
            }

            var animateY = (DoubleAnimation) storyboard.Children[(int) PegStoryAnimationChildren.YFirstColumn];
            double durationXY = 0;
            var to = GetAnimationPoint(data, animateScore);
            animateY.To += to.Y;

            if (animateScore == 36)
            {
                var targetPeg = GetEllipseForScore(data, animateScore);
                var pegY = ((TranslateTransform) ((TransformGroup) targetPeg.RenderTransform).Children[0]).Y;
                pegY = pegY - (data.BackPeg.ActualWidth - targetPeg.ActualWidth) / 2.0;
                animateY.To = pegY;
                var animateX = (DoubleAnimation) storyboard.Children[(int) PegStoryAnimationChildren.XFirstColumn];
            }

            var pointsBeforeCurveStart = animateScore - data.BackPeg.Score;
            durationXY = durationPerPoint * pointsBeforeCurveStart;
            return durationXY;
        }

        private double AnimateAroundTop(PlayerDataObject data, Storyboard storyboard, int newScore,
            double durationPerPoint)
        {
            if (newScore < 36)
            {
                return 0; // haven't gotten here yet
            }

            if (data.BackPeg.Score > SCORE_END_FIRST_CURVE)
            {
                return 0; // past here
            }

            var animateScore = Math.Min(newScore, SCORE_END_FIRST_CURVE);

            var rotateAnimation = (DoubleAnimation) storyboard.Children[(int) PegStoryAnimationChildren.RotateTopAngle];
            var ellipseRotateTransform =
                ((TransformGroup) data.Pegs[animateScore + 1].RenderTransform).Children[1] as RotateTransform;
            var pegRotate = ((TransformGroup) data.BackPeg.RenderTransform).Children[1] as RotateTransform;
            var radiusDiff = Math.Round((data.BackPeg.ActualWidth - data.Pegs[0].Width) / 2.0, _pixelRoundDigits);
            pegRotate.CenterX = ellipseRotateTransform.CenterX - radiusDiff;
            pegRotate.CenterY = ellipseRotateTransform.CenterY - radiusDiff;
            var oldAngle = (double) rotateAnimation.To;
            rotateAnimation.To = ellipseRotateTransform.Angle;

            if (newScore > SCORE_END_FIRST_CURVE)
            {
                rotateAnimation.To = 180;
            }

            var duration = (Math.Min(animateScore, 45) - Math.Max(data.BackPeg.Score, 36)) * durationPerPoint * 4;
            return duration;
        }

        private double AnimateAroundBottom(PlayerDataObject data, Storyboard storyboard, int newScore,
            double durationPerPoint)
        {
            double duration = 0;
            if (newScore < 81)
            {
                return 0; // haven't gotten here yet
            }

            var animateScore = newScore;
            if (newScore > 85)
            {
                animateScore = 85;
            }

            var rotateAnimation =
                (DoubleAnimation) storyboard.Children[
                    (int) PegStoryAnimationChildren
                        .RotateBottomAngle]; // there is TransformGroup on all pegs and this is the Transform in the TransformGroup we want
            var ellipseRotateTransform =
                ((TransformGroup) data.Pegs[animateScore + 1].RenderTransform)
                .Children[3] as RotateTransform; // get the angle from the peg hole
            var pegRotate = ((TransformGroup) data.BackPeg.RenderTransform).Children[3] as RotateTransform;
            var radiusDiff = Math.Round((data.BackPeg.ActualWidth - data.Pegs[0].Width) * 0.5, _pixelRoundDigits);
            pegRotate.CenterX = ellipseRotateTransform.CenterX - radiusDiff;
            pegRotate.CenterY = ellipseRotateTransform.CenterY - radiusDiff;
            rotateAnimation.To = ellipseRotateTransform.Angle;

            var translateTransform = ((TransformGroup) data.BackPeg.RenderTransform).Children[4] as TranslateTransform;
            translateTransform.X = radiusDiff;
            translateTransform.Y = radiusDiff;

            if (newScore > 85)
            {
                rotateAnimation.To = 180;
            }

            duration = (Math.Min(animateScore, 85) - Math.Max(data.BackPeg.Score, 80)) * durationPerPoint * 4;

            if (duration < 0)
            {
                duration = 0; // HACK
            }
            // this.TraceMessage($"AnimateAroundBottom: newScore:{newScore} Duration:{duration} CenterX:{pegRotate.CenterX} CenterY:{pegRotate.CenterY} Angle: {rotateAnimation.To}"); 

            return duration;
        }

        private double AnimateDownThirdColumn(PlayerDataObject data, Storyboard storyboard, int newScore,
            double durationPerPoint)
        {
            if (newScore <= SCORE_END_FIRST_CURVE)
            {
                return 0; // haven't gotten here yet
            }

            if (data.BackPeg.Score >= SCORE_BEFORE_SECOND_CURVE)
            {
                return 0;
            }

            var animateScore = newScore;
            if (newScore > SCORE_BEFORE_SECOND_CURVE) // going past the end - we set up the animation to 81
            {
                animateScore = SCORE_BEFORE_SECOND_CURVE;
            }

            var animateY = (DoubleAnimation) storyboard.Children[(int) PegStoryAnimationChildren.YThirdColumn];
            var diff = GetThirdColumnHeightForScore(data, animateScore);
            animateY.To += Math.Abs(diff);
            if (animateScore >= 81) // need to go a little extra here
            {
                animateY.To = ThirdColumnDistanceBetweenCurves;
            }

            double duration = 0;
            var scoreDelta = animateScore - Math.Max(data.BackPeg.Score, SCORE_END_FIRST_CURVE);
            var deltaCenterY = scoreDelta * data.Score.Diameter / 2.0;
            duration = scoreDelta * durationPerPoint;
            return duration;
        }

        //
        //  the general strategy here is to use a target ellipse (_p86Target) that is in the exact position you want
        //  to exit the AnimateAroundBottom animation to end (because the back peg could be in the ring when you run
        //  this animation to move up the second column).  compare the coordinates of the place you want to go to the
        //  coordinates of _p86Target and set the animation to animate that far.
        //
        //
        //
        private double AnimateUpSecondColumn(PlayerDataObject data, Storyboard storyboard, int newScore,
            double durationPerPoint)
        {
            if (newScore <= 85)
            {
                return 0; // haven't gotten here yet
            }

            if (data.BackPeg.Score >= 120)
            {
                return 0; // at the top
            }

            var animateScore = newScore;
            if (newScore > 120) // going past the end - we set up the animation to the end of this animation
            {
                animateScore = 120;
            }

            var animateCorrectX = (DoubleAnimation) storyboard.Children[(int) PegStoryAnimationChildren.XCorrectLayout];

            //
            //  STRANGE:  I tried hard to use YSecondColumn to animate the peg up the second column.  But it would start from the 
            //            0 position each time the animation run, as if it was being reset.  but I couldn't find the place where 
            //            the reset was happening, and this works.  so I leave it.  sigh.  not happy.


            var animateYSecondColumn =
                (DoubleAnimation) storyboard.Children[(int) PegStoryAnimationChildren.YCorrectLayout];
            //DoubleAnimation animateYSecondColumn = (DoubleAnimation)storyboard.Children[(int)PegStoryAnimationChildren.YSecondColumn];
            var pt = GetSecondColumnPositionForScore(data, animateScore);


            animateCorrectX.To = -pt.X;
            animateYSecondColumn.To = -pt.Y;
            var scoreDelta = animateScore - Math.Max(data.BackPeg.Score, 85);
            var duration = scoreDelta * durationPerPoint;
            return duration;
        }

        private double AnimateToWinningPosition(PlayerDataObject data, Storyboard storyboard, int newScore,
            double durationPerPoint)
        {
            double duration = 250;
            if (newScore < 121)
            {
                return 0;
            }

            var from = data.Pegs[121]; // point 120
            var to = _ellipseWinningPeg; //winning peg
            var gt = from.TransformToVisual(to);
            var pt = gt.TransformPoint(new Point(0, 0));
            var radDiff = Math.Abs(data.BackPeg.ActualHeight - _ellipseWinningPeg.ActualHeight) / 2.0;
            pt.X = pt.X - radDiff;
            pt.Y = pt.Y - radDiff;
            var animateX = (DoubleAnimation) storyboard.Children[(int) PegStoryAnimationChildren.XToWinning];
            var animateY = (DoubleAnimation) storyboard.Children[(int) PegStoryAnimationChildren.YToWinning];
            animateX.To = -pt.X;
            animateY.To = -pt.Y;

            return duration;
        }

        public void HighlightPeg(PlayerType playerType, int score, bool highlight)
        {
            if (score > 121)
            {
                score = 121;
            }

            if (score < 1)
            {
                score = 1;
            }

            var data = GetPlayerData(playerType);
            var br = data.Pegs[0].Fill;
            if (highlight)
            {
                br = _brushHighlight;
            }

            data.Pegs[score + 1].Fill = br;
        }


        private void Animate(PlayerDataObject data, int newScore, List<Task> taskList, double duration, bool async)
        {
            var storyboard = data.BackStoryBoard;


            if (newScore == 0)
            {
                storyboard = data.Score.FirstPegControl.TraditionalBoardStoryboard; // for Reset()
            }

            if (newScore == -1)
            {
                storyboard = data.Score.SecondPegControl.TraditionalBoardStoryboard; // for Reset()
            }

            if (newScore == 0 || newScore == -1)
            {
                foreach (DoubleAnimation da in storyboard.Children)
                {
                    da.To = 0;
                    da.BeginTime = TimeSpan.FromMilliseconds(0);
                    da.Duration = TimeSpan.FromMilliseconds(0);
                }

                var animateX = (DoubleAnimation) storyboard.Children[(int) PegStoryAnimationChildren.XFirstColumn];
                var animateY = (DoubleAnimation) storyboard.Children[(int) PegStoryAnimationChildren.YFirstColumn];
                var to = GetAnimationPoint(data, newScore);
                animateX.To = to.X;
                animateY.To = to.Y;
                if (async)
                {
                    storyboard.Begin();
                }
                else
                {
                    taskList.Add(storyboard.ToTask());
                }

                return;
            }

            //double durationPerPoint = 500 / 5.0; 
            var durationPerPoint = 100;
            //if (newScore > 85) durationPerPoint = 100;
            var durationXY = AnimateUpFirstColumn(data, storyboard, newScore, durationPerPoint);
            var durationTop = AnimateAroundTop(data, storyboard, newScore, durationPerPoint / 2);
            double durationThirdColumnY = durationThirdColumnY =
                AnimateDownThirdColumn(data, storyboard, newScore, durationPerPoint);
            var durationBottomRotation = AnimateAroundBottom(data, storyboard, newScore, durationPerPoint / 2);

            var durationUpSecondCol = AnimateUpSecondColumn(data, storyboard, newScore, durationPerPoint);
            var durationToWin = AnimateToWinningPosition(data, storyboard, newScore, durationPerPoint);

            SetDurations(storyboard, durationXY, durationTop, durationThirdColumnY, durationBottomRotation,
                durationUpSecondCol, durationToWin);
            if (async)
            {
                storyboard.Begin();
            }
            else
            {
                taskList.Add(storyboard.ToTask());
            }
        }

        public void TraceBackPegPosition()
        {
            this.TraceMessage(
                $"backpeg Y {((TranslateTransform) ((TransformGroup) _playerBackPeg.RenderTransform).Children[4]).Y}");
        }
    }


    public enum PegStoryAnimationChildren
    {
        XFirstColumn = 0,
        YFirstColumn = 1,
        RotateTopAngle = 2,
        YThirdColumn = 3,
        RotateBottomAngle = 4,
        YSecondColumn = 5,
        XToWinning = 6,
        YToWinning = 7,
        XCorrectLayout = 8,
        YCorrectLayout = 9
    }

    public class PlayerDataObject
    {
        public List<Ellipse> Pegs = new List<Ellipse>();
        public PegScore Score = new PegScore();
        private PlayerType Type;

        public PlayerDataObject(PlayerType playerType, Storyboard sbFront, Storyboard sbBack, PegControl frontPeg,
            PegControl backPeg)
        {
            Type = playerType;
            Score.FirstPegControl = frontPeg;
            Score.SecondPegControl = backPeg;

            Score.FirstPegControl.TraditionalBoardStoryboard = sbFront;
            Score.SecondPegControl.TraditionalBoardStoryboard = sbBack;

            Score.Score1 = -1;
            Score.Score2 = 0;
        }

        public Storyboard BackStoryBoard
        {
            get
            {
                if (Score.FirstPegControl.Score < Score.SecondPegControl.Score)
                {
                    return Score.FirstPegControl.TraditionalBoardStoryboard;
                }

                return Score.SecondPegControl.TraditionalBoardStoryboard;
            }
        }

        public Storyboard FrontStoryBoards
        {
            get
            {
                if (Score.FirstPegControl.Score > Score.SecondPegControl.Score)
                {
                    return Score.SecondPegControl.TraditionalBoardStoryboard;
                }

                return Score.FirstPegControl.TraditionalBoardStoryboard;
            }
        }

        public DoubleAnimation TranslateX =>
            (DoubleAnimation) BackStoryBoard.Children[(int) PegStoryAnimationChildren.XFirstColumn];

        public DoubleAnimation TranslateY =>
            (DoubleAnimation) BackStoryBoard.Children[(int) PegStoryAnimationChildren.YFirstColumn];

        public DoubleAnimation TopRotation =>
            (DoubleAnimation) BackStoryBoard.Children[(int) PegStoryAnimationChildren.RotateTopAngle];

        public DoubleAnimation CenterY =>
            (DoubleAnimation) BackStoryBoard.Children[(int) PegStoryAnimationChildren.YThirdColumn];

        public DoubleAnimation RotateBottomAngle =>
            (DoubleAnimation) BackStoryBoard.Children[(int) PegStoryAnimationChildren.RotateBottomAngle];

        public PegControl BackPeg
        {
            get
            {
                if (Score.FirstPegControl.Score < Score.SecondPegControl.Score)
                {
                    return Score.FirstPegControl;
                }

                return Score.SecondPegControl;
            }
        }

        public PegControl FrontPeg
        {
            get
            {
                if (Score.FirstPegControl.Score > Score.SecondPegControl.Score)
                {
                    return Score.FirstPegControl;
                }

                return Score.SecondPegControl;
            }
        }

        public int FrontScore
        {
            get
            {
                if (Score.Score1 > Score.Score2)
                {
                    return Score.Score1;
                }

                return Score.Score2;
            }
        }

        public void ResetScore(double width)
        {
            Score.Score1 = -1;
            Score.Score2 = 0;

            var tg = Score.FirstPegControl.RenderTransform as TransformGroup;
            ((RotateTransform) tg.Children[1]).CenterY = width - BackPeg.ActualWidth / 2.0;

            tg = Score.SecondPegControl.RenderTransform as TransformGroup;
            ((RotateTransform) tg.Children[1]).CenterY = width - BackPeg.ActualWidth / 2.0;
        }
    }
}