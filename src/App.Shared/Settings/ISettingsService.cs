using System;
using System.Threading.Tasks;

namespace Lazarus.Shared.Settings;

public interface ISettingsService
{
    AppSettings Current { get; }

    Task<AppSettings> LoadAsync();
    Task SaveAsync(AppSettings? settings = null);

    event EventHandler<AppSettings>? SettingsChanged;
}
