using DominiShop.Model;
using DominiShop.Service;
using DominiShop.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace DominiShop.View;

public sealed partial class OrderPage : Page
{
    public OrderViewModel ViewModel { get; }

    public OrderPage()
    {
        ViewModel = App.Services.GetRequiredService<OrderViewModel>();
        this.InitializeComponent();
        this.Loaded += (s, e) => { _ = ViewModel.LoadDataAsync(); };
    }

    public string GetToggleCustomerText(bool isCreating) =>
        isCreating ? "← Select existing customer" : "+ Create new customer";

    private bool _isWizardActive = false;

    // ===== ADD ORDER — open step 1 =====
    private async void OnAddOrderClick(object sender, RoutedEventArgs e)
    {
        if (_isWizardActive) return;
        _isWizardActive = true;
        try
        {
            // Start initialization (resets state synchronously, then loads data async)
            var initTask = ViewModel.InitializeCreateFlowCommand.ExecuteAsync(null);
            
            // Open the wizard immediately without waiting for network/DB calls
            await ShowWizardAsync();
            
            // Ensure initialization is fully complete
            await initTask;
        }
        finally
        {
            _isWizardActive = false;
        }
    }

    private async Task ShowWizardAsync()
    {
        bool isFirstStep = true;
        while (ViewModel.CurrentStep >= 1 && ViewModel.CurrentStep <= 3)
        {
            // Only delay when transitioning between steps to allow UI state to settle.
            // On the first show, we want it to be immediate.
            if (!isFirstStep)
            {
                await Task.Delay(50);
            }
            isFirstStep = false;

            int stepToRender = ViewModel.CurrentStep;
            ContentDialog dialog;

            if (stepToRender == 1)
            {
                Step1Error.IsOpen = false;
                dialog = Step1Dialog;
            }
            else if (stepToRender == 2)
            {
                Step2Error.IsOpen = false;
                dialog = Step2Dialog;
            }
            else if (stepToRender == 3)
            {
                Step3Error.IsOpen = false;
                dialog = Step3Dialog;
            }
            else break;

            dialog.XamlRoot = this.XamlRoot;
            
            try
            {
                var result = await dialog.ShowAsync();

                // If user cancelled (clicked outside or closed via 'X'), 
                // and the ViewModel hasn't already moved to another step, stop the wizard.
                if (result == ContentDialogResult.None && ViewModel.CurrentStep == stepToRender)
                {
                    ViewModel.CurrentStep = 0;
                }
                // Step 3 'Primary' (Create/Update) also closes the wizard on success
                else if (result == ContentDialogResult.Primary && stepToRender == 3)
                {
                    ViewModel.CurrentStep = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ContentDialog transition error: {ex.Message}");
                // If we hit a collision, wait a bit longer and retry or exit
                await Task.Delay(50);
                if (ViewModel.CurrentStep == stepToRender) ViewModel.CurrentStep = 0;
            }
        }
    }

    // ===== STEP 1 handlers =====
    private void OnCustomerChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is Customer customer)
        {
            ViewModel.SelectedCustomer = customer;
            ViewModel.IsCreatingNewCustomer = false;
        }
    }

    private async void OnCreateCustomerClick(object sender, RoutedEventArgs e)
    {
        Step1Error.IsOpen = false;
        await ViewModel.CreateNewCustomerCommand.ExecuteAsync(null);
        if (ViewModel.CreateFlowError != null)
        {
            Step1Error.Message = ViewModel.CreateFlowError;
            Step1Error.IsOpen = true;
        }
    }

    private void OnStep1Next(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Step1Error.IsOpen = false;
        if (!ViewModel.GoToStep2())
        {
            args.Cancel = true;
            Step1Error.Message = ViewModel.CreateFlowError ?? "Please select a customer.";
            Step1Error.IsOpen = true;
        }
    }

    // ===== STEP 2 handlers =====
    private void OnIncrementClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Product product)
            ViewModel.IncrementQuantityCommand.Execute(product);
    }

    private void OnDecrementClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Product product)
            ViewModel.DecrementQuantityCommand.Execute(product);
    }

    private void OnStep2Next(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Step2Error.IsOpen = false;
        if (!ViewModel.GoToStep3())
        {
            args.Cancel = true;
            Step2Error.Message = ViewModel.CreateFlowError ?? "Please select at least 1 product.";
            Step2Error.IsOpen = true;
        }
    }

    private void OnStep2Back(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ViewModel.GoBackToStep1Command.Execute(null);
    }

    // ===== STEP 3 handlers =====
    private void OnTaxCheckChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.OnTaxSelectionChanged();
    }

    private async void OnSubmitOrder(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Step3Error.IsOpen = false;
        var deferral = args.GetDeferral();
        try
        {
            bool success = await ViewModel.SubmitOrderAsync();
            if (!success)
            {
                args.Cancel = true;
                Step3Error.Message = ViewModel.CreateFlowError ?? "Failed to create order.";
                Step3Error.IsOpen = true;
            }
        }
        finally { deferral.Complete(); }
    }

    private void OnStep3Back(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ViewModel.GoBackToStep2Command.Execute(null);
    }

    private async void OnCycleStatusClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Order order)
        {
            await ViewModel.CycleOrderStatusAsync(order);
        }
    }

    private async void OnDeleteOrderClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Order order)
        {
            ContentDialog confirmDialog = new ContentDialog
            {
                Title = "Xác nhận xóa",
                Content = $"Bạn có chắc chắn muốn xóa đơn hàng #{order.Id} không? " +
                          (order.Status == "Pending" ? "\n(Số lượng kho và điểm tích lũy sẽ được hoàn lại)" : ""),
                PrimaryButtonText = "Xóa",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteOrderCommand.ExecuteAsync(order);
            }
        }
    }

    private async void OnEditOrderClick(object sender, RoutedEventArgs e)
    {
        if (_isWizardActive || ViewModel.SelectedOrder == null) return;
        
        _isWizardActive = true;
        try
        {
            // Start edit initialization
            var editTask = ViewModel.StartEditOrderCommand.ExecuteAsync(ViewModel.SelectedOrder);
            
            // Show wizard immediately
            await ShowWizardAsync();
            
            await editTask;
        }
        finally
        {
            _isWizardActive = false;
        }
    }

    private async void OnExportOrderPdfClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedOrder == null) return;

        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"Order-{ViewModel.SelectedOrder.Id}.pdf"
        };
        picker.FileTypeChoices.Add("PDF file", new[] { ".pdf" });

        var window = App.MainWindow;
        if (window != null)
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

        StorageFile file = await picker.PickSaveFileAsync();
        if (file == null) return;

        var bytes = OrderPdfExportService.CreateOrderPdf(
            ViewModel.SelectedOrder,
            ViewModel.SelectedOrderCustomerName,
            ViewModel.SelectedOrderCustomerTier,
            ViewModel.SelectedOrderCustomerTierDiscount);

        await FileIO.WriteBytesAsync(file, bytes);
    }
}
