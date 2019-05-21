using System;
using System.Linq;
using System.Collections.Generic;

namespace LostCities
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WindowHeight = 38;
            var game = new Game();

            while (game.Deck.Count > 0)
            {
                TakeTurn(game.Player1);
                if (game.Deck.Count > 0) // make sure the last player didn't draw the last card
                    TakeTurn(game.Player2);
            }

            Console.WriteLine("=======================");
            Console.WriteLine($"Player 1 scored {game.Player1.Score}");
            Console.WriteLine($"Player 2 scored {game.Player2.Score}");

            Console.ReadKey(true);
        }

        static void TakeTurn(Player player)
        {
            DrawPlayingArea(player.Game);
            Console.SetCursorPosition(0, player.Number == 1 ? 0 : 36);

            PickCard(player);

            DrawPlayingArea(player.Game);
            Console.SetCursorPosition(0, player.Number == 1 ? 0 : 36);

            {
                var choices = GetChoices(player.Game);
                var choice = ReadOptions(player.Game, $"From where will you draw a card? {choices.prompt}", choices.choices);
                player.DrawFrom(choice);
            }

            DrawPlayingArea(player.Game);
            Console.SetCursorPosition(0, player.Number == 1 ? 0 : 36);

            (string prompt, IDictionary<char, Stack<Card>> choices) GetChoices(Game game)
            {
                var piles = game.Discards.Where(d=>player.CanDrawFrom(d.Value)).ToDictionary(d=>d.Key.ToString(), d=>d.Value);
                piles.Add("Deck", game.Deck);

                var options = piles.ToDictionary(p=>p.Key.ToLowerInvariant()[0], p=>p.Value);
                var prompt = string.Join(", ", piles.Select(p=>p.Key.Insert(0, "[").Insert(2, "]")));
                return (prompt, options);
            }
        }

        static Stack<Card> ReadOptions(Game game, string prompt, IDictionary<char, Stack<Card>> options)
        {
            Console.Write(prompt);
            do
            {
                var key = Console.ReadKey(true);
                if (options.ContainsKey(key.KeyChar))
                    return options[key.KeyChar];
            } while(true);
        }

        static void PickCard(Player player)
        {
            var candidateIndex = 0;
            do
            {
                player.Candidate = player.Hand[candidateIndex];

                Console.SetCursorPosition(0, player.Number == 1 ? 0 : 36);

                if (player.CanInvest(player.Candidate))
                {
                    Console.Write("Use the arrow keys to select a card. [I]nvest or [D]iscard");
                }
                else
                {
                    Console.Write("Use the arrow keys to select a card. [D]iscard            ");
                }

                Console.SetCursorPosition(6, player.Number == 1 ? 2 : 34);
                DrawHand(player);

                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.LeftArrow:
                        candidateIndex = Math.Max(candidateIndex-1, 0);
                        break;
                    case ConsoleKey.RightArrow:
                        candidateIndex = Math.Min(candidateIndex+1, player.Hand.Count - 1);
                        break;
                    case ConsoleKey.I:
                        if (player.CanInvest(player.Candidate))
                        {
                            player.Invest(player.Candidate);
                            return;
                        }
                        break;
                    case ConsoleKey.D:
                        player.Discard(player.Candidate);
                        return;
                }
            } while(true);
        }

        static void WriteCards(string title, IEnumerable<Card> cards, Func<Card, bool> usableCallback = null)
        {
            Console.WriteLine("====================");
            Console.WriteLine(title);
            Console.WriteLine("--------------------");
            var i = 0;
            foreach (var c in cards)
            {
                Console.Write($"{i++}. ");
                Console.ForegroundColor = GetConsoleColor(c.Suit, usableCallback?.Invoke(c) ?? true);
                Console.WriteLine(c);
                Console.ResetColor();
            }
        }

        static ConsoleColor GetConsoleColor(Suit suit, bool usable = true)
        {
            switch (suit)
            {
                case Suit.Blue: return usable ? ConsoleColor.Blue : ConsoleColor.DarkBlue;
                case Suit.Green: return usable ? ConsoleColor.Green : ConsoleColor.DarkGreen;
                case Suit.Red: return usable ? ConsoleColor.Red : ConsoleColor.DarkRed;
                case Suit.White: return usable ? ConsoleColor.White : ConsoleColor.DarkGray;
                case Suit.Yellow: return usable ? ConsoleColor.Yellow : ConsoleColor.DarkYellow;
                default: throw new InvalidOperationException("Unknown suit");
            }
        }

        static void DrawPlayingArea(Game game)
        {
            Console.Clear();
            Console.SetCursorPosition(0,1);
            Console.WriteLine($"Player {game.Player1.Number} Score: {game.Player1.Score}");
            if (game.CurrentPlayer == game.Player1) DrawHand(game.Player1);

            Console.SetCursorPosition(0,34);
            if (game.CurrentPlayer == game.Player2) DrawHand(game.Player2);
            Console.WriteLine($"Player {game.Player2.Number} Score: {game.Player2.Score}");

            var boardPosition = 18;

            var (left,top) = (1, boardPosition);
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = GetConsoleColor(suit, game.CurrentPlayer.CanDrawFrom(game.Discards[suit]));
                Console.SetCursorPosition(left, top);
                if (game.Discards[suit].TryPeek(out var lastDiscard))
                    Console.Write(GetCardDisplayText(lastDiscard));
                else
                    Console.Write(suit.ToString()[0].ToString());

                Console.ResetColor();

                Console.ForegroundColor = GetConsoleColor(suit);
                Console.SetCursorPosition(left-1, 4);
                Console.Write(game.Player1.Adventures[suit].Value);

                foreach (var c in game.Player1.Adventures[suit].Investments)
                {
                    Console.SetCursorPosition(left, --top);
                    Console.Write(GetCardDisplayText(c));
                }
                top = boardPosition;

                Console.SetCursorPosition(left-1, 32);
                Console.Write(game.Player2.Adventures[suit].Value);

                foreach (var c in game.Player2.Adventures[suit].Investments)
                {
                    Console.SetCursorPosition(left, ++top);
                    Console.Write(GetCardDisplayText(c));
                }
                top = boardPosition;
                left += 4;
            }

            top = boardPosition;

            Console.SetCursorPosition(left, boardPosition);
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.Write("D");
            Console.ResetColor();
            Console.Write($" = {game.Deck.Count}");
        }

        static void DrawHand(Player player)
        {
            Console.Write($"Hand: ");
            foreach (var card in player.Hand)
            {
                if (card == player.Candidate)
                {
                    Console.BackgroundColor = GetConsoleColor(card.Suit, player.CanInvest(card));
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                else
                {
                    Console.ForegroundColor = GetConsoleColor(card.Suit, player.CanInvest(card));
                }
                Console.Write(GetCardDisplayText(card));
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        static string GetCardDisplayText(Card card) => card.IsMultiplier ? "$" : card.Value == 10 ? "#" : card.Value.ToString();
    }

    public enum Suit
    {
        Red,
        Green,
        White,
        Blue,
        Yellow
    }

    public class Card : IComparable<Card>
    {
        public Card(int value, Suit suit)
        {
            this.IsMultiplier = value < 2;
            this.Value = IsMultiplier ? 0 : value;
            this.Suit = suit;
        }

        public Suit Suit {get;}
        public int Value{get;}
        public bool IsMultiplier {get;}

        public override string ToString()
        {
            if (IsMultiplier)
                return $"{Suit} Investment";
            else
                return $"{Suit} {Value}";
        }

        public int CompareTo(Card other)
        {
            if(other == null)
                return 1;

            if (Suit != other.Suit)
                return Suit - other.Suit;
            
            return Value - other.Value;
        }
    }

    public class Adventure
    {
        public Adventure(Suit suit)
        {
            this.Suit = suit;
            this.investments = new List<Card>();
        }

        private readonly List<Card> investments;

        public Suit Suit { get; }
        public IReadOnlyList<Card> Investments => investments;
        
        public int Cost => investments.Any() ? 20 : 0;

        public int Value => ((investments.Sum(c => c.Value) - Cost) * Multiplier) + Bonus;

        public int Multiplier => investments.Count(c => c.IsMultiplier) + 1;

        public int Bonus => investments.Count >= 8 ? 20 : 0;

        public void Invest(Card card)
        {
            this.investments.Add(card);
        }

        public bool CanInvest(Card card)
        {
            return card.Value >= (this.investments.LastOrDefault()?.Value ?? 0);
        }
    }

    public class Player
    {
        public Player(Game game, int number, List<Card> hand)
        {
            this.hand = hand;
            this.hand.Sort();
            this.Number = number;
            this.Adventures = Enum.GetValues(typeof(Suit)).OfType<Suit>().ToDictionary(s=>s, s=>new Adventure(s));
            this.Game = game;
        }

        public int Number {get;}

        private readonly List<Card> hand;

        public IReadOnlyList<Card> Hand => hand;
        
        public IDictionary<Suit, Adventure> Adventures { get; }
        
        public int Score => Adventures.Values.Sum(a => a.Value);

        public Game Game {get;}

        public Player Invest(Card card)
        {
            this.hand.Remove(card);
            this.Adventures[card.Suit].Invest(card);
            this.Candidate = null;
            return this;
        }

        public bool CanInvest(Card card)
        {
            return this.Adventures[card.Suit].CanInvest(card);
        }

        public Player Discard(Card card)
        {
            this.hand.Remove(card);
            this.Game.Discard(card);
            this.LastDiscardedCard = card;
            this.Candidate = null;
            return this;
        }

        public Card LastDiscardedCard {get;set;}

        public Card Candidate {get;set;}

        public void DrawFrom(Stack<Card> deck)
        {
            var card = deck.Pop();
            this.hand.Add(card);
            this.hand.Sort();
            this.Game.NextPlayer();
            this.LastDiscardedCard = null;
        }

        public bool CanDrawFrom(Stack<Card> stack)
        {
            return stack.TryPeek(out var topCard) && (this.LastDiscardedCard == null || this.LastDiscardedCard != topCard);
        }
    }

    public class Game
    {
        public static IList<Card> GenerateDeck()
        {
            return new List<Card>(from suit in Enum.GetValues(typeof(Suit)).OfType<Suit>()
                                  from v in Enumerable.Range(-1, 12)
                                  select new Card(v, suit));
        }

        public Game()
        {
            this.Deck = new Stack<Card>(GenerateDeck().Shuffle());
            this.Discards = Enum.GetValues(typeof(Suit)).OfType<Suit>().ToDictionary(s=>s, s => (Stack<Card>)new Stack<Card>());
            this.Players = Deal(8, 2).Select((hand, index) => new Player(this, index+1, hand.ToList())).ToArray();

            Card[][] Deal(int numberOfCards, int numberOfPlayers)
            {
                var result = new Card[numberOfPlayers][];
                for (var p = 0; p < numberOfPlayers; p++)
                    result[p] = new Card[numberOfCards];

                for (var i = 0; i<numberOfCards; i++)
                {
                    for (var p = 0; p < numberOfPlayers; p++)
                    {
                        var card = this.Deck.Pop();
                        result[p][i] = card;
                    }
                }

                return result;
            }
        }

        public Stack<Card> Deck {get;}
        public IReadOnlyDictionary<Suit, Stack<Card>> Discards {get;}
        public Player Player1 => this.Players[0];
        public Player Player2 => this.Players[1];

        public int CurrentPlayerIndex {get; set;} = 0;
        public Player CurrentPlayer => this.Players[CurrentPlayerIndex];

        public Player[] Players {get;}

        public void Discard(Card card)
        {
            this.Discards[card.Suit].Push(card);
        }

        public void NextPlayer()
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Length;
        }
    }

    public static class Extensions
    {
        private static readonly Random rng = new Random();  

        public static IList<T> Shuffle<T>(this IList<T> list)  
        {  
            int n = list.Count;  
            while (n > 1) {  
                n--;  
                int k = rng.Next(n + 1);  
                T value = list[k];  
                list[k] = list[n];  
                list[n] = value;  
            }  

            return list;
        }
    }
}
