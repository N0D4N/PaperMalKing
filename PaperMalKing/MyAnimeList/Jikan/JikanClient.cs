﻿using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DSharpPlus;
using Newtonsoft.Json;
using PaperMalKing.Data;
using PaperMalKing.MyAnimeList.Exceptions;
using PaperMalKing.MyAnimeList.Jikan.Data;
using PaperMalKing.MyAnimeList.Jikan.Data.Models;
using PaperMalKing.Services;
using PaperMalKing.Utilities;

namespace PaperMalKing.MyAnimeList.Jikan
{
	public sealed class JikanClient
	{
		private readonly HttpClient _httpClient;

		private readonly LogService _logService;
		private readonly ClockService _clock;

		private readonly JikanRateLimiter _rateLimiter;

		private const string LogName = "JikanClient";

		private readonly string _baseAddress;

		/// <summary>
		/// Constructor.
		/// </summary>
		public JikanClient(LogService logService, BotConfig config, ClockService clock, HttpClient httpClient)
		{
			var jConfig = config.Jikan;
			this._logService = logService;
			this._baseAddress = config.Jikan.Uri;
			this._rateLimiter =
				new JikanRateLimiter(
					new RateLimit(jConfig.RateLimit.RequestsCount, TimeSpan.FromMilliseconds(jConfig.RateLimit.TimeConstraint)), clock,
					this._logService);
			this._httpClient = httpClient;
		}


		private HttpRequestMessage PrepareHttpRequest(string url)
		{
			var request = new HttpRequestMessage(HttpMethod.Get, url)
			{
				Headers =
				{
					Accept =
					{
						new MediaTypeWithQualityHeaderValue("application/json")
					},
					UserAgent =
					{
						new ProductInfoHeaderValue(new ProductHeaderValue("PaperMalKing"))
					}
				}
			};
			return request;
		}

		private async Task<T> ExecuteGetRequestAsync<T>(string[] args) where T : BaseJikanRequest
		{
			T returnedObject = null;
			var requestUrl = string.Join("/", args);


			bool tryAgain;
			var url = this._baseAddress + requestUrl;
			do
			{
				tryAgain = false;
				HttpResponseMessage response = null;
				try
				{
					var request = this.PrepareHttpRequest(url);
					var token = await this._rateLimiter.GetTokenAsync();
					response = await this._httpClient.SendAsync(request);
					var statusCode = (int) response.StatusCode;
					if (response.IsSuccessStatusCode)
					{
						var json = await response.Content.ReadAsStringAsync();

						returnedObject = JsonConvert.DeserializeObject<T>(json);
						if (returnedObject.RequestCached)
							await this._rateLimiter.PopulateTokenAsync(token);
					}
					else if (response.StatusCode == HttpStatusCode.TooManyRequests)
					{
						this._logService.Log(LogLevel.Warning, LogName,
							"Got ratelimited for Jikan, waiting 10 s and retrying request again", this._clock.Now);
						await Task.Delay(TimeSpan.FromSeconds(10));
						tryAgain = true;
					}
					else if (statusCode >= 500 && statusCode < 600)
					{
						throw new ServerSideException(url,
							$"Encountered server-side issue while accessing '{url}' with status code '{statusCode}'");
					}
					else
					{
						throw new Exception($"Status code: '{response.StatusCode}'. Message: '{response.Content}'");
					}
				}
				catch (TaskCanceledException)
				{
					throw new ServerSideException(url, "Waited too long for getting info");
				}
				catch (JsonSerializationException ex)
				{
					throw new Exception("Serialization failed" + ex.Message);
				}
				finally
				{
					response?.Dispose();
				}
			} while (tryAgain);


			return returnedObject;
		}

		/// <summary>
		/// Returns information about user's profile with given username.
		/// </summary>
		/// <param name="username">Username.</param>
		/// <returns>Information about user's profile with given username.</returns>
		public Task<UserProfile> GetUserProfileAsync(string username)
		{
			var endpointParts = new[] {EndpointCategories.User, username, "profile"};

			return this.ExecuteGetRequestAsync<UserProfile>(endpointParts);
		}

		/// <summary>
		/// Return anime with given MAL id.
		/// </summary>
		/// <param name="id">MAL id of anime.</param>
		/// <returns>Anime with given MAL id.</returns>
		public Task<Anime> GetAnimeAsync(long id)
		{
			var endpointParts = new[] {EndpointCategories.Anime, id.ToString()};
			return this.ExecuteGetRequestAsync<Anime>(endpointParts);
		}

		/// <summary>
		/// Returns entries on user's anime list.
		/// </summary>
		/// <param name="username">Username.</param>
		/// <param name="searchQuery">Query to search.</param>
		/// <returns>Entries on user's anime list.</returns>
		public Task<UserAnimeList> GetUserAnimeListAsync(string username, string searchQuery)
		{
			var query = string.Concat("animelist", $"?q={searchQuery}");
			var endpointParts = new[] {EndpointCategories.User, username, query};
			return this.ExecuteGetRequestAsync<UserAnimeList>(endpointParts);
		}

		/// <summary>
		/// Gets entries on user's anime list from in order from latest updates to first updates
		/// </summary>
		/// <param name="username">Username</param>
		/// <returns>Entries on user's anime list ordered by latest update date</returns>
		public Task<UserAnimeList> GetUserRecentlyUpdatedAnimeAsync(string username)
		{
			const string query = "animelist/all?order_by=last_updated&sort=desc";
			var endpointParts = new[] {EndpointCategories.User, username, query};
			return this.ExecuteGetRequestAsync<UserAnimeList>(endpointParts);
		}

		/// <summary>
		/// Return manga with given MAL id.
		/// </summary>
		/// <param name="id">MAL id of manga.</param>
		/// <returns>Manga with given MAL id.</returns>
		public Task<Manga> GetMangaAsync(long id)
		{
			var endpointParts = new[] {EndpointCategories.Manga, id.ToString()};
			return this.ExecuteGetRequestAsync<Manga>(endpointParts);
		}

		/// <summary>
		/// Returns entries on user's manga list.
		/// </summary>
		/// <param name="username">Username.</param>
		/// <param name="searchQuery">Query to search.</param>
		/// <returns>Entries on user's manga list.</returns>
		public Task<UserMangaList> GetUserMangaList(string username, string searchQuery)
		{
			var query = string.Concat("mangalist", $"?q={searchQuery}");
			var endpointParts = new[] {EndpointCategories.User, username, query};
			return this.ExecuteGetRequestAsync<UserMangaList>(endpointParts);
		}

		public Task<UserMangaList> GetUserRecentlyUpdatedMangaAsync(string username)
		{
			const string query = "mangalist/all?order_by=last_updated&sort=desc";
			var endpointParts = new[] {EndpointCategories.User, username, query};
			return this.ExecuteGetRequestAsync<UserMangaList>(endpointParts);
		}
	}
}