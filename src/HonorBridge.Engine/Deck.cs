using System;
using System.Collections.Generic;
using System.Linq;

namespace HonorBridge.Engine;

public class Deck
{
    private List<Card> _cards = null!;
    private Random _rng;

    public Deck()
    {
        _rng = new Random();
        Reset();
    }

    public void Reset()
    {
        _cards = new List<Card>();
        foreach (Suit s in Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank r in Enum.GetValues(typeof(Rank)))
            {
                _cards.Add(new Card(s, r));
            }
        }
    }

    public void Shuffle()
    {
        // Fisher-Yates shuffle
        int n = _cards.Count;
        while (n > 1)
        {
            n--;
            int k = _rng.Next(n + 1);
            Card value = _cards[k];
            _cards[k] = _cards[n];
            _cards[n] = value;
        }
    }

    public Dictionary<Compass, Hand> Deal()
    {
        if (_cards.Count != 52)
            throw new InvalidOperationException("Deck must be full to deal.");

        var hands = new Dictionary<Compass, Hand>
        {
            { Compass.North, new Hand() },
            { Compass.East, new Hand() },
            { Compass.South, new Hand() },
            { Compass.West, new Hand() }
        };

        // Standard deal: North, East, South, West...
        for (int i = 0; i < 52; i++)
        {
            var compass = (Compass)(i % 4);
            hands[compass].Add(_cards[i]);
        }

        return hands;
    }
}
