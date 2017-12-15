using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Cribbage
{
    public class AddConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double d = (double)value;
            double m = System.Convert.ToDouble(parameter);
            return (double)d + m;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            double d = (double)value;
            double m = System.Convert.ToDouble(parameter);
            return (double)d - m;

        }
    }

    public class IntegerToNegativeInteger : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Thickness input = (Thickness)value;
            Thickness t = new Thickness(-input.Left, -input.Top, -input.Right, -input.Bottom);
            return t;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
          
            throw new NotImplementedException();
        }
    }

    public class ThickNessToInteger : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Thickness input = (Thickness)value;
            return input.Left;

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
          
            throw new NotImplementedException();
        }
    }

    public class EllipseToCenterX : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Ellipse to = (Ellipse)value;
            Ellipse from = (Ellipse)parameter;
            Point center = new Point(0, 0);
            GeneralTransform gt = to.TransformToVisual(from);
            center = gt.TransformPoint(new Point(0, 0));
            center.X += 6;
            center.Y = +6;
            return center;

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
           
            throw new NotImplementedException();
        }
    }

    public class MarginTopBottomConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return new Thickness(0, System.Convert.ToDouble(value), 0, System.Convert.ToDouble(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
           
            throw new NotImplementedException();
        }
    }

    public class MarginTopConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return new Thickness(0, 0, 0, System.Convert.ToDouble(value));
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
           
            throw new NotImplementedException();
        }
    }

    public class MarginBottomConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return new Thickness(0, System.Convert.ToDouble(value), 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
          
            throw new NotImplementedException();
        }
    }

    public class RectConverter : IValueConverter
    {
        public  object Convert(object value, Type targetType, object parameter, string language)
        {
            FrameworkElement el = (FrameworkElement)value;
            el.UpdateLayout();
            Rect rect = new Rect();
            rect.X = 0;
            rect.Y = el.Height / 2.0;
            rect.Width = el.Width;
            rect.Height = el.Height;
            return rect;            
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            
            throw new NotImplementedException();
        }
    }

    public class DoubleToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double d = (double)value;
            double m = System.Convert.ToDouble(parameter);
            return (double)d * m;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            double ret = (double)value / System.Convert.ToDouble(parameter);
            return ret;

        }
    }

   

    public class CenterXConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double containerWidth = ((double)value);
            double width = containerWidth * .0625;
            double ret = containerWidth * 0.5 - width * 0.5;
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
          
            throw new NotImplementedException();
        }
    }


}
