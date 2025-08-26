using Npgsql;
using System.Threading.Tasks;

namespace Rental.IntegrationTests;

public static class DbTestUtils
{
    private const string ConnString =
        "Host=localhost;Port=5432;Database=mottu;Username=postgres;Password=postgres";

    public static async Task TruncateAllAsync()
    {
        await using var conn = new NpgsqlConnection(ConnString);
        await conn.OpenAsync();

        // adicione aqui outras tabelas quando surgirem (ordem importa se tiver FK)
        var sql = """
                  TRUNCATE TABLE motos RESTART IDENTITY CASCADE;
                  TRUNCATE TABLE couriers RESTART IDENTITY CASCADE;
                  """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }
}
