namespace DemoMcpServer.Web.Models;

sealed record DatabaseSchemaDto(
    string Database,
    IReadOnlyList<DatabaseObjectDto> Objects,
    IReadOnlyList<DatabaseRelationshipDto> Relationships,
    IReadOnlyList<DatabaseKeyDto> Keys);

sealed record DatabaseObjectDto(
    string Schema,
    string Name,
    string Type,
    IReadOnlyList<DatabaseColumnDto> Columns);

sealed record DatabaseColumnDto(
    string Name,
    string DataType,
    int? MaxLength,
    bool Nullable,
    bool Identity,
    bool PrimaryKey);

sealed record DatabaseRelationshipDto(
    string Name,
    DatabaseObjectReferenceDto Source,
    DatabaseObjectReferenceDto Target,
    IReadOnlyList<DatabaseRelationshipColumnDto> Columns,
    bool Disabled,
    bool NotTrusted);

sealed record DatabaseObjectReferenceDto(
    string Schema,
    string Table);

sealed record DatabaseRelationshipColumnDto(
    string Source,
    string Target,
    int Ordinal);

sealed record DatabaseKeyDto(
    string Schema,
    string Table,
    string Name,
    string Type,
    IReadOnlyList<DatabaseKeyColumnDto> Columns);

sealed record DatabaseKeyColumnDto(
    string Name,
    int Ordinal);
