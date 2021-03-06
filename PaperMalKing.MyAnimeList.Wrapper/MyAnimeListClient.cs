﻿#region LICENSE
// PaperMalKing.
// Copyright (C) 2021 N0D4N
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using PaperMalKing.Common.Converters;
using PaperMalKing.MyAnimeList.Wrapper.Models;
using PaperMalKing.MyAnimeList.Wrapper.Models.List;
using PaperMalKing.MyAnimeList.Wrapper.Models.List.Types;
using PaperMalKing.MyAnimeList.Wrapper.Models.Rss;
using PaperMalKing.MyAnimeList.Wrapper.Models.Rss.Types;
using PaperMalKing.MyAnimeList.Wrapper.Parsers;

namespace PaperMalKing.MyAnimeList.Wrapper
{
	public sealed class MyAnimeListClient
	{
		private readonly HttpClient _httpClient;
		private readonly ILogger<MyAnimeListClient> _logger;

		private readonly JsonSerializerOptions _jsonSerializerOptions = new()
		{
			Converters =
			{
				new JsonNumberToStringConverter(),
				new JsonToBoolConverter()
			}
		};

		private readonly XmlSerializer _xmlSerializer;

		internal MyAnimeListClient(ILogger<MyAnimeListClient> logger, HttpClient httpClient)
		{
			this._logger = logger;
			this._httpClient = httpClient;
			this._xmlSerializer = new(typeof(Feed));
		}

		private async Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken = default)
		{
			var response = await this._httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			return response.EnsureSuccessStatusCode();
		}

		private async Task<HtmlNode> GetAsHtmlAsync(string url, CancellationToken cancellationToken = default)
		{
			using var response = await this.GetAsync(url, cancellationToken);
			var doc = new HtmlDocument();
			await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
			doc.Load(stream);
			return doc.DocumentNode;
		}

		internal async Task<IEnumerable<FeedItem>> GetRecentRssUpdatesAsync<TR>(string username, CancellationToken cancellationToken = default)
			where TR : struct, IRssFeedType
		{
			var rssType = new TR();
			username = WebUtility.UrlEncode(username);
			var url = $"{rssType.Url}{username}";
			using var response = await this.GetAsync(url, cancellationToken);

			await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
			Feed? feed;
			try
			{
				feed = (Feed?) this._xmlSerializer.Deserialize(stream);
			}
			catch
			{
				return Enumerable.Empty<FeedItem>();
			}

			return feed?.Items ?? Enumerable.Empty<FeedItem>();
		}

		internal async Task<User> GetUserAsync(string username, ParserOptions options, CancellationToken cancellationToken = default)
		{
			if (options == ParserOptions.None)
				throw new ArgumentException("No reason to parse profile without anime/manga lists and favorites");
			this._logger.LogDebug("Requesting {@Username} profile", username);
			username = WebUtility.UrlEncode(username);
			var requestUrl = Constants.PROFILE_URL + username;
			var htmlNode = await this.GetAsHtmlAsync(requestUrl, cancellationToken);
			this._logger.LogTrace("Starting parsing {@Username} profile", username);
			var user = UserProfileParser.Parse(htmlNode, options);
			this._logger.LogTrace("Ended parsing {@Username} profile", username);
			return user;
		}

		internal async Task<string> GetUsernameAsync(ulong id, CancellationToken cancellationToken = default)
		{
			var url = $"{Constants.COMMENTS_URL}{id.ToString()}";
			this._logger.LogDebug("Requesting username by id {@Id}", id);
			var htmlNode = await this.GetAsHtmlAsync(url, cancellationToken);
			return CommentsParser.Parse(htmlNode);
		}

		internal async Task<IReadOnlyList<TE>> GetLatestListUpdatesAsync<TE, TListType>(string username,
																						CancellationToken cancellationToken = default)
			where TE : class, IListEntry where TListType : struct, IListType<TE>
		{
			var tl = new TListType();
			this._logger.LogDebug("Requesting {@Username} {@Type} list", username, tl.ListEntryType);

			username = WebUtility.UrlEncode(username);
			var url = tl.LatestUpdatesUrl(username);
			using var response = await this.GetAsync(url, cancellationToken);

			await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
			var updates = await JsonSerializer.DeserializeAsync<TE[]>(stream, this._jsonSerializerOptions, cancellationToken);
			return updates ?? Array.Empty<TE>();
		}
	}
}