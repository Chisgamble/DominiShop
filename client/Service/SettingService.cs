using Windows.Foundation.Collections;
using Windows.Storage;

namespace DominiShop.Service;

public class SettingService
{
    private readonly IPropertySet _settings = ApplicationData.Current.LocalSettings.Values;

    private const string KEY_LAST_PAGE = "LastVisitedPage";
    private const string KEY_PAGE_SIZE_PRODUCT = "PageSize_Product";
    private const string KEY_PAGE_SIZE_CATEGORY = "PageSize_Category";
    private const string KEY_PAGE_SIZE_CUSTOMER = "PageSize_Customer";
    private const string KEY_PAGE_SIZE_ORDER = "PageSize_Order";
    private const string KEY_PAGE_SIZE_TAX = "PageSize_Tax";
    private const string KEY_PAGE_SIZE_VOUCHER = "PageSize_Voucher";

    public string GetLastVisitedPage() => _settings[KEY_LAST_PAGE]?.ToString() ?? "Dashboard";
    public void SaveLastVisitedPage(string tag) => _settings[KEY_LAST_PAGE] = tag;

    public int GetProductPageSize() => (int)(_settings[KEY_PAGE_SIZE_PRODUCT] ?? 10);
    public void SaveProductPageSize(int value) => _settings[KEY_PAGE_SIZE_PRODUCT] = value;

    public int GetCategoryPageSize() => (int)(_settings[KEY_PAGE_SIZE_CATEGORY] ?? 10);
    public void SaveCategoryPageSize(int value) => _settings[KEY_PAGE_SIZE_CATEGORY] = value;

    public int GetCustomerPageSize() => (int)(_settings[KEY_PAGE_SIZE_CUSTOMER] ?? 10);
    public void SaveCustomerPageSize(int value) => _settings[KEY_PAGE_SIZE_CUSTOMER] = value;

    public int GetTaxPageSize() => (int)(_settings[KEY_PAGE_SIZE_TAX] ?? 10);
    public void SaveTaxPageSize(int value) => _settings[KEY_PAGE_SIZE_TAX] = value;

    public int GetOrderPageSize() => (int)(_settings[KEY_PAGE_SIZE_ORDER] ?? 10);
    public void SaveOrderPageSize(int value) => _settings[KEY_PAGE_SIZE_ORDER] = value;

    public int GetVoucherPageSize() => (int)(_settings[KEY_PAGE_SIZE_VOUCHER] ?? 10);
    public void SaveVoucherPageSize(int value) => _settings[KEY_PAGE_SIZE_VOUCHER] = value;
}