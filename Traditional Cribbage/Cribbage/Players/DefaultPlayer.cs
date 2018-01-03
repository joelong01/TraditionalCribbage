using System.Collections.Generic;
using System.Threading.Tasks;
using Cards;
using Combinatorics.Collections;
using Cribbage.Players;

namespace CribbagePlayers
{
    public class DefaultPlayer : Player
    {
        public DefaultPlayer(int score = 0)
        {
            Score = score;
        }

        public DefaultPlayer(bool useDropTable)
        {
            UseDropTable = useDropTable;
            Description = UseDropTable ? "Default player using Drop Table" : "Default player no Drop Table";
        }

        public bool UseDropTable { get; set; }


        public override Task<Card> GetCountCard(List<Card> playedCards, List<Card> uncountedCards, int currentCount)
        {
            var maxScore = -1;
            Card maxCard = null;
            var score = 0;


            //
            // if we have only 1 card, play it if we legally can
            //  NOTE: we assume that the Player correctly returns a legal card!
            //
            if (uncountedCards.Count == 1)
                if (uncountedCards[0].Value + currentCount <= 31)
                    return Task.FromResult(uncountedCards[0]);
                else
                    return Task.FromResult<Card>(null);

            //
            //  see which card we can play that gives us the most points
            foreach (var c in uncountedCards)
            {
                score = CardScoring.ScoreCountingCardsPlayed(playedCards, c, currentCount, out var scoreList);
                if (score > maxScore)
                {
                    maxScore = score;
                    maxCard = c;
                }
            }

            if (maxScore == -1)
                return Task.FromResult<Card>(null);
            ; // we have no valid card to play

            if (maxScore > 0) return Task.FromResult(maxCard);

            if (maxScore == 0) // there isn't a card for us to play that generates points
            {
                //
                //  play a card that we have a pair so we can get 3 of a kind - as long as it isn't a 5 and the 3 of a kind makes > 31
                //
                //  this optimization changes the average count score from 2.59948 to 2.66909 over 100,000 games
                //
                for (var i = 0; i < uncountedCards.Count - 1; i++)
                {
                    //  dont' do it if it will force us over 31
                    if (uncountedCards[i].Rank * 3 + currentCount > 31)
                        continue;

                    if (uncountedCards[i].Rank == uncountedCards[i + 1].Rank)
                        if (uncountedCards[i].Rank != 5)
                            return Task.FromResult(uncountedCards[i]);
                }

                //
                //  make the right choice if assuming they'll play a 10
                //
                //  this optimization changes the average count score from 2.64235 to 2.67764 over 100,000 games
                //
                var combinations = new Combinations<Card>(uncountedCards, 2); // at most 6 of these: 4 choose 2
                foreach (List<Card> cards in combinations)
                {
                    var sum = cards[0].Value + cards[1].Value;
                    if (sum + currentCount == 5) // i'll 15 them if they play a 10
                        return Task.FromResult(cards[1]);

                    if (sum + currentCount == 21) // i'll 31 them if they play a 10
                        return Task.FromResult(cards[1]);
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
                foreach (var c in uncountedCards)
                    if (c.Rank != 5 && c.Value + currentCount <= 31)
                    {
                        maxCard = c;
                        break;
                    }

            //
            //   don't play a card that adds up to 5 because there are so many 10's

            if (maxCard.Value + currentCount != 5)
            {
                return Task.FromResult(maxCard);
            }


            foreach (var c in uncountedCards)
            {
                //
                //  also don't play a card that if they play the same card will give them
                //  both a pair and a 15
                if (c.Value + currentCount == 5 || c.Value * 2 + currentCount == 15)
                {
                }
                else
                {
                    maxCard = c;
                    break;
                }
            }


            return Task.FromResult(maxCard);
        }


        public override Task<List<Card>> SelectCribCards(List<Card> hand, bool myCrib)
        {
            var combinations = new Combinations<Card>(hand, 4);
            List<Card> maxCrib = null;
            var maxScore = -1000.0;
            if (hand.Count == 6)
                foreach (List<Card> cards in combinations)
                {
                    double score = CardScoring.ScoreHand(cards, null, HandType.Hand, out var scoreList);
                    var crib = GetCrib(hand, cards);
                    if (UseDropTable)
                    {
                        var expectedValue = 0.0;
                        if (myCrib)
                        {
                            expectedValue = CribbageStats.DropTableToMyCrib[crib[0].Rank - 1, crib[1].Rank - 1];
                            score += expectedValue;
                        }
                        else
                        {
                            expectedValue = CribbageStats.DropTableToYouCrib[crib[0].Rank - 1, crib[1].Rank - 1];
                            score -= expectedValue;
                        }
                    }

                    if (score > maxScore)
                    {
                        maxScore = score;
                        maxCrib = crib;
                    }
                }

            return Task.FromResult(maxCrib);
        }

        private List<Card> GetCrib(List<Card> hand, List<Card> cards)
        {
            var crib = new List<Card>(hand);

            foreach (var card in cards) crib.Remove(card);
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

            {
                Description = "Default player no Drop Table";
            }
        }
    }
}