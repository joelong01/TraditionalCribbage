using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using LongShotHelpers;
using Windows.UI.Xaml.Media.Animation;

using Windows.UI.Xaml.Shapes;
using System.Diagnostics;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{



    public sealed partial class InnerTraditionalBoard : UserControl, INotifyPropertyChanged
    {
        private PlayerDataObject _player = null;
        private PlayerDataObject _computer = null;
        private Point _firstPegLocation = new Point(13, 698);   // for 768 pixels high
        private Point _topPegLocation = new Point(13, 114);     // for 768 pixels high
        Point _centerBottom = new Point(130.5, 684);            // for 768 pixels high
        int _pixelRoundDigits = 2;

        public event PropertyChangedEventHandler PropertyChanged;

        public double CenterBottomY
        {
            get
            {
                return Math.Round(_centerBottom.Y, _pixelRoundDigits);
            }
            //NotifyPropertyChanged("CenterBottomY") set in Control_SizeChanged
        }
        public double CenterBottomX
        {
            get
            {
                return Math.Round(_centerBottom.X, _pixelRoundDigits);
            }
            //NotifyPropertyChanged("CenterBottomX") set in Control_SizeChanged
        }
        public double PegHoleDiameter
        {
            get
            {
                double d = Math.Round(this.Height / 64.0, _pixelRoundDigits);
                return Math.Min(d, 22.0);
            }

            //NotifyPropertyChanged("PegHoleDiameter") set in Control_SizeChanged
        }
        public double ThirdColumnDistanceBetweenCurves
        {
            get
            {
                double distance = Math.Round(CenterBottomY - this.ActualWidth / 2.0 + _player.Pegs[0].Width / 2.0, _pixelRoundDigits);
                return distance;
            }
        }

        public Double FirstPegLocationX
        {
            get
            {
                return Math.Round(_firstPegLocation.X, _pixelRoundDigits);
            }
            set
            {
                _firstPegLocation.X = value;
                NotifyPropertyChanged();
            }
        }
        public Double FirstPegLocationY
        {
            get
            {
                return Math.Round(_firstPegLocation.Y, _pixelRoundDigits);
            }
            set
            {
                _firstPegLocation.Y = value;
                NotifyPropertyChanged();
            }
        }

        public Double TopCenterY
        {
            get
            {
                return Math.Round(_topPegLocation.Y, _pixelRoundDigits);
            }
            set
            {
                _topPegLocation.Y = value;
                NotifyPropertyChanged();
            }
        }

        public double ControlWidth
        {
            get
            {
                return this.ActualWidth;
            }
            set
            {
                this.Width = value;
                NotifyPropertyChanged();
            }
        }

        public double ControlHeight
        {
            get
            {
                return this.ActualHeight;
            }
            set
            {
                this.Height = value;
                NotifyPropertyChanged();
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public InnerTraditionalBoard()
        {
            this.InitializeComponent();
            this.DataContext = this;

            _player = new PlayerDataObject(PlayerType.Player, _sbMovePFPeg, _sbMovePBPeg, _playerFrontPeg, _playerBackPeg);
            _computer = new PlayerDataObject(PlayerType.Computer, _sbMoveCFPeg, _sbMoveCBPeg, _computerFrontPeg, _computerBackPeg);

            BuildPegLists();

            if (DesignMode.DesignModeEnabled)
            {
                _targetCenterBottom.Opacity = 0.5;
                _targetCenterTop.Opacity = 0.5;
            }


        }

        public async Task Reset()
        {
            _player.ResetScore(this.Width / 2.0);
            _computer.ResetScore(this.Width / 2.0);

            List<Task> taskList = new List<Task>();
            Animate(_player, 0, taskList, 250, false);
            Animate(_player, -1, taskList, 250, false);
            Animate(_computer, 0, taskList, 250, false);
            Animate(_computer, -1, taskList, 250, false);
            await Task.WhenAll(taskList);

        }


        void BuildPegLists()
        {

            _player.Pegs.Add(_pStart1);
            _player.Pegs.Add(_pStart0);
            _computer.Pegs.Add(_cStart1);
            _computer.Pegs.Add(_cStart0);

            Ellipse e = null;
            string name = "";
            for (int i = 1; i < 121; i++)
            {
                name = "_p" + i.ToString();
                e = (Ellipse)LayoutRoot.FindName(name);
                _player.Pegs.Add(e);
                name = "_c" + i.ToString();
                e = (Ellipse)LayoutRoot.FindName(name);
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

            GeneralTransform gt = _pStart0.TransformToVisual(LayoutRoot);
            Point pt = gt.TransformPoint(new Point(0, 0));
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
            double radius = PegHoleDiameter / 2.0;

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
            return (type == PlayerType.Player) ? _player : _computer;
        }

        public const int SCORE_BEFORE_FIRST_ROTATION = 37;
        public const int SCORE_END_FIRST_CURVE = 45;
        public const int SCORE_BEFORE_SECOND_CURVE = 81;
        public const int SCORE_END_SECOND_CURVE = 85;


        public List<Task> AnimateScore(PlayerType type, int scoreDelta, bool async)
        {
            PlayerDataObject data = GetPlayerData(type);
            int newScore = data.FrontScore + scoreDelta;
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
            return taskList;

        }

        public void AnimateScoreAsync(PlayerType type, int scoreDelta)
        {
            AnimateScore(type, scoreDelta, true);
        }



        private Point GetAnimationPointDown(PlayerDataObject data, int targetPeg)
        {
            double diameterDiff = data.Score.Diameter - data.Pegs[0].ActualHeight;
            Ellipse targetEllipse = data.Pegs[SCORE_BEFORE_FIRST_ROTATION];
            Ellipse movePeg = data.Pegs[targetPeg + 1];
            GeneralTransform gt = targetEllipse.TransformToVisual(movePeg);
            Point pt = gt.TransformPoint(new Point(-diameterDiff / 2.0, -diameterDiff / 2.0));
            return pt;
        }
        private double GetThirdColumnHeightForScore(PlayerDataObject data, int newScore)
        {
            int backScore = data.BackPeg.Score;
            FrameworkElement from = data.Pegs[newScore + 1];
            FrameworkElement target = data.Pegs[backScore + 1];
            double offset = 0.0;
            if (backScore < 46)
            {
                target = _targetCenterTop;
                offset = data.Pegs[0].Height;
            }

            GeneralTransform gt = from.TransformToVisual(target);
            Point pt = gt.TransformPoint(new Point(0, 0));
            return Math.Abs(Math.Round(pt.Y, _pixelRoundDigits)) + offset;
        }

        private Point GetSecondColumnPositionForScore(PlayerDataObject data, int newScore)
        {
           
            //  you need to worry the issue that when you do these transforms, the Storyboard hasn't run yet,
            //  so the BackPeg can still be in the bottom Ring - so your (x,y) to the right hole will be wrong.
            //  so we use a target ellipse that is in the same location that you exit the ring 
            //  animation

            int backScore = data.BackPeg.Score;
            FrameworkElement from = data.Pegs[newScore + 1];            
            FrameworkElement target = _p86Target;

            if (data.BackPeg.Owner == Owner.Computer)
                target = _c86Target;


            GeneralTransform gt = target.TransformToVisual(from);

            double radiusDiff = (data.BackPeg.ActualHeight - data.Pegs[0].ActualHeight)*0.5;

            Point pt = gt.TransformPoint(new Point(-radiusDiff, -radiusDiff));
            pt.Y = Math.Round(pt.Y, 1);
            pt.X = Math.Round(pt.X, 1);
           // this.TraceMessage($"Target:{target.Name} from {backScore} to {newScore} pt: {pt}");
            return pt;
        }

        private Point GetAnimationPoint(PlayerDataObject data, int score)
        {

            Ellipse target =  GetEllipseForScore(data, score);
            FrameworkElement movePeg = data.BackPeg;
            double targetRadius = movePeg.ActualHeight / 2.0;
            Point targetPoint = new Point(target.ActualWidth / 2.0, target.ActualHeight / 2.0);

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


            GeneralTransform gt = target.TransformToVisual(movePeg);
            Point pt = gt.TransformPoint(targetPoint);
            pt.X = pt.X - targetRadius;
            pt.Y = pt.Y - targetRadius;
            pt.X = Math.Round(pt.X, _pixelRoundDigits);
            pt.Y = Math.Round(pt.Y, _pixelRoundDigits);

            // Debug.WriteLine("{0} to {1} = ({2}, {3})", movePeg.Name, target.Name, pt.X, pt.Y);
            return pt;
        }

        Ellipse GetEllipseForScore(PlayerDataObject data, int score)
        {
            return data.Pegs[score + 1];
        }

        public void SetDurations(Storyboard pegStoryBoard, double xy, double topAngle, double CenterY, double bottomAngle, double durationUpSecondCol, double durationToWin)
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

          

            DoubleAnimation animateX = (DoubleAnimation)pegStoryBoard.Children[(int)PegStoryAnimationChildren.XFirstColumn];
            DoubleAnimation animateY = (DoubleAnimation)pegStoryBoard.Children[(int)PegStoryAnimationChildren.YFirstColumn];
            DoubleAnimation rotateTop = (DoubleAnimation)pegStoryBoard.Children[(int)PegStoryAnimationChildren.RotateTopAngle];
            DoubleAnimation animateCenterY = (DoubleAnimation)pegStoryBoard.Children[(int)PegStoryAnimationChildren.YThirdColumn];
            DoubleAnimation rotateBottom = (DoubleAnimation)pegStoryBoard.Children[(int)PegStoryAnimationChildren.RotateBottomAngle];
            DoubleAnimation animateUpSecondCol = (DoubleAnimation)pegStoryBoard.Children[(int)PegStoryAnimationChildren.YSecondColumn];            
            DoubleAnimation animateCorrectX = (DoubleAnimation)pegStoryBoard.Children[(int)PegStoryAnimationChildren.XCorrectLayout];
            DoubleAnimation animateCorrectY = (DoubleAnimation)pegStoryBoard.Children[(int)PegStoryAnimationChildren.YCorrectLayout];
            DoubleAnimation animateToWinX = (DoubleAnimation)pegStoryBoard.Children[(int)PegStoryAnimationChildren.XToWinning];
            DoubleAnimation animateToWinY = (DoubleAnimation)pegStoryBoard.Children[(int)PegStoryAnimationChildren.YToWinning];



            Duration xyDuration = TimeSpan.FromMilliseconds(xy);

            animateX.Duration = xyDuration;
            animateY.Duration = xyDuration;

            Duration rotateTopDuration = TimeSpan.FromMilliseconds(topAngle);
            rotateTop.BeginTime = xyDuration.TimeSpan;
            rotateTop.Duration = rotateTopDuration;

            Duration centerYDuration = TimeSpan.FromMilliseconds(CenterY);
            TimeSpan animateCenterYBeginTime = xyDuration.TimeSpan.Add(rotateTopDuration.TimeSpan);
            animateCenterY.BeginTime = animateCenterYBeginTime;
            animateCenterY.Duration = centerYDuration;

            Duration bottomAngleDuration = TimeSpan.FromMilliseconds(bottomAngle);
            rotateBottom.BeginTime = animateCenterYBeginTime.Add(animateCenterY.Duration.TimeSpan);
            rotateBottom.Duration = TimeSpan.FromMilliseconds(bottomAngle);

            Duration thirdYDuration = TimeSpan.FromMilliseconds(durationUpSecondCol);
            animateUpSecondCol.BeginTime = ((TimeSpan)rotateBottom.BeginTime).Add(rotateBottom.Duration.TimeSpan);
            animateUpSecondCol.Duration = thirdYDuration;
            animateCorrectX.Duration = TimeSpan.FromSeconds(0);
            animateCorrectX.BeginTime = animateUpSecondCol.BeginTime;
            animateCorrectY.Duration = thirdYDuration;
            animateCorrectY.BeginTime = animateUpSecondCol.BeginTime; 

            Duration winDuration = TimeSpan.FromMilliseconds(durationToWin);
            animateToWinX.Duration = winDuration;
            animateToWinY.Duration = winDuration;
            animateToWinY.BeginTime = ((TimeSpan)animateUpSecondCol.BeginTime).Add(animateUpSecondCol.Duration.TimeSpan);
            animateToWinX.BeginTime = animateToWinY.BeginTime;

        }


        double AnimateUpFirstColumn(PlayerDataObject data, Storyboard storyboard, int newScore, double durationPerPoint)
        {
            if (data.BackPeg.Score > SCORE_BEFORE_FIRST_ROTATION) return 0;

            int animateScore = newScore;
            if (newScore >= SCORE_BEFORE_FIRST_ROTATION)
                animateScore = SCORE_BEFORE_FIRST_ROTATION - 1;


            DoubleAnimation animateY = (DoubleAnimation)storyboard.Children[(int)PegStoryAnimationChildren.YFirstColumn];
            double durationXY = 0;
            Point to = GetAnimationPoint(data, animateScore);
            animateY.To += to.Y;

            if (animateScore == 36)
            {
                Ellipse targetPeg = GetEllipseForScore(data, animateScore);
                double pegY = ((TranslateTransform)((TransformGroup)targetPeg.RenderTransform).Children[0]).Y;
                pegY = pegY - (data.BackPeg.ActualWidth - targetPeg.ActualWidth) / 2.0;
                animateY.To = pegY;
                DoubleAnimation animateX = (DoubleAnimation)storyboard.Children[(int)PegStoryAnimationChildren.XFirstColumn];
            }

            int pointsBeforeCurveStart = animateScore - data.BackPeg.Score;
            durationXY = durationPerPoint * pointsBeforeCurveStart;
            return durationXY;
        }

        double AnimateAroundTop(PlayerDataObject data, Storyboard storyboard, int newScore, double durationPerPoint)
        {
            if (newScore < 36) return 0; // haven't gotten here yet
            if (data.BackPeg.Score > SCORE_END_FIRST_CURVE) return 0; // past here

            int animateScore = Math.Min(newScore, SCORE_END_FIRST_CURVE);

            DoubleAnimation rotateAnimation = (DoubleAnimation)storyboard.Children[(int)PegStoryAnimationChildren.RotateTopAngle];
            RotateTransform ellipseRotateTransform = ((TransformGroup)data.Pegs[animateScore + 1].RenderTransform).Children[1] as RotateTransform;
            RotateTransform pegRotate = ((TransformGroup)data.BackPeg.RenderTransform).Children[1] as RotateTransform;
            double radiusDiff = Math.Round((data.BackPeg.ActualWidth - data.Pegs[0].Width) / 2.0, _pixelRoundDigits);
            pegRotate.CenterX = ellipseRotateTransform.CenterX - radiusDiff;
            pegRotate.CenterY = ellipseRotateTransform.CenterY - radiusDiff;
            double oldAngle = (double)rotateAnimation.To;
            rotateAnimation.To = ellipseRotateTransform.Angle;

            if (newScore > SCORE_END_FIRST_CURVE)
                rotateAnimation.To = 180;

            double duration = (Math.Min(animateScore, 45) - Math.Max(data.BackPeg.Score, 36)) * durationPerPoint * 4;
            return duration;
        }
        double AnimateAroundBottom(PlayerDataObject data, Storyboard storyboard, int newScore, double durationPerPoint)
        {
            double duration = 0;
            if (newScore < 81) return 0; // haven't gotten here yet
            

            int animateScore = newScore;
            if (newScore > 85)
            {
                animateScore = 85;
            }

            DoubleAnimation rotateAnimation = (DoubleAnimation)storyboard.Children[(int)PegStoryAnimationChildren.RotateBottomAngle]; // there is TransformGroup on all pegs and this is the Transform in the TransformGroup we want
            RotateTransform ellipseRotateTransform = ((TransformGroup)data.Pegs[animateScore + 1].RenderTransform).Children[3] as RotateTransform;  // get the angle from the peg hole
            RotateTransform pegRotate = ((TransformGroup)data.BackPeg.RenderTransform).Children[3] as RotateTransform;
            double radiusDiff = Math.Round((data.BackPeg.ActualWidth - data.Pegs[0].Width) * 0.5, _pixelRoundDigits);
            pegRotate.CenterX = ellipseRotateTransform.CenterX - radiusDiff;
            pegRotate.CenterY = ellipseRotateTransform.CenterY - radiusDiff;
            rotateAnimation.To = ellipseRotateTransform.Angle;

            TranslateTransform translateTransform = ((TransformGroup)data.BackPeg.RenderTransform).Children[4] as TranslateTransform;
            translateTransform.X = radiusDiff;
            translateTransform.Y = radiusDiff;

            if (newScore > 85)
                rotateAnimation.To = 180;

            duration = (Math.Min(animateScore, 85) - Math.Max(data.BackPeg.Score, 81)) * durationPerPoint * 4;

            if (duration < 0) duration = 0; // HACK
           // this.TraceMessage($"AnimateAroundBottom: newScore:{newScore} Duration:{duration} CenterX:{pegRotate.CenterX} CenterY:{pegRotate.CenterY} Angle: {rotateAnimation.To}"); 

            return duration;
        }
        double AnimateDownThirdColumn(PlayerDataObject data, Storyboard storyboard, int newScore, double durationPerPoint)
        {
            if (newScore <= SCORE_END_FIRST_CURVE) return 0; // haven't gotten here yet
            if (data.BackPeg.Score >= SCORE_BEFORE_SECOND_CURVE) return 0;
            int animateScore = newScore;
            if (newScore > SCORE_BEFORE_SECOND_CURVE)              // going past the end - we set up the animation to 81
                animateScore = SCORE_BEFORE_SECOND_CURVE;


            DoubleAnimation animateY = (DoubleAnimation)storyboard.Children[(int)PegStoryAnimationChildren.YThirdColumn];
            double diff = GetThirdColumnHeightForScore(data, animateScore);
            animateY.To += Math.Abs(diff);
            if (animateScore >= 81) // need to go a little extra here
            {
                animateY.To = ThirdColumnDistanceBetweenCurves;
            }
            double duration = 0;
            int scoreDelta = animateScore - Math.Max(data.BackPeg.Score, SCORE_END_FIRST_CURVE);
            double deltaCenterY = scoreDelta * data.Score.Diameter / 2.0;
            duration = (scoreDelta) * durationPerPoint;
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
        double AnimateUpSecondColumn(PlayerDataObject data, Storyboard storyboard, int newScore, double durationPerPoint)
        {
            if (newScore <= 85) return 0; // haven't gotten here yet
            if (data.BackPeg.Score >= 120) return 0; // at the top
            int animateScore = newScore;
            if (newScore > 120)              // going past the end - we set up the animation to the end of this animation
                animateScore = 120;

            
            DoubleAnimation animateCorrectX = (DoubleAnimation)storyboard.Children[(int)PegStoryAnimationChildren.XCorrectLayout];
            
            //
            //  STRANGE:  I tried hard to use YSecondColumn to animate the peg up the second column.  But it would start from the 
            //            0 position each time the animation run, as if it was being reset.  but I couldn't find the place where 
            //            the reset was happening, and this works.  so I leave it.  sigh.  not happy.


            
            DoubleAnimation animateYSecondColumn = (DoubleAnimation)storyboard.Children[(int)PegStoryAnimationChildren.YCorrectLayout];
            //DoubleAnimation animateYSecondColumn = (DoubleAnimation)storyboard.Children[(int)PegStoryAnimationChildren.YSecondColumn];
            Point pt = GetSecondColumnPositionForScore(data, animateScore);

            
            animateCorrectX.To = -pt.X;
            animateYSecondColumn.To = -pt.Y;
            int scoreDelta = animateScore - Math.Max(data.BackPeg.Score, 85);
            double duration = (scoreDelta) * durationPerPoint;
            return duration;
        }

        double AnimateToWinningPosition(PlayerDataObject data, Storyboard storyboard, int newScore, double durationPerPoint)
        {

            double duration = 250;
            if (newScore < 121) return 0;
            Ellipse from = data.Pegs[121]; // point 120
            Ellipse to = _ellipseWinningPeg; //winning peg
            GeneralTransform gt = from.TransformToVisual(to);
            Point pt = gt.TransformPoint(new Point(0, 0));
            double radDiff = Math.Abs(data.BackPeg.ActualHeight - _ellipseWinningPeg.ActualHeight) / 2.0;
            pt.X = pt.X - radDiff;
            pt.Y = pt.Y - radDiff;
            DoubleAnimation animateX = (DoubleAnimation)storyboard.Children[(int)PegStoryAnimationChildren.XToWinning];
            DoubleAnimation animateY = (DoubleAnimation)storyboard.Children[(int)PegStoryAnimationChildren.YToWinning];
            animateX.To = -pt.X;
            animateY.To = -pt.Y;

            return duration;
        }

        Brush _brushHighlight = new SolidColorBrush(Colors.Red);
        public void HighlightPeg(PlayerType playerType, int score, bool highlight)
        {
            if (score > 121) score = 121;
            if (score < 1) score = 1;
            PlayerDataObject data = GetPlayerData(playerType);
            Brush br = data.Pegs[0].Fill;
            if (highlight) br = _brushHighlight;
            data.Pegs[score + 1].Fill = br;
        }

        public int PlayerBackScore
        {
            get
            {
                return _player.BackPeg.Score;
            }

        }
        public int PlayerFrontScore
        {
            get
            {
                return _player.FrontScore;
            }
        }

        public int ComputerBackScore
        {
            get
            {
                return _computer.BackPeg.Score;
            }

        }
        public int ComputerFrontScore
        {
            get
            {
                return _computer.FrontScore;
            }
        }

        

        private void Animate(PlayerDataObject data, int newScore, List<Task> taskList, double duration, bool async)
        {

            Storyboard storyboard = data.BackStoryBoard;


            if (newScore == 0) storyboard = data.Score.FirstPegControl.TraditionalBoardStoryboard;     // for Reset()
            if (newScore == -1) storyboard = data.Score.SecondPegControl.TraditionalBoardStoryboard;   // for Reset()

            if (newScore == 0 || newScore == -1)
            {
                foreach (DoubleAnimation da in storyboard.Children)
                {
                    da.To = 0;
                    da.BeginTime = TimeSpan.FromMilliseconds(0);
                    da.Duration = TimeSpan.FromMilliseconds(0);
                }

                DoubleAnimation animateX = (DoubleAnimation)storyboard.Children[(int)PegStoryAnimationChildren.XFirstColumn];
                DoubleAnimation animateY = (DoubleAnimation)storyboard.Children[(int)PegStoryAnimationChildren.YFirstColumn];
                Point to = GetAnimationPoint(data, newScore);
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
            double durationPerPoint = 100 / 5.0;
            if (newScore > 85) durationPerPoint = 100;
            double durationXY = AnimateUpFirstColumn(data, storyboard, newScore, durationPerPoint);
            double durationTop = AnimateAroundTop(data, storyboard, newScore, durationPerPoint / 2);
            double durationThirdColumnY = durationThirdColumnY = AnimateDownThirdColumn(data, storyboard, newScore, durationPerPoint);
            double durationBottomRotation = AnimateAroundBottom(data, storyboard, newScore, durationPerPoint / 2);
            
            double durationUpSecondCol = AnimateUpSecondColumn(data, storyboard, newScore, durationPerPoint);
            double durationToWin = AnimateToWinningPosition(data, storyboard, newScore, durationPerPoint);

            SetDurations(storyboard, durationXY, durationTop, durationThirdColumnY, durationBottomRotation, durationUpSecondCol, durationToWin);
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
            this.TraceMessage($"backpeg Y {((TranslateTransform)((TransformGroup)_playerBackPeg.RenderTransform).Children[4]).Y}");
        }

    }

   

    public enum PegStoryAnimationChildren { XFirstColumn = 0, YFirstColumn = 1, RotateTopAngle = 2, YThirdColumn = 3, RotateBottomAngle = 4, YSecondColumn = 5, XToWinning = 6, YToWinning = 7, XCorrectLayout = 8, YCorrectLayout = 9 };

    public class PlayerDataObject
    {
        PlayerType Type;
        public List<Ellipse> Pegs = new List<Ellipse>();
        public PegScore Score = new PegScore();

        public PlayerDataObject(PlayerType playerType, Storyboard sbFront, Storyboard sbBack, PegControl frontPeg, PegControl backPeg)
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
                else
                {
                    return Score.SecondPegControl.TraditionalBoardStoryboard;
                }
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
                else
                {
                    return Score.FirstPegControl.TraditionalBoardStoryboard;
                }
            }
        }

        public DoubleAnimation TranslateX
        {
            get
            {
                return (DoubleAnimation)BackStoryBoard.Children[(int)PegStoryAnimationChildren.XFirstColumn];
            }
        }

        public DoubleAnimation TranslateY
        {
            get
            {
                return (DoubleAnimation)BackStoryBoard.Children[(int)PegStoryAnimationChildren.YFirstColumn];
            }
        }

        public DoubleAnimation TopRotation
        {
            get
            {
                return (DoubleAnimation)BackStoryBoard.Children[(int)PegStoryAnimationChildren.RotateTopAngle];
            }
        }

        public DoubleAnimation CenterY
        {
            get
            {
                return (DoubleAnimation)BackStoryBoard.Children[(int)PegStoryAnimationChildren.YThirdColumn];
            }
        }

        public DoubleAnimation RotateBottomAngle
        {
            get
            {
                return (DoubleAnimation)BackStoryBoard.Children[(int)PegStoryAnimationChildren.RotateBottomAngle];
            }
        }

        public PegControl BackPeg
        {
            get
            {
                if (Score.FirstPegControl.Score < Score.SecondPegControl.Score)
                {
                    return Score.FirstPegControl;
                }
                else
                {
                    return Score.SecondPegControl;
                }
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
                else
                {
                    return Score.SecondPegControl;
                }
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
                else
                {
                    return Score.Score2;
                }
            }
        }
        public void ResetScore(double width)
        {
            Score.Score1 = -1;
            Score.Score2 = 0;

            TransformGroup tg = Score.FirstPegControl.RenderTransform as TransformGroup;
            ((RotateTransform)tg.Children[1]).CenterY = width - this.BackPeg.ActualWidth / 2.0;

            tg = Score.SecondPegControl.RenderTransform as TransformGroup;
            ((RotateTransform)tg.Children[1]).CenterY = width - this.BackPeg.ActualWidth / 2.0;

        }



    }

}
