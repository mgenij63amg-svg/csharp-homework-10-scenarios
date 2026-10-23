using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot;

public static class BotKeyboards
{
    public static ReplyKeyboardMarkup Main => new(new[]
    {
        new KeyboardButton[] { new("/addtask"), new("/showtasks") },
        new KeyboardButton[] { new("/showalltasks"), new("/report") },
        new KeyboardButton[] { new("/help") }
    }) { ResizeKeyboard = true };
}
