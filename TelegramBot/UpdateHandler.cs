using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Models;
using TelegramBot.Services;

namespace TelegramBot;

public sealed class UpdateHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly IUserService _userService;
    private readonly IToDoService _toDoService;

    public UpdateHandler(ITelegramBotClient bot, IUserService userService, IToDoService toDoService)
    {
        _bot = bot;
        _userService = userService;
        _toDoService = toDoService;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (update.Message is not { From: { IsBot: false } sender } message ||
            message.Chat.Type != ChatType.Private) return;

        var text = message.Text?.Trim() ?? "";
        var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var command = parts.FirstOrDefault()?.ToLowerInvariant() ?? "";
        var argument = parts.Length > 1 ? parts[1].Trim() : "";

        if (command == "/start")
        {
            await _userService.RegisterUser(sender.Id, sender.Username, ct);
            await Reply(message.Chat.Id, "Добро пожаловать в ToDo бот! Команды доступны на клавиатуре. Справка: /help.", ct);
            return;
        }
        if (command == "/help")
        {
            await Reply(message.Chat.Id, "/start - регистрация\n/addtask <название> - добавить задачу\n/showtasks - активные задачи\n/showalltasks - все задачи\n/completetask <id> - завершить\n/removetask <id> - удалить\n/report - статистика", ct);
            return;
        }

        var user = await _userService.GetUser(sender.Id, ct);
        if (user is null)
        {
            await Reply(message.Chat.Id, "Сначала зарегистрируйтесь командой /start.", ct);
            return;
        }

        switch (command)
        {
            case "/addtask":
                if (string.IsNullOrWhiteSpace(argument) || argument.Length > ToDoService.MaxNameLength)
                    await Reply(message.Chat.Id, "Укажите название после /addtask (от 1 до 128 символов).", ct);
                else
                {
                    var item = await _toDoService.Add(user, argument, ct);
                    await Reply(message.Chat.Id, $"Задача {item.Name} успешно добавлена.", ct);
                }
                break;
            case "/showtask":
            case "/showtasks":
                await ShowTasks(user, message.Chat.Id, onlyActive: true, ct);
                break;
            case "/showalltasks":
                await ShowTasks(user, message.Chat.Id, onlyActive: false, ct);
                break;
            case "/completetask":
            case "/removetask":
                if (!Guid.TryParse(argument, out var id))
                {
                    await Reply(message.Chat.Id, $"Формат: {command} <id>. Идентификатор возьмите из /showalltasks.", ct);
                    break;
                }
                var changed = command == "/completetask"
                    ? await _toDoService.Complete(user.UserId, id, ct)
                    : await _toDoService.Delete(user.UserId, id, ct);
                await Reply(message.Chat.Id, changed
                    ? command == "/completetask" ? "Задача завершена." : "Задача удалена."
                    : "Задача не найдена.", ct);
                break;
            case "/report":
                var tasks = await _toDoService.GetAllByUserId(user.UserId, ct);
                await Reply(message.Chat.Id, $"Всего: {tasks.Count}\nАктивных: {tasks.Count(x => x.State == ToDoItemState.Active)}\nЗавершённых: {tasks.Count(x => x.State == ToDoItemState.Completed)}", ct);
                break;
            default:
                await Reply(message.Chat.Id, "Выберите команду на клавиатуре или откройте /help.", ct);
                break;
        }
    }

    private async Task ShowTasks(ToDoUser user, long chatId, bool onlyActive, CancellationToken ct)
    {
        var tasks = await _toDoService.GetAllByUserId(user.UserId, ct);
        var filtered = tasks.Where(x => !onlyActive || x.State == ToDoItemState.Active).ToArray();
        if (filtered.Length == 0)
        {
            await Reply(chatId, "Список задач пуст.", ct);
            return;
        }
        foreach (var item in filtered)
            await Reply(chatId, $"{item.Name}\nСтатус: {(item.State == ToDoItemState.Active ? "Активна" : "Завершена")}\nID: {item.Id}", ct);
    }

    private Task Reply(long chatId, string text, CancellationToken ct) =>
        _bot.SendMessage(chatId, text, replyMarkup: BotKeyboards.Main, cancellationToken: ct);
}
