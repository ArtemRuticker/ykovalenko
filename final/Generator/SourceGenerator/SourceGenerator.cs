using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SourceGenerator
{
    [Generator]
#pragma warning disable RS1036 // Specify analyzer banned API enforcement setting
    public class SourceGenerator : ISourceGenerator
#pragma warning restore RS1036 // Specify analyzer banned API enforcement setting
    {
        public void Initialize(GeneratorInitializationContext context)
        {

        }

        public void Execute(GeneratorExecutionContext context)
        {

            var connectionString = "Host=localhost;Username=postgres;Password=121212;Database=test_db";
            if (string.IsNullOrEmpty(connectionString))
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("GEN002", "Error", "Connection string not provided", "SourceGenerator", DiagnosticSeverity.Error, true), Location.None));
                return;
            }

            var tableNames = GetTableNames(connectionString);

            foreach (var tableName in tableNames)
            {
                var columns = GetTableColumns(connectionString, tableName);
                if (columns.Any(x => FirstUpper(x.Name) == "Id"))
                {
                    var classSourceText = GenerateClassSource(tableName, columns);
                    context.AddSource($"{tableName}.g.cs", SourceText.From(classSourceText, Encoding.UTF8));
                    var mappingSourceText = GenerateMappingSource(tableName, columns);
                    context.AddSource($"{tableName}Map.g.cs", SourceText.From(mappingSourceText, Encoding.UTF8));
                }
            }
        }

        private List<string> GetTableNames(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            var tables = new List<string>();
            using (var command = new NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'", connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    tables.Add(reader.GetString(0));
                }
            }
            return tables;
        }

        private List<(string Name, string Type)> GetTableColumns(string connectionString, string tableName)
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            var columns = new List<(string Name, string Type)>();
            using (var command = new NpgsqlCommand($"SELECT column_name, data_type FROM information_schema.columns WHERE table_name = '{tableName}'", connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    columns.Add((reader.GetString(0), reader.GetString(1)));
                }
            }
            return columns;
        }

        private string GenerateClassSource(string tableName, List<(string Name, string Type)> columns)
        {
            var className = FirstUpper(tableName);
            var properties = string.Join("\r\n", columns.Select(c => $"public virtual {ConvertToCSharpType(c.Type)} {FirstUpper(c.Name)} {{ get; set; }}"));

            return $@"
namespace GeneratedClasses
{{
    public class {className}
    {{
        
        {properties}
    }}
}}";
        }

        private string GenerateMappingSource(string tableName, List<(string Name, string Type)> columns)
        {
            var className = FirstUpper(tableName);
            var mappings = string.Join("\r\n",
                columns.Where(c => FirstUpper(c.Name) != "Id").
                Select(c => $"Map(x => x.{FirstUpper(c.Name)});"));

            return $@"
using FluentNHibernate.Mapping;

namespace GeneratedClasses
{{
    public class {className}Map : ClassMap<{className}>
    {{
        public {className}Map()
        {{
            Id(x => x.Id);
            Table(""{tableName}"");
            {mappings}
        }}
    }}
}}";
        }

        private string FirstUpper(string tableName)
        {
            return char.ToUpper(tableName[0]) + tableName.Substring(1);
        }

        private string ConvertToCSharpType(string sqlType)
        {
            return sqlType switch
            {
                "integer" => "int",
                "serial" => "int",
                "bigint" => "long",
                "boolean" => "bool",
                "text" => "string",
                "timestamp without time zone" => "DateTime",
                _ => "string"
            };
        }
    }
}