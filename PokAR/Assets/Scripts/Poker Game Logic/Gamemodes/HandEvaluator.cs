using System.Collections.Generic;

public class HandEvaluator
{
    // EvaluateHand: Given 7 cards (2 hole + 5 community),
    // find the best 5-card poker hand and return an integer score.
    // Higher score means better hand.
    public int EvaluateHand(List<SinglePlayerGameManager.Card> cards)
    {
        // We have 7 cards total
        // Generate all combinations of 5 out of these 7:
        List<List<SinglePlayerGameManager.Card>> fiveCardCombinations = Generate5CardCombinations(cards);

        int bestScore = -1;
        foreach (var combo in fiveCardCombinations)
        {
            int score = Evaluate5CardHand(combo);
            if (score > bestScore)
            {
                bestScore = score;
            }
        }
        return bestScore;
    }

    private List<List<SinglePlayerGameManager.Card>> Generate5CardCombinations(List<SinglePlayerGameManager.Card> cards)
    {
        List<List<SinglePlayerGameManager.Card>> result = new List<List<SinglePlayerGameManager.Card>>();
        // C(7,5)=21 combinations
        int count = cards.Count;
        for (int a=0; a<count-4; a++)
        for (int b=a+1; b<count-3; b++)
        for (int c=b+1; c<count-2; c++)
        for (int d=c+1; d<count-1; d++)
        for (int e=d+1; e<count; e++)
        {
            result.Add(new List<SinglePlayerGameManager.Card>(){cards[a],cards[b],cards[c],cards[d],cards[e]});
        }
        return result;
    }

    private int Evaluate5CardHand(List<SinglePlayerGameManager.Card> hand)
    {
        // Convert card values: Ace (1) should be treated as 14 for ranking, but also handle A-2-3-4-5
        // We'll just map value=1 to 14 here for ranking. We'll detect Ace-low straight separately.
        List<(int rank, string suit)> cards = new List<(int, string)>();
        foreach (var c in hand)
        {
            int val = c.Value;
            if (val == 1) val = 14; // Treat Ace as high by default
            cards.Add((val, c.Suit));
        }

        cards.Sort((a, b) => b.rank.CompareTo(a.rank)); // Sort descending by rank

        bool isFlush = IsFlush(cards);
        bool isStraight = IsStraight(cards, out int topStraightRank);
        // Check for Ace-low straight (A=14, but we have A,5,4,3,2)
        // topStraightRank will be the highest card in the straight
        var rankCounts = GetRankCounts(cards);
        var distinctRanks = new List<int>(rankCounts.Keys);
        distinctRanks.Sort((a,b)=>b.CompareTo(a)); // sort ranks descending
        // Determine hand category
        // Count occurrences
        // Format: 
        // Hand ranks (from best to worst): 
        // Royal flush (straight flush with top card=14), Straight flush, Four of a kind, Full house, Flush, Straight, Three of a kind, Two pair, One pair, High card
        
        // Identify patterns
        // rankCounts like: rank -> count
        // We identify largest groups:
        int fourOfKindRank = -1;
        int threeOfKindRank = -1;
        List<int> pairs = new List<int>();

        foreach (var kvp in rankCounts)
        {
            if (kvp.Value == 4) fourOfKindRank = kvp.Key;
            else if (kvp.Value == 3) threeOfKindRank = kvp.Key;
            else if (kvp.Value == 2) pairs.Add(kvp.Key);
        }
        pairs.Sort((a,b)=>b.CompareTo(a));

        // Check straight flush
        if (isFlush && isStraight)
        {
            // Straight flush
            // Royal flush if topStraightRank == 14
            // We'll just treat it as straight flush with top card
            // Category code: 9 million + topStraightRank
            // For a tie: only top card matters
            return 9000000 + topStraightRank;
        }

        // Four of a kind
        if (fourOfKindRank != -1)
        {
            // four of a kind: 8 million + rank of four + kicker
            int kicker = GetBestKickerForFourOfKind(cards, fourOfKindRank);
            return 8000000 + (fourOfKindRank * 100) + kicker;
        }

        // Full house (three of a kind + a pair)
        if (threeOfKindRank != -1 && pairs.Count > 0)
        {
            // full house: 7 million + (3ofKindRank *100)+ pairRank
            return 7000000 + (threeOfKindRank * 100) + pairs[0];
        }

        // Flush
        if (isFlush)
        {
            // flush: 6 million + ranks of cards in descending order
            return 6000000 + GetTieBreakerScore(cards);
        }

        // Straight
        if (isStraight)
        {
            // straight: 5 million + topStraightRank
            return 5000000 + topStraightRank;
        }

        // Three of a kind
        if (threeOfKindRank != -1)
        {
            // 3 of a kind: 4 million + threeRank *10000 + kicker ranks
            // Kickers: top two kickers not in the trip
            var kickers = GetKickers(cards, new List<int>(){threeOfKindRank});
            return 4000000 + (threeOfKindRank*10000) + kickers;
        }

        // Two pair
        if (pairs.Count >= 2)
        {
            // two pair: 3 million + highPair*10000 + lowPair*100 + kicker
            int highPair = pairs[0];
            int lowPair = pairs[1];
            var kickers = GetKickers(cards, new List<int>(){highPair, lowPair});
            return 3000000 + (highPair*10000) + (lowPair*100) + kickers;
        }

        // One pair
        if (pairs.Count == 1)
        {
            // one pair: 2 million + pairRank*10000 + kickers
            int pairRank = pairs[0];
            var kickers = GetKickers(cards, new List<int>(){pairRank});
            return 2000000 + (pairRank*10000) + kickers;
        }

        // High card
        // high card: 1 million + tiebreakers
        return 1000000 + GetTieBreakerScore(cards);
    }

    private bool IsFlush(List<(int rank, string suit)> cards)
    {
        string suit = cards[0].suit;
        for (int i=1; i<cards.Count; i++)
        {
            if (cards[i].suit != suit) return false;
        }
        return true;
    }

    private bool IsStraight(List<(int rank, string suit)> cards, out int topRank)
    {
        // Check sequence of ranks
        // Cards are sorted descending
        List<int> ranks = new List<int>();
        foreach(var c in cards)
        {
            if (!ranks.Contains(c.rank)) ranks.Add(c.rank);
        }

        // Now ranks is distinct sorted descending
        // Check for 5 in a row
        if (ranks.Count < 5)
        {
            topRank = 0;
            return false;
        }

        // Normal straight check
        for (int i=0; i<=ranks.Count-5; i++)
        {
            if (ranks[i]-1 == ranks[i+1] &&
                ranks[i+1]-1 == ranks[i+2] &&
                ranks[i+2]-1 == ranks[i+3] &&
                ranks[i+3]-1 == ranks[i+4])
            {
                topRank = ranks[i];
                return true;
            }
        }

        // Check Ace-low straight: If we have A(14),5,4,3,2
        // ranks would contain 14... check if we have 14,5,4,3,2
        // In that case, topRank = 5
        if (ranks.Contains(14) && ranks.Contains(5) && ranks.Contains(4) && ranks.Contains(3) && ranks.Contains(2))
        {
            topRank = 5; // Ace-low straight top rank is 5
            return true;
        }

        topRank = 0;
        return false;
    }

    private Dictionary<int,int> GetRankCounts(List<(int rank,string suit)> cards)
    {
        Dictionary<int,int> counts = new Dictionary<int,int>();
        foreach(var c in cards)
        {
            if (!counts.ContainsKey(c.rank)) counts[c.rank] = 0;
            counts[c.rank]++;
        }
        return counts;
    }

    private int GetBestKickerForFourOfKind(List<(int rank,string suit)> cards, int fourRank)
    {
        // one card not part of four
        foreach(var c in cards)
        {
            if (c.rank != fourRank) return c.rank;
        }
        return 0; 
    }

    private int GetKickers(List<(int rank,string suit)> cards, List<int> excludeRanks)
    {
        // Returns a number encoding kickers in descending order
        // We'll just do: kicker ranks * decreasing power of 100
        // e.g. if we have 2 kickers: kicker1*100 + kicker2
        List<int> kickers = new List<int>();
        foreach(var c in cards)
        {
            if (!excludeRanks.Contains(c.rank)) kickers.Add(c.rank);
        }
        kickers.Sort((a,b)=>b.CompareTo(a));

        int result = 0;
        int multiplier = 1;
        for (int i=kickers.Count-1; i>=0; i--)
        {
            result += kickers[i]*multiplier;
            multiplier *= 100; 
        }
        return result;
    }

    private int GetTieBreakerScore(List<(int rank,string suit)> cards)
    {
        // For hands like flush or high card, just encode all card ranks
        // descending in a single integer
        int result = 0;
        int multiplier = 1;
        // cards are descending
        for (int i=cards.Count-1; i>=0; i--)
        {
            result += cards[i].rank * multiplier;
            multiplier *= 100;
        }
        return result;
    }
}