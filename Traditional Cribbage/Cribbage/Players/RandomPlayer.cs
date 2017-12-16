using Cards;
using System;
using System.Collections.Generic;

namespace CribbagePlayers
{
    class RandomPlayer : Player
    {

        Random _random = new Random(DateTime.Now.Millisecond);
        
        
        public RandomPlayer() { }
        
        public override Card GetCountCard(List<Card> playedCards, List<Card> uncountedCards, int currentCount)
        {
            //
            //  randomly pick a card until there are no cards left to pick.
            List<Card> myCards = new List<Card>(uncountedCards);
            do
            {
                if (myCards.Count == 0)
                    break;

                int n = _random.Next(myCards.Count);


                if (myCards[n].Value + currentCount <= 31)
                    return myCards[n];

                myCards.RemoveAt(n);

            } while (myCards.Count > 0);


            return null;
        }


        public override List<Card> SelectCribCards(List<Card> hand, bool myCrib)
        {
            //
            //  randomly pick two different cards

            List<Card> crib = new List<Card>();

            int c1 = 0;
            int c2 = 0;
            do
            {
                c1 = _random.Next(hand.Count);
                c2 = _random.Next(hand.Count);

            } while (c1 == c2);

            crib.Add(hand[c1]);
            crib.Add(hand[c2]);
            return crib;
        }

        public override void Init(string parameters)
        {
            Description = "Random Player";
            base.PlayerAlgorithm = PlayerAlgorithm.Random;
        }
    }
}
