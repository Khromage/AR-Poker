using System;
using System.Collections.Generic;

public class SinglePlayerGame
{
    private Deck deck;
    private Player user;
    private Player npc;
    private List<Card> communityCards;
    private int pot;
    private int smallBlind;
    private int bigBlind;
    private int dealerPosition;

    public SinglePlayerGame(string userName)
    {
        deck = new Deck();
        deck.Shuffle();

        user = new Player(userName, 1000);
        npc = new Player("NPC", 1000, isNPC: true);
        communityCards = new List<Card>();
        pot = 0;
        smallBlind = 10;
        bigBlind = 20;
        dealerPosition = 0; // 0: User is dealer, 1: NPC is dealer
    }

    public void StartGame()
    {
        while (user.Chips > 0 && npc.Chips > 0)
        {
            Console.WriteLine("Starting a new hand...");
            // Blinds
            PostBlinds();

            // Reset pot and hands
            pot = 0;
            user.ClearCards();
            npc.ClearCards();
            communityCards.Clear();
            deck = new Deck();
            deck.Shuffle();

            // Pre-Flop
            DealHoleCards();
            pot += user.CurrentBet + npc.CurrentBet;
            BettingRound();

            if (BothPlayersActive())
            {
                // Flop
                DealCommunityCards(3);
                ResetBets();
                BettingRound();
            }

            if (BothPlayersActive())
            {
                // Turn
                DealCommunityCards(1);
                ResetBets();
                BettingRound();
            }

            if (BothPlayersActive())
            {
                // River
                DealCommunityCards(1);
                ResetBets();
                BettingRound();
            }

            // Showdown
            DetermineWinner();

            // Switch dealer
            dealerPosition = (dealerPosition + 1) % 2;
        }

        Console.WriteLine(user.Chips > 0 ? $"{user.Name} wins the game!" : $"{npc.Name} wins the game!");
    }

    private void ResetBets()
    {
        user.CurrentBet = 0;
        npc.CurrentBet = 0;
        user.HasActed = false;
        npc.HasActed = false;
    }

    private void PostBlinds()
    {
        if (dealerPosition == 0)
        {
            // User is dealer
            user.CurrentBet = smallBlind;
            user.Chips -= smallBlind;

            npc.CurrentBet = bigBlind;
            npc.Chips -= bigBlind;
        }
        else
        {
            // NPC is dealer
            npc.CurrentBet = smallBlind;
            npc.Chips -= smallBlind;

            user.CurrentBet = bigBlind;
            user.Chips -= bigBlind;
        }
    }

    private void DealHoleCards()
    {
        user.ReceiveCard(deck.DealCard());
        user.ReceiveCard(deck.DealCard());

        npc.ReceiveCard(deck.DealCard());
        npc.ReceiveCard(deck.DealCard());
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
        int currentBet = Math.Max(user.CurrentBet, npc.CurrentBet);
        bool bettingComplete = false;

        while (!bettingComplete)
        {
            if (!user.HasFolded && !user.HasActed)
            {
                int userActionBet = GetUserAction(currentBet);
                if (userActionBet == -1)
                {
                    user.HasFolded = true;
                    break;
                }
                else
                {
                    int betDifference = userActionBet - user.CurrentBet;
                    user.CurrentBet = userActionBet;
                    user.Chips -= betDifference;
                    pot += betDifference;
                    currentBet = Math.Max(currentBet, userActionBet);
                }
                user.HasActed = true;
            }

            if (!npc.HasFolded && !npc.HasActed)
            {
                int npcActionBet = GetNpcAction(currentBet);
                if (npcActionBet == -1)
                {
                    npc.HasFolded = true;
                    break;
                }
                else
                {
                    int betDifference = npcActionBet - npc.CurrentBet;
                    npc.CurrentBet = npcActionBet;
                    npc.Chips -= betDifference;
                    pot += betDifference;
                    currentBet = Math.Max(currentBet, npcActionBet);
                }
                npc.HasActed = true;
            }

            // Check if betting is complete
            if ((user.HasActed || user.HasFolded) && (npc.HasActed || npc.HasFolded))
            {
                if (user.HasFolded || npc.HasFolded)
                {
                    bettingComplete = true;
                }
                else if (user.CurrentBet == npc.CurrentBet)
                {
                    bettingComplete = true;
                }
                else
                {
                    // Reset HasActed to false for players who need to act again
                    if (user.CurrentBet < currentBet && !user.HasFolded)
                    {
                        user.HasActed = false;
                    }
                    if (npc.CurrentBet < currentBet && !npc.HasFolded)
                    {
                        npc.HasActed = false;
                    }
                }
            }
        }
    }

    private int GetUserAction(int currentBet)
    {
        Console.WriteLine($"Your chips: {user.Chips}");
        Console.WriteLine($"Current bet to call: {currentBet}");
        Console.WriteLine("Your hand:");
        foreach (var card in user.HoleCards)
        {
            Console.WriteLine(card);
        }
        Console.WriteLine("Community cards:");
        foreach (var card in communityCards)
        {
            Console.WriteLine(card);
        }
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

    private int GetNpcAction(int currentBet)
    {
        // Simple AI logic based on hand strength
        HandValue npcHandValue = HandEvaluator.EvaluateHand(npc.HoleCards, communityCards);
        int handStrength = (int)npcHandValue.HandRank;

        if (handStrength >= (int)HandRank.TwoPair)
        {
            Console.WriteLine("NPC raises.");
            return currentBet + 20;
        }
        else if (handStrength >= (int)HandRank.OnePair)
        {
            Console.WriteLine("NPC calls.");
            return currentBet;
        }
        else
        {
            Console.WriteLine("NPC folds.");
            return -1;
        }
    }

    private bool BothPlayersActive()
    {
        return !user.HasFolded && !npc.HasFolded;
    }

    private void DetermineWinner()
    {
        if (user.HasFolded)
        {
            Console.WriteLine($"{npc.Name} wins the pot of {pot} chips (user folded).");
            npc.Chips += pot;
            return;
        }

        if (npc.HasFolded)
        {
            Console.WriteLine($"{user.Name} wins the pot of {pot} chips (NPC folded).");
            user.Chips += pot;
            return;
        }

        HandValue userHandValue = HandEvaluator.EvaluateHand(user.HoleCards, communityCards);
        HandValue npcHandValue = HandEvaluator.EvaluateHand(npc.HoleCards, communityCards);

        int comparison = userHandValue.CompareTo(npcHandValue);

        if (comparison > 0)
        {
            Console.WriteLine($"{user.Name} wins with {userHandValue.HandRank}!");
            user.Chips += pot;
        }
        else if (comparison < 0)
        {
            Console.WriteLine($"{npc.Name} wins with {npcHandValue.HandRank}!");
            npc.Chips += pot;
        }
        else
        {
            Console.WriteLine("It's a tie! Pot is split.");
            user.Chips += pot / 2;
            npc.Chips += pot / 2;
        }
    }
}
