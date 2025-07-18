using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Extensions.Hosting;
using ARM.Models;


namespace ARM.Services;

public class PostgresDBService : IDBService
{
    private static string? connectionString;
    private static string? _connectionString;
    private NpgsqlDataSource dataSource;
    private string? _currentUser;



    private void InitializeDataSource(string login, string decryptedPass)
    {
        string portKey = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "WindowsDBPort"
            : "LinuxDBPort";
        string hostKey = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "WindowsDBHost"
            : "LinuxDBHost";
        string dbKey = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "WindowsDBase"
            : "LinuxDBase";

        //if (!int.TryParse(ConfigurationManager.AppSettings[portKey], out int port))
        //    throw new ArgumentException("Invalid port configuration");

        //string host = ConfigurationManager.AppSettings[hostKey];
        //string dBase = ConfigurationManager.AppSettings[dbKey];

    //    if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(dBase))
    //        throw new ArgumentException("Missing database configuration");

    //    var builder = new NpgsqlConnectionStringBuilder
    //    {
    //        Host = host,
    //        Database = dBase,
    //        Port = port,
    //        Username = login, // Используем параметр метода
    //        Password = decryptedPass // Используем параметр метода
    //    };

    //    dataSource = NpgsqlDataSource.Create(builder.ConnectionString);
    //    _connectionString = builder.ConnectionString;
    }

    public string GetConnectionString()
    {
        return _connectionString ?? throw new InvalidOperationException("Data source is not initialized.");
    }

    public bool CheckConnection()
    {
        try
        {
            var connection = dataSource.OpenConnection();
            var result = connection != null;
            if (result) connection.Close();
            Log.Information("Cоединение с БД установлено.");
            return result;
        }
        catch (Exception ex)
        {
            Log.Information("Не удалось установить соединение с БД. " + ex.Message);
            return false;
        }
    }


    public async Task<int?> Login(string username, string password)
    {
        int? privileges = null;

        try
        {


            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder
            {
                /*Host = ConfigurationManager.AppSettings[
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "WindowsDBHost" : "LinuxDBHost"],
                Database = ConfigurationManager.AppSettings[
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "WindowsDBase" : "LinuxDBase"],
                Port = int.Parse(ConfigurationManager.AppSettings[
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "WindowsDBPort" : "LinuxDBPort"]),*/
                Host = "localhost",
                Database = "OilCtrl",
                Port = 5432,
                Username = username, // Переданный логин
                Password = password  // Переданный пароль
            };

            dataSource = NpgsqlDataSource.Create(builder.ConnectionString);

            await using var connection = await dataSource.OpenConnectionAsync();

            using var groupCommand = new NpgsqlCommand(@"SELECT max(
                          case when pg_roles.rolname = 'admin' then 5
                            else 
                          case when pg_roles.rolname = 'configurator' then 4
                            else
                          case when pg_roles.rolname = 'master' then 3
                            else 
                          case when pg_roles.rolname = 'operator' then 2
                            else 
                          case when pg_roles.rolname = 'reader' then 1
                            else
                          0
                          end end end end end) as level
                        FROM pg_roles
                        WHERE
                           EXISTS (SELECT * FROM pg_user WHERE pg_user.usename = @Login)
                           AND pg_has_role(@Login, oid, 'member')", connection);

            groupCommand.Parameters.AddWithValue("@Login", username);

            var reader = await groupCommand.ExecuteReaderAsync();
            if (reader.Read())
            {
                privileges = reader.IsDBNull(0) ? null : reader.GetInt32(0);
            }
            _currentUser = username;
            await LogActionAsync($"Пользователь {username} вошел в систему."); // Логируем вход
            Log.Information($"Пользователь {username} вошел в систему.");
            return privileges;
        }
        catch (Exception ex)
        {
            Log.Information($"Ошибка в методе 'Login': {ex.Message}");
            Console.WriteLine($"Ошибка в методе 'Login': {ex.Message}");
            return null;
        }
    }

    private async Task LogActionAsync(string actionDescription)
    {
        try
        {
            if (_currentUser == null)
            {
                _currentUser = "postgres";
            }

            string pcName = Environment.MachineName; // Получаем имя компьютера

            await using var connection = await dataSource.OpenConnectionAsync();
            using var command = new NpgsqlCommand(
                @"SET ROLE admin; INSERT INTO public.""UserLog"" (""Time"", ""PcName"", ""UserName"", ""Message"") 
              VALUES (@time, @pcName, @userName, @message);", connection);

            command.Parameters.AddWithValue("@time", DateTime.UtcNow);
            command.Parameters.AddWithValue("@pcName", pcName);
            command.Parameters.AddWithValue("@userName", _currentUser);
            command.Parameters.AddWithValue("@message", actionDescription);

            await command.ExecuteNonQueryAsync();
            Log.Information($"Создан лог авторизации пользователя (LogActionAsync)");
        }
        catch (Exception ex)
        {
            Log.Information($"Ошибка логирования действия: {ex.Message}");
        }
    }


    public async Task<List<PostModel>> GetPostsAsync()
    {
        var posts = new List<PostModel>();

        try
        {
            string DecryptedPass = CryptoPass.GetPass();
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder
            {
                /*Host = ConfigurationManager.AppSettings[
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "WindowsDBHost" : "LinuxDBHost"],
                Database = ConfigurationManager.AppSettings[
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "WindowsDBase" : "LinuxDBase"],
                Port = int.Parse(ConfigurationManager.AppSettings[
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "WindowsDBPort" : "LinuxDBPort"]),*/
                Host = "localhost",
                Database = "OilCtrl",
                Port = 5432,
                Username = "postgres", // Переданный логин
                Password = "1"  // Переданный пароль
            };

            dataSource = NpgsqlDataSource.Create(builder.ConnectionString);

            await using var connection = await dataSource.OpenConnectionAsync();
            using var command = new NpgsqlCommand(@"SELECT ""PostNumber"", ""VehicleNumber"", ""DriverName"", ""FuelType"", ""Volume"", ""Dose"", ""Side"", ""Earth"", ""MachineType"", ""id"" FROM public.""Posts"";", connection);

            var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                posts.Add(new PostModel
                {
                    PostNumber = reader.GetInt32(0),
                    VehicleNumber = reader.GetString(1),
                    DriverName = reader.GetString(2),
                    FuelType = reader.GetString(3),
                    Volume = reader.GetInt32(4),
                    Dose = reader.GetInt32(5),
                    Side = reader.GetInt32(6),
                    Earth = reader.GetInt32(7),
                    MachineType = reader.GetInt32(8),
                    id = reader.GetInt32(9),
                });
            }
        }
        catch (Exception ex)
        {
            Log.Information($"Ошибка при получении постов: {ex.Message}");
        }

        return posts;
    }


    public async Task UpdatePostAsync(PostModel post)
    {


        await using var connection = await dataSource.OpenConnectionAsync();
        
        string query = @"
            UPDATE public.""Posts""
            SET ""VehicleNumber"" = @VehicleNumber,
                ""DriverName"" = @DriverName,
                ""PostNumber"" = @PostNumber,
                ""MachineType"" = @MachineType,
                ""FuelType"" = @FuelType,
                ""Volume"" = @Volume,
                ""Dose"" = @Dose,
                ""Side"" = @Side,
                ""Earth"" = @Earth
            WHERE ""id"" = @id;";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("PostNumber", post.PostNumber);
        command.Parameters.AddWithValue("id", post.id);
        command.Parameters.AddWithValue("VehicleNumber", post.VehicleNumber);
        command.Parameters.AddWithValue("DriverName", post.DriverName);
        command.Parameters.AddWithValue("MachineType", post.MachineType);
        command.Parameters.AddWithValue("PostNumber", post.PostNumber);
        command.Parameters.AddWithValue("FuelType", post.FuelType);
        command.Parameters.AddWithValue("Volume", post.Volume);
        command.Parameters.AddWithValue("Dose", post.Dose);
        command.Parameters.AddWithValue("Side", post.Side);
        command.Parameters.AddWithValue("Earth", post.Earth);

        await command.ExecuteNonQueryAsync();
    }

    public async Task AddPostAsync(PostModel post)
    {


        await using var connection = await dataSource.OpenConnectionAsync();

        string query = @"
            INSERT INTO public.""Posts""(
            ""VehicleNumber"", 
                ""DriverName"", 
                ""MachineType"",
                ""PostNumber"",
                ""FuelType"", 
                ""Volume"",
                ""Dose"",
                ""Side"", 
                ""Earth"", 
                ""id"")
            VALUES(@VehicleNumber, @DriverName,@MachineType,@PostNumber,@FuelType,@Volume,@Dose,@Side,@Earth,@id);";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("PostNumber", post.PostNumber);
        command.Parameters.AddWithValue("id", post.id);
        command.Parameters.AddWithValue("VehicleNumber", post.VehicleNumber);
        command.Parameters.AddWithValue("DriverName", post.DriverName);
        command.Parameters.AddWithValue("FuelType", post.FuelType);
        command.Parameters.AddWithValue("Volume", post.Volume);
        command.Parameters.AddWithValue("Dose", post.Dose);
        command.Parameters.AddWithValue("Side", post.Side);
        command.Parameters.AddWithValue("Earth", post.Earth);
        command.Parameters.AddWithValue("MachineType", post.MachineType);
        command.Parameters.AddWithValue("PostNumber", post.PostNumber);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeletePostAsync(PostModel post)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        string query = @"DELETE FROM public.""Posts"" where ""id"" = @id;";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", post.id);
        await command.ExecuteNonQueryAsync(); 
    }

    public async Task<List<ARMReport>> GetAllReportsAsync()
    {
        var reports = new List<ARMReport>();
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync();
            await using var command = new NpgsqlCommand(@" SELECT * FROM public.""Rep_REPORTS"" 
                                                               where ""UNIQUE_ID"" is not null 
                                                             ", connection);
            {
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var report = new ARMReport
                    {
                        ARMReportID = reader.GetInt32(reader.GetOrdinal("REPORT_ID")),                        
                        Name = reader.IsDBNull(reader.GetOrdinal("NAME")) ? null : reader.GetString(reader.GetOrdinal("NAME")),                        
                    };
                    reports.Add(report);
                }
            }

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while fetching reports from the database.");
            throw;
        }

        return reports;
    }

}