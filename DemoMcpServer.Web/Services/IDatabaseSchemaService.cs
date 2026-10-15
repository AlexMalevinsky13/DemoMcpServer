namespace DemoMcpServer.Web.Services;

public interface IDatabaseSchemaService
{
    Task<string> GetObjectsAsync(CancellationToken cancellationToken = default);

    Task<string> GetObjectsCatalogAsync(string? schema = null, CancellationToken cancellationToken = default);

    Task<string> GetRelationshipsAsync(CancellationToken cancellationToken = default);

    Task<string> GetKeysAsync(CancellationToken cancellationToken = default);

    Task<string> SearchObjectsAsync(string search, string? schema = null, CancellationToken cancellationToken = default);

    Task<string> ExecuteReadOnlySqlAsync(string sql, CancellationToken cancellationToken = default);

    Task<string> GetObjectDetailsAsync(string[] objects, CancellationToken cancellationToken = default);
}
