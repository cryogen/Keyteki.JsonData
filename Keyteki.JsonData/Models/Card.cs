namespace Keyteki.JsonData.Models
{
    using System;

    public class Card
    {
        public Guid Id { get; set; }
        public string CardTitle { get; set; }
        public string House { get; set; }
        public string CardType { get; set; }
        public string FrontImage { get; set; }
        public string CardText { get; set; }
        public string Traits { get; set; }
        public int Amber { get; set; }
        public string Power { get; set; }
        public string Armor { get; set; }
        public string Rarity { get; set; }
        public string FlavorText { get; set; }
        public string CardNumber { get; set; }
        public int Expansion { get; set; }
        public bool IsMaverick { get; set; }
        public bool IsAnomaly { get; set; }
        public bool IsEnhanced { get; set; }
    }
}