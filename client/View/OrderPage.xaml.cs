using DominiShop.Model;
using DominiShop.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

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

    // ===== ADD ORDER — open step 1 =====
    private async void OnAddOrderClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeCreateFlowCommand.ExecuteAsync(null);
        await ShowWizardAsync();
    }

    private async Task ShowWizardAsync()
    {
        while (ViewModel.CurrentStep >= 1 && ViewModel.CurrentStep <= 3)
        {
            if (ViewModel.CurrentStep == 1)
            {
                Step1Error.IsOpen = false;
                Step1Dialog.XamlRoot = this.XamlRoot;
                var result = await Step1Dialog.ShowAsync();
                if (result == ContentDialogResult.None) ViewModel.CurrentStep = 0;
            }
            else if (ViewModel.CurrentStep == 2)
            {
                Step2Error.IsOpen = false;
                Step2Dialog.XamlRoot = this.XamlRoot;
                var result = await Step2Dialog.ShowAsync();
                if (result == ContentDialogResult.None) ViewModel.CurrentStep = 0;
            }
            else if (ViewModel.CurrentStep == 3)
            {
                Step3Error.IsOpen = false;
                Step3Dialog.XamlRoot = this.XamlRoot;
                var result = await Step3Dialog.ShowAsync();
                if (result == ContentDialogResult.None || result == ContentDialogResult.Primary) 
                    ViewModel.CurrentStep = 0;
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
}
