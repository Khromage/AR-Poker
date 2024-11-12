using System;
using System.Collections.Generic;
using System.Linq;

public enum HandRank
{
    HighCard = 1,
    OnePair,
    TwoPair,
    ThreeOfAKind,
    Straight,
    Flush,
    FullHouse,
    FourOfAKind,
    StraightFlush,
    RoyalFlush
}

public class HandValue : IComparable<HandValue>
{
    public HandRank HandRank { get; set; }
    public List<Rank> HighCards { get; set; } // For tiebreakers

    public int CompareTo(HandValue other)
    {
        if (HandRank != other.HandRank)
            return HandRank.CompareTo(other.HandRank);
        else
        {
            for (int i = 0; i < HighCards.Count; i++)
            {
                if (HighCards[i] != other.HighCards[i])
                    return HighCards[i].CompareTo(other.HighCards[i]);
            }
            return 0; // Hands are completely equal
        }
    }
}

public static class HandEvaluator
{
    public static HandValue EvaluateHand(List<Card> playerCards, List<Card> communityCards)
    {
        List<Card> allCards = new List<Card>();
        allCards.AddRange(playerCards);
        allCards.AddRange(communityCards);

        // Sort cards by rank descending
        allCards = allCards.OrderByDescending(card => card.Rank).ThenByDescending(card => card.Suit).ToList();

        // Initialize hand value
        HandValue handValue = new HandValue();

        if (IsRoyalFlush(allCards, out List<Rank> highCards))
        {
            handValue.HandRank = HandRank.RoyalFlush;
            handValue.HighCards = highCards;
        }
        else if (IsStraightFlush(allCards, out highCards))
        {
            handValue.HandRank = HandRank.StraightFlush;
            handValue.HighCards = highCards;
        }
        else if (IsFourOfAKind(allCards, out highCards))
        {
            handValue.HandRank = HandRank.FourOfAKind;
            handValue.HighCards = highCards;
        }
        else if (IsFullHouse(allCards, out highCards))
        {
            handValue.HandRank = HandRank.FullHouse;
            handValue.HighCards = highCards;
        }
        else if (IsFlush(allCards, out highCards))
        {
            handValue.HandRank = HandRank.Flush;
            handValue.HighCards = highCards;
        }
        else if (IsStraight(allCards, out highCards))
        {
            handValue.HandRank = HandRank.Straight;
            handValue.HighCards = highCards;
        }
        else if (IsThreeOfAKind(allCards, out highCards))
        {
            handValue.HandRank = HandRank.ThreeOfAKind;
            handValue.HighCards = highCards;
        }
        else if (IsTwoPair(allCards, out highCards))
        {
            handValue.HandRank = HandRank.TwoPair;
            handValue.HighCards = highCards;
        }
        else if (IsOnePair(allCards, out highCards))
        {
            handValue.HandRank = HandRank.OnePair;
            handValue.HighCards = highCards;
        }
        else
        {
            handValue.HandRank = HandRank.HighCard;
            handValue.HighCards = allCards.Take(5).Select(card => card.Rank).ToList();
        }

        return handValue;
    }

    private static bool IsRoyalFlush(List<Card> cards, out List<Rank> highCards)
    {
        highCards = new List<Rank>();
        if (IsStraightFlush(cards, out highCards) && highCards[0] == Rank.Ace)
        {
            return true;
        }
        highCards = null;
        return false;
    }

    private static bool IsStraightFlush(List<Card> cards, out List<Rank> highCards)
    {
        highCards = new List<Rank>();
        var flushCards = GetFlushCards(cards);
        if (flushCards.Count >= 5)
        {
            if (IsStraight(flushCards, out highCards))
            {
                return true;
            }
        }
        highCards = null;
        return false;
    }

    private static bool IsFourOfAKind(List<Card> cards, out List<Rank> highCards)
    {
        highCards = new List<Rank>();
        var rankGroups = cards.GroupBy(card => card.Rank).OrderByDescending(group => group.Count());
        var fourOfAKindGroup = rankGroups.FirstOrDefault(group => group.Count() == 4);
        if (fourOfAKindGroup != null)
        {
            highCards.Add(fourOfAKindGroup.Key);
            var highestKicker = rankGroups.Where(group => group.Count() < 4).Select(group => group.Key).Max();
            highCards.Add(highestKicker);
            return true;
        }
        highCards = null;
        return false;
    }

    private static bool IsFullHouse(List<Card> cards, out List<Rank> highCards)
    {
        highCards = new List<Rank>();
        var rankGroups = cards.GroupBy(card => card.Rank).OrderByDescending(group => group.Count());
        var threeOfAKindGroup = rankGroups.FirstOrDefault(group => group.Count() == 3);
        var pairGroup = rankGroups.Where(group => group.Count() >= 2 && group.Key != threeOfAKindGroup?.Key).FirstOrDefault();
        if (threeOfAKindGroup != null && pairGroup != null)
        {
            highCards.Add(threeOfAKindGroup.Key);
            highCards.Add(pairGroup.Key);
            return true;
        }
        highCards = null;
        return false;
    }

    private static bool IsFlush(List<Card> cards, out List<Rank> highCards)
    {
        highCards = new List<Rank>();
        var suitGroups = cards.GroupBy(card => card.Suit).FirstOrDefault(group => group.Count() >= 5);
        if (suitGroups != null)
        {
            highCards = suitGroups.OrderByDescending(card => card.Rank).Take(5).Select(card => card.Rank).ToList();
            return true;
        }
        highCards = null;
        return false;
    }

    private static bool IsStraight(List<Card> cards, out List<Rank> highCards)
    {
        highCards = new List<Rank>();
        var distinctRanks = cards.Select(card => (int)card.Rank).Distinct().OrderByDescending(rank => rank).ToList();

        // Handle the low-Ace straight (Ace-2-3-4-5)
        if (distinctRanks.Contains((int)Rank.Ace))
        {
            distinctRanks.Add(1);
        }

        int consecutiveCount = 1;
        for (int i = 0; i < distinctRanks.Count - 1; i++)
        {
            if (distinctRanks[i] - 1 == distinctRanks[i + 1])
            {
                consecutiveCount++;
                if (consecutiveCount >= 5)
                {
                    highCards.Add((Rank)distinctRanks[i - 3]);
                    return true;
                }
            }
            else
            {
                consecutiveCount = 1;
            }
        }
        highCards = null;
        return false;
    }

    private static bool IsThreeOfAKind(List<Card> cards, out List<Rank> highCards)
    {
        highCards = new List<Rank>();
        var rankGroups = cards.GroupBy(card => card.Rank).OrderByDescending(group => group.Count());
        var threeOfAKindGroup = rankGroups.FirstOrDefault(group => group.Count() == 3);
        if (threeOfAKindGroup != null)
        {
            highCards.Add(threeOfAKindGroup.Key);
            var kickers = rankGroups.Where(group => group.Count() < 3).Select(group => group.Key).OrderByDescending(rank => rank).Take(2);
            highCards.AddRange(kickers);
            return true;
        }
        highCards = null;
        return false;
    }

    private static bool IsTwoPair(List<Card> cards, out List<Rank> highCards)
    {
        highCards = new List<Rank>();
        var rankGroups = cards.GroupBy(card => card.Rank).Where(group => group.Count() == 2).OrderByDescending(group => group.Key).ToList();
        if (rankGroups.Count >= 2)
        {
            highCards.Add(rankGroups[0].Key);
            highCards.Add(rankGroups[1].Key);
            var kicker = cards.Where(card => card.Rank != rankGroups[0].Key && card.Rank != rankGroups[1].Key)
                              .OrderByDescending(card => card.Rank)
                              .FirstOrDefault();
            highCards.Add(kicker.Rank);
            return true;
        }
        highCards = null;
        return false;
    }

    private static bool IsOnePair(List<Card> cards, out List<Rank> highCards)
    {
        highCards = new List<Rank>();
        var rankGroups = cards.GroupBy(card => card.Rank).OrderByDescending(group => group.Count());
        var pairGroup = rankGroups.FirstOrDefault(group => group.Count() == 2);
        if (pairGroup != null)
        {
            highCards.Add(pairGroup.Key);
            var kickers = rankGroups.Where(group => group.Key != pairGroup.Key)
                                    .Select(group => group.Key)
                                    .OrderByDescending(rank => rank)
                                    .Take(3);
            highCards.AddRange(kickers);
            return true;
        }
        highCards = null;
        return false;
    }

    private static List<Card> GetFlushCards(List<Card> cards)
    {
        var suitGroups = cards.GroupBy(card => card.Suit).FirstOrDefault(group => group.Count() >= 5);
        if (suitGroups != null)
        {
            return suitGroups.OrderByDescending(card => card.Rank).ToList();
        }
        return new List<Card>();
    }
}
