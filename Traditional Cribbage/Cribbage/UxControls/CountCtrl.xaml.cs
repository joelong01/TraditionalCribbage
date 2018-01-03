using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using LongShotHelpers;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public sealed partial class CountCtrl : UserControl
    {
        public static readonly DependencyProperty LogicalParentProperty =
            DependencyProperty.Register("LogicalParent", typeof(Control), typeof(CountCtrl), null);

        public static readonly DependencyProperty CountProperty =
            DependencyProperty.Register("Count", typeof(int), typeof(CountCtrl), null);

        private int _count;

        public CountCtrl()
        {
            InitializeComponent();
        }

        public Control LogicalParent
        {
            get => (Control) GetValue(LogicalParentProperty);
            set => SetValue(LogicalParentProperty, value);
        }

        public int Count
        {
            get => _count;

            set
            {
                _count = value;
                _txtCount.Text = _count.ToString();
                _txtCountShowdow.Text = _count.ToString();
            }
        }

        public void Locate()
        {
            if (LogicalParent == null)
                return;

            var parent = LogicalParent;


            var scaleX = parent.ActualWidth / ActualWidth;
            var scaleY = parent.ActualHeight / ActualHeight;

            if (scaleX < scaleY)
                scaleY = scaleX;
            else
                scaleX = scaleY;


            var gt = LogicalParent.TransformToVisual(this);
            var ptTo = new Point(parent.ActualWidth * .5 - ActualWidth * .5,
                parent.ActualHeight * 0.5 - ActualHeight * .5);
            ptTo = gt.TransformPoint(ptTo);


            _compositeTransform.ScaleX = scaleX;
            _compositeTransform.ScaleY = scaleY;
            _compositeTransform.TranslateX = ptTo.X;
            _compositeTransform.TranslateY = ptTo.Y;
            UpdateLayout();
        }

        public void Show()
        {
            _daOpacity.To = 1.0;
            var ignore = StaticHelpers.RunStoryBoard(_sbOpacity, true, 1000, true);
        }

        public void Hide()
        {
            _daOpacity.To = 0.0;
            var ignore = StaticHelpers.RunStoryBoard(_sbOpacity, true, 1000, true);
        }
    }
}