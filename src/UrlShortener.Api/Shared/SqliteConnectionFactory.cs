using Microsoft.Data.Sqlite;

namespace UrlShortener.Api.Shared;

public class SqliteConnectionFactory
{
    private readonly string connectionString;

    public SqliteConnectionFactory(IConfiguration configuration, string contentRoot)
    {
        var value = configuration.GetConnectionString("Links");
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("A configuração ConnectionStrings:Links é obrigatória.");

        SqliteConnectionStringBuilder settings;
        try
        {
            settings = new SqliteConnectionStringBuilder(value);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException("A configuração ConnectionStrings:Links é inválida.", exception);
        }

        if (string.IsNullOrWhiteSpace(settings.DataSource) || settings.DataSource == ":memory:" || settings.Mode == SqliteOpenMode.Memory)
            throw new InvalidOperationException("A ligação deve apontar para um ficheiro SQLite persistente.");

        settings.DataSource = Path.GetFullPath(settings.DataSource, contentRoot);
        settings.Pooling = true;
        settings.DefaultTimeout = 5;
        settings.ForeignKeys = true;
        settings.Cache = SqliteCacheMode.Default;
        connectionString = settings.ToString();
    }

    public virtual SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(connectionString);
        try
        {
            connection.Open();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }
}
