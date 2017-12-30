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
        private async void OnTestDeal(object sender, RoutedEventArgs e)
        {
            try
            {
                MyMenu.IsPaneOpen = false;
                await OnDeal();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in OnDeal: {ex.Message}");
            }
            finally
            {
                ((Button)(sender)).IsEnabled = true;
            }

           ((Button)(sender)).IsEnabled = true;

        }

        private async Task OnDeal()
        {
            ResetCards();

            var (computerCards, playerCards, sharedCard) = Game.GetHands();

            Point orig = _cgDeck.GetNextCardPosition(sharedCard[0]);
            
            await this.Deal(playerCards, computerCards, sharedCard, new List<CardCtrl> { computerCards[0], computerCards[2] }, PlayerType.Computer);
            List<CardCtrl> playerCribCards = new List<CardCtrl>() { _cgPlayer.Cards[0], _cgPlayer.Cards[1] };
            int index = 2;
            foreach (CardCtrl card in playerCribCards)
            {
                await CardGrid.AnimateMoveOneCard(_cgPlayer, _cgDiscarded, card, index++, false, MOVE_CARDS_ANIMATION_DURATION, 0);
            }

            CardGrid.TransferCards(_cgPlayer, _cgDiscarded, playerCribCards);

        }

        private async void OnTestCribToOwner(object sender, RoutedEventArgs e)
        {
            await this.AnimateMoveCribCardsBackToOwner(PlayerType.Computer);
        }

        private void OnShowScrollingText(object sender, RoutedEventArgs e)
        {
            _scoreViewCtrl.AddMessage("one");
            _scoreViewCtrl.AddMessage("This is the first one");            
            _scoreViewCtrl.AddMessage("This is the second one 55555555555555555555555555555555555555555555555555555555555555555555555555555555555555");

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
                if (_testScore < 79)
                {

                    delta = 79;
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
