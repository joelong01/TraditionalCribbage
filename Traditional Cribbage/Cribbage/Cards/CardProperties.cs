using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Cards
{
    public partial class Card : INotifyPropertyChanged
    {
        int _Index = 0;
        int _Rank = 0;
        Suit _Suit = Suit.Uninitialized;
        CardOrdinal _CardOrdinal = CardOrdinal.Uninitialized;
        public CardOrdinal CardOrdinal
        {
            get
            {
                return _CardOrdinal;
            }
            set
            {
                if (_CardOrdinal != value)
                {
                    _CardOrdinal = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public Suit Suit
        {
            get
            {
                return _Suit;
            }
            set
            {
                if (_Suit != value)
                {
                    _Suit = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public CardNames CardName
        {
            get
            {
                return (CardNames)(((int)(Suit - 1) * 13 + Rank - 1));

            }
            set
            {
                //
                //  given a number of 0-51, set the suit and the rank

                if ((int)value < 0 || (int)value > 51)
                    throw new Exception($"The value {(int)value} is an invalid CardNumber");

                int val = (int)value;
                Index = val;
                Suit = (Suit)((int)(val / 13) + 1);
                Rank = val % 13 + 1;
                CardOrdinal = (CardOrdinal)Rank;
            }
        }

        public int Value
        {
            get
            {
                if (Rank <= 10)
                    return Rank;

                return 10;
            }
           
        }
        public int Index
        {
            get
            {
                return _Index;
            }
            set
            {
                if (_Index != value)
                {
                    _Index = value;
                    NotifyPropertyChanged();
                }
            }
        }
        
        public int Rank
        {
            get
            {
                return _Rank;
            }
            set
            {
                if (_Rank != value)
                {
                    _Rank = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
