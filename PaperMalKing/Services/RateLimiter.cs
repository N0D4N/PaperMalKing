﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using PaperMalKing.Data;
using PaperMalKing.Utilities;

namespace PaperMalKing.Services
{
	public class RateLimiter
	{
		public readonly RateLimit RateLimit;
		protected readonly ClockService Clock;
		public string RateLimiterName { get;}
		public LogDelegate Log { get; }
		private DateTime _lastUpdateTime;

		protected readonly FixedSizeQueue<RateLimiterToken> Tokens;

		protected readonly SemaphoreSlim SemaphoreSlim;

		public RateLimiter(RateLimit rateLimit, ClockService clock, string rateLimiterName, LogDelegate log)
		{
			this.RateLimit = rateLimit;
			this.Clock = clock;
			this.RateLimiterName = rateLimiterName;
			this.Log = log;
			this._lastUpdateTime = this.Clock.UtcNow.Subtract(this.RateLimit.TimeConstraint);
			this.SemaphoreSlim = new SemaphoreSlim(1, 1);
			this.Tokens = new FixedSizeQueue<RateLimiterToken>(this.RateLimit.AmountOfRequests);
		}

		public async Task<RateLimiterToken> GetTokenAsync()
		{
			await this.SemaphoreSlim.WaitAsync();
			try
			{
				var nextRefillDateTime = this._lastUpdateTime.Add(this.RateLimit.TimeConstraint);
				var now = this.Clock.UtcNow;
				var areTokensAvailable = this.Tokens.Any();
				var isTooEarlyToRefill = DateTime.Compare(now, nextRefillDateTime) < 0;
				if (isTooEarlyToRefill && !areTokensAvailable)
				{
					var delay = (nextRefillDateTime - now).Duration();
					var delayInMs = Convert.ToInt32(delay.TotalMilliseconds);
					this.Log(LogLevel.Debug, this.RateLimiterName,
						$"Waiting {delayInMs}ms before getting next token.", this.Clock.Now);
					await Task.Delay(delay);
				}
				else if (isTooEarlyToRefill) // && TokensAreAvailable
				{
					if(this.Tokens.TryDequeue(out var token))
						return token;
					throw new ArgumentException("Queue is empty");
				}
				else // Removing old tokens to generate new
				{
					this.Tokens.Clear();
				}

				this._lastUpdateTime = this.Clock.UtcNow;
				for (int i = 0; i < this.RateLimit.AmountOfRequests - 1; i++)
					this.Tokens.Enqueue(new RateLimiterToken(this.Clock.UtcNow));
				return new RateLimiterToken(this.Clock.UtcNow);
			}
			finally
			{
				this.SemaphoreSlim.Release();
			}
		}
	}
}