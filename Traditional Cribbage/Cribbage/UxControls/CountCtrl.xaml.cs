using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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


    public sealed partial class CountCtrl : UserControl
    {
        int _count = 0;
       
        public Control LogicalParent
        {
            get { return (Control)GetValue(LogicalParentProperty); }
            set { SetValue(LogicalParentProperty, value); }
        }

        public CountCtrl()
        {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty LogicalParentProperty = DependencyProperty.Register("LogicalParent", typeof(Control), typeof(CountCtrl), null);
        public static readonly DependencyProperty CountProperty = DependencyProperty.Register("Count", typeof(int), typeof(CountCtrl), null);

        public int Count
        {
            get
            {
                return _count;

            }

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

            Control parent = this.LogicalParent;

            

            double scaleX = parent.ActualWidth / this.ActualWidth;
            double scaleY = parent.ActualHeight / this.ActualHeight;

            if (scaleX < scaleY)
                scaleY = scaleX;
            else
                scaleX = scaleY;

            


            GeneralTransform gt = LogicalParent.TransformToVisual(this);
            Point ptTo = new Point(parent.ActualWidth * .5 - this.ActualWidth * .5, parent.ActualHeight * 0.5 - this.ActualHeight* .5);
            ptTo = gt.TransformPoint(ptTo);

        

            _compositeTransform.ScaleX = scaleX;
            _compositeTransform.ScaleY = scaleY;
            _compositeTransform.TranslateX = ptTo.X;
            _compositeTransform.TranslateY = ptTo.Y;
            this.UpdateLayout();

        }

        public void Show()
        {
            _daOpacity.To = 1.0;
            StaticHelpers.RunStoryBoardAsync(_sbOpacity, 1000, true);
            
        }

        public void Hide()
        {
            _daOpacity.To = 0.0;
            StaticHelpers.RunStoryBoardAsync(_sbOpacity,1000, true);
        }

     




    }
}
