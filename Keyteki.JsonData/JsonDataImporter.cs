namespace Keyteki.JsonData
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Keyteki.JsonData.Models;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    public class JsonDataImporter
    {
        private readonly ILogger<JsonDataImporter> logger;
        private readonly HttpClient httpClient;
        private readonly JsonSerializerOptions jsonSerializerOptions;

        private readonly List<string> languages = new List<string>
        {
            "de",
            "es",
            "fr",
            "it",
            "ko",
            "pl",
            "pt",
            "th",
            "zhhans",
            "zhhant"
        };

        public JsonDataImporter(ILogger<JsonDataImporter> logger)
        {
            this.logger = logger;
            httpClient = new HttpClient { BaseAddress = new Uri("https://www.keyforgegame.com/api/decks") };

            jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new JsonSnakeCaseNamingPolicy(),
                WriteIndented = true
            };
        }

        public async Task Run(IConfiguration configuration)
        {
            logger.LogInformation("JSON data importer starting.");

            const int pageSize = 25;
            await using var fileStream = File.Open("expansions.json", FileMode.Open);
            await using var cardDataStream = File.Open("carddata.json", FileMode.OpenOrCreate);

            var expansionArg = configuration.GetValue("expansion", "all");
            var availableExpansions =
                await JsonSerializer.DeserializeAsync<Dictionary<string, Expansion>>(fileStream, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            Dictionary<string, Dictionary<string, List<MappedCard>>> existingCardData;
            try
            {
                existingCardData = await JsonSerializer.DeserializeAsync<Dictionary<string, Dictionary<string, List<MappedCard>>>>(cardDataStream, jsonSerializerOptions);
            }
            catch
            {
                existingCardData = new Dictionary<string, Dictionary<string, List<MappedCard>>>();
            }

            var expansionsById = new Dictionary<int, Expansion>();

            foreach (var expansion in availableExpansions.Values)
            {
                foreach (var id in expansion.Ids)
                {
                    expansionsById[id] = expansion;
                }
            }

            var expansionsToFetch = GetExpansionsToFetch(expansionArg, availableExpansions);
            var cardCounts = new Dictionary<string, int>();
            var cards = existingCardData.ToDictionary(expansion => expansion.Key, cardList => cardList.Value.ToDictionary(language => language.Key, mappedCards => mappedCards.Value.ToDictionary(cardKey => cardKey.Id, c => c)));

            foreach (var expansion in expansionsToFetch)
            {
                cardCounts[expansion] = availableExpansions[expansion].CardCount;

                logger.LogInformation($"Expansion Id {expansion}, looking for {cardCounts[expansion]} cards.");

                foreach (var expansionId in availableExpansions[expansion].Ids)
                {
                    foreach (var language in languages)
                    {
                        logger.LogInformation($"Processing language {language}");

                        var totalPages = Math.Ceiling(await GetPageCount(expansionId) / (double) pageSize);

                        for (var pageNumber = 1; pageNumber < totalPages; pageNumber++)
                        {
                            var page = await RetryApiRequestUntilSucceeds($"?expansion={expansionId}&page={pageNumber}&links=cards&page_size={pageSize}&ordering=-date", language);
                            var response = JsonSerializer.Deserialize<DecksResponse>(page, jsonSerializerOptions);

                            foreach (var card in response.LinkedData.Cards)
                            {
                                // Exchange office is "special", it never appears in it's home house and is always a maverick, so if we don't include it here, it will never get picked up
                                if (card.IsMaverick && expansion != "mm" && card.CardNumber != "341" || card.IsEnhanced)
                                {
                                    continue;
                                }

                                var cardExpansion = expansionsById[card.Expansion].Code.ToLower();
                                if (!cards.ContainsKey(cardExpansion))
                                {
                                    cards[cardExpansion] = new Dictionary<string, Dictionary<Guid, MappedCard>>();
                                }

                                if (!cards[cardExpansion].ContainsKey(language))
                                {
                                    cards[cardExpansion][language] = new Dictionary<Guid, MappedCard>();
                                }

                                if (!cards[cardExpansion][language].ContainsKey(card.Id))
                                {
                                    cards[cardExpansion][language][card.Id] = new MappedCard(card);
                                }
                            }

                            logger.LogInformation($"Page {pageNumber}/{totalPages} fetched");

                            var cardsNeeded = cardCounts[expansion];
                            var cardsFound = cards[expansion][language].Values.Count;

                            logger.LogInformation($"Found {cardsFound}/{cardsNeeded} cards.");

                            if (cardsFound < cardsNeeded)
                            {
                                continue;
                            }

                            logger.LogInformation("Found all cards.");
                            break;
                        }
                    }
                }
            }

            cardDataStream.Seek(0, SeekOrigin.Begin);
            await JsonSerializer.SerializeAsync(cardDataStream, cards.ToDictionary(k => k.Key, v => v.Value.ToDictionary(k => k.Key, v => v.Value.Values.OrderBy(c => c.CardNumber))), jsonSerializerOptions);
        }

        private IEnumerable<string> GetExpansionsToFetch(string expansions, Dictionary<string, Expansion> availableExpansions)
        {
            var expansionsToFetch = new List<string>();

            if (expansions == "all")
            {
                expansionsToFetch.AddRange(availableExpansions.Keys);
            }
            else if (expansions.Contains(","))
            {
                var split = expansions.Split(",");

                foreach (var expansion in split)
                {
                    if (!availableExpansions.ContainsKey(expansion))
                    {
                        logger.LogWarning($"Unknown expansion {expansion}, skipping.");
                        continue;
                    }

                    expansionsToFetch.Add(expansion);
                }
            }
            else
            {
                expansionsToFetch.Add(expansions);
            }

            return expansionsToFetch;
        }

        private async Task<int> GetPageCount(int expansionId)
        {
            var result = await RetryApiRequestUntilSucceeds($"?expansion={expansionId}", "en");
            var response = JsonSerializer.Deserialize<DecksResponse>(result, jsonSerializerOptions);

            return response.Count;
        }

        private async Task<string> RetryApiRequestUntilSucceeds(string path, string language)
        {
            string result = null;
            var currentDelay = 1000;

            while (result == null)
            {
                httpClient.DefaultRequestHeaders.Remove("Accept-Language");
                httpClient.DefaultRequestHeaders.Add("Accept-Language", language);
                var response = httpClient.GetAsync(path);

                if (response.Result.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    logger.LogWarning($"API throttling, backing off for {currentDelay / 1000} seconds.");

                    await Task.Delay(currentDelay);
                    currentDelay *= 2;

                    continue;
                }

                result = await response.Result.Content.ReadAsStringAsync();
            }

            return result;
        }
    }
}