using System.Collections.Generic;

public class Player
{
    public string Name { get; set; }
    public List<Card> HoleCards { get; private set; }
    public int Chips { get; set; }
    public bool HasFolded { get; set; }
    public int CurrentBet { get; set; }
    public bool IsNPC { get; set; }
    public bool HasActed { get; set; } // For betting rounds

    public Player(string name, int chips, bool isNPC = false)
    {
        Name = name;
        Chips = chips;
        HoleCards = new List<Card>();
        HasFolded = false;
        CurrentBet = 0;
        IsNPC = isNPC;
        HasActed = false;
    }

    public void ReceiveCard(Card card)
    {
        HoleCards.Add(card);
    }

    public void Fold()
    {
        HasFolded = true;
    }

    public void ClearCards()
    {
        HoleCards.Clear();
        HasFolded = false;
        CurrentBet = 0;
        HasActed = false;
    }
}
