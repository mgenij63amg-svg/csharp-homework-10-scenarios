# Telegram ToDo бот

Консольное приложение C# для личного чата с Telegram-ботом. Пользователи и задачи хранятся в памяти до завершения процесса.

## Запуск

Требуется .NET SDK 8.0. Токен задаётся переменной окружения TELEGRAM_BOT_TOKEN.

```powershell
dotnet restore
$env:TELEGRAM_BOT_TOKEN = Read-Host 'Токен бота'
dotnet run --project TelegramBot
```

После запуска отправьте /start. Команда /help показывает список команд.
