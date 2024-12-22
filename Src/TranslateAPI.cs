using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Tsundoku.Helpers
{
    public partial class TranslateAPI : IDisposable
	{
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
		private static readonly string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.66 Safari/537.36";
		private static GraphQLHttpClient AniListClient = new GraphQLHttpClient("https://graphql.anilist.co", new SystemTextJsonSerializer());
        private bool disposedValue;

		static TranslateAPI()
		{
			AniListClient.HttpClient.DefaultRequestHeaders.Add("RequestType", "POST");
			AniListClient.HttpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
			AniListClient.HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
			AniListClient.HttpClient.DefaultRequestHeaders.Add("UserAgent", USER_AGENT);
		}

        /// <summary>
		/// Translates a given manga or light novel title from Japanese or Romaji to English
		/// </summary>
		/// <param name="title">The title of the series as a string, this can be any title including ones under synonyms</param>
		/// <param name="format">The format of the series either MANGA or NOVEL</param>
		/// <returns></returns>
        public static async Task<string?> ToEnglish(string title, string format)
		{
			try
			{
				GraphQLRequest queryRequest = new()
				{
					Query = @"query ($title: String, $format: MediaFormat) {
                                Media(search: $title, format: $format) {
                                  title {
                                    english
                                  }
                                  relations {
                                    edges {
                                      relationType(version: 2)
                                    }
                                    nodes {
                                        title {
                                          native
                                        }
                                      }
                                    }
                                  }
                                }",
					Variables = new
					{
						title,
                        format
					}
				};
                GraphQLResponse<JsonDocument?> response = await AniListClient.SendQueryAsync<JsonDocument?>(queryRequest);
                short rateCheck = RateLimitCheck(response.AsGraphQLHttpResponse().ResponseHeaders);
				if (rateCheck != -1)
                {
                    LOGGER.Info($"Waiting {rateCheck} Seconds for Rate Limit To Reset");
                    await Task.Delay(TimeSpan.FromSeconds(rateCheck));
                    response = await AniListClient.SendQueryAsync<JsonDocument?>(queryRequest);
                }
                return response.Data.RootElement.GetProperty("Media").GetProperty("title").GetProperty("english").GetString();
			
			}
			catch(Exception e)
			{
				LOGGER.Error("AniList GetSeriesByTitle w/ {} Request Failed -> {}", title, e.Message);
			}
			return null;
		}


		/// <summary>
		/// Translates a given manga or light novel title from Japanese or English to Romaji
		/// </summary>
		/// <param name="title">The title of the series as a string, this can be any title including ones under synonyms</param>
		/// <param name="format">The format of the series either MANGA or NOVEL</param>
		/// <returns></returns>
        public static async Task<string?> ToRomaji(string title, string format)
		{
			try
			{
				GraphQLRequest queryRequest = new()
				{
					Query = @"query ($title: String, $format: MediaFormat) {
                                Media(search: $title, format: $format) {
                                  title {
                                    romaji
                                  }
                                  relations {
                                    edges {
                                      relationType(version: 2)
                                    }
                                    nodes {
                                        title {
                                          native
                                        }
                                      }
                                    }
                                  }
                                }",
					Variables = new
					{
						title,
                        format
					}
				};
                GraphQLResponse<JsonDocument?> response = await AniListClient.SendQueryAsync<JsonDocument?>(queryRequest);
                short rateCheck = RateLimitCheck(response.AsGraphQLHttpResponse().ResponseHeaders);
				if (rateCheck != -1)
                {
                    LOGGER.Info($"Waiting {rateCheck} Seconds for Rate Limit To Reset");
                    await Task.Delay(TimeSpan.FromSeconds(rateCheck));
                    response = await AniListClient.SendQueryAsync<JsonDocument?>(queryRequest);
                }
                return response.Data.RootElement.GetProperty("Media").GetProperty("title").GetProperty("romaji").GetString();
			
			}
			catch(Exception e)
			{
				LOGGER.Error("AniList GetSeriesByTitle w/ {} Request Failed -> {}", title, e.Message);
			}
			return null;
		}

        /// <summary>
		/// Translates a given manga or light novel title from Romaji or English to Japanese
		/// </summary>
		/// <param name="title">The title of the series as a string, this can be any title including ones under synonyms</param>
		/// <param name="format">The format of the series either MANGA or NOVEL</param>
		/// <returns></returns>
        public static async Task<string?> ToJapanese(string title, string format)
		{
			try
			{
				GraphQLRequest queryRequest = new()
				{
					Query = @"query ($title: String, $format: MediaFormat) {
                                Media(search: $title, format: $format) {
                                  title {
                                    native
                                  }
                                  relations {
                                    edges {
                                      relationType(version: 2)
                                    }
                                    nodes {
                                        title {
                                          native
                                        }
                                      }
                                    }
                                  }
                                }",
					Variables = new
					{
						title,
                        format
					}
				};
                GraphQLResponse<JsonDocument?> response = await AniListClient.SendQueryAsync<JsonDocument?>(queryRequest);
                short rateCheck = RateLimitCheck(response.AsGraphQLHttpResponse().ResponseHeaders);
				if (rateCheck != -1)
                {
                    LOGGER.Info($"Waiting {rateCheck} Seconds for Rate Limit To Reset");
                    await Task.Delay(TimeSpan.FromSeconds(rateCheck));
                    response = await AniListClient.SendQueryAsync<JsonDocument?>(queryRequest);
                }

                return response.Data.RootElement.GetProperty("Media").GetProperty("title").GetProperty("native").GetString();
			
			}
			catch(Exception e)
			{
				LOGGER.Error("AniList GetSeriesByTitle w/ {} Request Failed -> {}", title, e.Message);
			}
			return null;
		}

        private static short RateLimitCheck(HttpResponseHeaders responseHeaders)
        {
            responseHeaders.TryGetValues("X-RateLimit-Remaining", out var rateRemainingValues);
            _ = short.TryParse(rateRemainingValues?.FirstOrDefault(), out var rateRemaining);
            LOGGER.Info($"AniList Rate Remaining = {rateRemaining}");
            if (rateRemaining > 0)
            {
                return -1;
            }
            else
            {
                responseHeaders.TryGetValues("Retry-After", out var retryAfter);
                _ = short.TryParse(retryAfter?.FirstOrDefault(), out var retryAfterInSeconds);
                return retryAfterInSeconds;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
				AniListClient.Dispose();
                disposedValue = true;
            }
        }

        ~TranslateAPI()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}