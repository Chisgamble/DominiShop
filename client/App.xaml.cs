using DominiShop.DataAccess;
using DominiShop.Model;
using DominiShop.Repository;
using DominiShop.Service;
using DominiShop.View;
using DominiShop.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Serilog;
using Supabase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DominiShop
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private static Window? _window;
        public static IServiceProvider Services { get; private set; } = null!;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug()
            .CreateLogger();

            Log.Information("Ứng dụng WinUI 3 đã bắt đầu khởi chạy.");
            Services = BuildServices();
        }

        private static ServiceProvider BuildServices()
        {
            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
                
            var url = configuration["Supabase:Url"];
            var key = configuration["Supabase:Key"];
            var options = new SupabaseOptions { AutoConnectRealtime = true };

            services.AddDbContext<PostgresContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null)
                ),
                ServiceLifetime.Transient);

            services.AddSingleton(provider => new Supabase.Client(url!, key, options));

            // navigation
            services.AddSingleton<INavigationService, NavigationService>();

            // auth and owner
            services.AddTransient<IRepo<Owner, int>, OwnerRepository>();
            services.AddSingleton<AuthService>();
            services.AddTransient<AuthViewModel>();
            
            // voucher
            services.AddTransient<IRepo<Voucher, int>, VoucherRepository>();
            services.AddTransient<VoucherRepository>();
            services.AddTransient<VoucherService>();
            services.AddTransient<VoucherViewModel>();

            // main
            services.AddSingleton<MainViewModel>();

            services.AddTransient<IRepo<Category, int>, CategoryRepository>();
            services.AddTransient<CategoryRepository>(); 
            services.AddSingleton<CategoryService>();
            services.AddTransient<CategoryViewModel>();

            services.AddTransient<ProductRepository>();
            services.AddTransient<ProductService>();
            services.AddTransient<ProductViewModel>();

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }

        public static void NavigateToMain()
        {
            if (_window is MainWindow mw)
                mw.Navigate(typeof(MainPage));
            else if (_window?.Content is Frame rootFrame)
            {
                rootFrame.Navigate(typeof(MainPage));
            }
        }
    }
}
