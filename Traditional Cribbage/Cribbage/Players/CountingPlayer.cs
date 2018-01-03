using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cards;
using Combinatorics.Collections;
using Cribbage.Players;

namespace CribbagePlayers
{
    internal class CountingPlayer : Player
    {
        public CountingPlayer()
        {
        }

        public CountingPlayer(bool useDropTable)
        {
            UseDropTable = useDropTable;
            Description = "Improved Counting using Drop Table";
        }

        public bool UseDropTable { get; set; } = true;


        public override Task<Card> GetCountCard(List<Card> playedCards, List<Card> uncountedCards, int currentCount)
        {
            var maxScore = -1;
            Card maxCard = null;
            var score = 0;

            if (uncountedCards.Count == 1)
                if (uncountedCards[0].Value + currentCount <= 31)
                    return Task.FromResult(uncountedCards[0]);
                else
                    return null;

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
                return null; // we have no valid card to play


            if (maxScore == 0) // there isn't a card for us to play that generates points
            {
                //
                //  play a card that we have a pair so we can get 3 of a kind - as long as it isn't a 5 and the 3 of a kind makes > 31
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
                //  try to play a card that will create a run
                var combinations = new Combinations<Card>(uncountedCards, 2); // at most 6 of these: 4 choose 2
                foreach (List<Card> cards in combinations)
                {
                    var diff = Math.Abs(cards[0].CardOrdinal - cards[1].CardOrdinal);
                    if (diff == 1) // they are consecutive
                    {
                        var val = cards[0].Value + cards[1].Value;
                        if (val + currentCount > 31) continue;
                        //
                        //  assume sorted

                        if (cards[0].CardOrdinal > CardOrdinal.Ace)
                        {
                            //
                            //  this means we have somehing like 3 and 4 in our hand.  if we play 7 and they play 6, we can play the 8
                            if (val + currentCount + cards[0].Value - 1 <= 31)
                                if (cards[0].Rank != 5)
                                    return Task.FromResult(cards[0]);
                                else
                                    return Task.FromResult(cards[1]);
                        }
                        else
                        {
                            var highCardVal = cards[1].Value;
                            if (highCardVal > 10) highCardVal = 10;
                            if (val + currentCount + highCardVal <= 31)
                                if (cards[0].Rank != 5)
                                    return Task.FromResult(cards[0]);
                                else
                                    return Task.FromResult(cards[1]);
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
                    var sum = cds[0].Value + cds[1].Value;
                    if (sum + currentCount == 5) // i'll 15 them if they play a 10
                        return Task.FromResult(cds[1]);

                    if (sum + currentCount == 21) // i'll 31 them if they play a 10
                        return Task.FromResult(cds[1]);
                }
            }

            if (maxCard.Rank == 5)
                foreach (var c in uncountedCards)
                    if (c.Rank != 5 && c.Value + currentCount <= 31)
                    {
                        maxCard = c;
                        break;
                    }

            return Task.FromResult(maxCard);
        }


        public override Task<List<Card>> SelectCribCards(List<Card> hand, bool myCrib)
        {
            var combinations = new Combinations<Card>(hand, 4);
            List<Card> maxCrib = null;
            var maxScore = -1000.0;

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
            PlayerAlgorithm = PlayerAlgorithm.ImprovedCounting;
        }
    }
}