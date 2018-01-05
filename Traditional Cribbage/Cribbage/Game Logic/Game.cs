using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Cards;
using CardView;
using Cribbage.Players;
using CribbagePlayers;
using LongShotHelpers;

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
    internal class Game
    {
        private int _currentCount;

        private readonly IGameView _gameView;

        public Game(IGameView gameView)
        {
            _gameView = gameView;
        }


        public Game(IGameView gameView, Player computer, InteractivePlayer player, int currentCount)
        {
            _gameView = gameView;
            Computer = computer;
            Player = player;
            Player.GameView = gameView;
            CurrentCount = 0;
        }

        public GameState State { get; set; } = GameState.Uninitialized;

        public List<Card> PlayerCards => CardCtrlToCards(_gameView.GetCards(CardType.Player));

        public CardList PlayerCardCtrls => _gameView.GetCards(CardType.Player);

        public List<Card> ComputerCards => CardCtrlToCards(_gameView.GetCards(CardType.Computer));

        public List<Card> CribCards => CardCtrlToCards(_gameView.GetCards(CardType.Crib));

        public Card SharedCard => _gameView.GetCards(CardType.Deck)[0].Card;


        public List<Card> CountedCards
        {
            get
            {
                var countedCards = new List<Card>();
                foreach (var card in _gameView.GetCards(CardType.Counted))
                    if (!card.Counted)
                        countedCards.Add(card.Card);
                return countedCards;
            }
        }


        public Player Computer { get; set; }
        public InteractivePlayer Player { get; set; }

        public PlayerType Dealer { get; set; } = PlayerType.Computer;
        public PlayerType PlayerTurn { get; set; } = PlayerType.Computer;

        public int CurrentCount
        {
            get => _currentCount;
            set
            {
                if (_currentCount != value)
                {
                    _currentCount = value;
                    _gameView.SetCount(value);
                }
            }
        }

        public bool AutoEnterScore { get; set; }

        private Owner LastCardOwner
        {
            get
            {
                if (CountedCards.Count == 0)
                    return Owner.Uninitialized;

                var lastCard = (CardCtrl) CountedCards.Last().Tag;
                return lastCard.Owner;
            }
        }

        private void ToggleDealer()
        {
            Dealer = Dealer == PlayerType.Computer ? PlayerType.Player : PlayerType.Computer;
        }

        public static (List<CardCtrl> computerCards, List<CardCtrl> playerCards, List<CardCtrl> sharedCard) GetHands()
        {
            var dealtCards = CardCtrl.GetCards(13, Owner.Uninitialized);
            var playerCards = new List<CardCtrl>();
            var computerCards = new List<CardCtrl>();
            var sharedCards = new List<CardCtrl>();

            var ret = (computerCards: computerCards, playerCards: playerCards, sharedCard: sharedCards);

            for (var i = 0; i < 12; i += 2)
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

        public async Task<PlayerType> StartGame(GameState state)
        {
            while (true)
            {
                State = state;
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
                        PlayerTurn = Dealer == PlayerType.Computer ? PlayerType.Player : PlayerType.Computer;
                        var (computerCards, playerCards, sharedCard) = GetHands();
                        var crib = ComputerSelectCrib(computerCards, Dealer == PlayerType.Computer);
                        await _gameView.Deal(playerCards, computerCards, sharedCard, crib, Dealer);
                        CurrentCount = 0;
                        state = GameState.PlayerSelectsCribCards;
                    }
                        break;
                    case GameState.PlayerSelectsCribCards:
                        var playerCrib =
                            await Player.SelectCribUiCards(null,
                                Dealer == PlayerType
                                    .Player); // when I return from this, the cards are already in the crib.                      
                        state = GameState.GiveToCrib;
                        break;
                    case GameState.GiveToCrib:
                        await _gameView.MoveCardsToCrib();
                        //
                        //  shared card has been flipped -- check for Jack

                        if (SharedCard.CardOrdinal == CardOrdinal.Jack)
                        {
                            var score = new Score(ScoreName.HisNibs, 2);
                            AddScore(new List<Score> {score}, Dealer);
                        }

                        state = GameState.Count;
                        break;
                    case GameState.Count:

                        Debug.Assert(ComputerCards.Count == 4);
                        Debug.Assert(PlayerCards.Count == 4);
                        Debug.Assert(CribCards.Count == 4);
                        CurrentCount = 0;
                        state = Dealer == PlayerType.Player ? GameState.CountComputer : GameState.CountPlayer;
                        break;
                    case GameState.CountPlayer:
                    {
                        SetPlayableCards();

                        PlayerTurn = PlayerType.Player;
                        var (computerCanPlay, playerCanPlay) = CanPlay(ComputerCards, PlayerCards, CurrentCount);
                        state = GameState.CountComputer;
                        if (playerCanPlay)
                            CurrentCount = await DoCountForPlayer(Player, PlayerType.Player, CountedCards, PlayerCards,
                                CurrentCount);

                        (computerCanPlay, playerCanPlay) = CanPlay(ComputerCards, PlayerCards, CurrentCount);

                        if (computerCanPlay == false && playerCanPlay == false)
                            if (PlayerCards.Count == 0 && ComputerCards.Count == 0)
                            {
                                state = GameState.CountingEnded;
                            }
                            else
                            {
                                var goPlayer = await ScoreGo();
                                state = goPlayer == PlayerType.Computer ? GameState.CountPlayer : GameState.CountComputer;
                            }
                    }

                        break;
                    case GameState.CountComputer:
                    {
                        PlayerTurn = PlayerType.Computer;
                        CurrentCount = await DoCountForPlayer(Computer, PlayerType.Computer, CountedCards,
                            ComputerCards, CurrentCount);
                        state = GameState.CountPlayer;
                        var (computerCanPlay, playerCanPlay) = CanPlay(ComputerCards, PlayerCards, CurrentCount);
                        if (!computerCanPlay && playerCanPlay) _gameView.AddMessage("Computer can't play.  Go again.");
                        if (computerCanPlay == false && playerCanPlay == false)
                            if (PlayerCards.Count == 0 && ComputerCards.Count == 0)
                            {
                                state = GameState.CountingEnded;
                            }
                            else
                            {
                                var goPlayer = await ScoreGo();
                                state = goPlayer == PlayerType.Computer ? GameState.CountPlayer : GameState.CountComputer;
                            }
                    }
                        break;

                    case GameState.ScoreHand:
                        break;
                    case GameState.CountingEnded:
                        await EndCounting();
                        state = Dealer == PlayerType.Computer ? GameState.ScorePlayerHand : GameState.ScoreComputerHand;
                        break;
                    case GameState.ScorePlayerHand:
                        PlayerTurn = PlayerType.Player;

                        var playerScore = await GetScoreFromPlayer(PlayerCards, SharedCard, HandType.Hand);
                        state = Dealer == PlayerType.Computer ? GameState.ScoreComputerHand : GameState.ScorePlayerCrib;
                        break;
                    case GameState.ScoreComputerHand:
                        PlayerTurn = PlayerType.Computer;
                        await ScoreComputerHandAndNotifyView(ComputerCards, SharedCard, HandType.Hand);
                        state = Dealer == PlayerType.Computer ? GameState.ScoreComputerCrib : GameState.ScorePlayerHand;
                        break;
                    case GameState.ScoreComputerCrib:
                        await _gameView.ReturnCribCards(Dealer);
                        //
                        //  above moves cards from Crib to Computer
                        await ScoreComputerHandAndNotifyView(ComputerCards, SharedCard, HandType.Crib);
                        state = GameState.EndOfHand;
                        break;
                    case GameState.ScorePlayerCrib:
                        await _gameView.ReturnCribCards(Dealer);
                        //
                        //  above moves cards to Player
                        await GetScoreFromPlayer(PlayerCards, SharedCard, HandType.Crib);
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


                if (Player.Score > 120) return PlayerType.Player;
                if (Computer.Score > 120) return PlayerType.Computer;
            }

            throw new Exception("Shouldn't have exited the state loop");
        }

        //
        //  this needs to operate on the UI cards becuase the data cards are always a new list
        private async Task<int> DoCountForPlayer(Player player, PlayerType playerTurn, List<Card> countedCards,
            List<Card> uncountedCards, int currentCount)
        {
            PlayerTurn = playerTurn;

            if (uncountedCards.Count == 0)
                return currentCount;

            var cardPlayed = await player.GetCountCard(countedCards, uncountedCards, CurrentCount);


            if (cardPlayed != null)
            {
                var uiCard = cardPlayed.Tag as CardCtrl;
                var score = CardScoring.ScoreCountingCardsPlayed(countedCards, cardPlayed, currentCount,
                    out var scoreList);
                currentCount += uiCard.Value;
                await _gameView.CountCard(playerTurn, uiCard, currentCount);
                AddScore(scoreList, playerTurn);
                if (currentCount == 31)
                {
                    CurrentCount = 31; // force the UI to show 31 before you hit Continue
                    await _gameView.RestartCounting(playerTurn);
                    currentCount = 0;
                }
            }

            return currentCount;
        }

        public async Task<CardCtrl> GetSuggestionForCount()
        {
            if (PlayerCards.Count == 0) return null;

            var cardPlayed = await Computer.GetCountCard(CountedCards, PlayerCards, CurrentCount);
            return cardPlayed.Tag as CardCtrl;
        }

        private (bool computerCanPlay, bool playerCanPlay) CanPlay(List<Card> computerCards, List<Card> playerCards,
            int currentCount)
        {
            var cCanPlay = false;
            var pCanPlay = false;
            foreach (var card in computerCards)
                if (card.Value + currentCount <= 31)
                {
                    cCanPlay = true;
                    break;
                }

            foreach (var card in playerCards)
                if (card.Value + currentCount <= 31)
                {
                    pCanPlay = true;
                    break;
                }

            return (computerCanPlay: cCanPlay, playerCanPlay: pCanPlay );
        }

        private async Task<int> GetScoreFromPlayer(List<Card> cards, Card sharedCard, HandType handType)
        {
            var playerScore = CardScoring.ScoreHand(cards, sharedCard, handType, out var scores);
            var enteredScore = 0;

            do
            {
                enteredScore = await Player.GetScoreFromUser(playerScore, AutoEnterScore);
                if (enteredScore != playerScore)
                    AutoEnterScore = await StaticHelpers.AskUserYesNoQuestion("Cribbage",
                        $"{enteredScore} is the wrong score.\n\nWould you like the computer to set the score?", "Yes", "No");

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

        private async Task<PlayerType> ScoreGo()
        {
            var scores = new List<Score>
            {
                new Score(ScoreName.Go, 1)
            };
            var goPlayer = LastPlayerCounted();
            AddScore(scores, goPlayer);

            await _gameView.RestartCounting(goPlayer);
            CurrentCount = 0;

            SetPlayableCards();
            return goPlayer;
        }

        private void AddScore(List<Score> scores, PlayerType playerTurn)
        {
            if (scores == null)
                return;

            if (scores.Count == 0)
                return;

            var player = playerTurn == PlayerType.Computer ? Computer : Player;
            var scoreDelta = _gameView.AddScore(scores, playerTurn);
            player.Score += scoreDelta;
        }

        private async Task ScoreComputerHandAndNotifyView(List<Card> cards, Card sharedCard, HandType handType)
        {
            var score = CardScoring.ScoreHand(cards, sharedCard, handType, out var scores);
            await _gameView.ScoreComputerHand(scores, handType);
            Computer.Score += score;
        }


        private async Task EndCounting()
        {
            //
            // score 1 for last card -- unless we hit 31

            if (CountedCards.Count != 0)
            {
                var scores = new List<Score>();
                var scoreName = ScoreName.LastCard;
                scores.Add(new Score(scoreName, 1));
                var player = PlayerType.Computer;

                if (LastCardOwner == Owner.Player)
                    player = PlayerType.Player;

                AddScore(scores, player);
            }

            await _gameView.SendCardsBackToOwner();
        }

        public List<CardCtrl> ComputerSelectCrib(List<CardCtrl> cards, bool computerCrib)
        {
            if (cards.Count != 6) return null;
            var crib = Computer.SelectCribCards(CardCtrlToCards(cards), computerCrib).Result;
            return CardsToCardCtrl(crib);
        }


        public static List<Card> CardCtrlToCards(List<CardCtrl> uiCards)
        {
            var cards = new List<Card>();
            foreach (var uiCard in uiCards) cards.Add(uiCard.Card);

            return cards;
        }

        public static List<CardCtrl> CardsToCardCtrl(List<Card> hand)
        {
            var newHand = new List<CardCtrl>();
            foreach (var card in hand) newHand.Add(card.Tag as CardCtrl);
            return newHand;
        }

        internal void SetPlayableCards()
        {
            foreach (var card in PlayerCardCtrls) card.IsEnabled = card.Value + CurrentCount <= 31;
        }
    }
}