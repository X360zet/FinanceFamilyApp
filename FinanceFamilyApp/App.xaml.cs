using FinanceFamilyApp.BLL.Services;
using FinanceFamilyApp.DAL;
using FinanceFamilyApp.DAL.Data;
using FinanceFamilyApp.ViewModels;
using FinanceFamilyApp.Views;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace FinanceFamilyApp
{
    public partial class App : Application
    {
        public static ServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Проверяем подключение к SQL Server перед запуском приложения
                if (!TestDatabaseConnection())
                {
                    MessageBox.Show("Не удалось подключиться к SQL Server. Убедитесь, что:\n" +
                        "1. SQL Server установлен и запущен\n" +
                        "2. Строка подключения в appsettings.json корректна\n" +
                        "3. База данных существует или может быть создана",
                        "Ошибка подключения к базе данных",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown();
                    return;
                }

                // Конфигурация сервисов
                var services = new ServiceCollection();
                ConfigureServices(services);

                ServiceProvider = services.BuildServiceProvider();
                services.AddTransient<BudgetManagementViewModel>();
                services.AddTransient<BudgetManagementWindow>();

                // Инициализация базы данных
                InitializeDatabase();

                // Показываем окно входа
                var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска приложения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Обработка аргументов командной строки
            foreach (var arg in e.Args)
            {
                if (arg == "/resetdb")
                {
                    var result = MessageBox.Show("Вы уверены, что хотите сбросить базу данных? Все данные будут удалены.",
                        "Сброс базы данных",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        ResetDatabase();
                    }
                }
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // Очистка ресурсов при выходе
            ServiceProvider?.Dispose();

            // Закрываем все подключения к базе данных
            try
            {
                using (var scope = ServiceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    context.Database.CloseConnection();
                }
            }
            catch
            {
                // Игнорируем ошибки при закрытии
            }
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Произошла непредвиденная ошибка: {e.Exception.Message}\n\n" +
                "Приложение будет закрыто.",
                "Критическая ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
            Shutdown(1);
        }

        private void ResetDatabase()
        {
            try
            {
                var connectionString = GetConnectionString();

                using (var connection = new SqlConnection(connectionString.Replace("Database=FinanceFamilyDB", "Database=master")))
                {
                    connection.Open();

                    var dropDbCommand = new SqlCommand(
                        @"IF DB_ID('FinanceFamilyDB') IS NOT NULL
                BEGIN
                    ALTER DATABASE [FinanceFamilyDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [FinanceFamilyDB];
                END",
                        connection);

                    dropDbCommand.ExecuteNonQuery();

                    var createDbCommand = new SqlCommand("CREATE DATABASE [FinanceFamilyDB]", connection);
                    createDbCommand.ExecuteNonQuery();

                    connection.Close();

                    MessageBox.Show("База данных успешно сброшена.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сбросе базы данных: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Регистрация контекста базы данных с использованием SQL Server
            services.AddDbContext<AppDbContext>(options =>
            {
                var connectionString = GetConnectionString();
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });
                options.EnableSensitiveDataLogging(); // Для отладки
                options.EnableDetailedErrors(); // Для отладки
            });

            // Регистрация UnitOfWork и сервисов
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<FinanceService>();
            services.AddScoped<ReportService>();
            services.AddScoped<AuthService>();

            // Регистрация ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<IncomeFormViewModel>();
            services.AddTransient<ExpenseFormViewModel>();
            services.AddTransient<BudgetFormViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<RegisterViewModel>();
            services.AddTransient<FamilyMemberViewModel>();
            services.AddTransient<DateRangeDialogViewModel>();
            services.AddTransient<EditTransactionViewModel>();

            // Регистрация окон
            services.AddSingleton<MainWindow>();
            services.AddSingleton<LoginWindow>();
            services.AddTransient<RegisterWindow>();
            services.AddTransient<IncomeWindow>();
            services.AddTransient<ExpenseWindow>();
            services.AddTransient<BudgetWindow>();
            services.AddTransient<FamilyMemberWindow>();
            services.AddTransient<DateRangeDialog>();
            services.AddTransient<EditTransactionWindow>();
        }

        private string GetConnectionString()
        {
            // Проверяем наличие строки подключения в appsettings.json
            try
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");

                if (string.IsNullOrEmpty(connectionString))
                {
                    // Если строка подключения не найдена, используем значение по умолчанию
                    return @"Server=(localdb)\MSSQLLocalDB;Database=FinanceFamilyDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true";
                }

                return connectionString;
            }
            catch
            {
                // В случае ошибки используем значение по умолчанию
                return @"Server=(localdb)\MSSQLLocalDB;Database=FinanceFamilyDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true";
            }
        }

        private bool TestDatabaseConnection()
        {
            try
            {
                var connectionString = GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Проверяем, существует ли база данных
                    var checkDbCommand = new SqlCommand(
                        "IF DB_ID('FinanceFamilyDB') IS NOT NULL SELECT 1 ELSE SELECT 0",
                        connection);

                    var dbExists = (int)checkDbCommand.ExecuteScalar() == 1;

                    connection.Close();

                    if (!dbExists)
                    {
                        var result = MessageBox.Show("База данных FinanceFamilyDB не найдена. Создать новую базу данных?",
                            "База данных не найдена",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        return result == MessageBoxResult.Yes;
                    }

                    return true;
                }
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show($"Ошибка SQL Server: {sqlEx.Message}\n\n" +
                    $"Проверьте:\n" +
                    $"1. Запущен ли SQL Server\n" +
                    $"2. Корректность строки подключения\n" +
                    $"3. Разрешения на доступ к базе данных",
                    "Ошибка подключения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}",
                    "Ошибка подключения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        private void InitializeDatabase()
        {
            try
            {
                using var scope = ServiceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Проверяем и создаем базу данных, если её нет
                if (!context.Database.CanConnect())
                {
                    MessageBox.Show("Создаю новую базу данных...", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    try
                    {
                        // Пытаемся удалить старую базу данных, если она существует
                        context.Database.EnsureDeleted();
                    }
                    catch
                    {
                        // Игнорируем ошибки при удалении
                    }

                    // Создаем новую базу данных и применяем миграции
                    context.Database.EnsureCreated();

                    MessageBox.Show("База данных успешно создана!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Проверяем наличие необходимых таблиц
                    context.Database.EnsureCreated();

                    // Проверяем, есть ли данные в базе
                    var hasUsers = context.Users.Any();
                    if (!hasUsers)
                    {
                        // База данных пустая, показываем сообщение
                        MessageBox.Show("База данных подключена. Создайте первого пользователя.", "Информация",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // База данных уже содержит данные
                        MessageBox.Show("База данных успешно подключена!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show($"Ошибка SQL Server при инициализации базы данных: {sqlEx.Message}\n\n" +
                    $"Код ошибки: {sqlEx.Number}\n" +
                    $"Состояние: {sqlEx.State}\n" +
                    $"Процедура: {sqlEx.Procedure}",
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации базы данных: {ex.Message}\n\n" +
                    "Приложение продолжит работу в демонстрационном режиме.",
                    "Предупреждение",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}
