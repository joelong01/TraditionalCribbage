using Cribbage;
using System.Collections.Generic;
using MersenneTwister;
using System;

namespace Cards
{



    public class Deck
    {
        
        int[] _randomIndeces = new int[52];
        int _index = 0; // the index of the cards handed out

        public Deck(int seed)
        {
            Shuffle(seed);
        }

        public void Shuffle(int seed)
        {

            Random twist = Randoms.Create(seed, RandomType.FastestInt32);
            
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

            _index = 0;
        }

        public List<Card> GetCards(int number, Owner owner)
        {
            List<Card> cards = new List<Card>();
            for (int i = _index; i < number + _index; i++)
            {
                Card c = new Card((CardNames)_randomIndeces[i])
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
