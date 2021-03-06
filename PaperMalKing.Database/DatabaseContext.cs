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
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;
using PaperMalKing.Common.Enums;
using PaperMalKing.Database.Models;
using PaperMalKing.Database.Models.AniList;
using PaperMalKing.Database.Models.MyAnimeList;
using PaperMalKing.Database.Models.Shikimori;

namespace PaperMalKing.Database
{
	public class DatabaseContext : DbContext
	{
		public DbSet<BotUser> BotUsers { get; init; } = null!;

		public DbSet<DiscordGuild> DiscordGuilds { get; init; } = null!;

		public DbSet<DiscordUser> DiscordUsers { get; init; } = null!;

		public DbSet<MalUser> MalUsers { get; init; } = null!;

		public DbSet<MalFavoriteAnime> MalFavoriteAnimes { get; init; } = null!;

		public DbSet<MalFavoriteManga> MalFavoriteMangas { get; init; } = null!;

		public DbSet<MalFavoriteCharacter> MalFavoriteCharacters { get; init; } = null!;

		public DbSet<MalFavoritePerson> MalFavoritePersons { get; init; } = null!;

		public DbSet<ShikiUser> ShikiUsers { get; init; } = null!;

		public DbSet<ShikiFavourite> ShikiFavourites { get; init; } = null!;

		public DbSet<AniListUser> AniListUsers { get; init; } = null!;

		public DbSet<AniListFavourite> AniListFavourites { get; init; } = null!;

		private readonly string _connectionString;

		/// <summary>
		/// Used for migrations
		/// </summary>
		internal DatabaseContext()
		{
			this._connectionString = "Data Source=migrations.db";
		}

		internal DatabaseContext(string connectionString)
		{
			this._connectionString = connectionString;
		}

		public DatabaseContext(IOptions<DatabaseOptions> config)
		{
			this._connectionString = config.Value.ConnectionString;
		}

		public DatabaseContext(DbContextOptions<DatabaseContext> options, IOptions<DatabaseOptions> config) : base(options)
		{
			this._connectionString = config.Value.ConnectionString;
		}

		/// <inheritdoc />
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				optionsBuilder.UseSqlite(this._connectionString,
					builder => { builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery); });
				
				SQLitePCL.Batteries_V2.Init();
				// SQLITE_CONFIG_MULTITHREAD
				// https://github.com/dotnet/efcore/issues/9994
				// https://sqlite.org/threadsafe.html
				SQLitePCL.raw.sqlite3_config(2);
			}
		}

		/// <inheritdoc />
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			static void RegisterConverter<T>(ValueConverter valueConverter, ModelBuilder modelBuilder)
			{
				foreach (var entityType in modelBuilder.Model.GetEntityTypes())
				{
					var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(T) || p.PropertyType == typeof(T?));
					foreach (var property in properties)
					{
						modelBuilder.Entity(entityType.Name).Property(property.Name).HasConversion(valueConverter);
					}
				}
			}

			base.OnModelCreating(modelBuilder);

			// Constant value because default in app can be changed anytime
			modelBuilder.Entity<MalUser>().Property(mu => mu.Features).HasDefaultValue((MalUserFeatures) 127ul); 

			modelBuilder.Entity<MalFavoriteAnime>().HasKey(k => new
			{
				k.Id,
				k.UserId
			});
			modelBuilder.Entity<MalFavoriteManga>().HasKey(k => new
			{
				k.Id,
				k.UserId
			});
			modelBuilder.Entity<MalFavoriteCharacter>().HasKey(k => new
			{
				k.Id,
				k.UserId
			});
			modelBuilder.Entity<MalFavoritePerson>().HasKey(k => new
			{
				k.Id,
				k.UserId
			});

			modelBuilder.Entity<ShikiUser>().HasKey(k => k.Id);

			modelBuilder.Entity<ShikiUser>().Property(u => u.Features)
						.HasDefaultValue((ShikiUserFeatures) 127ul); // Constant value because default in app can be changed anytime

			modelBuilder.Entity<ShikiFavourite>().HasKey(k => new
			{
				k.Id,
				k.FavType,
				k.UserId
			});

			modelBuilder.Entity<AniListUser>().HasKey(k => k.Id);

			modelBuilder.Entity<AniListUser>().Property(u => u.Features).HasDefaultValue((AniListUserFeatures) 127ul);
			modelBuilder.Entity<AniListFavourite>().HasKey(k => new
			{
				k.Id,
				k.FavouriteType,
				k.UserId
			});

			var dtoConverter = new DateTimeOffsetToBinaryConverter();
			RegisterConverter<DateTimeOffset>(dtoConverter, modelBuilder);

			var ulongConverter = new ValueConverter<ulong, long>(ul => (long) ul, l => (ulong) l);
			RegisterConverter<ulong>(ulongConverter, modelBuilder);

			var favConverter = new EnumToNumberConverter<FavoriteType, byte>();
			RegisterConverter<FavoriteType>(favConverter, modelBuilder);
		}
	}
}