using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DemoMcpServer.Web.Services;

public sealed class SqlQueryValidator
{
    public void Validate(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL query is empty.");

        var parser = new TSql160Parser(initialQuotedIdentifiers: true);

        using var reader = new StringReader(sql);

        var fragment = parser.Parse(reader, out var errors);

        if (errors.Count > 0)
            throw new ArgumentException("Invalid SQL: " + string.Join("; ", errors.Select(x => x.Message)));

        if (fragment is not TSqlScript script ||
            script.Batches.Count != 1 ||
            script.Batches[0].Statements.Count != 1 ||
            script.Batches[0].Statements[0] is not SelectStatement select)
        {
            throw new ArgumentException("Only a single SELECT statement is allowed.");
        }

        var visitor = new SqlSafetyVisitor();
        select.Accept(visitor);

        if (visitor.HasSelectInto)
            throw new ArgumentException("SELECT INTO is not allowed.");
    }

    private sealed class SqlSafetyVisitor : TSqlFragmentVisitor
    {
        public bool HasSelectInto { get; private set; }

        public override void ExplicitVisit(SelectStatement node)
        {
            if (node.Into is not null)
                HasSelectInto = true;

            base.ExplicitVisit(node);
        }
    }
}
