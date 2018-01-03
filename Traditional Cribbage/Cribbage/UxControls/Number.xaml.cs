using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public sealed partial class NumberControl : UserControl
    {
        public NumberControl()
        {
            InitializeComponent();
        }

        public string Text
        {
            get => _txtCount.Text;
            set
            {
                _txtCount.Text = value;
                _txtShadow.Text = value;
            }
        }

        public new double FontSize
        {
            get => _txtCount.FontSize;
            set
            {
                _txtCount.FontSize = value;
                _txtShadow.FontSize = value;
            }
        }
    }
}