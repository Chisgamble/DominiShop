using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominiShop.Model;
using DominiShop.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace DominiShop.ViewModel;

// Helper class for cart items
public partial class CartItem : ObservableObject
{
    [ObservableProperty] public partial Product Product { get; set; }
    [ObservableProperty] public partial int Quantity { get; set; }

    public decimal SubTotal => Product.Price * Quantity;
    public string FormattedSubTotal => SubTotal.ToString("N0") + " ₫";
    public string ProductName => Product.Name;
    public string FormattedPrice => Product.Price.ToString("N0") + " ₫";
    public int Stock => Product.Quantity;

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(SubTotal));
        OnPropertyChanged(nameof(FormattedSubTotal));
    }
}

// Helper class for tax selection
public partial class TaxSelection : ObservableObject
{
    [ObservableProperty] public partial Tax Tax { get; set; }
    [ObservableProperty] public partial bool IsSelected { get; set; }

    public string TaxName => Tax?.Name ?? "—";
    public string FormattedValue => Tax?.FormattedValue ?? "—";
    public bool IsAutoApply => Tax?.AutoApply == true;
    public string AutoLabel => IsAutoApply ? " [Auto]" : "";
}

public partial class OrderViewModel : BaseViewModel
{
    private readonly OrderService _orderService;
    private readonly CustomerService _customerService;
    private readonly ProductService _productService;
    private readonly CategoryService _categoryService;
    private readonly VoucherService _voucherService;
    private readonly TaxService _taxService;

    private List<Order> _masterOrders = new();

    public OrderViewModel(
        OrderService orderService,
        CustomerService customerService,
        ProductService productService,
        CategoryService categoryService,
        VoucherService voucherService,
        TaxService taxService)
    {
        _orderService = orderService;
        _customerService = customerService;
        _productService = productService;
        _categoryService = categoryService;
        _voucherService = voucherService;
        _taxService = taxService;
    }

    // ============ MASTER LIST STATE ============

    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;

    public ObservableCollection<Order> FilteredOrders { get; } = new();

    [ObservableProperty] public partial Order? SelectedOrder { get; set; }

    partial void OnSearchTextChanged(string value) => FilterOrders();
    partial void OnSelectedOrderChanged(Order? value) => LoadSelectedOrderCustomerName();

    // Selected order detail — customer name loaded via Phone lookup
    [ObservableProperty] public partial string SelectedOrderCustomerName { get; set; } = "—";

    private async void LoadSelectedOrderCustomerName()
    {
        if (SelectedOrder == null || string.IsNullOrEmpty(SelectedOrder.Phone))
        {
            SelectedOrderCustomerName = "Walk-in";
            return;
        }

        // Try to find customer name from already-loaded customer list
        var customer = _cachedCustomers.FirstOrDefault(c => c.Phone == SelectedOrder.Phone);
        if (customer != null)
        {
            SelectedOrderCustomerName = customer.Username;
        }
        else
        {
            // Lazy load customers if not yet loaded
            var result = await _customerService.GetCustomersAsync();
            if (result.Success && result.Data != null)
            {
                _cachedCustomers = result.Data;
                var found = result.Data.FirstOrDefault(c => c.Phone == SelectedOrder.Phone);
                SelectedOrderCustomerName = found?.Username ?? SelectedOrder.Phone;
            }
            else
            {
                SelectedOrderCustomerName = SelectedOrder.Phone;
            }
        }
    }

    private List<Customer> _cachedCustomers = new();

    // ============ LOAD & FILTER ============

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _orderService.GetOrdersAsync();
            if (result.Success && result.Data != null)
            {
                _masterOrders = result.Data;
                FilterOrders();
            }

            // Preload customers
            var custResult = await _customerService.GetCustomersAsync();
            if (custResult.Success && custResult.Data != null)
                _cachedCustomers = custResult.Data;
        }
        finally { IsLoading = false; }
    }

    private void FilterOrders()
    {
        var query = _masterOrders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            if (int.TryParse(SearchText, out int searchId))
                query = query.Where(o => o.Id == searchId);
            else
                query = query.Where(o => (o.Phone ?? "").Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        var results = query.ToList();
        FilteredOrders.Clear();
        foreach (var item in results) FilteredOrders.Add(item);
    }

    // ============ CREATE FLOW — STEP 1: CUSTOMER ============

    [ObservableProperty] public partial int CurrentStep { get; set; } = 1;

    // Customer selection
    public ObservableCollection<Customer> SuggestedCustomers { get; } = new();
    [ObservableProperty] public partial Customer? SelectedCustomer { get; set; }
    [ObservableProperty] public partial string CustomerSearchText { get; set; } = string.Empty;

    // Create new customer
    [ObservableProperty] public partial bool IsCreatingNewCustomer { get; set; }
    [ObservableProperty] public partial string NewCustomerName { get; set; } = string.Empty;
    [ObservableProperty] public partial string NewCustomerEmail { get; set; } = string.Empty;
    [ObservableProperty] public partial string NewCustomerPhone { get; set; } = string.Empty;

    [ObservableProperty] public partial string? CreateFlowError { get; set; }

    partial void OnCustomerSearchTextChanged(string value) => FilterCustomerSuggestions();

    private void FilterCustomerSuggestions()
    {
        SuggestedCustomers.Clear();
        if (string.IsNullOrWhiteSpace(CustomerSearchText))
        {
            foreach (var c in _cachedCustomers.Take(20)) SuggestedCustomers.Add(c);
            return;
        }

        var matches = _cachedCustomers
            .Where(c => c.Username.Contains(CustomerSearchText, StringComparison.OrdinalIgnoreCase)
                     || c.Phone.Contains(CustomerSearchText, StringComparison.OrdinalIgnoreCase))
            .Take(20);
        foreach (var c in matches) SuggestedCustomers.Add(c);
    }

    [RelayCommand]
    private void ToggleCreateCustomer()
    {
        IsCreatingNewCustomer = !IsCreatingNewCustomer;
        if (IsCreatingNewCustomer) SelectedCustomer = null;
    }

    [RelayCommand]
    private async Task CreateNewCustomerAsync()
    {
        CreateFlowError = null;
        var customer = new Customer
        {
            Username = NewCustomerName.Trim(),
            Email = NewCustomerEmail.Trim(),
            Phone = NewCustomerPhone.Trim()
        };

        var result = await _customerService.CreateCustomerAsync(customer);
        if (result.Success && result.Data != null)
        {
            _cachedCustomers.Add(result.Data);
            SelectedCustomer = result.Data;
            IsCreatingNewCustomer = false;
            NewCustomerName = string.Empty;
            NewCustomerEmail = string.Empty;
            NewCustomerPhone = string.Empty;
        }
        else
        {
            CreateFlowError = result.Error ?? "Failed to create customer.";
        }
    }

    // ============ CREATE FLOW — STEP 2: PRODUCTS ============

    private List<Product> _allProducts = new();
    public ObservableCollection<Product> FilteredAvailableProducts { get; } = new();
    public ObservableCollection<CartItem> CartItems { get; } = new();

    [ObservableProperty] public partial string ProductSearchText { get; set; } = string.Empty;
    [ObservableProperty] public partial Category? SelectedProductCategory { get; set; }
    public ObservableCollection<Category> ProductFilterCategories { get; } = new();

    partial void OnProductSearchTextChanged(string value) => FilterProducts();
    partial void OnSelectedProductCategoryChanged(Category? value) => FilterProducts();

    private void FilterProducts()
    {
        var query = _allProducts.AsQueryable().Where(p => p.IsDeleted != true && p.Quantity > 0);

        if (!string.IsNullOrWhiteSpace(ProductSearchText))
            query = query.Where(p => p.Name.Contains(ProductSearchText, StringComparison.OrdinalIgnoreCase));

        if (SelectedProductCategory != null && SelectedProductCategory.Id != 0)
            query = query.Where(p => p.CategoryId == SelectedProductCategory.Id);

        var results = query.OrderBy(p => p.Name).ToList();
        FilteredAvailableProducts.Clear();
        foreach (var p in results) FilteredAvailableProducts.Add(p);
    }

    [RelayCommand]
    private void IncrementQuantity(Product product)
    {
        var existing = CartItems.FirstOrDefault(c => c.Product.Id == product.Id);
        if (existing != null)
        {
            if (existing.Quantity < product.Quantity)
            {
                existing.Quantity++;
                product.CartQuantity = existing.Quantity;
                UpdateCartTotals();
            }
        }
        else
        {
            CartItems.Add(new CartItem { Product = product, Quantity = 1 });
            product.CartQuantity = 1;
            UpdateCartTotals();
        }
    }

    [RelayCommand]
    private void DecrementQuantity(Product product)
    {
        var existing = CartItems.FirstOrDefault(c => c.Product.Id == product.Id);
        if (existing != null)
        {
            existing.Quantity--;
            product.CartQuantity = existing.Quantity;
            if (existing.Quantity <= 0)
                CartItems.Remove(existing);
            UpdateCartTotals();
        }
    }

    public int GetCartQuantity(int productId)
    {
        return CartItems.FirstOrDefault(c => c.Product.Id == productId)?.Quantity ?? 0;
    }

    // ============ CREATE FLOW — STEP 3: VOUCHER, TAX, SHIPPING ============

    public ObservableCollection<Voucher> AvailableVouchers { get; } = new();
    [ObservableProperty] public partial Voucher? SelectedVoucher { get; set; }

    public ObservableCollection<TaxSelection> AvailableTaxes { get; } = new();

    [ObservableProperty] public partial bool IsOnlineOrder { get; set; }
    [ObservableProperty] public partial string ShippingAddress { get; set; } = string.Empty;
    [ObservableProperty] public partial double ShippingFeeInput { get; set; }

    partial void OnSelectedVoucherChanged(Voucher? value) => UpdateCartTotals();
    partial void OnIsOnlineOrderChanged(bool value) => UpdateCartTotals();
    partial void OnShippingFeeInputChanged(double value) => UpdateCartTotals();

    // Computed totals
    [ObservableProperty] public partial decimal SubTotal { get; set; }
    [ObservableProperty] public partial decimal DiscountAmount { get; set; }
    [ObservableProperty] public partial decimal TaxAmount { get; set; }
    [ObservableProperty] public partial decimal GrandTotal { get; set; }

    [ObservableProperty] public partial string FormattedSubTotal { get; set; } = "0 ₫";
    [ObservableProperty] public partial string FormattedDiscount { get; set; } = "0 ₫";
    [ObservableProperty] public partial string FormattedTax { get; set; } = "0 ₫";
    [ObservableProperty] public partial string FormattedShipping { get; set; } = "0 ₫";
    [ObservableProperty] public partial string FormattedGrandTotal { get; set; } = "0 ₫";

    public void UpdateCartTotals()
    {
        // Subtotal
        SubTotal = CartItems.Sum(c => c.SubTotal);
        FormattedSubTotal = SubTotal.ToString("N0") + " ₫";

        // Discount
        DiscountAmount = 0;
        if (SelectedVoucher != null)
        {
            switch (SelectedVoucher.Type)
            {
                case "percent":
                    DiscountAmount = SubTotal * (SelectedVoucher.Percent ?? 0) / 100m;
                    break;
                case "fixed":
                    DiscountAmount = SelectedVoucher.Percent ?? 0;
                    break;
                case "free_shipping":
                    // handled in shipping
                    break;
            }
        }
        FormattedDiscount = DiscountAmount > 0 ? $"-{DiscountAmount:N0} ₫" : "0 ₫";

        // Tax
        var afterDiscount = SubTotal - DiscountAmount;
        TaxAmount = 0;
        foreach (var ts in AvailableTaxes.Where(t => t.IsSelected))
        {
            if (ts.Tax.Type == "Percentage")
                TaxAmount += afterDiscount * (ts.Tax.Value ?? 0) / 100m;
            else
                TaxAmount += ts.Tax.Value ?? 0;
        }
        FormattedTax = TaxAmount > 0 ? $"+{TaxAmount:N0} ₫" : "0 ₫";

        // Shipping
        decimal shippingFee = 0;
        if (IsOnlineOrder)
        {
            if (SelectedVoucher?.Type == "free_shipping")
                shippingFee = 0;
            else
                shippingFee = (decimal)ShippingFeeInput;
        }
        FormattedShipping = IsOnlineOrder ? (shippingFee > 0 ? $"+{shippingFee:N0} ₫" : "Miễn phí") : "—";

        // Grand total
        GrandTotal = afterDiscount + TaxAmount + shippingFee;
        if (GrandTotal < 0) GrandTotal = 0;
        FormattedGrandTotal = GrandTotal.ToString("N0") + " ₫";

        OnPropertyChanged(nameof(CartItems));
    }

    public void OnTaxSelectionChanged()
    {
        UpdateCartTotals();
    }

    // ============ WIZARD NAVIGATION ============

    [RelayCommand]
    private async Task InitializeCreateFlowAsync()
    {
        CreateFlowError = null;
        CurrentStep = 1;
        SelectedCustomer = null;
        IsCreatingNewCustomer = false;
        CustomerSearchText = string.Empty;
        NewCustomerName = string.Empty;
        NewCustomerEmail = string.Empty;
        NewCustomerPhone = string.Empty;

        foreach (var item in _allProducts) item.CartQuantity = 0;
        CartItems.Clear();
        ProductSearchText = string.Empty;
        SelectedProductCategory = null;

        SelectedVoucher = null;
        IsOnlineOrder = false;
        ShippingAddress = string.Empty;
        ShippingFeeInput = 0;

        SubTotal = 0;
        DiscountAmount = 0;
        TaxAmount = 0;
        GrandTotal = 0;
        FormattedSubTotal = "0 ₫";
        FormattedDiscount = "0 ₫";
        FormattedTax = "0 ₫";
        FormattedShipping = "—";
        FormattedGrandTotal = "0 ₫";

        IsLoading = true;
        try
        {
            // Load customers
            if (_cachedCustomers.Count == 0)
            {
                var custResult = await _customerService.GetCustomersAsync();
                if (custResult.Success && custResult.Data != null)
                    _cachedCustomers = custResult.Data;
            }
            FilterCustomerSuggestions();

            // Load products
            var prodResult = await _productService.GetProductsAsync();
            if (prodResult.Success && prodResult.Data != null)
                _allProducts = prodResult.Data;

            // Load categories
            var catResult = await _categoryService.GetCategoriesAsync();
            ProductFilterCategories.Clear();
            ProductFilterCategories.Add(new Category { Id = 0, Name = "Tất cả" });
            if (catResult.Success && catResult.Data != null)
            {
                foreach (var c in catResult.Data) ProductFilterCategories.Add(c);
            }
            SelectedProductCategory = ProductFilterCategories.First();
            FilterProducts();

            // Load vouchers
            var voucherResult = await _voucherService.GetVouchersAsync();
            AvailableVouchers.Clear();
            if (voucherResult.Success && voucherResult.Data != null)
            {
                foreach (var v in voucherResult.Data.Where(v => v.IsActive && !v.IsExpired))
                    AvailableVouchers.Add(v);
            }

            // Load taxes (all, not paged)
            var taxResult = await _taxService.GetTaxesAsync(
                new Repository.PagingRequest { PageSize = 100, PageNumber = 1 }, null, null);
            AvailableTaxes.Clear();
            if (taxResult.Success && taxResult.Data?.Items != null)
            {
                foreach (var t in taxResult.Data.Items)
                {
                    AvailableTaxes.Add(new TaxSelection
                    {
                        Tax = t,
                        IsSelected = t.AutoApply == true // Auto-apply taxes
                    });
                }
            }
        }
        finally { IsLoading = false; }
    }

    public bool GoToStep2()
    {
        CreateFlowError = null;
        if (SelectedCustomer == null)
        {
            CreateFlowError = "Please select a customer before continuing.";
            return false;
        }
        CurrentStep = 2;
        return true;
    }

    [RelayCommand]
    private void GoBackToStep1()
    {
        CreateFlowError = null;
        CurrentStep = 1;
    }

    public bool GoToStep3()
    {
        CreateFlowError = null;
        if (CartItems.Count == 0)
        {
            CreateFlowError = "Please add at least 1 product.";
            return false;
        }
        CurrentStep = 3;
        UpdateCartTotals();
        return true;
    }

    [RelayCommand]
    private void GoBackToStep2()
    {
        CreateFlowError = null;
        CurrentStep = 2;
    }

    // ============ SUBMIT ORDER ============

    public async Task<bool> SubmitOrderAsync()
    {
        CreateFlowError = null;

        if (SelectedCustomer == null)
        {
            CreateFlowError = "No customer selected.";
            return false;
        }
        if (CartItems.Count == 0)
        {
            CreateFlowError = "Cart is empty.";
            return false;
        }
        if (IsOnlineOrder && string.IsNullOrWhiteSpace(ShippingAddress))
        {
            CreateFlowError = "Please enter a delivery address.";
            return false;
        }

        UpdateCartTotals();

        var order = new Order
        {
            Phone = SelectedCustomer.Phone,
            TotalPrice = GrandTotal,
            ShippingFee = IsOnlineOrder ? (decimal)ShippingFeeInput : 0,
            Address = IsOnlineOrder ? ShippingAddress.Trim() : null,
            OrderDetails = CartItems.Select(c => new OrderDetail
            {
                ProductId = c.Product.Id,
                Quantity = c.Quantity,
                Price = c.Product.Price,
                CreatedAt = DateTime.UtcNow
            }).ToList(),
            OrderVouchers = SelectedVoucher != null
                ? new List<OrderVoucher> { new OrderVoucher { VoucherId = SelectedVoucher.Id, CreatedAt = DateTime.UtcNow } }
                : new List<OrderVoucher>(),
            OrderTaxes = AvailableTaxes
                .Where(t => t.IsSelected)
                .Select(t => new OrderTax { TaxId = t.Tax.Id, CreatedAt = DateTime.UtcNow })
                .ToList()
        };

        IsLoading = true;
        try
        {
            var result = await _orderService.CreateOrderAsync(order);
            if (result.Success)
            {
                await LoadDataAsync();
                return true;
            }
            else
            {
                CreateFlowError = result.Error ?? "Failed to create order.";
                return false;
            }
        }
        finally { IsLoading = false; }
    }
}
