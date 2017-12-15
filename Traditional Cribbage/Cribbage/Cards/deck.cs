using Cribbage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Cards
{
    public class Deck
    {

        int[] _randomIndeces = new int[52];
        int _index = 0; // the index of the cards handed out

        public Deck()
        {
            Shuffle();
        }

        public void Shuffle()
        {
            MersenneTwister twist = new MersenneTwister();


            for (int i = 0; i < 52; i++)
            {
                _randomIndeces[i] = i;

            }

            int temp = 0;
            for (int n = 0; n < 52; n++)
            {
                int k = twist.Next(n + 1);
                temp = _randomIndeces[n];
                _randomIndeces[n] = _randomIndeces[k];
                _randomIndeces[k] = temp;
            }

        }
        /// <summary>
        ///     This is where the cards are created...
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public List<CardCtrl> GetCards(int number, Owner owner)
        {
            List<CardCtrl> cards = new List<CardCtrl>();
            for (int i = _index; i < number + _index; i++)
            {
                CardCtrl c = new CardCtrl();
                c.Width = 125;
                c.Height = 175;
                c.Margin = new Thickness(0);
                Grid.SetColumnSpan(c, 99); // should be enough...
                Grid.SetRowSpan(c, 99); // should be enough...
                c.HorizontalAlignment = HorizontalAlignment.Left;
                c.Owner = owner;
                c.VerticalAlignment = VerticalAlignment.Top;
                c.Orientation = CardOrientation.FaceDown;
                c.CardName = (CardNames)_randomIndeces[i];
                c.ShowDebugInfo = false;
                cards.Add(c);
            }

            _index += number;

            return cards;
        }


    }
}
