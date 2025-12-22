using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace FinanceFamilyApp.BLL.Services
{
    public class DatabaseConnectionService
    {
        private readonly IConfiguration _configuration;

        public DatabaseConnectionService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Проверяем версию SQL Server
                    var versionCommand = new SqlCommand("SELECT @@VERSION", connection);
                    var version = await versionCommand.ExecuteScalarAsync();

                    // Проверяем существование базы данных
                    var dbCommand = new SqlCommand(
                        "SELECT COUNT(*) FROM sys.databases WHERE name = 'FinanceFamilyDB'",
                        connection);
                    var dbCount = (int)await dbCommand.ExecuteScalarAsync();

                    await connection.CloseAsync();

                    return dbCount > 0;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка подключения к базе данных: {ex.Message}", ex);
            }
        }

        public async Task<string> GetServerInfoAsync()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var infoCommand = new SqlCommand(
                        "SELECT " +
                        "SERVERPROPERTY('ServerName') as ServerName, " +
                        "SERVERPROPERTY('ProductVersion') as Version, " +
                        "SERVERPROPERTY('ProductLevel') as ProductLevel, " +
                        "SERVERPROPERTY('Edition') as Edition",
                        connection);

                    using (var reader = await infoCommand.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return $"Сервер: {reader["ServerName"]}\n" +
                                   $"Версия: {reader["Version"]}\n" +
                                   $"Редакция: {reader["Edition"]}\n" +
                                   $"Уровень: {reader["ProductLevel"]}";
                        }
                    }

                    await connection.CloseAsync();
                }
            }
            catch (Exception)
            {
                // Игнорируем ошибки при получении информации
            }

            return "Информация о сервере недоступна";
        }

        public async Task<bool> BackupDatabaseAsync(string backupPath)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var backupCommand = new SqlCommand(
                        $"BACKUP DATABASE [FinanceFamilyDB] TO DISK = '{backupPath}' WITH FORMAT, MEDIANAME = 'FinanceFamilyBackup', NAME = 'Full Backup of FinanceFamilyDB'",
                        connection);

                    await backupCommand.ExecuteNonQueryAsync();
                    await connection.CloseAsync();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> RestoreDatabaseAsync(string backupPath)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Устанавливаем базу данных в однопользовательский режим
                    var singleUserCommand = new SqlCommand(
                        "ALTER DATABASE [FinanceFamilyDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE",
                        connection);
                    await singleUserCommand.ExecuteNonQueryAsync();

                    // Восстанавливаем базу данных
                    var restoreCommand = new SqlCommand(
                        $"RESTORE DATABASE [FinanceFamilyDB] FROM DISK = '{backupPath}' WITH REPLACE",
                        connection);
                    await restoreCommand.ExecuteNonQueryAsync();

                    // Возвращаем многопользовательский режим
                    var multiUserCommand = new SqlCommand(
                        "ALTER DATABASE [FinanceFamilyDB] SET MULTI_USER",
                        connection);
                    await multiUserCommand.ExecuteNonQueryAsync();

                    await connection.CloseAsync();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}