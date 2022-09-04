using System.Collections.Generic;
using System.Linq;
using System.Text;
using YesSql.Sql;
using YesSql.Sql.Schema;

// ReSharper disable once CheckNamespace
namespace YesSql.Provider.SqlServer
{
    public class SqlServerCommandInterpreter : BaseCommandInterpreter
    {
        public SqlServerCommandInterpreter(ISqlDialect dialect) : base(dialect)
        {
        }

        public override IEnumerable<string> Run(ICreateTableCommand command)
        {
            var builder = new StringBuilder();
            
            var tableName = $"{_dialect.SchemaNameQuotedPrefix()}{_dialect.QuoteForTableName(command.Name)}";
            builder.Append(_dialect.CreateTableString)
                .Append(' ')
                .Append(tableName)
                .Append(" (");

            var appendComma = false;
            foreach (var createColumn in command.TableCommands.OfType<CreateColumnCommand>())
            {
                if (appendComma)
                {
                    builder.Append(", ");
                }
                appendComma = true;

                Run(builder, createColumn);
            }

            // We only create PK statements on columns that don't have IsIdentity since IsIdentity statements also contains the notion of primary key.

            var primaryKeys = command.TableCommands.OfType<CreateColumnCommand>().Where(ccc => ccc.IsPrimaryKey && !ccc.IsIdentity).Select(ccc => _dialect.QuoteForColumnName(ccc.ColumnName)).ToArray();

            if (primaryKeys.Any())
            {
                if (appendComma)
                {
                    builder.Append(", ");
                }

                builder.Append(_dialect.PrimaryKeyString)
                    .Append(" ( ")
                    .Append(string.Join(", ", primaryKeys.ToArray()))
                    .Append(" )");
            }

            builder.Append(" )");
            var script = new StringBuilder();
            script.AppendLine(string.Format(_dialect.CreateTableIdempotentString, tableName))
                .AppendLine("BEGIN")
                .AppendLine(builder.ToString())
                .AppendLine("END");
            
            yield return script.ToString();
        }
        
        public override void Run(StringBuilder builder, IRenameColumnCommand command)
        {
            builder.AppendFormat("EXEC sp_RENAME {0}, {1}, 'COLUMN'",
                _dialect.GetSqlValue(command.Name + "." + command.ColumnName),
                _dialect.GetSqlValue(command.NewColumnName)
                );            
        }
        
        private void Run(StringBuilder builder, ICreateColumnCommand command)
        {
            // name
            builder.Append(_dialect.QuoteForColumnName(command.ColumnName)).Append(Space);

            if (!command.IsIdentity || _dialect.HasDataTypeInIdentityColumn)
            {
                var dbType = _dialect.ToDbType(command.DbType);

                builder.Append(_dialect.GetTypeName(dbType, command.Length, command.Precision, command.Scale));
            }

            // append identity if handled
            if (command.IsIdentity && _dialect.SupportsIdentityColumns)
            {
                builder.Append(Space).Append(_dialect.IdentityColumnString);
            }

            // [default value]
            if (command.Default != null)
            {
                builder.Append(" default ").Append(_dialect.GetSqlValue(command.Default)).Append(Space);
            }

            // nullable
            builder.Append(command.IsNotNull
                ? " not null"
                : !command.IsPrimaryKey && !command.IsUnique
                    ? _dialect.NullColumnString
                    : string.Empty);

            // append unique if handled, otherwise at the end of the satement
            if (command.IsUnique && _dialect.SupportsUnique)
            {
                builder.Append(" unique");
            }
        }
    }
}