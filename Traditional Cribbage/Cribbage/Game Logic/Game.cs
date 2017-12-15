using Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cribbage
{

    public enum GameState
    {
        Uninitialized,
        Start,
        Deal,
        PlayerSelectsCribCards,
        GiveToCrib,
        Count,
        ScoreHand,
        CountingEnded,
        ScoreCrib,
        ShowCrib,
        EndOfHand,
        GameOver,
        None
    }

    public class Score
    {
        public string Description { get; set; }
        public int Value { get; set; }
        private Score() { }
        public Score(string description, int value)
        {
            Description = description;
            Value = value;
        }
        public override string ToString()
        {
            return String.Format($"{Description} for {Value} ");
        }
    }

    public interface IGameView
    {

        Task<PlayerType> ChooseDealer();


        //
        // Remove any cards that are in the grids  
        // add the cards to the grid and then run the Deal Animation
        Task Deal(List<CardCtrl> playerCards, List<CardCtrl> computerCards, List<CardCtrl> sharedCard, List<CardCtrl> computerGribCards, PlayerType dealer);

        //
        //  Move cards to Crib and flip the shared card
        //  called in Count state
        Task MoveCardsToCrib();

        //
        //  Count Computer Card - animate the right card and update the current count
        Task CountCard(CardCtrl card, int newCount);

        //
        // Animate the cards back to the player and the computer so that scoring can take place
        // hide the Count control
        Task SendCardsBackToOwner();
        void AddScore(List<Score> scores, PlayerType playerTurn);
        void PlayerCardsAddedToCrib(List<CardCtrl> cards);
        void SetState(GameState state);
        Task OnGo();
        Task ScoreHand(List<Score> scores, PlayerType playerType, HandType handType);
        Task ReturnCribCards(PlayerType dealer);
        Task EndHand(PlayerType dealer);
    }


    /// <summary>
    ///     This class should hold all the state for a game.  I expect the view to new Game and then start it.
    /// </summary>
    class Game
    {
        private GameState _state = GameState.Uninitialized;

        List<CardCtrl> _playerCards;
        List<CardCtrl> _computerCards;
        List<CardCtrl> _sharedCard;
        List<CardCtrl> _crib;
        List<CardCtrl> _countedCards = new List<CardCtrl>();
        IGameView _gameView;

        public PlayerType Dealer { get; set; } = PlayerType.Computer;
        public PlayerType PlayerTurn { get; set; } = PlayerType.Computer;

        int _currentCount = 0;          // 
        //int _playerScore = 0;
        //int _computerScore = 0;

        public Game(IGameView gameView)
        {
            _gameView = gameView;
        }

        private void ToggleDealer()
        {
            Dealer = (Dealer == PlayerType.Computer) ? PlayerType.Player : PlayerType.Computer;
        }

        public async Task SetState(GameState newState)
        {
            try
            {

                _state = newState;
                switch (_state)
                {
                    case GameState.Uninitialized:
                        break;
                    case GameState.Start:
                        Dealer = await _gameView.ChooseDealer();
                        ToggleDealer(); // will be reset in GameState.Deal
                        await SetState(GameState.Deal);

                        break;
                    case GameState.Deal:
                        ToggleDealer();
                        PlayerTurn = (Dealer == PlayerType.Computer) ? PlayerType.Player : PlayerType.Computer;
                        Deck d = new Deck();
                        _playerCards = d.GetCards(6, Owner.Player);
                        _computerCards = d.GetCards(6, Owner.Computer);
                        _sharedCard = d.GetCards(1, Owner.Shared);
                        _crib = GetBestCribCards(_computerCards);

                        await _gameView.Deal(_playerCards, _computerCards, _sharedCard, _crib, Dealer);
                        _countedCards.Clear();
                        foreach (CardCtrl card in _crib)
                        {
                            _computerCards.Remove(card);
                        }
                        _currentCount = 0;
                        await SetState(GameState.PlayerSelectsCribCards);
                        break;
                    case GameState.PlayerSelectsCribCards:
                        //
                        //  do nothing -- user has to drop cards...
                        break;
                    case GameState.GiveToCrib:
                        await _gameView.MoveCardsToCrib();
                        await SetState(GameState.Count);
                        break;
                    case GameState.Count:

                        if (PlayerTurn == PlayerType.Player)
                            break;

                        CardCtrl computerCard = PickCountingCard(_countedCards, _computerCards, _currentCount);
                        await CountComputerCard(computerCard);
                        PlayerTurn = PlayerType.Player;


                        break;
                    case GameState.ScoreHand:
                        break;
                    case GameState.CountingEnded:
                        await _gameView.SendCardsBackToOwner();
                        if (Dealer == PlayerType.Computer)
                        {
                            await ScoreHandAndNotifyView(_playerCards, PlayerType.Player, HandType.Regular);
                            await ScoreHandAndNotifyView(_computerCards, PlayerType.Computer, HandType.Regular);
                            await _gameView.ReturnCribCards(Dealer);
                            await ScoreHandAndNotifyView(_crib, PlayerType.Computer, HandType.Crib);
                        }
                        else
                        {
                            await ScoreHandAndNotifyView(_computerCards, PlayerType.Computer, HandType.Regular);
                            await ScoreHandAndNotifyView(_playerCards, PlayerType.Player, HandType.Regular);
                            await _gameView.ReturnCribCards(Dealer);
                            await ScoreHandAndNotifyView(_crib, PlayerType.Player, HandType.Crib);

                        }

                        await _gameView.EndHand(Dealer);

                        await SetState(GameState.Deal);
                        break;
                    case GameState.ScoreCrib:
                        break;
                    case GameState.ShowCrib:
                        break;
                    case GameState.EndOfHand:
                        break;
                    case GameState.GameOver:
                        break;
                    case GameState.None:
                        break;
                    default:
                        break;
                }
            }
            finally
            {
                _gameView.SetState(_state);
            }
        }

        private List<Score> ScoreHand(List<CardCtrl> cards, HandType handType)
        {
            List<Score> scores = new List<Score>();
            scores.Add(new Score("Test", 7));
            return scores;
        }

        private async Task ScoreHandAndNotifyView(List<CardCtrl> cards, PlayerType playerType, HandType handType)
        {
            List<Score> scores = ScoreHand(cards, handType);
            await _gameView.ScoreHand(scores, playerType, handType);
        }

        public async Task<bool> CardsDropped(List<CardCtrl> cards)
        {
            if (_state == GameState.PlayerSelectsCribCards)
            {
                _gameView.PlayerCardsAddedToCrib(cards);  //updates the max selected flag   
                foreach (CardCtrl card in cards)
                {
                    _crib.Add(card);
                    _playerCards.Remove(card);
                }
                if (_crib.Count == 4)
                {
                    await SetState(GameState.GiveToCrib);
                    return true;
                }

                return true;
            }

            if (_state == GameState.Count)
            {
                CardCtrl playerCard = cards[0];
                CardCtrl computerCard = null;

                if (PlayerTurn == PlayerType.Player)
                {
                    _currentCount += playerCard.Value;
                    _countedCards.Add(playerCard);
                    _playerCards.Remove(playerCard);
                    await _gameView.CountCard(computerCard, _currentCount);
                    List<Score> scores = ScoreCountedCards(_countedCards);
                    if (scores != null)
                        _gameView.AddScore(scores, PlayerTurn);

                }
                //
                //  the right card for the computer to play
                computerCard = PickCountingCard(_countedCards, _computerCards, _currentCount);



                if (computerCard != null)
                {
                    PlayerTurn = PlayerType.Computer;
                    await SetState(GameState.Count);
                }
                else // computer can't go.  can the player?
                {
                    playerCard = PickCountingCard(_countedCards, _playerCards, _currentCount); // is there a valid card for the player to play?
                    if (playerCard == null) // player can't go either
                    {
                        if (_currentCount != 31) // already scored above
                        {
                            //
                            //   score go!
                            List<Score> scores = new List<Score>();
                            scores.Add(new Score("Go", 1));
                            _gameView.AddScore(scores, PlayerTurn);
                        }

                        await _gameView.OnGo();
                        _currentCount = 0;
                        _countedCards.Clear();
                        if (_computerCards.Count > 0)
                        {
                            PlayerTurn = PlayerType.Computer;
                            await SetState(GameState.Count);
                        }
                        else if (_playerCards.Count == 0)
                        {
                            await EndCounting();
                            return true;
                        }
                    }

                    SetPlayableCards();
                }
            }

            return true;
        }

        private async Task EndCounting()
        {
            foreach (CardCtrl card in _countedCards)
            {
                if (card.Owner == Owner.Computer)
                {
                    _computerCards.Add(card);
                }
                else
                {
                    _playerCards.Add(card);
                }
            }

            _countedCards.Clear();

            await SetState(GameState.CountingEnded);
        }


        private async Task CountComputerCard(CardCtrl computerCard)
        {
            CardCtrl playerCard = null;


            do
            {
                _currentCount += computerCard.Value;
                _countedCards.Add(computerCard);
                _computerCards.Remove(computerCard);

                await _gameView.CountCard(computerCard, _currentCount);
                List<Score> scores = ScoreCountedCards(_countedCards);
                if (scores != null)
                    _gameView.AddScore(scores, PlayerTurn);

                //
                //  the card that the computer thinks the player can/should play
                playerCard = PickCountingCard(_countedCards, _playerCards, _currentCount);
                computerCard = PickCountingCard(_countedCards, _computerCards, _currentCount);


            } while (computerCard != null && playerCard == null); // player can NOT play and the computer CAN play

            if (computerCard == null)
            {
                if (playerCard == null)
                {
                    if (_currentCount != 31)
                    {
                        List<Score> scores = new List<Score>();
                        scores.Add(new Score("Go", 1));
                        _gameView.AddScore(scores, PlayerTurn);
                    }
                    await _gameView.OnGo();
                    _currentCount = 0;
                    _countedCards.Clear();

                }

                if (_playerCards.Count > 0)
                {
                    PlayerTurn = PlayerType.Player;
                    SetPlayableCards();
                    return;
                }
                else if (_computerCards.Count > 0)
                {
                    await SetState(GameState.Count);
                }
                else
                {
                    await EndCounting();
                    return;
                }


            }
            else // computer can play, but so can the player
            {
                PlayerTurn = PlayerType.Player;
                SetPlayableCards();
            }

        }

        private CardCtrl PickCountingCard(List<CardCtrl> countedCards, List<CardCtrl> heldCards, int currentCount)
        {
            foreach (var card in heldCards)
            {

                if (card.Location == Location.Discarded) continue;
                if (card.Location == Location.Crib) continue;

                if (currentCount + card.Value <= 31)
                    return card;
            }

            return null;
        }

        //
        //  called by the View when a a card is played .  
        //  The computer always instantly plays a card when it is its turn
        public async Task CountCard(CardCtrl card)
        {
            _countedCards.Add(card);
            card.Location = Location.Discarded;

            List<Score> scores = ScoreCountedCards(_countedCards);
            if (scores != null)
            {
                _gameView.AddScore(scores, PlayerTurn);

            }

            if (PlayerTurn == PlayerType.Player)
            {
                PlayerTurn = PlayerType.Computer;
                await SetState(GameState.Count);
            }
        }

        private List<Score> ScoreCountedCards(List<CardCtrl> countedCards)
        {
            List<Score> scores = new List<Score>();
            if (_currentCount == 31)
            {
                scores.Add(new Score("Hit 31", 2));
                return scores;
            }

            return null;
        }

        private List<CardCtrl> GetBestCribCards(List<CardCtrl> cards)
        {
            List<CardCtrl> list = new List<CardCtrl>();
            list.Add(cards[0]);
            list.Add(cards[3]);
            return list;
        }

        internal void SetPlayableCards()
        {
            foreach (CardCtrl card in _playerCards)
            {
                if (card.Location == Location.Player)
                {
                    if (card.Value + _currentCount > 31)
                        card.IsEnabled = false;
                    else
                        card.IsEnabled = true;
                }



            }
        }
    }
}
