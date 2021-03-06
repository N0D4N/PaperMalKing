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

namespace PaperMalKing.Shikimori.Wrapper
{
	internal static class Constants
	{
		public const string BASE_URL = "https://shikimori.one";

		public const string BASE_API_URL = "/api";

		public const string BASE_USERS_API_URL = BASE_API_URL + "/users";

		public const byte HISTORY_LIMIT = 100;

		public const ushort LIST_LIMIT = 5000;
	}
}