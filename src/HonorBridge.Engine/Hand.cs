using System.Collections.Generic;
using System.Linq;

namespace HonorBridge.Engine;

public class Hand
{
    private readonly List<Card> _cards;

    public IReadOnlyList<Card> Cards => _cards;

    public int HighCardPoints => _cards.Sum(c => c.HighCardPoints);

    public int Size => _cards.Count;

    public Hand()
    {
        _cards = new List<Card>();
    }

    public Hand(IEnumerable<Card> cards)
    {
        _cards = new List<Card>(cards);
        Sort();
    }

    public void Add(Card card)
    {
        _cards.Add(card);
        Sort();
    }

    public bool Remove(Card card)
    {
        return _cards.Remove(card);
    }

    public void Sort()
    {
        // Sort descending: Spades (High->Low), Hearts, Diamonds, Clubs
        _cards.Sort((a, b) => 
        {
            int suitCompare = b.Suit.CompareTo(a.Suit); // Descending Suit
            if (suitCompare != 0) return suitCompare;
            
            return b.Rank.CompareTo(a.Rank); // Descending Rank
        });
    }

    public override string ToString()
    {
        return string.Join(", ", _cards.Select(c => c.ToShortString()));
    }

    public Hand Clone()
    {
        return new Hand(new List<Card>(_cards));
    }
}
