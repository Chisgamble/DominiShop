using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominiShop.Service;

namespace DominiShop.ViewModel;

public partial class ConfigViewModel : BaseViewModel
{
    private readonly ConfigService _config;

    [ObservableProperty] public partial string Url { get; set; }
    [ObservableProperty] public partial string Key { get; set; }
    [ObservableProperty] public partial string DbConn { get; set; }

    [ObservableProperty] public partial bool ShowRestartMessage { get; set; }

    public ConfigViewModel(ConfigService config)
    {
        _config = config;
        Url = _config.SupabaseUrl;
        Key = _config.SupabaseKey;
        DbConn = _config.DbConnection;
    }

    [RelayCommand]
    private void Save()
    {
        _config.SupabaseUrl = Url;
        _config.SupabaseKey = Key;
        _config.DbConnection = DbConn;

        ShowRestartMessage = true;
    }
}