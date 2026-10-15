using DemoMcpServer.Web.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DemoMcpServer.Web;

[McpServerToolType]
public sealed class DatabaseTools(IDatabaseSchemaService schemaService)
{
    [McpServerTool(Name = "get_database_tables", ReadOnly = true)]
    [Description("Returns SQL Server tables, views, columns, data types and primary key flags as JSON.")]
    public Task<string> GetTablesAsync(CancellationToken cancellationToken) =>
        schemaService.GetObjectsAsync(cancellationToken);

    [McpServerTool(Name = "get_database_relationships", ReadOnly = true)]
    [Description("Returns SQL Server foreign key relationships with exact source and target columns as JSON.")]
    public Task<string> GetRelationshipsAsync(CancellationToken cancellationToken) =>
        schemaService.GetRelationshipsAsync(cancellationToken);

    [McpServerTool(Name = "get_database_keys", ReadOnly = true)]
    [Description("Returns SQL Server primary key and unique constraints, including composite keys, as JSON.")]
    public Task<string> GetKeysAsync(CancellationToken cancellationToken) =>
        schemaService.GetKeysAsync(cancellationToken);

    [McpServerTool(Name = "get_database_objects", ReadOnly = true)]
    [Description("Returns a lightweight catalog of database tables and views. Includes schema, object name, object type and description. " +
    "Does not return columns or relationships. Use this tool to discover database objects before requesting detailed metadata.")]
    public Task<string> GetDatabaseObjectsAsync([Description("Optional database schema filter, for example dbo. Leave null to retrieve objects from all schemas.")] string? schema = null, CancellationToken cancellationToken = default) =>
        schemaService.GetObjectsCatalogAsync(schema, cancellationToken);

    [McpServerTool(Name = "execute_readonly_sql", ReadOnly = true)]
    [Description("Executes a read-only SELECT query against the Northwind SQL Server database and returns the results as JSON. Use database schema and relationships tools to construct correct SQL queries.")]
    public Task<string> ExecuteReadOnlySqlAsync([Description("SQL SELECT query to execute.")]string sql, CancellationToken cancellationToken)  =>
        schemaService.ExecuteReadOnlySqlAsync(sql, cancellationToken);

    [McpServerTool(Name = "search_database_schema", ReadOnly = true)]
    [Description("Searches database tables and views by object name, column name, object description and column description. " +
    "Returns matching objects with reasons for each match. Use this tool to discover relevant database objects before requesting their detailed metadata.")]
    public Task<string> SearchDatabaseSchemaAsync([Description("Text to search for in database object and column metadata.")]string search, [Description("Optional database schema filter, for example dbo.")]string? schema = null, CancellationToken cancellationToken = default) =>
        schemaService.SearchObjectsAsync(search, schema,cancellationToken);

    [McpServerTool(Name = "get_database_object_details", ReadOnly = true)]
    [Description("Returns detailed metadata for selected database tables and views. " +
    "Includes columns, data types, primary and unique keys, incoming and outgoing foreign key relationships, and MS_Description metadata. " +
    "Use after get_database_objects or search_database_schema. Maximum 20 objects per request.")]
    public Task<string> GetDatabaseObjectDetailsAsync([Description("Array of fully qualified database object names, for example: dbo.Orders, dbo.Order Details, dbo.Products.")] string[] objects, CancellationToken cancellationToken = default) =>
        schemaService.GetObjectDetailsAsync(objects, cancellationToken);
}
