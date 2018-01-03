using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using CardView;
using LongShotHelpers;

namespace Cribbage
{
    public sealed partial class MainPage : Page
    {
        private int _testScore;

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
                ((Button) sender).IsEnabled = true;
            }

            ((Button) sender).IsEnabled = true;
        }

        private async Task OnDeal()
        {
            ResetCards();

            var (computerCards, playerCards, sharedCard) = Game.GetHands();

            var orig = _cgDeck.GetNextCardPosition(sharedCard[0]);

            await Deal(playerCards, computerCards, sharedCard, new List<CardCtrl> {computerCards[0], computerCards[2]},
                PlayerType.Computer);
            var playerCribCards = new List<CardCtrl> {_cgPlayer.Cards[0], _cgPlayer.Cards[1]};
            var index = 2;
            foreach (var card in playerCribCards)
                await CardGrid.AnimateMoveOneCard(_cgPlayer, _cgDiscarded, card, index++, false,
                    MOVE_CARDS_ANIMATION_DURATION, 0);

            CardGrid.TransferCards(_cgPlayer, _cgDiscarded, playerCribCards);
        }

        private async void OnTestCribToOwner(object sender, RoutedEventArgs e)
        {
            await AnimateMoveCribCardsBackToOwner(PlayerType.Computer);
        }

        private void OnShowScrollingText(object sender, RoutedEventArgs e)
        {
        }

        private void OnTestMoveToCrib(object sender, RoutedEventArgs e)
        {
            MoveCrib(PlayerType.Computer);
        }

        private async void OnTestAddScore(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button) sender).IsEnabled = false;

                var delta = 0;
                if (_testScore < 79)
                    delta = 79;
                else if (_testScore > 85)
                    delta = 5;
                else
                    delta = 1;

                _testScore += delta;

                var taskList = new List<Task>();
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
                ((Button) sender).IsEnabled = true;
            }
        }

        private async void OnTestReset(object sender, RoutedEventArgs e)
        {
            _testScore = 0;
            await _board.Reset();
        }
    }
}