using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Shapes;
using Cribbage.Annotations;

namespace Cribbage
{
    public class EnumBooleanConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;

            object parameterValue = Enum.Parse(value.GetType(), parameterString);

            return parameterValue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            return Enum.Parse(targetType, parameterString);
        }
        #endregion

       
    }
    public class AddConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var d = (double) value;
            var m = System.Convert.ToDouble(parameter);
            return d + m;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var d = (double) value;
            var m = System.Convert.ToDouble(parameter);
            return d - m;
        }
    }

    public class IntegerToNegativeInteger : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var input = (Thickness) value;
            var t = new Thickness(-input.Left, -input.Top, -input.Right, -input.Bottom);
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
            var input = (Thickness) value;
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
            var to = (Ellipse) value;
            var from = (Ellipse) parameter;
            var center = new Point(0, 0);
            var gt = to.TransformToVisual(from);
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
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var el = (FrameworkElement) value;
            el.UpdateLayout();
            var rect = new Rect
            {
                X = 0,
                Y = el.Height / 2.0,
                Width = el.Width,
                Height = el.Height
            };
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
            var d = (double) value;
            var m = System.Convert.ToDouble(parameter);
            return d * m;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var ret = (double) value / System.Convert.ToDouble(parameter);
            return ret;
        }
    }


    public class CenterXConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var containerWidth = (double) value;
            var width = containerWidth * .0625;
            var ret = containerWidth * 0.5 - width * 0.5;
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}