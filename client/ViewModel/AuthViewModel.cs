using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominiShop.Service;
using System;
using System.Threading.Tasks;

namespace DominiShop.ViewModel;

public partial class AuthViewModel(AuthService authService) : BaseViewModel
{
    private readonly AuthService _auth = authService;

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
            if (!ok)
            {
                SetError(err);
                return;
            }
            App.NavigateToMain();
        });
    }

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
            if (!ok) { SetError(err); return; }

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
}