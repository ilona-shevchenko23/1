namespace SpotifyLyricsBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Додаємо контролери (це було згенеровано автоматично)
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSingleton<DatabaseService>();

            // === ОСЬ РЯДОК ЯКИЙ ТРЕБА ДОДАТИ ===
            builder.Services.AddHostedService<TelegramBotService>();
            // ===================================

            var app = builder.Build();

            // Налаштування Swagger (веб-інтерфейсу)
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            // Запускаємо веб-сервер
            app.Run();
        }
    }
}