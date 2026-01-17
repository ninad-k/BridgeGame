using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace HonorBridge.Client.Wpf.ViewModels;

public partial class CardViewModel : ObservableObject
{
    public string Id { get; }

    public string Rank { get; }
    public string Suit { get; }
    public string SuitSymbol { get; }
    public Brush Color { get; }
    public bool IsRed { get; }

    [ObservableProperty]
    private bool _isEnabled = true;

    public CardViewModel(string id)
    {
        Id = id;
        if (id.Length < 2)
        {
            Rank = "?";
            Suit = "?";
            SuitSymbol = "?";
            Color = Brushes.Gray;
            return;
        }

        char rankChar = id[0];
        char suitChar = id[^1];

        Rank = rankChar.ToString();
        Suit = suitChar.ToString();

        switch (suitChar)
        {
            case 'H':
                SuitSymbol = "♥";
                IsRed = true;
                break;
            case 'D':
                SuitSymbol = "♦";
                IsRed = true;
                break;
            case 'C':
                SuitSymbol = "♣";
                IsRed = false;
                break;
            case 'S':
                SuitSymbol = "♠";
                IsRed = false;
                break;
            default:
                SuitSymbol = "?";
                IsRed = false;
                break;
        }

        Color = IsRed ? Brushes.Red : Brushes.Black;
    }
}
