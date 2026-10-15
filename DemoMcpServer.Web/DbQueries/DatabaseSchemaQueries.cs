namespace DemoMcpServer.Web.DbQueries;

internal static class DatabaseSchemaQueries
{
    public const string ObjectsCatalog = """
        SELECT
            s.name AS [schema],
            o.name AS [name],
            CASE o.type
                WHEN 'U' THEN 'TABLE'
                WHEN 'V' THEN 'VIEW'
            END AS [type],
            CAST(ep.value AS nvarchar(max)) AS [description]
        FROM sys.objects o
        INNER JOIN sys.schemas s
            ON s.schema_id = o.schema_id
        LEFT JOIN sys.extended_properties ep
            ON ep.class = 1
            AND ep.major_id = o.object_id
            AND ep.minor_id = 0
            AND ep.name = 'MS_Description'
        WHERE o.type IN ('U', 'V')
            AND o.is_ms_shipped = 0
            AND (@Schema IS NULL OR s.name = @Schema)
        ORDER BY s.name, o.name
        FOR JSON PATH, INCLUDE_NULL_VALUES;
        """;

    public const string Objects = """
        SELECT
            s.name AS [schema],
            o.name AS [name],
            CASE o.type
                WHEN 'U' THEN 'TABLE'
                WHEN 'V' THEN 'VIEW'
            END AS [type],
            JSON_QUERY((
                SELECT
                    c.name AS [name],
                    TYPE_NAME(c.user_type_id) AS [dataType],
                    CASE
                        WHEN c.max_length = -1 THEN -1
                        WHEN TYPE_NAME(c.user_type_id)
                             IN ('nvarchar', 'nchar')
                            THEN c.max_length / 2
                        ELSE c.max_length
                    END AS [maxLength],
                    c.precision AS [precision],
                    c.scale AS [scale],
                    c.is_nullable AS [nullable],
                    c.is_identity AS [identity],
                    CAST(
                        CASE WHEN EXISTS (
                            SELECT 1
                            FROM sys.indexes i
                            JOIN sys.index_columns ic
                                ON ic.object_id = i.object_id
                                AND ic.index_id = i.index_id
                            WHERE i.object_id = o.object_id
                              AND i.is_primary_key = 1
                              AND ic.column_id = c.column_id
                        ) THEN 1 ELSE 0 END
                    AS bit) AS [primaryKey]
                FROM sys.columns c
                WHERE c.object_id = o.object_id
                ORDER BY c.column_id
                FOR JSON PATH
            )) AS [columns]
        FROM sys.objects o
        JOIN sys.schemas s ON s.schema_id = o.schema_id
        WHERE o.type IN ('U', 'V')
          AND o.is_ms_shipped = 0
        ORDER BY s.name, o.name
        FOR JSON PATH;
        """;

    public const string Relationships = """
        SELECT
            fk.name AS [name],
            OBJECT_SCHEMA_NAME(fk.parent_object_id)
                AS [source.schema],
            OBJECT_NAME(fk.parent_object_id)
                AS [source.table],
            OBJECT_SCHEMA_NAME(fk.referenced_object_id)
                AS [target.schema],
            OBJECT_NAME(fk.referenced_object_id)
                AS [target.table],
            JSON_QUERY((
                SELECT
                    pc.name AS [source],
                    rc.name AS [target],
                    fkc.constraint_column_id AS [ordinal]
                FROM sys.foreign_key_columns fkc
                JOIN sys.columns pc
                    ON pc.object_id = fkc.parent_object_id
                    AND pc.column_id = fkc.parent_column_id
                JOIN sys.columns rc
                    ON rc.object_id = fkc.referenced_object_id
                    AND rc.column_id = fkc.referenced_column_id
                WHERE fkc.constraint_object_id = fk.object_id
                ORDER BY fkc.constraint_column_id
                FOR JSON PATH
            )) AS [columns],
            fk.is_disabled AS [disabled],
            fk.is_not_trusted AS [notTrusted]
        FROM sys.foreign_keys fk
        ORDER BY fk.name
        FOR JSON PATH;
        """;

    public const string Keys = """
        SELECT
            s.name AS [schema],
            t.name AS [table],
            kc.name AS [name],
            CASE kc.type
                WHEN 'PK' THEN 'PRIMARY KEY'
                WHEN 'UQ' THEN 'UNIQUE'
            END AS [type],
            JSON_QUERY((
                SELECT
                    c.name AS [name],
                    ic.key_ordinal AS [ordinal]
                FROM sys.index_columns ic
                JOIN sys.columns c
                    ON c.object_id = ic.object_id
                    AND c.column_id = ic.column_id
                WHERE ic.object_id = kc.parent_object_id
                  AND ic.index_id = kc.unique_index_id
                  AND ic.key_ordinal > 0
                ORDER BY ic.key_ordinal
                FOR JSON PATH
            )) AS [columns]
        FROM sys.key_constraints kc
        JOIN sys.tables t
            ON t.object_id = kc.parent_object_id
        JOIN sys.schemas s
            ON s.schema_id = t.schema_id
        ORDER BY s.name, t.name, kc.name
        FOR JSON PATH;
        """;

    public const string SearchObjects = """
        SELECT
            s.name AS [schema],
            o.name AS [name],
            CASE o.type
                WHEN 'U' THEN 'TABLE'
                WHEN 'V' THEN 'VIEW'
            END AS [type],
            CAST(ep.value AS nvarchar(max)) AS [description],

            JSON_QUERY((
                SELECT DISTINCT
                    m.match_type AS [type],
                    m.match_value AS [value]
                FROM (
                    SELECT
                        'object_name' AS match_type,
                        o.name AS match_value
                    WHERE o.name COLLATE Latin1_General_100_CI_AI
                        LIKE @Pattern ESCAPE '\'

                    UNION ALL

                    SELECT
                        'description',
                        CAST(ep.value AS nvarchar(max))
                    WHERE CAST(ep.value AS nvarchar(max))
                        COLLATE Latin1_General_100_CI_AI
                        LIKE @Pattern ESCAPE '\'

                    UNION ALL

                    SELECT
                        'column_name',
                        c.name
                    FROM sys.columns c
                    WHERE c.object_id = o.object_id
                        AND c.name COLLATE Latin1_General_100_CI_AI
                            LIKE @Pattern ESCAPE '\'

                    UNION ALL

                    SELECT
                        'column_description',
                        CAST(cep.value AS nvarchar(max))
                    FROM sys.columns c
                    INNER JOIN sys.extended_properties cep
                        ON cep.class = 1
                        AND cep.major_id = c.object_id
                        AND cep.minor_id = c.column_id
                        AND cep.name = 'MS_Description'
                    WHERE c.object_id = o.object_id
                        AND CAST(cep.value AS nvarchar(max))
                            COLLATE Latin1_General_100_CI_AI
                            LIKE @Pattern ESCAPE '\'
                ) m
                FOR JSON PATH
            )) AS [matches]

        FROM sys.objects o
        INNER JOIN sys.schemas s
            ON s.schema_id = o.schema_id

        LEFT JOIN sys.extended_properties ep
            ON ep.class = 1
            AND ep.major_id = o.object_id
            AND ep.minor_id = 0
            AND ep.name = 'MS_Description'

        WHERE o.type IN ('U', 'V')
            AND o.is_ms_shipped = 0
            AND (@Schema IS NULL OR s.name = @Schema)
            AND (
                o.name COLLATE Latin1_General_100_CI_AI
                    LIKE @Pattern ESCAPE '\'

                OR CAST(ep.value AS nvarchar(max))
                    COLLATE Latin1_General_100_CI_AI
                    LIKE @Pattern ESCAPE '\'

                OR EXISTS (
                    SELECT 1
                    FROM sys.columns c
                    WHERE c.object_id = o.object_id
                        AND c.name COLLATE Latin1_General_100_CI_AI
                            LIKE @Pattern ESCAPE '\'
                )

                OR EXISTS (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.extended_properties cep
                        ON cep.class = 1
                        AND cep.major_id = c.object_id
                        AND cep.minor_id = c.column_id
                        AND cep.name = 'MS_Description'
                    WHERE c.object_id = o.object_id
                        AND CAST(cep.value AS nvarchar(max))
                            COLLATE Latin1_General_100_CI_AI
                            LIKE @Pattern ESCAPE '\'
                )
            )

        ORDER BY s.name, o.name
        FOR JSON PATH, INCLUDE_NULL_VALUES;
        """;

    public const string ObjectDetails = """
    WITH RequestedObjects AS
    (
        SELECT DISTINCT
            JSON_VALUE(j.value, '$.schema') AS SchemaName,
            JSON_VALUE(j.value, '$.name') AS ObjectName
        FROM OPENJSON(@ObjectsJson) j
    )
    SELECT
        s.name AS [schema],
        o.name AS [name],
        CASE o.type
            WHEN 'U' THEN 'TABLE'
            WHEN 'V' THEN 'VIEW'
        END AS [type],
        CAST(ep.value AS nvarchar(max)) AS [description],

        JSON_QUERY((
            SELECT
                c.name AS [name],
                TYPE_NAME(c.user_type_id) AS [dataType],
                c.max_length AS [maxLength],
                c.precision AS [precision],
                c.scale AS [scale],
                c.is_nullable AS [nullable],
                c.is_identity AS [identity],
                c.is_computed AS [computed],
                CAST(cep.value AS nvarchar(max)) AS [description]
            FROM sys.columns c
            LEFT JOIN sys.extended_properties cep
                ON cep.class = 1
                AND cep.major_id = c.object_id
                AND cep.minor_id = c.column_id
                AND cep.name = 'MS_Description'
            WHERE c.object_id = o.object_id
            ORDER BY c.column_id
            FOR JSON PATH, INCLUDE_NULL_VALUES
        )) AS [columns],

        JSON_QUERY((
            SELECT
                kc.name AS [name],
                CASE kc.type
                    WHEN 'PK' THEN 'PRIMARY_KEY'
                    WHEN 'UQ' THEN 'UNIQUE'
                END AS [type],
                JSON_QUERY((
                    SELECT
                        col.name AS [name]
                    FROM sys.index_columns ic
                    INNER JOIN sys.columns col
                        ON col.object_id = ic.object_id
                        AND col.column_id = ic.column_id
                    WHERE ic.object_id = kc.parent_object_id
                        AND ic.index_id = kc.unique_index_id
                        AND ic.key_ordinal > 0
                    ORDER BY ic.key_ordinal
                    FOR JSON PATH
                )) AS [columns]
            FROM sys.key_constraints kc
            WHERE kc.parent_object_id = o.object_id
            FOR JSON PATH
        )) AS [keys],

        JSON_QUERY((
            SELECT
                fk.name AS [name],
                CASE
                    WHEN fk.parent_object_id = o.object_id
                        THEN 'OUTGOING'
                    ELSE 'INCOMING'
                END AS [direction],
                ps.name AS [sourceSchema],
                po.name AS [sourceObject],
                rs.name AS [targetSchema],
                ro.name AS [targetObject],
                fk.is_disabled AS [disabled],
                fk.is_not_trusted AS [notTrusted],
                JSON_QUERY((
                    SELECT
                        pc.name AS [sourceColumn],
                        rc.name AS [targetColumn]
                    FROM sys.foreign_key_columns fkc
                    INNER JOIN sys.columns pc
                        ON pc.object_id = fkc.parent_object_id
                        AND pc.column_id = fkc.parent_column_id
                    INNER JOIN sys.columns rc
                        ON rc.object_id = fkc.referenced_object_id
                        AND rc.column_id = fkc.referenced_column_id
                    WHERE fkc.constraint_object_id = fk.object_id
                    ORDER BY fkc.constraint_column_id
                    FOR JSON PATH
                )) AS [columns]
            FROM sys.foreign_keys fk
            INNER JOIN sys.objects po
                ON po.object_id = fk.parent_object_id
            INNER JOIN sys.schemas ps
                ON ps.schema_id = po.schema_id
            INNER JOIN sys.objects ro
                ON ro.object_id = fk.referenced_object_id
            INNER JOIN sys.schemas rs
                ON rs.schema_id = ro.schema_id
            WHERE fk.parent_object_id = o.object_id
               OR fk.referenced_object_id = o.object_id
            ORDER BY fk.name
            FOR JSON PATH
        )) AS [relationships]

    FROM RequestedObjects r
    INNER JOIN sys.schemas s
        ON s.name = r.SchemaName
    INNER JOIN sys.objects o
        ON o.schema_id = s.schema_id
        AND o.name = r.ObjectName
        AND o.type IN ('U', 'V')
        AND o.is_ms_shipped = 0
    LEFT JOIN sys.extended_properties ep
        ON ep.class = 1
        AND ep.major_id = o.object_id
        AND ep.minor_id = 0
        AND ep.name = 'MS_Description'
    ORDER BY s.name, o.name
    FOR JSON PATH, INCLUDE_NULL_VALUES;
    """;
}
