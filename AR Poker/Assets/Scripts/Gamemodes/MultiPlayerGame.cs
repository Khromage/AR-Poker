using System;
using System.Collections.Generic;
using System.Linq;

public class MultiPlayerGame
{
    private Deck deck;
    private List<Player> players;
    private List<Card> communityCards;
    private int pot;
    private int dealerPosition;
    private int smallBlind;
    private int bigBlind;

    public MultiPlayerGame(List<string> playerNames)
    {
        if (playerNames.Count < 2 || playerNames.Count > 8)
            throw new ArgumentException("Number of players must be between 2 and 8.");

        deck = new Deck();
        deck.Shuffle();

        players = new List<Player>();
        foreach (var name in playerNames)
        {
            bool isNPC = name.StartsWith("NPC");
            players.Add(new Player(name, 1000, isNPC));
        }

        communityCards = new List<Card>();
        pot = 0;
        dealerPosition = 0;
        smallBlind = 10;
        bigBlind = 20;
    }

    public void StartGame()
    {
        while (players.Count(p => p.Chips > 0) > 1)
        {
            Console.WriteLine("Starting a new hand...");

            // Blinds
            PostBlinds();

            // Reset pot and hands
            pot = 0;
            foreach (var player in players)
            {
                player.ClearCards();
            }
            communityCards.Clear();
            deck = new Deck();
            deck.Shuffle();

            // Pre-Flop
            DealHoleCards();
            foreach (var player in players)
            {
                pot += player.CurrentBet;
            }
            BettingRound();

            if (ActivePlayersCount() > 1)
            {
                // Flop
                DealCommunityCards(3);
                ResetBets();
                BettingRound();
            }

            if (ActivePlayersCount() > 1)
            {
                // Turn
                DealCommunityCards(1);
                ResetBets();
                BettingRound();
            }

            if (ActivePlayersCount() > 1)
            {
                // River
                DealCommunityCards(1);
                ResetBets();
                BettingRound();
            }

            // Showdown
            DetermineWinner();

            // Move dealer button
            dealerPosition = (dealerPosition + 1) % players.Count;
        }

        var winner = players.FirstOrDefault(p => p.Chips > 0);
        Console.WriteLine($"{winner.Name} wins the game!");
    }

    private void ResetBets()
    {
        foreach (var player in players)
        {
            player.CurrentBet = 0;
            player.HasActed = false;
        }
    }

    private void PostBlinds()
    {
        int smallBlindPosition = (dealerPosition + 1) % players.Count;
        int bigBlindPosition = (dealerPosition + 2) % players.Count;

        players[smallBlindPosition].CurrentBet = smallBlind;
        players[smallBlindPosition].Chips -= smallBlind;

        players[bigBlindPosition].CurrentBet = bigBlind;
        players[bigBlindPosition].Chips -= bigBlind;
    }

    private void DealHoleCards()
    {
        for (int i = 0; i < 2; i++)
        {
            foreach (var player in players)
            {
                player.ReceiveCard(deck.DealCard());
            }
        }
    }

    private void DealCommunityCards(int number)
    {
        for (int i = 0; i < number; i++)
        {
            communityCards.Add(deck.DealCard());
        }
    }

    private void BettingRound()
    {
        int currentBet = players.Max(p => p.CurrentBet);
        bool bettingComplete = false;

        while (!bettingComplete)
        {
            foreach (var player in players)
            {
                if (!player.HasFolded && !player.HasActed && player.Chips > 0)
                {
                    int actionBet = GetPlayerAction(player, currentBet);
                    if (actionBet == -1)
                    {
                        player.HasFolded = true;
                    }
                    else
                    {
                        int betDifference = actionBet - player.CurrentBet;
                        player.CurrentBet = actionBet;
                        player.Chips -= betDifference;
                        pot += betDifference;
                        currentBet = Math.Max(currentBet, actionBet);
                    }
                    player.HasActed = true;
                }
            }

            // Check if betting is complete
            if (players.All(p => p.HasActed || p.HasFolded || p.Chips == 0))
            {
                if (players.Count(p => !p.HasFolded && p.Chips > 0) <= 1)
                {
                    bettingComplete = true;
                }
                else if (players.Where(p => !p.HasFolded && p.Chips > 0).All(p => p.CurrentBet == currentBet))
                {
                    bettingComplete = true;
                }
                else
                {
                    // Reset HasActed for players who need to act again
                    foreach (var player in players)
                    {
                        if (!player.HasFolded && player.Chips > 0 && player.CurrentBet < currentBet)
                        {
                            player.HasActed = false;
                        }
                    }
                }
            }
        }
    }

    private int GetPlayerAction(Player player, int currentBet)
    {
        if (player.IsNPC)
        {
            // Simple AI for NPCs
            HandValue handValue = HandEvaluator.EvaluateHand(player.HoleCards, communityCards);
            int handStrength = (int)handValue.HandRank;

            if (handStrength >= (int)HandRank.TwoPair)
            {
                Console.WriteLine($"{player.Name} raises.");
                return currentBet + 20;
            }
            else if (handStrength >= (int)HandRank.OnePair)
            {
                Console.WriteLine($"{player.Name} calls.");
                return currentBet;
            }
            else
            {
                Console.WriteLine($"{player.Name} folds.");
                return -1;
            }
        }
        else
        {
            Console.WriteLine($"{player.Name}'s turn. Chips: {player.Chips}");
            Console.WriteLine("Your hand:");
            foreach (var card in player.HoleCards)
            {
                Console.WriteLine(card);
            }
            Console.WriteLine("Community cards:");
            foreach (var card in communityCards)
            {
                Console.WriteLine(card);
            }
            Console.WriteLine($"Current highest bet: {currentBet}");
            Console.WriteLine("Choose your action: 1) Fold 2) Call 3) Raise");
            string input = Console.ReadLine();
            switch (input)
            {
                case "1":
                    return -1; // Fold
                case "2":
                    return currentBet; // Call
                case "3":
                    Console.WriteLine("Enter raise amount:");
                    int raiseAmount = int.Parse(Console.ReadLine());
                    return currentBet + raiseAmount; // Raise
                default:
                    Console.WriteLine("Invalid input. Folding by default.");
                    return -1;
            }
        }
    }

    private int ActivePlayersCount()
    {
        return players.Count(p => !p.HasFolded && p.Chips > 0);
    }

    private void DetermineWinner()
    {
        var activePlayers = players.Where(p => !p.HasFolded && p.Chips > 0).ToList();

        if (activePlayers.Count == 1)
        {
            // Only one player remaining
            var winner = activePlayers.First();
            Console.WriteLine($"{winner.Name} wins the pot of {pot} chips (others folded).");
            winner.Chips += pot;
            return;
        }

        // Showdown
        Player winnerPlayer = null;
        HandValue bestHand = null;

        foreach (var player in activePlayers)
        {
            HandValue handValue = HandEvaluator.EvaluateHand(player.HoleCards, communityCards);
            if (bestHand == null || handValue.CompareTo(bestHand) > 0)
            {
                bestHand = handValue;
                winnerPlayer = player;
            }
        }

        Console.WriteLine($"{winnerPlayer.Name} wins the pot of {pot} chips with {bestHand.HandRank}!");
        winnerPlayer.Chips += pot;
    }
}
