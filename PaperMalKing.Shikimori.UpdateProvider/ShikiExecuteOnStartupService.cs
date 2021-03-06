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

using System.Threading;
using System.Threading.Tasks;
using PaperMalKing.Database.Models.Shikimori;
using PaperMalKing.UpdatesProviders.Base;
using PaperMalKing.UpdatesProviders.Base.Features;

namespace PaperMalKing.Shikimori.UpdateProvider
{
	public sealed class ShikiExecuteOnStartupService : IExecuteOnStartupService
	{
		private readonly ICommandsService _commandsService;

		public ShikiExecuteOnStartupService(ICommandsService commandsService)
		{
			this._commandsService = commandsService;
		}

		public Task ExecuteAsync(CancellationToken cancellationToken = default)
		{
			this._commandsService.CommandsExtension.RegisterConverter(new FeatureArgumentConverter<ShikiUserFeatures>());
			return Task.CompletedTask;
		}
	}
}