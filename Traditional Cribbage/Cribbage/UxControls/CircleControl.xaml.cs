
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public sealed partial class CircleControl : UserControl, INotifyPropertyChanged

    {

        public static readonly DependencyProperty TranslateXProperty = DependencyProperty.Register("TranslateX", typeof(double), typeof(CircleControl), null);
        public static readonly DependencyProperty TranslateYProperty = DependencyProperty.Register("TranslateY", typeof(double), typeof(CircleControl), null);
        public static readonly DependencyProperty CenterXProperty = DependencyProperty.Register("CenterX", typeof(double), typeof(CircleControl), null);
        public static readonly DependencyProperty CenterYProperty = DependencyProperty.Register("CenterY", typeof(double), typeof(CircleControl), null);
        public static readonly DependencyProperty RotationProperty = DependencyProperty.Register("Rotation", typeof(double), typeof(CircleControl), null);
        public static readonly DependencyProperty PlayerTypeProperty = DependencyProperty.Register("PlayerType", typeof(double), typeof(CircleControl), null);
        
        PlayerType _type = PlayerType.Player;
        
        public CircleControl()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        public Brush Fill
        {
            set
            {
                _ellipse.Fill = value;

            }
            get
            {

                return (Brush)_ellipse.Fill;
            }

        }

        private void Control_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double diameter = Math.Min(e.NewSize.Width, e.NewSize.Height);
            _ellipse.Width = diameter;
            _ellipse.Height = diameter;
            
        }

        public PlayerType PlayerType
        {
            get
            {
                return (PlayerType)GetValue(PlayerTypeProperty);
            }
            set
            {

                SetValue(PlayerTypeProperty, value);
                _type = value;
                NotifyPropertyChanged();
               
            }

        }

        public double TranslateX
        {
            get
            {
                return (double)GetValue(TranslateXProperty); ;
            }
            set
            {
                SetValue(TranslateXProperty, value);
                _transform.TranslateX = value;
                NotifyPropertyChanged();
            }
        }

        public double TranslateY
        {
            get
            {
                return (double)GetValue(TranslateYProperty); ;
            }
            set
            {
                SetValue(TranslateYProperty, value);
                _transform.TranslateY = value;
                NotifyPropertyChanged();
            }
        }

        public double CenterX
        {
            get
            {
                return (double)GetValue(CenterXProperty); ;
            }
            set
            {
                SetValue(CenterXProperty, value);
                _transform.CenterX = value;
                NotifyPropertyChanged();
            }
        }

        public double CenterY
        {
            get
            {
                return (double)GetValue(CenterYProperty); ;
            }
            set
            {
                SetValue(CenterYProperty, value);
                _transform.CenterY = value;
                NotifyPropertyChanged();
            }
        }

        public double Rotation
        {
            get
            {
                return (double)GetValue(RotationProperty); ;
            }
            set
            {
                SetValue(RotationProperty, value);
                _transform.Rotation = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
       
    }
}
