using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cards;
using Cribbage.Players;

namespace CribbagePlayers
{
    internal class RandomPlayer : Player
    {
        private readonly Random _random = new Random(DateTime.Now.Millisecond);


        public override Task<Card> GetCountCard(List<Card> playedCards, List<Card> uncountedCards, int currentCount)
        {
            //
            //  randomly pick a card until there are no cards left to pick.
            var myCards = new List<Card>(uncountedCards);
            do
            {
                if (myCards.Count == 0)
                {
                    break;
                }

                var n = _random.Next(myCards.Count);


                if (myCards[n].Value + currentCount <= 31)
                {
                    return Task.FromResult(myCards[n]);
                }

                myCards.RemoveAt(n);
            } while (myCards.Count > 0);


            return null;
        }


        public override Task<List<Card>> SelectCribCards(List<Card> hand, bool myCrib)
        {
            //
            //  randomly pick two different cards

            var crib = new List<Card>();

            var c1 = 0;
            var c2 = 0;
            do
            {
                c1 = _random.Next(hand.Count);
                c2 = _random.Next(hand.Count);
            } while (c1 == c2);

            crib.Add(hand[c1]);
            crib.Add(hand[c2]);
            return Task.FromResult(crib);
        }

        public override void Init(string parameters)
        {
            Description = "Random Player";
            PlayerAlgorithm = PlayerAlgorithm.Random;
        }
    }
}