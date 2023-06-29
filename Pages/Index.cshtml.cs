using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace TwitchStreamsVkNotifications.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IOptions<MyOptions> _options;

    [BindProperty]
    public string? Text { get; set; }

    public static string? LastState { get; private set; } = null;
    public string? RedirectUrl { get; set; } = null;

    public bool HasAuth => _options.Value.Auth != null;

    public IndexModel(ILogger<IndexModel> logger, IOptions<MyOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public void OnGet()
    {
        LastState = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, int.MaxValue).ToString();
        var param = new Dictionary<string, string?>()
            {
                { "client_id", _options.Value.VkClientId.ToString() },
                { "redirect_uri", "https://oauth.vk.com/blank.html" },
                { "scope", "wall,offline" },
                { "response_type", "token" },
                { "state", LastState.ToString() },
            };

        RedirectUrl = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString("https://oauth.vk.com/authorize", param);

        _logger.LogInformation("Получили гет.");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(Text))
        {
            _logger.LogWarning("Попытка отправить текст без текста.");

            return BadRequest();
        }

        var content = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(Text[(Text.IndexOf('#') + 1)..]);

        if (content.TryGetValue("error", out StringValues errorValues))
        {
            var description = content.GetValueOrDefault("error_description").FirstOrDefault();

            _logger.LogError("Получили ошибку в посте {error} ({description})", errorValues.First(), description);

            return BadRequest($"Ошибка... Вот сообщение: {errorValues.First()}\n{description}");
        }

        var accessToken = content["access_token"].First()!;
        var expires_in = content["expires_in"].First()!;
        var state = content["state"].First()!;

        if (IndexModel.LastState != state)
        {
            _logger.LogCritical("Стейты не совпадают.");
            return BadRequest("Случилось что-то невероятно плохое.");
        }

        _options.Value.Auth = new AuthInfo()
        {
            AccessToken = accessToken,
            Date = DateTime.UtcNow,
            ExpiresIn = TimeSpan.FromSeconds(int.Parse(expires_in))
        };

        await System.IO.File.WriteAllTextAsync("options.json", JsonSerializer.Serialize(_options.Value, new JsonSerializerOptions()
        {
            WriteIndented = true
        }));

        _logger.LogInformation("Получили пост.");

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        if (_options.Value.Auth == null)
        {
            _logger.LogWarning("Попытка удалить ауф, когда его уже нет.");
            return BadRequest();
        }

        _options.Value.Auth = null;

        await System.IO.File.WriteAllTextAsync("options.json", JsonSerializer.Serialize(_options.Value, new JsonSerializerOptions()
        {
            WriteIndented = true
        }));

        _logger.LogInformation("Получили делит.");

        return RedirectToPage();
    }
}
