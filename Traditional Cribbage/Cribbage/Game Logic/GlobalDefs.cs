using CardView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace Cribbage
{
    public enum Owner
    {
        Player,
        Computer ,
        Shared,
        Crib,
        Uninitialized
    };


    public enum PlayerType { Player = 0, Computer = 1 };
    

    public enum Open { Up, Left, Right, Down };
    public enum ScoreType { Count, Hand, Crib, Cut, Saved, Unspecified }


    public enum StatType { Count, Min, Max, Average, Total };
    public enum StatViewType { Game, Hand, Crib, Counting };

    public enum StatName
    {
        Ignored, Saved,
        /* Game stats */
        WonDeal, GamesStarted, GamesWon, GamesLost, TotalHandsPlayed, TotalCribsPlayed, TotalCountingSessions, SmallestWinMargin, LargestWinMargin, SkunkWins, CutAJack,
        /* Hand stats */
        HandMostPoints, HandTotalPoints, HandAveragePoints, Hand0Points, HandJackOfTheSameSuit, HandPairs, Hand3OfAKind, Hand4OfAKind, Hand4CardFlush, Hand5CardFlush, Hand3CardRun, Hand4CardRun, Hand5CardRun, Hand15s,
        /* Crib stats */
        CribMostPoints, CribTotalPoints, CribAveragePoints, Crib0Points, CribJackOfTheSameSuit, CribPairs, Crib3OfAKind, Crib4OfAKind, Crib5CardFlush, Crib3CardRun, Crib4CardRun, Crib5CardRun, Crib15s,
        /* Counting stats */
        CountingMostPoints, CountingTotalPoints, CountingAveragePoints, CountingPair, Counting3OfAKind, Counting4OfAKind, Counting3CardRun, Counting4CardRun,
        Counting5CardRun, Counting6CardRun, Counting7CardRun, CountingLastCard, CountingHit31, CountingGo, CountingHit15, CountingMuggins3CardRunLost
    };

    public class PegScore
    {
        Owner _owner;

        public PegScore()
        {

            FirstPegControl = new PegControl
            {
                Width = 15.0,
                Height = 15.0
            };
            SecondPegControl = new PegControl
            {
                Width = 15.0,
                Height = 15.0
            };
            Canvas.SetZIndex(SecondPegControl, 75);
            Canvas.SetZIndex(FirstPegControl, 75);
            Score1 = 0;
            Score2 = 0;

        }

        public int Score1
        {
            get
            {
                return FirstPegControl.Score;
            }
            set
            {
                FirstPegControl.Score = value;
            }
        }
        public int Score2
        {
            get
            {
                return SecondPegControl.Score;
            }
            set
            {
                SecondPegControl.Score = value;
            }
        }
        public Owner Owner
        {
            get
            {
                return _owner;
            }
            set
            {
                _owner = value;
                FirstPegControl.Owner = _owner;
                SecondPegControl.Owner = _owner;
            }

        }

        public void Reset()
        {

            Score1 = 0;
            Score2 = 0;

        }

        public PegControl FirstPegControl { get; set; }
        public PegControl SecondPegControl { get; set; }
        public double Diameter
        {
            get
            {
                return FirstPegControl.Width;
            }
            set
            {
                FirstPegControl.Width = value;
                FirstPegControl.Height = value;
                SecondPegControl.Width = value;
                SecondPegControl.Height = value;

            }

        }


    }

    public class ScoreCollection
    {

        public ObservableCollection<ScoreInstance> Scores { get; set; }

        public ScoreType ScoreType { get; set; }

        public int Total { get; set; }
        public bool Accepted { get; set; }

        public int ActualScore { get; set; } // Total might be wrong because of Muggins

        public ScoreCollection()
        {
            Accepted = false;
            Scores = new ObservableCollection<ScoreInstance>();
            Total = 0;
            ScoreType = ScoreType.Unspecified;
            ActualScore = 0;
        }

        public ScoreCollection(string s)
        {
            Scores = new ObservableCollection<ScoreInstance>();
            Load(s);
        }
        //
        //  one line. everything afer the "=" sign.
        public string Save()
        {
            string s = String.Format("{0}-{1}-{2}-{3}|", ScoreType, Total, Accepted, ActualScore);
            foreach (ScoreInstance scoreInstance in Scores)
            {
                s += scoreInstance.Save() + "|";
            }
            return s;
        }

        private bool Load(string s)
        {
            char[] sep1 = new char[] { '|' };
            char[] sep2 = new char[] { '-' };

            string[] tokens = s.Split(sep1, StringSplitOptions.RemoveEmptyEntries);
            string[] tokens2 = tokens[0].Split(sep2, StringSplitOptions.RemoveEmptyEntries);

            ScoreType = (ScoreType)Enum.Parse(typeof(ScoreType), tokens2[0]);
            Total = Convert.ToInt32(tokens2[1]);
            Accepted = Convert.ToBoolean(tokens2[2]);
            ActualScore = Convert.ToInt32(tokens2[3]);

            for (int i = 1; i < tokens.Count(); i++)
            {
                ScoreInstance scoreInstance = new ScoreInstance(tokens[i]);
                Scores.Add(scoreInstance);
            }
            return true;
        }

        public string LogString()
        {
            string s = "";

            foreach (ScoreInstance p in this.Scores)
            {
                s += String.Format("|{0}, {1}, {2}|-", p.Description, p.Count, p.Score);
            }

            s += String.Format("Total: {0}", Total);

            return s;

        }

        public string Format(bool includeHeader = true, bool formatForMessagebox = true, bool smallFormat = false)
        {
            string story = "";
            string tabs = "\t\t";
            string tab = "\t";

            string line = "";
            int len;

            if (smallFormat)
            {
                foreach (ScoreInstance p in this.Scores)
                {

                    line = String.Format("{0}{1}{2}\n", p.Description, tabs, p.Score);
                    story += line;
                }
                story += String.Format("\nTotal:\t{0}", Total);
                return story;
            }

            if (includeHeader)
            {
                if (formatForMessagebox)
                    story = String.Format("{0}\t\t{1}\t\t{2}\n", "Type", "Count", "Score");
                else
                    story = String.Format("{0}\t\t{1}\t{2}\n", "Type", "     Count", "      Score");
            }


            foreach (ScoreInstance p in this.Scores)
            {
                len = p.Description.Length;
                if ((len > 5 && !formatForMessagebox) || p.Description == "Saved")
                {
                    line = String.Format("{0}{1}{2}{3}{4}\n", p.Description, tabs, p.Count, tabs, p.Score);
                }
                else
                {
                    if (formatForMessagebox)
                    {
                        line = String.Format("{0}{1}{2}{3}{4}\n", p.Description, tabs, p.Count, tabs, p.Score);

                    }
                    else
                    {
                        line = String.Format("{0}{1}{2}{3}{4}\n", p.Description, tabs + tab, p.Count, tabs, p.Score);

                    }

                }
                story += line;
            }
            story += String.Format("\n\t\t\tTotal:\t{0}", Total);
            return story;

        }


        internal ScoreInstance GetScoreType(StatName statName)
        {
            foreach (ScoreInstance s in this.Scores)
            {
                if (s.ScoreType == statName)
                {
                    return s;
                }
            }

            return null;

        }
    }

    public class ScoreInstance
    {

        public string Description
        {
            get
            {
                string resourceKey = "Score" + ScoreType.ToString();

                return (string)App.Current.Resources[resourceKey];

            }

        }
        public int Count { get; set; }
        public int Score { get; set; }
        public StatName ScoreType { get; set; }


        public int ActualScore { get; set; }
        public StatName ActualScoreType { get; set; }


        public List<int> Cards { get; set; }

        public ScoreInstance(string s)
        {
            Cards = new List<int>();
            Load(s);
        }

        public ScoreInstance(StatName name, int count, int score, List<int> cards)
        {
            ScoreType = name;
            Count = count;
            Score = score;
            Cards = new List<int>(cards);

        }

        public ScoreInstance(StatName name, int count, int score, int cardIndex)
        {
            ScoreType = name;
            Count = count;
            Score = score;
            Cards = new List<int>
            {
                cardIndex
            };

        }

        //  put all state on one line
        public string Save()
        {
            string s = String.Format("{0},{1},{2},{3},{4},", Count, Score, ScoreType, ActualScore, ActualScoreType);
            foreach (int i in Cards)
            {
                s += String.Format("{0},", i);
            }

            return s;
        }

        public bool Load(string s)
        {
            char[] sep1 = new char[] { ',' };

            string[] tokens = s.Split(sep1, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Count() < 7) return false;

            Count = Convert.ToInt32(tokens[0]);
            Score = Convert.ToInt32(tokens[1]);
            ScoreType = (StatName)Enum.Parse(typeof(StatName), tokens[2]);

            ActualScore = Convert.ToInt32(tokens[4]);
            ActualScoreType = (StatName)Enum.Parse(typeof(StatName), tokens[5]);

            for (int i = 7; i < tokens.Count(); i++)
            {
                Cards.Add(Convert.ToInt32(tokens[i]));
            }

            return true;
        }

    }

    public class HandsFromServer
    {
        public List<CardCtrl> PlayerCards { get; set; }
        public List<CardCtrl> ComputerCards { get; set; }

        public CardCtrl SharedCard { get; set; }
    }

}
