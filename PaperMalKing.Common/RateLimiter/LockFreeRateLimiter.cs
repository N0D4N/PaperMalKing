﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PaperMalKing.Common.RateLimiter
{
	public sealed class LockFreeRateLimiter<T> : IRateLimiter<T>
	{
		private readonly string _serviceName;
		public RateLimit RateLimit { get; }
		private ILogger<IRateLimiter<T>> Logger { get; }
		private long _lastUpdateTime;

		private long _availablePermits;

		private readonly long _delayBetweenRefills;

		private SpinWait _spinner = new();

		internal LockFreeRateLimiter(RateLimit rateLimit, ILogger<IRateLimiter<T>>? logger)
		{
			this._serviceName = $"{typeof(LockFreeRateLimiter<T>).Name}<{typeof(T).Name}>";
			this.RateLimit = rateLimit;

			this.Logger = logger ?? NullLogger<LockFreeRateLimiter<T>>.Instance;
			this._lastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			this._availablePermits = rateLimit.AmountOfRequests;
			this._delayBetweenRefills = rateLimit.PeriodInMilliseconds;
		}

		public async Task TickAsync()
		{
			while (true)
			{
				var lastUpdateTime = Interlocked.Read(ref this._lastUpdateTime);
				var availablePermits = Interlocked.Read(ref this._availablePermits);
				var nextRefillDateTime = lastUpdateTime + this._delayBetweenRefills;
				var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				var arePermitsAvailable = availablePermits > 0;
				var isTooEarlyToRefill = now < nextRefillDateTime;
				switch (isTooEarlyToRefill)
				{
					case true when !arePermitsAvailable:
					{
						var delay = nextRefillDateTime - now;
						var delayInMs = Convert.ToInt32(delay);
						this.Logger.LogDebug("[{ServiceName}] Waiting {@Delay}ms.", this._serviceName, delayInMs);
						await Task.Delay(delayInMs);
						break;
					}
					// && arePermitsAvailable
					case true:
					{
						this.Logger.LogTrace("[{ServiceName}] Passing", this._serviceName);
						Interlocked.Decrement(ref this._availablePermits);
						return;
					}
				}

				if (Interlocked.CompareExchange(ref this._lastUpdateTime, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), lastUpdateTime) ==
					lastUpdateTime)
				{
					this.Logger.LogTrace("[{ServiceName}] Updating {AvailablePermits}", this._serviceName, this._availablePermits);
					Interlocked.Exchange(ref this._availablePermits, this.RateLimit.AmountOfRequests - 1);
					return;
				}
				else
				{
					this.Logger.LogTrace("[{ServiceName}] Couldn't update {LastUpdateTime}. Spinning.", this._serviceName, this._lastUpdateTime);
					this._spinner.SpinOnce();
				}
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"[{this._serviceName}] with rate limits {this.RateLimit}";
		}
	}
}