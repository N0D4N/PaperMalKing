﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaperMalKing.Database.Models;
using PaperMalKing.Database.Models.MyAnimeList;
using PaperMalKing.Options;
using MALUserFavManga = PaperMalKing.Database.Models.MyAnimeList.UserFavoriteManga;
using MALUserFavChar = PaperMalKing.Database.Models.MyAnimeList.UserFavoriteCharacter;
using MALUserFavPerson = PaperMalKing.Database.Models.MyAnimeList.UserFavoritePerson;


namespace PaperMalKing.Services
{
	public class DatabaseContext : DbContext
	{
		public DbSet<DiscordGuild> DiscordGuilds { get; set; } = null!;

		public DbSet<DiscordUser> DiscordUsers { get; set; } = null!;

		public DbSet<User> MyAnimeListUsers { get; set; } = null!;

		public DbSet<UserFavoriteAnime> MyAnimeListUserFavoriteAnimes { get; set; } = null!;

		public DbSet<MALUserFavManga> MyAnimeListUserFavoriteMangas { get; set; } = null!;

		public DbSet<MALUserFavChar> MyAnimeListUserFavoriteCharacters { get; set; } = null!;

		public DbSet<MALUserFavPerson> MyAnimeListUserFavoritePersons { get; set; } = null!;

		private readonly string _connectionString;

		/// <summary>
		/// Used for migrations
		/// </summary>
		public DatabaseContext()
		{
			this._connectionString = "Data Source=migrations.db";
		}

		public DatabaseContext(IOptions<DatabaseOptions> config)
		{
			this._connectionString = config.Value.ConnectionString;
		}

		public DatabaseContext(DbContextOptions<DatabaseContext> options, IOptions<DatabaseOptions> config) :
			base(options)
		{
			this._connectionString = config.Value.ConnectionString;
		}

		/// <inheritdoc />
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
				optionsBuilder.UseSqlite(this._connectionString);
		}

		/// <inheritdoc />
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<DiscordGuildUser>().HasKey(dgu => new
			{
				dgu.DiscordGuildId,
				dgu.DiscordUserId
			});

			modelBuilder.Entity<DiscordGuildUser>().HasOne(dgu => dgu.DiscordUser).WithMany(u => u.Guilds)
						.HasForeignKey(dgu => dgu.DiscordUserId);

			modelBuilder.Entity<DiscordGuildUser>().HasOne(dgu => dgu.DiscordGuild).WithMany(u => u.Users)
						.HasForeignKey(dgu => dgu.DiscordGuildId);
		}
	}
}