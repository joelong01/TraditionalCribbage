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
        ScorePlayerCrib,
        ShowCrib,
        EndOfHand,
        GameOver,
        None,
        SelectCrib,
        ScorePlayerHand,
        ScoreComputerHand,
        ScoreComputerCrib
    }
    

    /// <summary>
    ///     This class should hold all the state for a game.  I expect the view to new Game and then start it.
    /// </summary>
    class Game
    {
        private GameState _state = GameState.Uninitialized;

       
        IGameView _gameView;

        public List<Card> PlayerCards
        {
            get
            {
                return CardCtrlToCards(_gameView.GetCards(CardType.Player));
            }
        }

        public CardList PlayerCardCtrls
        {
            get
            {
                return _gameView.GetCards(CardType.Player);
            }
        }

        public List<Card> ComputerCards
        {
            get
            {
                return CardCtrlToCards(_gameView.GetCards(CardType.Computer));
            }
        }

        public List<Card> CribCards
        {
            get
            {
                return CardCtrlToCards(_gameView.GetCards(CardType.Crib));
            }
        }

        public Card SharedCard
        {
            get
            {
                return _gameView.GetCards(CardType.Deck)[0].Card;
            }
        }
        

        public List<Card> CountedCards
        {
            get
            {
                List<Card> countedCards = new List<Card>();
                foreach (var card in _gameView.GetCards(CardType.Counted))
                {
                    if (!card.Counted)
                    {
                        countedCards.Add(card.Card);
                    }
                }
                return countedCards;
            }
        }



        Player _computer = null;
        InteractivePlayer _player = null;
        
        public PlayerType Dealer { get; set; } = PlayerType.Computer;
        public PlayerType PlayerTurn { get; set; } = PlayerType.Computer;

        int _currentCount = 0;          // 
      
        public Game(IGameView gameView)
        {
            _gameView = gameView;
        }

        public GameState State { get { return _state; } }

        

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

        public async Task<Player> StartGame()
        {
            GameState state = GameState.Start;
            
            while (true)
            {
                _state = state;
                _gameView.SetState(state);
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
                            List<CardCtrl> crib = ComputerSelectCrib(computerCards, Dealer == PlayerType.Computer);

                            await _gameView.Deal(playerCards, computerCards, sharedCard, crib, Dealer);
                            

                            

                            
                            _currentCount = 0;
                            state = GameState.PlayerSelectsCribCards;
                        }
                        break;
                    case GameState.PlayerSelectsCribCards:
                        _gameView.SetState(state);
                        List<CardCtrl> playerCrib = await _player.SelectCribUiCards(null, Dealer == PlayerType.Player); // when I return from this, the cards are already in the crib.                      
                        state = GameState.GiveToCrib;
                        break;
                    case GameState.GiveToCrib:
                        await _gameView.MoveCardsToCrib();
                        //
                        //  shared card has been flipped -- check for Jack

                        if (SharedCard.CardOrdinal == CardOrdinal.Jack)
                        {
                            Score score = new Score(ScoreName.HisNibs, 2);
                            AddScore(new List<Score>() { score }, Dealer);
                        }
                        state = GameState.Count;
                        break;
                    case GameState.Count:
                        
                        Debug.Assert(ComputerCards.Count == 4);
                        Debug.Assert(PlayerCards.Count == 4);
                        Debug.Assert(CribCards.Count == 4);
                        _currentCount = 0;                        
                        state = Dealer == PlayerType.Player ? GameState.CountComputer : GameState.CountPlayer;
                        break;
                    case GameState.CountPlayer:
                        {
                            SetPlayableCards();
                            _gameView.SetState(state); // updates UI based on the state of the game    
                            PlayerTurn = PlayerType.Player;
                            var (computerCanPlay, playerCanPlay) = CanPlay(ComputerCards, PlayerCards, _currentCount);
                            state = GameState.CountComputer;
                            if (playerCanPlay)
                            {
                                _currentCount = await DoCountForPlayer(_player, PlayerType.Player, CountedCards, PlayerCards, _currentCount);
                            }

                            (computerCanPlay, playerCanPlay) = CanPlay(ComputerCards, PlayerCards, _currentCount);

                            if (computerCanPlay == false && playerCanPlay == false)
                            {
                                if (PlayerCards.Count == 0 && ComputerCards.Count == 0)
                                {
                                    state = GameState.CountingEnded;
                                }
                                else
                                {
                                    PlayerType goPlayer = await ScoreGo();
                                    if (goPlayer == PlayerType.Computer)
                                        state = GameState.CountPlayer;
                                    else
                                        state = GameState.CountComputer;
                                }
                            }                            
                        }

                        break;
                    case GameState.CountComputer:
                        {
                            _gameView.SetState(state); // updates UI based on the state of the game
                            PlayerTurn = PlayerType.Computer;                            
                            _currentCount = await DoCountForPlayer(_computer, PlayerType.Computer, CountedCards, ComputerCards, _currentCount);
                            state = GameState.CountPlayer;
                            var (computerCanPlay, playerCanPlay) = CanPlay(ComputerCards, PlayerCards, _currentCount);
                            if (!computerCanPlay && playerCanPlay)
                            {
                                _gameView.AddMessage("Computer can't play.  Go again.");
                            }
                            if (computerCanPlay == false && playerCanPlay == false)
                            {
                                if (PlayerCards.Count == 0 && ComputerCards.Count == 0)
                                {                                    
                                    state = GameState.CountingEnded;
                                }
                                else
                                {
                                    PlayerType goPlayer = await ScoreGo();
                                    if (goPlayer == PlayerType.Computer)
                                            state = GameState.CountPlayer;
                                        else
                                            state = GameState.CountComputer;
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
                                    
                        int playerScore = await GetScoreFromPlayer(this.PlayerCards, SharedCard, HandType.Hand);
                        if (Dealer == PlayerType.Computer)
                            state = GameState.ScoreComputerHand;
                        else
                            state = GameState.ScorePlayerCrib;
                        break;
                    case GameState.ScoreComputerHand:
                        PlayerTurn = PlayerType.Computer;
                        await ScoreHandAndNotifyView(ComputerCards, SharedCard, PlayerType.Computer, HandType.Hand);
                        if (Dealer == PlayerType.Computer)
                            state = GameState.ScoreComputerCrib;
                        else
                            state = GameState.ScorePlayerHand;
                        break;
                    case GameState.ScoreComputerCrib:
                        await _gameView.ReturnCribCards(Dealer);
                        //
                        //  above moves cards from Crib to Computer
                        await ScoreHandAndNotifyView(ComputerCards, SharedCard, PlayerType.Computer, HandType.Crib);
                        state = GameState.EndOfHand;
                        break;
                    case GameState.ScorePlayerCrib:
                        await _gameView.ReturnCribCards(Dealer);
                        //
                        //  above moves cards to Player
                        await GetScoreFromPlayer(PlayerCards,  SharedCard, HandType.Crib);                                               
                        state = GameState.EndOfHand;
                        break;
                    case GameState.ShowCrib:
                        break;
                    case GameState.EndOfHand:
                        await _gameView.EndHand(Dealer);
                        state = GameState.Deal;
                        break;
                    case GameState.GameOver:
                        return null;                       
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

        //
        //  this needs to operate on the UI cards becuase the data cards are always a new list
        private async Task<int> DoCountForPlayer(Player player, PlayerType playerTurn, List<Card> countedCards, List<Card> uncountedCards, int currentCount)
        {
            PlayerTurn = playerTurn;

            if (uncountedCards.Count == 0)
                return currentCount;

            Card cardPlayed  = await player.GetCountCard(countedCards, uncountedCards, _currentCount); 
            

            if (cardPlayed != null)
            {
                CardCtrl uiCard = cardPlayed.Tag as CardCtrl;                
                int score = CardScoring.ScoreCountingCardsPlayed(countedCards, cardPlayed, currentCount, out List<Score> scoreList);
                currentCount += uiCard.Value;               
                await _gameView.CountCard(uiCard, currentCount);
                AddScore(scoreList, playerTurn);
                if (currentCount == 31)
                {
                    currentCount = 0;
                    await _gameView.RestartCounting(playerTurn);
                }
            }
            
            return currentCount;
        }

        private (bool computerCanPlay, bool playerCanPlay) CanPlay(List<Card> computerCards, List<Card> playerCards, int currentCount)
        {
            bool cCanPlay = false;
            bool pCanPlay = false;
            foreach (var card in computerCards)
            {
                if (card.Value + currentCount <= 31)
                {
                    cCanPlay = true;
                    break;
                }
            }
            foreach (var card in playerCards)
            {
                if (card.Value + currentCount <= 31)
                {
                    pCanPlay = true;
                    break;
                }
            }

            return (computerCanPlay: cCanPlay, playerCanPlay: pCanPlay );
        }

        bool _autoEnterScore = false;
        private async Task<int> GetScoreFromPlayer(List<Card> cards, Card sharedCard, HandType handType)
        {            
            int playerScore = CardScoring.ScoreHand(cards, sharedCard, handType, out List<Score> scores);
            int enteredScore = 0;

            do
            {
                enteredScore = await _player.GetScoreFromUser(playerScore, _autoEnterScore);
                if (enteredScore != playerScore)
                {
                    _autoEnterScore = await StaticHelpers.AskUserYesNoQuestion("Cribbage", $"{enteredScore} is the wrong score.\n\nWould you like the computer to set the score?", "Yes", "No");
                }
            } while (enteredScore != playerScore);

            AddScore(scores, PlayerType.Player);

            return playerScore;
        }


        private PlayerType LastPlayerCounted()
        {
            if (CountedCards.Count == 0)
                throw new Exception("no counted cards");

            
            if (LastCardOwner == Owner.Player)
                return PlayerType.Player;

            return PlayerType.Computer;


        }

        private Owner LastCardOwner
        {
            get
            {
                CardCtrl lastCard = (CardCtrl)(CountedCards.Last()).Tag;
                return lastCard.Owner;
            }
        }

        private async Task<PlayerType> ScoreGo()
        {

            List<Score> scores = new List<Score>
            {
                new Score(ScoreName.Go, 1)
            };
            PlayerType goPlayer = LastPlayerCounted();
            AddScore(scores, goPlayer);

            await _gameView.RestartCounting(goPlayer);
            _currentCount = 0;
           
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

        private async Task ScoreHandAndNotifyView(List<Card> cards, Card sharedCard, PlayerType playerType, HandType handType)
        {
            int score = CardScoring.ScoreHand(cards, sharedCard, handType, out List<Score> scores);
            await _gameView.ScoreHand(scores, playerType, handType);
            Player player = (playerType == PlayerType.Computer) ? _computer : _player;
            player.Score += score;            
        }

      
        private async Task EndCounting()
        {
            //
            // score 1 for last card
            List<Score> scores = new List<Score>();
            ScoreName scoreName = ScoreName.LastCard;
            scores.Add(new Score(scoreName, 1));
            PlayerType player = PlayerType.Computer;

            if (LastCardOwner == Owner.Player)
                player = PlayerType.Player;

            AddScore(scores, player);
            await _gameView.SendCardsBackToOwner();

        }

        public List<CardCtrl> ComputerSelectCrib(List<CardCtrl> cards, bool computerCrib)
        {
            List<Card> crib = _computer.SelectCribCards(CardCtrlToCards(cards), computerCrib).Result;
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
            foreach (var card in PlayerCardCtrls)
            {
                if (card.Value + _currentCount <= 31)
                    card.IsEnabled = true;
                else
                    card.IsEnabled = false;
            }
        }
    }
}
