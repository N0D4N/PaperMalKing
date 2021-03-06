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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PaperMalKing.Common.RateLimiter
{
	public sealed class RateLimiter<T> : IRateLimiter<T>, IDisposable
	{
		private readonly string _serviceName;
		public RateLimit RateLimit { get; }
		private ILogger<IRateLimiter<T>> Logger { get; }
		private long _lastUpdateTime;

		private long _availablePermits;
		private SemaphoreSlim? _semaphoreSlim;

		internal RateLimiter(RateLimit rateLimit, ILogger<IRateLimiter<T>>? logger)
		{
			this._serviceName = $"{typeof(RateLimiter<T>).Name}<{typeof(T).Name}>";
			this.RateLimit = rateLimit;

			this.Logger = logger ?? NullLogger<RateLimiter<T>>.Instance;
			this._lastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			this._availablePermits = rateLimit.AmountOfRequests;
			this._semaphoreSlim = new (1, 1);
		}

		public async Task TickAsync(CancellationToken cancellationToken = default)
		{
			if (this._semaphoreSlim == null)
				return;
			await this._semaphoreSlim.WaitAsync(cancellationToken);
			try
			{
				var nextRefillDateTime = this._lastUpdateTime + this.RateLimit.PeriodInMilliseconds;
				var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				var arePermitsAvailable = this._availablePermits > 0;
				var isTooEarlyToRefill = now < nextRefillDateTime;
				if (isTooEarlyToRefill && !arePermitsAvailable)
				{
					var delay = nextRefillDateTime - now;
					var delayInMs = Convert.ToInt32(delay);
					this.Logger.LogDebug(
						$"[{this._serviceName}] Waiting {delayInMs.ToString()}ms.");
					await Task.Delay(delayInMs, cancellationToken);
				}
				else if (isTooEarlyToRefill) // && arePermitsAvailable
				{
					this.Logger.LogInformation("[{ServiceName}] Passing", this._serviceName);
					this._availablePermits--;
					return;
				}

				this._lastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				this._availablePermits = this.RateLimit.AmountOfRequests - 1;
			}
			finally
			{
				this._semaphoreSlim?.Release();
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"[{this._serviceName}] with rate limits {this.RateLimit}";
		}

		/// <inheritdoc />
		public void Dispose()
		{
			var semaphore = this._semaphoreSlim;
			this._semaphoreSlim = null;
			semaphore?.Dispose();
		}
	}
}