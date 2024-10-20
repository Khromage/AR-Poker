using System;
using System.Collections.Generic;

public class Deck
{
    private List<Card> cards;
    private Random rng = new Random();

    public Deck()
    {
        cards = new List<Card>();
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank rank in Enum.GetValues(typeof(Rank)))
            {
                cards.Add(new Card(suit, rank));
            }
        }
    }

    public void Shuffle()
    {
        int n = cards.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Card value = cards[k];
            cards[k] = cards[n];
            cards[n] = value;
        }
    }

    public Card DealCard()
    {
        if (cards.Count == 0)
            return null;

        Card card = cards[0];
        cards.RemoveAt(0);
        return card;
    }
}
