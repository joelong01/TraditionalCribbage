using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public sealed partial class NumberControl : UserControl
    {
        public NumberControl()
        {
            this.InitializeComponent();
        }

        public string Text
        {
            get
            {
                return _txtCount.Text;
            }
            set
            {
                _txtCount.Text = value;
                _txtShadow.Text = value;
            }
        }

        public new double FontSize
        {
            get
            {
                return _txtCount.FontSize;
            }
            set
            {

                _txtCount.FontSize = value;
                _txtShadow.FontSize = value;
            }

        }
    }
}
