using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ArchivumWpf.Services;

public class SchemaValidationResult
{
    public bool IsValid { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class SchemaValidationService
{
    public async Task<SchemaValidationResult> ValidateAsync(string connectionString)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await using (var countCmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';", connection))
            {
                var tableCount = Convert.ToInt64(await countCmd.ExecuteScalarAsync());
                if (tableCount == 0)
                    return new SchemaValidationResult
                    {
                        IsValid = false,
                        Message = "This database is empty. Use 'Connect New Empty Database' instead."
                    };
            }
            
            await using (var historyExistsCmd = new NpgsqlCommand(
                             "SELECT to_regclass('public.\"__EFMigrationsHistory\"')::text;", connection))
            {
                var result = await historyExistsCmd.ExecuteScalarAsync();
                if (result == null || result is DBNull)
                    return new SchemaValidationResult
                    {
                        IsValid = false,
                        Message = "No migration history was found in this database. It does not appear to " +
                                  "have been created by ArchivumWpf, so its structure cannot be confirmed to match."
                    };
            }
            
            var appliedMigrations = new HashSet<string>(StringComparer.Ordinal);
            await using (var historyCmd = new NpgsqlCommand(
                "SELECT \"MigrationId\" FROM public.\"__EFMigrationsHistory\";", connection))
            await using (var reader = await historyCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    appliedMigrations.Add(reader.GetString(0));
            }

            var expectedMigrations = new HashSet<string>(GetExpectedMigrationIds(), StringComparer.Ordinal);

            var missing = expectedMigrations.Except(appliedMigrations).ToList();
            var unknown = appliedMigrations.Except(expectedMigrations).ToList();

            if (missing.Count > 0)
                return new SchemaValidationResult
                {
                    IsValid = false,
                    Message = "This database's schema is older than what this application expects. " +
                              $"Missing migration(s): {string.Join(", ", missing)}."
                };

            if (unknown.Count > 0)
                return new SchemaValidationResult
                {
                    IsValid = false,
                    Message = "This database's schema does not match this application's model. " +
                              $"Unrecognized migration(s) present: {string.Join(", ", unknown)}. " +
                              "It may have been created by a different or newer version of ArchivumWpf."
                };

            return new SchemaValidationResult { IsValid = true, Message = "Schema matches exactly." };
        }
        catch (PostgresException ex)
        {
            return new SchemaValidationResult { IsValid = false, Message = $"Database error: {ex.MessageText}" };
        }
        catch (Exception ex)
        {
            return new SchemaValidationResult { IsValid = false, Message = $"Validation failed: {ex.Message}" };
        }
    }
    
    
    private static IEnumerable<string> GetExpectedMigrationIds()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=schema_check_only;Username=dummy_user;Password=dummy_pw");
        using var context = new AppDbContext(optionsBuilder.Options);
        return context.Database.GetMigrations();
    }
}