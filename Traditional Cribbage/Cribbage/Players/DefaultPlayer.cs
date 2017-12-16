using System;
using System.Collections.Generic;
using Cards;
using Facet.Combinatorics;



namespace CribbagePlayers
{



    public class DefaultPlayer : Player
    {
        public bool UseDropTable { get; set; } = false;
        public DefaultPlayer() { }

        public DefaultPlayer(bool useDropTable)
        {
            UseDropTable = useDropTable;
            if (UseDropTable)
                Description = "Default player using Drop Table";
            else
                Description = "Default player no Drop Table";


        }



        public override Card GetCountCard(List<Card> playedCards, List<Card> uncountedCards, int currentCount)
        {
            int maxScore = -1;
            Card maxCard = null;
            int score = 0;

            //
            // if we have only 1 card, play it if we legally can
            //  NOTE: we assume that the Player correctly returns a legal card!
            //
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
                score = CardScoring.ScoreCountingCardsPlayed(playedCards, c, currentCount);
                if (score > maxScore)
                {
                    maxScore = score;
                    maxCard = c;
                }
            }

            if (maxScore == -1)
                return null; // we have no valid card to play

            if (maxScore > 0)
            {
                // if we can play a card to score points, play it
                return maxCard;
            }

            if (maxScore == 0) // there isn't a card for us to play that generates points
            {
                //
                //  play a card that we have a pair so we can get 3 of a kind - as long as it isn't a 5 and the 3 of a kind makes > 31
                //
                //  this optimization changes the average count score from 2.59948 to 2.66909 over 100,000 games
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
                //  make the right choice if assuming they'll play a 10
                //
                //  this optimization changes the average count score from 2.64235 to 2.67764 over 100,000 games
                //
                Combinations<Card> combinations = new Combinations<Card>(uncountedCards, 2); // at most 6 of these: 4 choose 2
                foreach (List<Card> cards in combinations)
                {
                    int sum = cards[0].Value + cards[1].Value;
                    if (sum + currentCount == 5) // i'll 15 them if they play a 10
                        return cards[1];

                    if (sum + currentCount == 21) // i'll 31 them if they play a 10
                        return cards[1];

                }

                // tried returning the smallest legal card -- no difference
                // tried returning the highest legal card -- no difference

            }

            // tried to generate a random card if currentCount == 0 -- no difference

            
            //
            //  this one is important -- it basically says "if you can't score any points, induce a 3 of a kind, or try to create a run play whatever card we ened up with.
            //  UNLESS IT IS A FIVE!...then pick a different one.  over the course of 100,000 games, this is the difference between 2.61423 and 2.71498 ave counting points
            //  turns out that if we don't do this, then both players get ~2.65 points / counting session - e.g. if one is not worried about dropping 5's and the other is, it
            //  adds about .1/count and if both are being silly and dropping 5's then both get about .04 ave point boost.  still a good optimization when playing humans.

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
                double score = (double)CardScoring.ScoreHand(cards, null, HandType.Regular);
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
            if (parameters == "-usedroptable")
            {
                UseDropTable = true;
                Description = "Default player using Drop Table";
            }
            else

                Description = "Default player no Drop Table";
        }
    }


}
