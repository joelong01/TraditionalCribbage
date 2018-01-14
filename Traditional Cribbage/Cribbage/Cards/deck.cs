using System.Collections.Generic;
using Cribbage;
using MersenneTwister;

namespace Cards
{
    public class Deck
    {
        private int _index; // the index of the cards handed out

        private readonly int[] _randomIndeces = new int[52];

        public Deck(int seed, bool shuffle = true)
        {
            for (var i = 0; i < 52; i++)
            {
                _randomIndeces[i] = i;
            }

            if (shuffle)
            {
                Shuffle(seed);
            }
        }

        public void Shuffle(int seed)
        {
            var twist = Randoms.Create(seed, RandomType.FastestInt32);

            for (var i = 0; i < 52; i++)
            {
                _randomIndeces[i] = i;
            }

            for (var n = 0; n < 52; n++)
            {
                var k = twist.Next(n + 1);
                var temp = _randomIndeces[n];
                _randomIndeces[n] = _randomIndeces[k];
                _randomIndeces[k] = temp;
            }

            _index = 0;
        }

        public List<Card> GetCards(int number, Owner owner)
        {
            var cards = new List<Card>();
            for (var i = _index; i < number + _index; i++)
            {
                var c = new Card((CardName) _randomIndeces[i])
                {
                    Owner = owner
                };
                cards.Add(c);
            }

            _index += number;

            return cards;
        }
    }
}