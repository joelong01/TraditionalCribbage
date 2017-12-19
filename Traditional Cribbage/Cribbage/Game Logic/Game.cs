using Cards;
using CardView;
using CribbagePlayers;
using LongShotHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;

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
        CountPlayer,
        CountComputer,
        ScoreHand,
        CountingEnded,
        ScoreCrib,
        ShowCrib,
        EndOfHand,
        GameOver,
        None,
        SelectCrib,
        ScorePlayerHand,
        ScoreComputerHand
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

        Player _computer = null;
        InteractivePlayer _player = null;
        
        public PlayerType Dealer { get; set; } = PlayerType.Computer;
        public PlayerType PlayerTurn { get; set; } = PlayerType.Computer;

        int _currentCount = 0;          // 
      
        public Game(IGameView gameView)
        {
            _gameView = gameView;
        }

        public Game(IGameView gameView, Player computer, InteractivePlayer player)
        {
            _gameView = gameView;
            _computer = computer;
            _player = player;
            _player.GameView = gameView;
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

        public async Task<Player> StartGame(GameState state)
        {
            while (true)
            {
                _state = state;
                switch (state)
                {
                    case GameState.Uninitialized:
                        break;
                    case GameState.Start:
                        Dealer = await _gameView.ChooseDealer();
                        ToggleDealer(); // will be reset in GameState.Deal
                        state = GameState.Deal;
                        break;
                    case GameState.Deal:
                        {
                            ToggleDealer();
                            PlayerTurn = (Dealer == PlayerType.Computer) ? PlayerType.Player : PlayerType.Computer;
                            var (computerCards, playerCards, sharedCard) = Game.GetHands();
                            _computerCards = computerCards;
                            _playerCards = playerCards;
                            _sharedCard = sharedCard;

                            _crib = ComputerSelectCrib(_computerCards, Dealer == PlayerType.Computer);

                            await _gameView.Deal(_playerCards, _computerCards, _sharedCard, _crib, Dealer);
                            _countedCards.Clear();
                            foreach (CardCtrl c in _crib)
                            {
                                _computerCards.Remove(c);
                            }
                            _currentCount = 0;
                            state = GameState.PlayerSelectsCribCards;
                        }
                        break;
                    case GameState.PlayerSelectsCribCards:
                        List<CardCtrl> playerCrib = await _player.SelectCribUiCards(null, Dealer == PlayerType.Player);
                        _crib.Add(playerCrib[0]);
                        _crib.Add(playerCrib[1]);
                        _playerCards.Remove(playerCrib[0]);
                        _playerCards.Remove(playerCrib[1]);
                        state = GameState.GiveToCrib;
                        break;
                    case GameState.GiveToCrib:
                        await _gameView.MoveCardsToCrib();
                        //
                        //  shared card has been flipped -- check for Jack

                        if (_sharedCard[0].Card.CardOrdinal == CardOrdinal.Jack)
                        {
                            Score score = new Score(ScoreName.HisNibs, 2);
                            AddScore(new List<Score>() { score }, Dealer);
                        }
                        state = GameState.Count;
                        break;
                    case GameState.Count:
                        Debug.Assert(_computerCards.Count == 4);
                        Debug.Assert(_playerCards.Count == 4);
                        Debug.Assert(_crib.Count == 4);
                        _currentCount = 0;
                        _countedCards.Clear();
                        state = Dealer == PlayerType.Player ? GameState.CountComputer : GameState.CountPlayer;
                        break;
                    case GameState.CountPlayer:
                        {
                            PlayerTurn = PlayerType.Player;
                            Card cardPlayerShouldPlay = _computer.GetCountCard(CardCtrlToCards(_countedCards), CardCtrlToCards(_playerCards), _currentCount).Result;
                            CardCtrl playerCard = null;

                            if (cardPlayerShouldPlay != null)
                            {
                                playerCard = await _player.GetCountCard();
                                if (playerCard != null)
                                {
                                    int score = CardScoring.ScoreCountingCardsPlayed(CardCtrlToCards(_countedCards), playerCard.Card, _currentCount, out List<Score> scoreList);

                                    _currentCount += playerCard.Value;
                                    _playerCards.Remove(playerCard);
                                    _countedCards.Add(playerCard);
                                    AddScore(scoreList, PlayerType.Player);
                                    await _gameView.CountCard(playerCard, _currentCount); // need to update the UI even when the player plays -- set the count and playable cards

                                    state = GameState.CountComputer;
                                    if (_currentCount == 31)
                                    {
                                        _currentCount = 0;
                                        _countedCards.Clear();
                                        break;
                                    }
                                }
                            }

                            Card cardComputerShouldPlay = _computer.GetCountCard(CardCtrlToCards(_countedCards), CardCtrlToCards(_computerCards), _currentCount).Result;

                            if (playerCard == null) // player couldn't go
                            {
                                if (cardComputerShouldPlay == null) // computer can't go
                                {
                                    if (_playerCards.Count == 0 && _computerCards.Count == 0)
                                    {
                                        //
                                        //  counting over                                        
                                        state = GameState.CountingEnded;

                                    }
                                    else // do a go
                                    {

                                        PlayerType goPlayer = await ScoreGo();

                                        if (goPlayer == PlayerType.Computer)
                                            state = GameState.CountPlayer;
                                        else
                                            state = GameState.CountComputer;

                                    }
                                }
                                else // computer can go
                                {
                                    state = GameState.CountComputer;

                                }
                            }
                            else // player went maybe can go again
                            {
                                if (cardComputerShouldPlay == null) // computer can't go
                                {
                                    state = GameState.CountPlayer; // this is the state that got us here, loop and do again
                                }
                                else
                                {
                                    state = GameState.CountComputer;
                                }


                            }
                        }

                        break;
                    case GameState.CountComputer:
                        {
                            PlayerTurn = PlayerType.Computer;
                            Card card = _computer.GetCountCard(CardCtrlToCards(_countedCards), CardCtrlToCards(_computerCards), _currentCount).Result; //PickCountingCard(_countedCards, _computerCards, _currentCount);
                            if (card != null)
                            {
                                CardCtrl computerCard = card.Tag as CardCtrl;
                                int score = CardScoring.ScoreCountingCardsPlayed(CardCtrlToCards(_countedCards), card, _currentCount, out List<Score> scoreList);
                                _currentCount += computerCard.Value;
                                _computerCards.Remove(computerCard);
                                _countedCards.Add(computerCard);
                                await _gameView.CountCard(computerCard, _currentCount);
                                AddScore(scoreList, PlayerType.Computer);
                                if (_currentCount == 31)
                                {
                                    _currentCount = 0;
                                    _countedCards.Clear();
                                    state = GameState.CountPlayer;
                                    break;
                                }
                            }
                            Card cardPlayerShouldPlay = _computer.GetCountCard(CardCtrlToCards(_countedCards), CardCtrlToCards(_playerCards), _currentCount).Result;
                            if (card == null) // computer couldn't go
                            {
                                if (cardPlayerShouldPlay == null) // player can't go
                                {
                                    if (_playerCards.Count == 0 && _computerCards.Count == 0)
                                    {
                                        //
                                        //  counting over

                                        state = GameState.CountingEnded;

                                    }
                                    else // do a go
                                    {
                                        PlayerType goPlayer = await ScoreGo();

                                        if (goPlayer == PlayerType.Computer)
                                            state = GameState.CountPlayer;
                                        else
                                            state = GameState.CountComputer;

                                    }
                                }
                                else
                                {
                                    state = GameState.CountPlayer;

                                }
                            }
                            else // computer went maybe can go again
                            {
                                if (cardPlayerShouldPlay == null) // player can't go
                                {
                                    state = GameState.CountComputer; // this is the state that got us here, loop and do again
                                }
                                else
                                {
                                    state = GameState.CountPlayer;
                                }


                            }
                        }
                        break;

                    case GameState.ScoreHand:
                        break;
                    case GameState.CountingEnded:
                        await EndCounting();
                        if (Dealer == PlayerType.Computer)
                            state = GameState.ScorePlayerHand;
                        else
                            state = GameState.ScoreComputerHand;
                        break;
                    case GameState.ScorePlayerHand:
                        PlayerTurn = PlayerType.Player;
                        await ScoreHandAndNotifyView(_playerCards, _sharedCard, PlayerType.Player, HandType.Regular);
                        if (Dealer == PlayerType.Computer)
                            state = GameState.ScoreComputerHand;
                        else
                            state = GameState.ScoreCrib;
                        break;
                    case GameState.ScoreComputerHand:
                        PlayerTurn = PlayerType.Computer;
                        await ScoreHandAndNotifyView(_computerCards, _sharedCard, PlayerType.Computer, HandType.Regular);
                        if (Dealer == PlayerType.Computer)
                            state = GameState.ScoreCrib;
                        else
                            state = GameState.ScorePlayerHand;
                        break;
                    case GameState.ScoreCrib:
                        await _gameView.ReturnCribCards(Dealer);
                        await ScoreHandAndNotifyView(_crib, _sharedCard, Dealer, HandType.Crib);
                        state = GameState.EndOfHand;
                        break;
                    case GameState.ShowCrib:
                        break;
                    case GameState.EndOfHand:
                        await _gameView.EndHand(Dealer);
                        state = GameState.Deal;
                        break;
                    case GameState.GameOver:
                        break;
                    case GameState.None:
                        break;
                    case GameState.SelectCrib:
                        break;
                    default:
                        break;
                }

                if (_player.Score > 120)
                {
                    return _player;
                }
                if (_computer.Score > 120)

                {
                    return _computer;
                }
            }

            throw new Exception("Shouldn't have exited the state loop");
        }


        private PlayerType LastPlayerCounted()
        {
            if (_countedCards.Count == 0)
                throw new Exception("no counted cards");

            if (_countedCards.Last().Owner == Owner.Player)
                return PlayerType.Player;

            return PlayerType.Computer;


        }

        private async Task<PlayerType> ScoreGo()
        {

            List<Score> scores = new List<Score>
            {
                new Score(ScoreName.Go, 1)
            };
            PlayerType goPlayer = LastPlayerCounted();
            AddScore(scores, goPlayer);

            await _gameView.OnGo();
            _currentCount = 0;
            _countedCards.Clear();
            SetPlayableCards();
            return goPlayer;
        }
        
        private void AddScore(List<Score> scores, PlayerType playerTurn)
        {
            if (scores == null)
                return;

            if (scores.Count == 0)
                return;

            Player player = (playerTurn == PlayerType.Computer) ? _computer : _player;
            int scoreDelta = _gameView.AddScore(scores, playerTurn);
            player.Score += scoreDelta;
        }

        private Task ScoreHandAndNotifyView(List<CardCtrl> cards, List<CardCtrl> sharedCard, PlayerType playerType, Cards.HandType handType)
        {
            int score = CardScoring.ScoreHand(CardCtrlToCards(cards), CardCtrlToCards(sharedCard)[0], handType, out List<Score> scores);
            _gameView.ScoreHand(scores, playerType, handType);
            Player player = (playerType == PlayerType.Computer) ? _computer : _player;
            player.Score += score;
            return Task.FromResult(true);
        }

      
        private async Task EndCounting()
        {
            //
            // score 1 for last card
            List<Score> scores = new List<Score>();
            ScoreName scoreName = ScoreName.LastCard;
            scores.Add(new Score(scoreName, 1));
            PlayerType player = PlayerType.Computer;

            if (_countedCards.Last().Owner == Owner.Player)
                player = PlayerType.Player;

            AddScore(scores, player);
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

            await _gameView.SendCardsBackToOwner();

        }
        

        public List<CardCtrl> ComputerSelectCrib(List<CardCtrl> cards, bool computerCrib)
        {
            List<Card> hand = CardCtrlToCards(cards);
            List<Card> crib = _computer.SelectCribCards(hand, computerCrib).Result;
            return CardsToCardCtrl(crib);
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

        private List<CardCtrl> CardsToCardCtrl(List<Card> hand)
        {
            List<CardCtrl> newHand = new List<CardCtrl>();
            foreach (var card in hand)
            {
                newHand.Add(card.Tag as CardCtrl);
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
