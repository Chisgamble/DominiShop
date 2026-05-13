using System;
using Microsoft.UI.Xaml;

namespace DominiShop.Model
{
    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty; // "User" or "AI"
        public string Text { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

        public bool IsUser => Role == "User";
        public bool IsAI => Role == "AI";

        public HorizontalAlignment Alignment => IsUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    }
}
