using Microsoft.Extensions.DependencyInjection;
using StageManagementSystem.Data;
using StageManagementSystem.Services;
using StageManagementSystem.ViewModels;
using System.Windows;

namespace StageManagementSystem
{
    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;
        public IServiceProvider Services { get; }

        public App()
        {
            try
            {
                Services = ConfigureServices();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Configuration Error: {ex.Message}\n\n{ex.StackTrace}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Services
            services.AddDbContext<AppDbContext>();
            services.AddSingleton<DatabaseService>();
            services.AddTransient<StudentService>();

            // ViewModels
             services.AddTransient<MainViewModel>();

            // Windows
            services.AddTransient<MainWindow>();

            return services.BuildServiceProvider();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var dbService = Services.GetRequiredService<DatabaseService>();
                dbService.Initialize();

                var mainWindow = Services.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("startup_error.txt", $"Startup Error: {ex.Message}\n\n{ex.StackTrace}");
                Shutdown(-1);
            }
        }
    }
}

