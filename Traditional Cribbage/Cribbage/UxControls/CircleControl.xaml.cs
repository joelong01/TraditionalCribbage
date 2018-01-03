using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public sealed partial class CircleControl : UserControl, INotifyPropertyChanged

    {
        public static readonly DependencyProperty TranslateXProperty =
            DependencyProperty.Register("TranslateX", typeof(double), typeof(CircleControl), null);

        public static readonly DependencyProperty TranslateYProperty =
            DependencyProperty.Register("TranslateY", typeof(double), typeof(CircleControl), null);

        public static readonly DependencyProperty CenterXProperty =
            DependencyProperty.Register("CenterX", typeof(double), typeof(CircleControl), null);

        public static readonly DependencyProperty CenterYProperty =
            DependencyProperty.Register("CenterY", typeof(double), typeof(CircleControl), null);

        public static readonly DependencyProperty RotationProperty =
            DependencyProperty.Register("Rotation", typeof(double), typeof(CircleControl), null);

        public static readonly DependencyProperty PlayerTypeProperty =
            DependencyProperty.Register("PlayerType", typeof(double), typeof(CircleControl), null);

        private PlayerType _type = PlayerType.Player;

        public CircleControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        public Brush Fill
        {
            set => _ellipse.Fill = value;
            get => _ellipse.Fill;
        }

        public PlayerType PlayerType
        {
            get => (PlayerType) GetValue(PlayerTypeProperty);
            set
            {
                SetValue(PlayerTypeProperty, value);
                _type = value;
                NotifyPropertyChanged();
            }
        }

        public double TranslateX
        {
            get => (double) GetValue(TranslateXProperty);
            set
            {
                SetValue(TranslateXProperty, value);
                _transform.TranslateX = value;
                NotifyPropertyChanged();
            }
        }

        public double TranslateY
        {
            get => (double) GetValue(TranslateYProperty);
            set
            {
                SetValue(TranslateYProperty, value);
                _transform.TranslateY = value;
                NotifyPropertyChanged();
            }
        }

        public double CenterX
        {
            get => (double) GetValue(CenterXProperty);
            set
            {
                SetValue(CenterXProperty, value);
                _transform.CenterX = value;
                NotifyPropertyChanged();
            }
        }

        public double CenterY
        {
            get => (double) GetValue(CenterYProperty);
            set
            {
                SetValue(CenterYProperty, value);
                _transform.CenterY = value;
                NotifyPropertyChanged();
            }
        }

        public double Rotation
        {
            get => (double) GetValue(RotationProperty);
            set
            {
                SetValue(RotationProperty, value);
                _transform.Rotation = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Control_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var diameter = Math.Min(e.NewSize.Width, e.NewSize.Height);
            _ellipse.Width = diameter;
            _ellipse.Height = diameter;
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}