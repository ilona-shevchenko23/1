namespace SpotifyLyricsBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Додаємо контролери
            builder.Services.AddControllers(); // Додаємо підтримку контролерів для обробки HTTP-запитів (щоб Render бачив, що це веб-додаток)
            builder.Services.AddEndpointsApiExplorer(); // Створює один примірник бази даних і використовує його для всіх запитів
            builder.Services.AddSwaggerGen(); // Запускає Telegram-бота як фонову службу

            builder.Services.AddSingleton<DatabaseService>();

            
            builder.Services.AddHostedService<TelegramBotService>();


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