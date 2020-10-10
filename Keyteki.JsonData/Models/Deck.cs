namespace Keyteki.JsonData.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class Deck
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Expansion { get; set; }
        public int PowerLevel { get; set; }
        public int Chains { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public bool IsMyDeck { get; set; }
        public List<Guid> Cards { get; set; }
        public List<string> Notes { get; set; }
        public bool IsMyFavorite { get; set; }
        public bool IsOnMyWatchlist { get; set; }
        public int CasualWins { get; set; }
        public int CasualLosses { get; set; }
        public string ShardBonus { get; set; }
        public Dictionary<string, List<string>> SetEraCards { get; set; }
        [JsonPropertyName("_links")]
        public Dictionary<string, List<string>> LinkedData { get; set; }
    }
}