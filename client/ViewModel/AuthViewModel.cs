using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominiShop.Service;
using DominiShop.View;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace DominiShop.ViewModel;

public partial class AuthViewModel : BaseViewModel
{
    private readonly AuthService _auth;
    private readonly ConfigService _config;
    private readonly INavigationService _nav;

    [ObservableProperty] public partial string AppVersion { get; set; }

    //State
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial bool IsLoginMode { get; set; } = true;
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial string? SuccessMessage { get; set; }

    // Login 
    [ObservableProperty] public partial string LoginEmail { get; set; } = string.Empty;
    [ObservableProperty] public partial string LoginPassword { get; set; } = string.Empty;

    //Signup 
    [ObservableProperty] public partial string SignUpUsername { get; set; } = string.Empty;
    [ObservableProperty] public partial string SignUpEmail { get; set; } = string.Empty;
    [ObservableProperty] public partial string SignUpPassword { get; set; } = string.Empty;
    [ObservableProperty] public partial string SignUpConfirmPassword { get; set; } = string.Empty;

    public AuthViewModel(AuthService auth, ConfigService config, INavigationService nav)
    {
        _auth = auth;
        _config = config;
        _nav = nav;

        var v = Package.Current.Id.Version;
        AppVersion = $"v{v.Major}.{v.Minor}.{v.Build}";

        _ = CheckAutoLogin();
    }

    private async Task CheckAutoLogin()
    {
        if (_config.HasAutoLogin)
        {
            var (email, password) = await _config.GetCredentials();
            LoginEmail = email;
            LoginPassword = password;
            await LoginAsync();
        }
    }

    public bool IsSignUpMode => !IsLoginMode;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);


    partial void OnIsLoginModeChanged(bool value)
    {
        OnPropertyChanged(nameof(IsSignUpMode));
        ClearMessages();
    }

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));
    partial void OnSuccessMessageChanged(string? value) => OnPropertyChanged(nameof(HasSuccess));

    [RelayCommand]
    private void SwitchMode()
    {
        IsLoginMode = !IsLoginMode;
        LoginEmail = LoginPassword = string.Empty;
        SignUpUsername = SignUpEmail = SignUpPassword = SignUpConfirmPassword = string.Empty;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(LoginEmail) || string.IsNullOrWhiteSpace(LoginPassword))
        {
            SetError("Email and password are required.");
            return;
        }

        await RunAsync(async () =>
        {
            var (ok, err) = await _auth.LoginAsync(LoginEmail, LoginPassword);
            if (err != null)
            {
                HandleAuthError(err);
            }
            if (!ok)
            {
                return;
            }

            await _config.SaveCredentials(LoginEmail, LoginPassword);
            App.NavigateToMain();
        });
    }

    [RelayCommand]
    private void GoToConfig() => _nav.NavigateTo(typeof(ConfigPage));

    [RelayCommand]
    private async Task SignUpAsync()
    {
        if (string.IsNullOrWhiteSpace(SignUpUsername) ||
            string.IsNullOrWhiteSpace(SignUpEmail) ||
            string.IsNullOrWhiteSpace(SignUpPassword))
        {
            SetError("Please fill in all fields.");
            return;
        }

        if (SignUpPassword != SignUpConfirmPassword)
        {
            SetError("Passwords do not match.");
            return;
        }

        await RunAsync(async () =>
        {
            var (ok, err) = await _auth.SignUpAsync(SignUpUsername, SignUpEmail, SignUpPassword);
            if (err != null)
            {
                HandleAuthError(err);
            }
            if (!ok)
            {
                return;
            }
            
            IsLoginMode = true;
            LoginEmail = SignUpEmail;
            SuccessMessage = "Account created! You can now sign in.";
        });
    }

    // Helpers
    private async Task RunAsync(Func<Task> action)
    {
        IsLoading = true;
        ClearMessages();
        try { await action(); }
        finally { IsLoading = false; }
    }

    private void SetError(string? msg)
    {
        SuccessMessage = null;
        ErrorMessage = msg;
    }

    private void ClearMessages()
    {
        ErrorMessage = null;
        SuccessMessage = null;
    }

    private void HandleAuthError(object err)
    {
        var errorMessage = "Unidentified error!";
        var errorStr = err?.ToString() ?? "";

        //lỗi mạng (Thường là HttpRequestException)
        if (errorStr.Contains("HttpRequestException") || errorStr.Contains("network") || errorStr.Contains("Failed to fetch"))
        {
            errorMessage = "Couldn't connect to the server. Please check your internet connection.";
        }
        //lỗi cấu hình (Sai URL, Key hoặc Database connection)
        else if (errorStr.Contains("UriFormatException") || errorStr.Contains("404") || errorStr.Contains("433") || errorStr.Contains("Invalid API key"))
        {
            errorMessage = "Incorrect server configuration. Please check the settings in Server Configuration.";
        }
        //lỗi sai tài khoản/mật khẩu
        else if (errorStr.Contains("Invalid login credentials") || errorStr.Contains("400") || errorStr.Contains("invalid_credentials"))
        {
            errorMessage = "Email or password is incorrect.";
        }
        //lỗi Email đã tồn tại (Dành cho SignUp)
        else if (errorStr.Contains("User already registered") || errorStr.Contains("already exists"))
        {
            errorMessage = "This Email has been attached to another account.";
        }
        //mật khẩu quá yếu
        else if (errorStr.Contains("Password should be"))
        {
            errorMessage = "The password should be stronger.";
        }
        else
        {
            // Nếu không rơi vào các trường hợp trên, hiện thông báo gốc nhưng rút gọn
            errorMessage = $"Lỗi: {errorStr.Split('\n')[0]}";
        }

        SetError(errorMessage);
    }
}