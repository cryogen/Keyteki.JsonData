namespace Keyteki.JsonData.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class DecksResponse
    {
        public List<Deck> Data { get; set; }
        [JsonPropertyName("_linked")]
        public DeckLinks LinkedData { get; set; }
        public int Count { get; set; }
    }
}