using Dapper;
using DemoMcpServer.Web.DbQueries;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.Json;

namespace DemoMcpServer.Web.Services;

public sealed class SqlServerSchemaService(IConfiguration configuration,
                                           SqlQueryValidator validator) : IDatabaseSchemaService
{
    private readonly string _connectionString =
        configuration.GetConnectionString("Northwind")
        ?? throw new InvalidOperationException("Connection string 'Northwind' is missing.");

    public Task<string> GetObjectsAsync(CancellationToken cancellationToken = default) =>
        ExecuteJsonAsync(DatabaseSchemaQueries.Objects, cancellationToken);

    public Task<string> GetRelationshipsAsync(CancellationToken cancellationToken = default) =>
        ExecuteJsonAsync(DatabaseSchemaQueries.Relationships, cancellationToken);

    public Task<string> GetKeysAsync(CancellationToken cancellationToken = default) =>
        ExecuteJsonAsync(DatabaseSchemaQueries.Keys, cancellationToken);

    public async Task<string> GetObjectsCatalogAsync(string? schema = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);

        var command = new CommandDefinition(
            commandText: DatabaseSchemaQueries.ObjectsCatalog,
            parameters: new { Schema = schema },
            commandTimeout: 30,
            cancellationToken: cancellationToken);

        var fragments = await connection.QueryAsync<string>(command);

        var json = new StringBuilder();

        foreach (var fragment in fragments)
            json.Append(fragment);

        return json.Length == 0 ? "[]" : json.ToString();
    }

    public async Task<string> ExecuteReadOnlySqlAsync(string sql, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL query cannot be empty.");

        validator.Validate(sql);

        await using var connection = new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            commandText: sql,
            commandTimeout: 15,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync(command);

        return JsonSerializer.Serialize(rows);
    }

    public async Task<string> SearchObjectsAsync(string search, string? schema = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(search))
            throw new ArgumentException("Search cannot be empty.");

        if (search.Length > 200)
            throw new ArgumentException("Search is too long.");

        var escaped = search.Trim()
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_")
            .Replace("[", "\\[");

        var pattern = $"%{escaped}%";

        await using var connection = new SqlConnection(_connectionString);

        var command = new CommandDefinition(
            commandText: DatabaseSchemaQueries.SearchObjects,
            parameters: new
            {
                Pattern = pattern,
                Schema = schema
            },
            commandTimeout: 30,
            cancellationToken: cancellationToken);

        var fragments = await connection.QueryAsync<string>(command);

        var json = new StringBuilder();

        foreach (var fragment in fragments)
            json.Append(fragment);

        return json.Length == 0 ? "[]" : json.ToString();
    }

    public async Task<string> GetObjectDetailsAsync(string[] objects, CancellationToken cancellationToken = default)
    {
        if (objects is null || objects.Length == 0)
            throw new ArgumentException("At least one object is required.");

        if (objects.Length > 20)
            throw new ArgumentException("Maximum 20 objects per request.");

        var requestedObjects = objects.Select(value =>
        {
            var separator = value.IndexOf('.');

            if (separator <= 0 || separator == value.Length - 1)
            {
                throw new ArgumentException(
                    $"Invalid object name: {value}. Expected schema.object.");
            }

            return new
            {
                schema = value[..separator],
                name = value[(separator + 1)..]
            };
        }).ToArray();

        var objectsJson = JsonSerializer.Serialize(requestedObjects);

        await using var connection = new SqlConnection(_connectionString);

        var command = new CommandDefinition(
            commandText: DatabaseSchemaQueries.ObjectDetails,
            parameters: new { ObjectsJson = objectsJson },
            commandTimeout: 30,
            cancellationToken: cancellationToken);

        var fragments = await connection.QueryAsync<string>(command);

        var json = new StringBuilder();

        foreach (var fragment in fragments)
            json.Append(fragment);

        return json.Length == 0 ? "[]" : json.ToString();
    }

    private async Task<string> ExecuteJsonAsync(string sql, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            commandText: sql,
            commandTimeout: 30,
            cancellationToken: cancellationToken);

        var fragments = await connection.QueryAsync<string>(command);

        var result = new StringBuilder();

        foreach (var fragment in fragments)
            result.Append(fragment);

        return result.Length == 0 ? "[]" : result.ToString();
    }
}
