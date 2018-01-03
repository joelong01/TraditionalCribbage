using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Cribbage;

namespace Cards
{
    public partial class Card : INotifyPropertyChanged
    {
        private CardOrdinal _CardOrdinal = CardOrdinal.Uninitialized;
        private int _Index;
        private int _Rank;
        private Suit _Suit = Suit.Uninitialized;
        public Owner Owner { get; set; } = Owner.Uninitialized;

        public CardOrdinal CardOrdinal
        {
            get => _CardOrdinal;
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
            get => _Suit;
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
            get => (CardNames) ((int) (Suit - 1) * 13 + Rank - 1);
            set
            {
                //
                //  given a number of 0-51, set the suit and the rank

                if ((int) value < 0 || (int) value > 51)
                    throw new Exception($"The value {(int) value} is an invalid CardNumber");

                var val = (int) value;
                Index = val;
                Suit = (Suit) (val / 13 + 1);
                Rank = val % 13 + 1;
                CardOrdinal = (CardOrdinal) Rank;
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
            get => _Index;
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
            get => _Rank;
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

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}