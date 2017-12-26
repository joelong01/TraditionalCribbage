using Cards;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Cribbage;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.UI.Core;
using CardView;
using LongShotHelpers;
using CribbagePlayers;
using System.Threading;
using System.Text;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.Provider;

namespace Cribbage
{
    public sealed partial class MainPage : Page
    {
        private async void OnTestCribToOwner(object sender, RoutedEventArgs e)
        {
            await this.AnimateMoveCribCardsBackToOwner(PlayerType.Computer);
        }

        private async void OnTestEndHandAnimation(object sender, RoutedEventArgs e)
        {
            await AnimationEndHand(PlayerType.Computer);
        }

        private void OnTestMoveToCrib(object sender, RoutedEventArgs e)
        {
            MoveCrib(PlayerType.Computer);
        }
        int _testScore = 0;
        private async void OnTestAddScore(object sender, RoutedEventArgs e)
        {

            try
            {


                ((Button)sender).IsEnabled = false;

                int delta = 0;
                if (_testScore < 80)
                {

                    delta = 84;
                }
                else if (_testScore > 85)
                {
                    delta = 5;

                }
                else
                {
                    delta = 1;
                }

                _testScore += delta;

                List<Task> taskList = new List<Task>();
                _board.TraceBackPegPosition();
                taskList.AddRange(_board.AnimateScore(PlayerType.Player, delta));
                taskList.AddRange(_board.AnimateScore(PlayerType.Computer, delta));
                await Task.WhenAll(taskList);
                _board.TraceBackPegPosition();

            }
            catch (Exception ex)
            {

                this.TraceMessage($"Exception: {ex.Message}");

            }
            finally
            {

                ((Button)sender).IsEnabled = true;

            }

        }

        private async void OnTestReset(object sender, RoutedEventArgs e)
        {
            _testScore = 0;
            await _board.Reset();


        }
    }
    
}
