using Cards;
using Facet.Combinatorics;
using System;
using System.Collections.Generic;


namespace CribbagePlayers
{
    class CountingPlayer : Player
    {

        public CountingPlayer() { }
        public bool UseDropTable { get; set; } = true;

        public CountingPlayer(bool useDropTable)
        {
            UseDropTable = useDropTable;
            Description = "Improved Counting using Drop Table";



        }



        public override Card GetCountCard(List<Card> playedCards, List<Card> uncountedCards, int currentCount)
        {
            int maxScore = -1;
            Card maxCard = null;
            int score = 0;

            if (uncountedCards.Count == 1)
            {
                if (uncountedCards[0].Value + currentCount <= 31)
                    return uncountedCards[0];
                else
                    return null;
            }

            //
            //  see which card we can play that gives us the most points
            foreach (Card c in uncountedCards)
            {
                score = CardScoring.ScoreCountingCardsPlayed(playedCards, c, currentCount, out List<Score> scoreList);
                if (score > maxScore)
                {
                    maxScore = score;
                    maxCard = c;
                }
            }

            if (maxScore == -1)
                return null; // we have no valid card to play


            if (maxScore == 0) // there isn't a card for us to play that generates points
            {

                //
                //  play a card that we have a pair so we can get 3 of a kind - as long as it isn't a 5 and the 3 of a kind makes > 31
                //

                for (int i = 0; i < uncountedCards.Count - 1; i++)
                {

                    //  dont' do it if it will force us over 31
                    if (uncountedCards[i].Rank * 3 + currentCount > 31)
                        continue;

                    if (uncountedCards[i].Rank == uncountedCards[i + 1].Rank)
                    {
                        if (uncountedCards[i].Rank != 5)
                            return uncountedCards[i];

                    }
                }

                //
                //  try to play a card that will create a run
                Combinations<Card> combinations = new Combinations<Card>(uncountedCards, 2); // at most 6 of these: 4 choose 2
                foreach (List<Card> cards in combinations)
                {
                    int diff = Math.Abs(cards[0].CardOrdinal - cards[1].CardOrdinal);
                    if (diff == 1) // they are consecutive
                    {
                        int val = cards[0].Value + cards[1].Value;
                        if (val + currentCount > 31) continue;
                        //
                        //  assume sorted

                        if (cards[0].CardOrdinal > CardOrdinal.Ace)
                        {
                            //
                            //  this means we have somehing like 3 and 4 in our hand.  if we play 7 and they play 6, we can play the 8
                            if (val + currentCount + cards[0].Value - 1 <= 31)
                            {
                                //Debug.WriteLine($"Trying to get a run! {cards[0].CardName} and {cards[1].CardName}");
                                if (cards[0].Rank != 5)
                                    return cards[0];
                                else
                                    return cards[1];
                            }

                        }
                        else
                        {
                            int highCardVal = cards[1].Value;
                            if (highCardVal > 10) highCardVal = 10;
                            if (val + currentCount + highCardVal <= 31)
                            {
                                //Debug.WriteLine($"Trying to get a run with a gap! {cards[0].Ordinal} and {cards[1].Ordinal}");
                                if (cards[0].Rank != 5)
                                    return cards[0];
                                else
                                    return cards[1];
                            }
                        }


                    }

                    if (diff == 2) // there is a gap between the two cards -- eg.  4 and 6
                    {


                    }

                }

                //
                //  make the right choice if assuming they'll play a 10
                //
                combinations = new Combinations<Card>(uncountedCards, 2); // at most 6 of these: 4 choose 2
                foreach (List<Card> cds in combinations)
                {
                    int sum = cds[0].Value + cds[1].Value;
                    if (sum + currentCount == 5) // i'll 15 them if they play a 10
                        return cds[1];

                    if (sum + currentCount == 21) // i'll 31 them if they play a 10
                        return cds[1];

                }

            }

            if (maxCard.Rank == 5)
            {
                // try to find a non 5 card to play
                foreach (Card c in uncountedCards)
                {
                    if (c.Rank != 5 && c.Value + currentCount <= 31)
                    {
                        maxCard = c;
                        break;
                    }
                }
            }

            return maxCard;
        }


        public override List<Card> SelectCribCards(List<Card> hand, bool myCrib)
        {
            Combinations<Card> combinations = new Combinations<Card>(hand, 4);
            List<Card> maxCrib = null;
            double maxScore = -1000.0;

            foreach (List<Card> cards in combinations)
            {
                double score = (double)CardScoring.ScoreHand(cards, null, HandType.Regular, out List<Score> scoreList);
                List<Card> crib = GetCrib(hand, cards);
                if (UseDropTable)
                {
                    double expectedValue = 0.0;
                    if (myCrib)
                    {
                        expectedValue = CribbageStats.dropTableToMyCrib[crib[0].Rank - 1, crib[1].Rank - 1];
                        score += expectedValue;
                    }
                    else
                    {
                        expectedValue = CribbageStats.dropTableToYouCrib[crib[0].Rank - 1, crib[1].Rank - 1];
                        score -= expectedValue;
                    }

                }
                if (score > maxScore)
                {
                    maxScore = score;
                    maxCrib = crib;
                }
            }

            return maxCrib;
        }

        private List<Card> GetCrib(List<Card> hand, List<Card> cards)
        {
            List<Card> crib = new List<Card>(hand);

            foreach (Card card in cards)
            {
                crib.Remove(card);
            }
            return crib;
        }

        public override void Init(string parameters)
        {
           
            base.PlayerAlgorithm = PlayerAlgorithm.ImprovedCounting;

        }
    }
}

