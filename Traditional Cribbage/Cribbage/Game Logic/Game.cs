using Cards;
using CardView;
using CribbagePlayers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        List<CardCtrl> DiscardedCards { get; }// needed after counting is over
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

        DefaultPlayer _computer = new DefaultPlayer(true);

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

        public static (List<CardCtrl> computerCards, List<CardCtrl> playerCards, List<CardCtrl> sharedCard) GetHands()
        {
            List<CardCtrl> dealtCards = CardCtrl.GetCards(13, Owner.Uninitialized);
            List<CardCtrl> playerCards = new List<CardCtrl>();
            List<CardCtrl> computerCards = new List<CardCtrl>();
            List<CardCtrl> sharedCards = new List<CardCtrl>();

            var ret = (computerCards: computerCards, playerCards: playerCards, sharedCard: sharedCards);

            for (int i = 0; i < 12; i += 2)
            {
                dealtCards[i].Owner = Owner.Player;
                ret.playerCards.Add(dealtCards[i]);
                dealtCards[i + 1].Owner = Owner.Computer;
                ret.computerCards.Add(dealtCards[i + 1]);
            }
            dealtCards[12].Owner = Owner.Shared;
            ret.sharedCard.Add(dealtCards[12]);
            return ret;

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
                        var (computerCards, playerCards, sharedCard) = Game.GetHands();
                        _computerCards = computerCards;
                        _playerCards = playerCards;
                        _sharedCard = sharedCard;

                        _crib = ComputerSelectCrib(_computerCards, Dealer == PlayerType.Computer);

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
                        //
                        //  shared card has been flipped -- check for Jack

                        if (_sharedCard[0].Card.CardOrdinal == CardOrdinal.Jack)
                        {
                            Score score = new Score(ScoreName.HisNibs, 2);
                            _gameView.AddScore(new List<Score>() { score }, PlayerTurn);
                        }
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
                            await ScoreHandAndNotifyView(_playerCards, _sharedCard, PlayerType.Player, HandType.Regular);
                            await ScoreHandAndNotifyView(_computerCards, _sharedCard, PlayerType.Computer, HandType.Regular);
                            await _gameView.ReturnCribCards(Dealer);
                            await ScoreHandAndNotifyView(_crib, _sharedCard, PlayerType.Computer, HandType.Crib);
                        }
                        else
                        {
                            await ScoreHandAndNotifyView(_computerCards, _sharedCard, PlayerType.Computer, HandType.Regular);
                            await ScoreHandAndNotifyView(_playerCards, _sharedCard, PlayerType.Player, HandType.Regular);
                            await _gameView.ReturnCribCards(Dealer);
                            await ScoreHandAndNotifyView(_crib, _sharedCard, PlayerType.Player, HandType.Crib);

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
           // scores.Add(new Score("Test", 7));
            return scores;
        }

        private async Task ScoreHandAndNotifyView(List<CardCtrl> cards, List<CardCtrl> sharedCard, PlayerType playerType, Cards.HandType handType)
        {
            int score = CardScoring.ScoreHand(CardCtrlToCards(cards), CardCtrlToCards(sharedCard)[0], handType, out List<Score> scores);            
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
                //
                //  1. get Count Card
                //  2. Score It
                //  3. add the value to the current count
                //  4. add the score
                //  5. add it to the counted cards
                //  6. remove it from the player cards (the UI can only be in one collection at a time)
                //
                CardCtrl playerCard = cards[0];
                CardCtrl computerCard = null;

                if (PlayerTurn == PlayerType.Player)
                {

                    List<Card> countedCards = CardCtrlToCards(_countedCards);
                    int score = CardScoring.ScoreCountingCardsPlayed(countedCards, playerCard.Card, _currentCount, out List<Score> scoreList);

                    _currentCount += playerCard.Value;
                    _playerCards.Remove(playerCard);
                    _countedCards.Add(playerCard);
                    
                    await _gameView.CountCard(computerCard, _currentCount); // need to update the UI even when the player plays -- set the count and playable cards
                    _gameView.AddScore(scoreList, PlayerTurn);
                    

                }
                //
                //  the right card for the computer to play - will be null if there is nothing for them to play
                //
                computerCard = PickCountingCard(_countedCards, _computerCards, _currentCount);
                
                if (computerCard != null)
                {
                    //
                    // this means the computer can play -- let them do so
                    PlayerTurn = PlayerType.Computer;
                    await SetState(GameState.Count);
                }
                else // computer can't go.  can the player?
                {
                    playerCard = PickCountingCard(_countedCards, _playerCards, _currentCount); // is there a valid card for the player to play? - ask the computer player what it would play.  if it returns a non-null value, the player can play
                    if (playerCard == null) // player can't go either
                    {
                        if (_playerCards.Count == 0 && _computerCards.Count == 0)
                        {
                            await EndCounting();
                            return true;
                        }

                        if (_currentCount != 31) // already scored above
                        {
                            //
                            //   score go!
                            List<Score> scores = new List<Score>();
                            ScoreName scoreName = ScoreName.Go;                            
                            scores.Add(new Score(scoreName, 1));
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
                        
                    }

                    SetPlayableCards();
                }
            }

            return true;
        }

        private async Task EndCounting()
        {
            //
            // score 1 for last card
            List<Score> scores = new List<Score>();
            ScoreName scoreName = ScoreName.LastCard;
            scores.Add(new Score(scoreName, 1));
            PlayerType player = PlayerType.Computer;
            
            if (_countedCards[_countedCards.Count - 1].Owner == Owner.Player)
                player = PlayerType.Player;

            _gameView.AddScore(scores, player);
            _countedCards.Clear();

            foreach (CardCtrl card in _gameView.DiscardedCards)
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

            Debug.Assert(_playerCards.Count == 4);
            Debug.Assert(_computerCards.Count == 4);

            await SetState(GameState.CountingEnded);
        }


        private async Task CountComputerCard(CardCtrl computerCard)
        {

            if (computerCard == null)
                return;

            CardCtrl playerCard = null;
            //
            //  1. get Count Card
            //  2. Score It
            //  3. add the value to the current count
            //  4. add the score
            //  5. add it to the counted cards
            //  6. remove it from the player cards (the UI can only be in one collection at a time)
            //

            do
            {
                List<Card> countedCards = CardCtrlToCards(_countedCards);
                int score = CardScoring.ScoreCountingCardsPlayed(countedCards, computerCard.Card, _currentCount, out List<Score> scoreList);

                _currentCount += computerCard.Value;
                _computerCards.Remove(computerCard);
                _countedCards.Add(computerCard);

                await _gameView.CountCard(computerCard, _currentCount); // update the UI

                _gameView.AddScore(scoreList, PlayerType.Computer);
                
                //
                //  the card that the computer thinks the player can/should play
                playerCard = PickCountingCard(_countedCards, _playerCards, _currentCount);
                computerCard = PickCountingCard(_countedCards, _computerCards, _currentCount);


            } while (computerCard != null && playerCard == null); // player can NOT play and the computer CAN play

            if (computerCard == null)
            {
                if (playerCard == null)
                {

                    if (_computerCards.Count == 0 && _playerCards.Count == 0)
                    {
                        await EndCounting();
                        return;
                    }


                    if (_currentCount != 31)
                    {
                        List<Score> scores = new List<Score>
                        {
                            new Score(ScoreName.Go, 1)
                        };
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

            }
            else // computer can play, but so can the player
            {
                PlayerTurn = PlayerType.Player;
                SetPlayableCards();
            }

        }

        private CardCtrl PickCountingCard(List<CardCtrl> countedCards, List<CardCtrl> heldCards, int currentCount)
        {

            Card card = _computer.GetCountCard(CardCtrlToCards(countedCards), CardCtrlToCards(heldCards), currentCount);
            if (card == null) return null;
            foreach (var uiCard in heldCards)
            {
                if (uiCard.CardName == card.CardName)
                    return uiCard;
            }


            throw new Exception("a CardCtrl was not found containing a Card");
        }

       

        private List<CardCtrl> ComputerSelectCrib(List<CardCtrl> cards, bool computerCrib)
        {
            List<Card> hand = CardCtrlToCards(cards);
            List<Card> crib =  _computer.SelectCribCards(hand, computerCrib);
            return CardsToCardCtrl(cards, crib);
        }

        private List<Card> CardCtrlToCards(List<CardCtrl> uiCards)
        {
            List<Card> cards = new List<Card>();
            foreach (var uiCard in uiCards)
            {
                cards.Add(uiCard.Card);
            }

            return cards;
        }

        private List<CardCtrl> CardsToCardCtrl(List<CardCtrl> hand, List<Card> cards)
        {
            List<CardCtrl> newHand = new List<CardCtrl>();
            foreach (var card in cards)
            {
                foreach (var uiCard in hand)
                {
                    if (card.CardName == uiCard.CardName)
                    {
                        newHand.Add(uiCard);
                        break;
                    }
                }
            }
            return newHand;
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
