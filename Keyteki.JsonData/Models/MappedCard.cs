namespace Keyteki.JsonData.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class MappedCard
    {
        private readonly List<string> validKeywords = new List<string>{
            "Elusive",
            "Skirmish",
            "Taunt",
            "Deploy",
            "Alpha",
            "Omega",
            "Hazardous",
            "Assault",
            "Poison",
            "Enhance"
        };

        public Guid Id { get; set; }
        public string CardTitle { get; set; }
        public string House { get; set; }
        public string CardType { get; set; }
        public string FrontImage { get; set; }
        public string CardText { get; set; }
        public List<string> Traits { get; set; }
        public List<string> Keywords { get; set; }
        public int Amber { get; set; }
        public string Power { get; set; }
        public string Armor { get; set; }
        public string Rarity { get; set; }
        public string FlavorText { get; set; }
        public string CardNumber { get; set; }
        public int Expansion { get; set; }

        public MappedCard()
        {
            Traits = new List<string>();
            Keywords = new List<string>();
        }

        public MappedCard(Card card)
        {
            Id = card.Id;
            CardTitle = card.CardTitle;
            House = card.House;
            CardText = card.CardType;
            CardType = card.CardType;
            FrontImage = card.FrontImage;
            Traits = card.Traits?.Split(" • ", StringSplitOptions.RemoveEmptyEntries).ToList();
            Keywords = ParseKeywords(card);
            Amber = card.Amber;
            Power = card.Power;
            Rarity = card.Rarity;
            Armor = card.Armor;
            FlavorText = card.FlavorText;
            CardNumber = card.CardNumber;
            Expansion = card.Expansion;
        }

        private List<string> ParseKeywords(Card card)
        {
            var lines = card.CardText.Split(new [] {'\r', '\v', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            var potentialKeywords = new List<string>();

            foreach (var line in lines)
            {
                potentialKeywords.AddRange(line.Split('.').Select(k => k.Trim().Replace(' ', ':')));
            }

            var printedKeywords = potentialKeywords.Where(potentialKeyword => validKeywords.Any(potentialKeyword.StartsWith));

            return printedKeywords.ToList();
        }
    }
}