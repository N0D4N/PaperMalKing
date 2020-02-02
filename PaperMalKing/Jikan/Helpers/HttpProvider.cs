﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace PaperMalKing.Jikan.Helpers
{
	/// <summary>
	/// Provider class for static HttpClient.
	/// </summary>
	public static class HttpProvider
	{
		/// <summary>
		/// Endpoint for not SSL encrypted requests.
		/// </summary>
		public const string httpEndpoint = "http://api.jikan.moe/v3/";

		/// <summary>
		/// Endpoint for SSL encrypted requests.
		/// </summary>
		public const string httpsEndpoint = "https://api.jikan.moe/v3/";

		/// <summary>
		/// Constructor.
		/// </summary>
		static HttpProvider()
		{
		}

		/// <summary>
		/// Get static HttpClient. Using default Jikan REST endpoint.
		/// </summary>
		/// <param name="useHttps">Define if request should be send to SSL encrypted endpoint.</param>
		/// <returns>Static HttpClient.</returns>
		public static HttpClient GetHttpClient(bool useHttps)
		{
			var endpoint = useHttps ? httpsEndpoint : httpEndpoint;
			var Client = new HttpClient
			{
				BaseAddress = new Uri(endpoint)
			};
			Client.DefaultRequestHeaders.Accept.Clear();
			Client.DefaultRequestHeaders.Accept.Add(
				new MediaTypeWithQualityHeaderValue("application/json"));

			return Client;
		}

		/// <summary>
		/// Get static HttpClient. Using custom, user defined Jikan REST endpoint.
		/// </summary>
		/// <param name="endpoint">Endpoint of the REST API.</param>
		/// <returns>Static HttpClient.</returns>
		public static HttpClient GetHttpClient(Uri endpoint)
		{
			var Client = new HttpClient
			{
				BaseAddress = endpoint
			};
			Client.DefaultRequestHeaders.Accept.Clear();
			Client.DefaultRequestHeaders.Accept.Add(
				new MediaTypeWithQualityHeaderValue("application/json"));

			return Client;
		}
	}
}
