using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage;

namespace DominiShop.Service;

public class ConfigService
{
    private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
    private const string Descriptor = "LOCAL=user"; // Chỉ user hiện tại của Windows mới giải mã được

    // Lưu cấu hình Server
    public string SupabaseUrl { get => Get("SupUrl", ""); set => Set("SupUrl", value); }
    public string SupabaseKey { get => Get("SupKey", ""); set => Set("SupKey", value); }
    public string DbConnection { get => Get("DbConn", ""); set => Set("DbConn", value); }

    // Lưu credentials đã mã hóa
    public async Task SaveCredentials(string email, string password)
    {
        _localSettings.Values["SavedEmail"] = await Encrypt(email);
        _localSettings.Values["SavedPassword"] = await Encrypt(password);
        _localSettings.Values["HasAutoLogin"] = true;
    }

    public async Task<(string email, string password)> GetCredentials()
    {
        var email = await Decrypt((string)_localSettings.Values["SavedEmail"]);
        var password = await Decrypt((string)_localSettings.Values["SavedPassword"]);
        return (email, password);
    }

    public void ClearCredentials() => _localSettings.Values["HasAutoLogin"] = false;
    public bool HasAutoLogin => (bool)(_localSettings.Values["HasAutoLogin"] ?? false);

    private async Task<string> Encrypt(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var provider = new DataProtectionProvider(Descriptor);
        var buffer = CryptographicBuffer.ConvertStringToBinary(text, BinaryStringEncoding.Utf8);
        var protectedBuffer = await provider.ProtectAsync(buffer);
        return CryptographicBuffer.EncodeToBase64String(protectedBuffer);
    }

    private async Task<string> Decrypt(string base64)
    {
        if (string.IsNullOrEmpty(base64)) return "";
        var provider = new DataProtectionProvider();
        var buffer = CryptographicBuffer.DecodeFromBase64String(base64);
        var unprotectedBuffer = await provider.UnprotectAsync(buffer);
        return CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, unprotectedBuffer);
    }

    private string Get(string key, string def) => (string)_localSettings.Values[key] ?? def;
    private void Set(string key, string val) => _localSettings.Values[key] = val;

}