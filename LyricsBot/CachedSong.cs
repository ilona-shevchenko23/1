using System;
using Npgsql;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace LyricsBot
{
    public class CachedSong
    {
        public int Id { get; set; }
        public string Artist { get; set; }
        public string Title { get; set; }
        public string Album { get; set; }
        public string Lyrics { get; set; }
        public string Language { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // 2. Сервіс бази даних
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Метод 1: Шукає пісню в базі та повертає об'єкт CachedSong
        public async Task<CachedSong> GetSongAsync(string artist, string title)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT \"Artist\", \"Title\", \"Album\", \"Lyrics\", \"Language\" FROM public.\"CachedSongs\" WHERE \"Artist\" ILIKE @artist AND \"Title\" ILIKE @title LIMIT 1";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("artist", $"%{artist}%");
            command.Parameters.AddWithValue("title", $"%{title}%");

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                // Формуємо красивий об'єкт замість кортежу
                return new CachedSong
                {
                    Artist = reader.GetString(0),
                    Title = reader.GetString(1),
                    Album = reader.GetString(2),
                    Lyrics = reader.GetString(3),
                    Language = reader.GetString(4)
                };
            }
            return null; // Пісню не знайдено
        }

        // Метод 2: Зберігає нову пісню в базу (приймає об'єкт CachedSong)
        public async Task SaveSongAsync(CachedSong song)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "INSERT INTO public.\"CachedSongs\" (\"Artist\", \"Title\", \"Album\", \"Lyrics\", \"Language\") VALUES (@artist, @title, @album, @lyrics, @language)";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("artist", song.Artist);
            command.Parameters.AddWithValue("title", song.Title);
            command.Parameters.AddWithValue("album", string.IsNullOrEmpty(song.Album) ? "Невідомий альбом" : song.Album);
            command.Parameters.AddWithValue("lyrics", song.Lyrics);
            command.Parameters.AddWithValue("language", song.Language);

            await command.ExecuteNonQueryAsync();
        }
    }
}