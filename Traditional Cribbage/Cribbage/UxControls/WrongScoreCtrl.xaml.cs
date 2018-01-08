using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Cribbage.Annotations;
using WinRTXamlToolkit.AwaitableUI;
using LongShotHelpers;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage.UxControls
{
    public sealed partial class WrongScoreCtrl : UserControl, INotifyPropertyChanged
    {
        public int WrongScore { get; set; } = 0;
        public WrongScoreOption Option { get; set; } = WrongScoreOption.DoNothing;

        public WrongScoreCtrl()
        {
            this.InitializeComponent();
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {

        }

        public async Task WaitForClose()
        {
            NotifyPropertyChanged(@"Option");
            _txtPrompt.Text = $"{WrongScore} is the wrong score.";
            await _btnClose.WaitForClickAsync();
        }

        private async void NonClientRectangle_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            await StaticHelpers.DragAsync(this, e, null);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
