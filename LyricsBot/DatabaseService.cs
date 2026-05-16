using Npgsql;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace SpotifyLyricsBot
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        // Підключаємо налаштування, щоб взяти пароль з appsettings.json
        public DatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Метод 1: Шукає пісню в базі
        public async Task<(string Artist, string Title, string Album, string Lyrics, string Language)?> GetSongAsync(string artist, string title)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Пошук без врахування великих/малих літер
            string query = "SELECT \"Artist\", \"Title\", \"Album\", \"Lyrics\", \"Language\" FROM public.\"CachedSongs\" WHERE \"Artist\" ILIKE @artist AND \"Title\" ILIKE @title LIMIT 1";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("artist", $"%{artist}%");
            command.Parameters.AddWithValue("title", $"%{title}%");

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (
                    reader.GetString(0), // Artist
                    reader.GetString(1), // Title
                    reader.GetString(2), // Album
                    reader.GetString(3), // Lyrics
                    reader.GetString(4)  // Language
                );
            }
            return null; // Пісню не знайдено в базі
        }

        // Метод 2: Зберігає нову пісню в базу
        public async Task SaveSongAsync(string artist, string title, string album, string lyrics, string language)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "INSERT INTO public.\"CachedSongs\" (\"Artist\", \"Title\", \"Album\", \"Lyrics\", \"Language\") VALUES (@artist, @title, @album, @lyrics, @language)";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("artist", artist);
            command.Parameters.AddWithValue("title", title);
            // Якщо альбом порожній, записуємо "Невідомий альбом"
            command.Parameters.AddWithValue("album", string.IsNullOrEmpty(album) ? "Невідомий альбом" : album);
            command.Parameters.AddWithValue("lyrics", lyrics);
            command.Parameters.AddWithValue("language", language);

            await command.ExecuteNonQueryAsync();
        }
    }
}