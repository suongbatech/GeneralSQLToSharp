using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace SqlToCSharpGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Server=127.0.0.1;Database=MinaCloudAdmin;User Id=sa;Password=t020808;TrustServerCertificate=True;MultipleActiveResultSets=True;";
            string outputPath = @"D:\Temp\Code\";

            var tables = GetTables(connectionString);
            foreach (var table in tables)
            {
                var columns = GetColumns(connectionString, table);
                var fks = GetForeignKeys(connectionString, table);

                WriteToFile(Path.Combine(outputPath, "Entities", table + "Entity.cs"), GenerateEntity(table, columns, fks));
                WriteToFile(Path.Combine(outputPath, "Requests", table + "Request.cs"), GenerateRequest(table, columns));
                WriteToFile(Path.Combine(outputPath, "Responses", table + "Response.cs"), GenerateResponse(table, columns, fks));
                WriteToFile(Path.Combine(outputPath, "Repositories", "I" + table + "Repository.cs"), GenerateRepositoryInterface(table));
                WriteToFile(Path.Combine(outputPath, "Repositories", table + "Repository.cs"), GenerateRepositoryImpl(table, columns, fks));
                WriteToFile(Path.Combine(outputPath, "Services", "I" + table + "Service.cs"), GenerateServiceInterface(table));
                WriteToFile(Path.Combine(outputPath, "Services", table + "Service.cs"), GenerateServiceImpl(table));
                WriteToFile(Path.Combine(outputPath, "Controllers", table + "Controller.cs"), GenerateController(table));
            }

            Console.WriteLine("Code generation completed!");
        }

        #region Utilities
        private static void WriteToFile(string path, string content)
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, content);
        }

        private static List<string> GetTables(string connStr)
        {
            var tables = new List<string>();
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read()) tables.Add(reader.GetString(0));
            }
            return tables;
        }

        private static List<ColumnInfo> GetColumns(string connStr, string table)
        {
            var cols = new List<ColumnInfo>();
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                var cmd = new SqlCommand($@"
                    SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = '{table}'", conn);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    cols.Add(new ColumnInfo
                    {
                        Name = reader.GetString(0),
                        DataType = reader.GetString(1),
                        IsNullable = reader.GetString(2) == "YES"
                    });
                }
            }
            return cols;
        }

        private static List<ForeignKeyInfo> GetForeignKeys(string connStr, string table)
        {
            var fks = new List<ForeignKeyInfo>();
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                var cmd = new SqlCommand($@"
                    SELECT 
                        f.name AS FK_Name,
                        COL_NAME(fc.parent_object_id, fc.parent_column_id) AS FK_Column,
                        OBJECT_NAME (fc.referenced_object_id) AS PK_Table,
                        COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS PK_Column
                    FROM sys.foreign_keys AS f
                    INNER JOIN sys.foreign_key_columns AS fc 
                        ON f.object_id = fc.constraint_object_id
                    WHERE OBJECT_NAME(f.parent_object_id) = '{table}'", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    fks.Add(new ForeignKeyInfo
                    {
                        Name = reader.GetString(0),
                        Column = reader.GetString(1),
                        ReferenceTable = reader.GetString(2),
                        ReferenceColumn = reader.GetString(3)
                    });
                }
            }
            return fks;
        }

        private static string SqlTypeToCSharpType(string sqlType, bool isNullable)
        {
            string type = sqlType switch
            {
                "int" => "int",
                "bigint" => "long",
                "smallint" => "short",
                "tinyint" => "byte",
                "bit" => "bool",
                "decimal" => "decimal",
                "numeric" => "decimal",
                "money" => "decimal",
                "float" => "double",
                "real" => "float",
                "date" => "DateTime",
                "datetime" => "DateTime",
                "datetime2" => "DateTime",
                "datetimeoffset" => "DateTimeOffset",
                "time" => "TimeSpan",
                "char" => "string",
                "varchar" => "string",
                "text" => "string",
                "nchar" => "string",
                "nvarchar" => "string",
                "ntext" => "string",
                "uniqueidentifier" => "Guid",
                _ => "string"
            };
            return isNullable && type != "string" ? type + "?" : type;
        }
        #endregion

        #region Generators
        private static string GenerateEntity(string table, List<ColumnInfo> columns, List<ForeignKeyInfo> fks)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine($"namespace Entities");
            sb.AppendLine("{");
            sb.AppendLine($"\tpublic class {table}Entity");
            sb.AppendLine("\t{");

            foreach (var col in columns)
            {
                sb.AppendLine($"\t\tpublic {SqlTypeToCSharpType(col.DataType, col.IsNullable)} {col.Name} {{ get; set; }}");
            }

            foreach (var fk in fks)
            {
                sb.AppendLine($"\t\tpublic {fk.ReferenceTable}Entity {fk.ReferenceTable} {{ get; set; }}");
            }

            sb.AppendLine("\t}");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string GenerateRequest(string table, List<ColumnInfo> columns)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine($"namespace Requests");
            sb.AppendLine("{");
            sb.AppendLine($"\tpublic class {table}Request");
            sb.AppendLine("\t{");
            foreach (var col in columns)
            {
                if (col.Name.ToLower() != "id") // skip ID for request
                    sb.AppendLine($"\t\tpublic {SqlTypeToCSharpType(col.DataType, col.IsNullable)} {col.Name} {{ get; set; }}");
            }
            sb.AppendLine("\t}");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string GenerateResponse(string table, List<ColumnInfo> columns, List<ForeignKeyInfo> fks)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine($"namespace Responses");
            sb.AppendLine("{");
            sb.AppendLine($"\tpublic class {table}Response");
            sb.AppendLine("\t{");
            foreach (var col in columns)
            {
                sb.AppendLine($"\t\tpublic {SqlTypeToCSharpType(col.DataType, col.IsNullable)} {col.Name} {{ get; set; }}");
            }

            foreach (var fk in fks)
            {
                sb.AppendLine($"\t\tpublic {fk.ReferenceTable}Response {fk.ReferenceTable} {{ get; set; }}");
            }

            sb.AppendLine("\t}");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string GenerateRepositoryInterface(string table)
        {
            return $@"using Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{{
    public interface I{table}Repository
    {{
        Task<{table}Entity> GetByIdAsync(int id);
        Task<List<{table}Entity>> GetAllAsync();
        Task<int> AddAsync({table}Entity entity);
        Task<bool> UpdateAsync({table}Entity entity);
        Task<bool> DeleteAsync(int id);
    }}
}}";
        }

        private static string GenerateRepositoryImpl(string table, List<ColumnInfo> columns, List<ForeignKeyInfo> fks)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using Entities;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using System.Data.SqlClient;");
            sb.AppendLine();
            sb.AppendLine("namespace Repositories");
            sb.AppendLine("{");
            sb.AppendLine($"\tpublic class {table}Repository : I{table}Repository");
            sb.AppendLine("\t{");
            sb.AppendLine("\t\tprivate readonly string _connectionString = \"YourConnectionStringHere\";");
            sb.AppendLine();
            sb.AppendLine("\t\tpublic async Task<int> AddAsync(" + table + "Entity entity) { /* TODO: implement INSERT */ return 0; }");
            sb.AppendLine("\t\tpublic async Task<bool> UpdateAsync(" + table + "Entity entity) { /* TODO: implement UPDATE */ return true; }");
            sb.AppendLine("\t\tpublic async Task<bool> DeleteAsync(int id) { /* TODO: implement DELETE */ return true; }");
            sb.AppendLine("\t\tpublic async Task<" + table + "Entity> GetByIdAsync(int id) { /* TODO: implement SELECT by Id */ return null; }");
            sb.AppendLine("\t\tpublic async Task<List<" + table + "Entity>> GetAllAsync() { /* TODO: implement SELECT all */ return new List<" + table + "Entity>(); }");
            sb.AppendLine("\t}");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string GenerateServiceInterface(string table)
        {
            return $@"using Requests;
using Responses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{{
    public interface I{table}Service
    {{
        Task<{table}Response> GetByIdAsync(int id);
        Task<List<{table}Response>> GetAllAsync();
        Task<int> AddAsync({table}Request request);
        Task<bool> UpdateAsync(int id, {table}Request request);
        Task<bool> DeleteAsync(int id);
    }}
}}";
        }

        private static string GenerateServiceImpl(string table)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using Entities;");
            sb.AppendLine("using Repositories;");
            sb.AppendLine("using Requests;");
            sb.AppendLine("using Responses;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine();
            sb.AppendLine("namespace Services");
            sb.AppendLine("{");
            sb.AppendLine($"\tpublic class {table}Service : I{table}Service");
            sb.AppendLine("\t{");
            sb.AppendLine($"\t\tprivate readonly I{table}Repository _repository;");
            sb.AppendLine($"\t\tpublic {table}Service(I{table}Repository repository) {{ _repository = repository; }}");
            sb.AppendLine($"\t\tpublic async Task<int> AddAsync({table}Request request) {{ /* TODO: map request to entity */ return 0; }}");
            sb.AppendLine($"\t\tpublic async Task<bool> UpdateAsync(int id, {table}Request request) {{ /* TODO: implement update */ return true; }}");
            sb.AppendLine($"\t\tpublic async Task<bool> DeleteAsync(int id) => await _repository.DeleteAsync(id);");
            sb.AppendLine($"\t\tpublic async Task<{table}Response> GetByIdAsync(int id) {{ /* TODO: map entity to response */ return null; }}");
            sb.AppendLine($"\t\tpublic async Task<List<{table}Response>> GetAllAsync() {{ /* TODO: map entity list to response list */ return new List<{table}Response>(); }}");
            sb.AppendLine("\t}");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string GenerateController(string table)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
            sb.AppendLine("using Requests;");
            sb.AppendLine("using Responses;");
            sb.AppendLine("using Services;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine();
            sb.AppendLine("namespace Controllers");
            sb.AppendLine("{");
            sb.AppendLine($"\t[ApiController]");
            sb.AppendLine($"\t[Route(\"api/[controller]\")]");
            sb.AppendLine($"\tpublic class {table}Controller : ControllerBase");
            sb.AppendLine("\t{");
            sb.AppendLine($"\t\tprivate readonly I{table}Service _service;");
            sb.AppendLine($"\t\tpublic {table}Controller(I{table}Service service) {{ _service = service; }}");
            sb.AppendLine();
            sb.AppendLine("\t\t[HttpGet]");
            sb.AppendLine($"\t\tpublic async Task<List<{table}Response>> GetAll() => await _service.GetAllAsync();");
            sb.AppendLine();
            sb.AppendLine("\t\t[HttpGet(\"{id}\")]");
            sb.AppendLine($"\t\tpublic async Task<{table}Response> GetById(int id) => await _service.GetByIdAsync(id);");
            sb.AppendLine();
            sb.AppendLine("\t\t[HttpPost]");
            sb.AppendLine($"\t\tpublic async Task<int> Create({table}Request request) => await _service.AddAsync(request);");
            sb.AppendLine();
            sb.AppendLine("\t\t[HttpPut(\"{id}\")]");
            sb.AppendLine($"\t\tpublic async Task<bool> Update(int id, {table}Request request) => await _service.UpdateAsync(id, request);");
            sb.AppendLine();
            sb.AppendLine("\t\t[HttpDelete(\"{id}\")]");
            sb.AppendLine("\t\tpublic async Task<bool> Delete(int id) => await _service.DeleteAsync(id);");
            sb.AppendLine("\t}");
            sb.AppendLine("}");
            return sb.ToString();
        }

        #endregion

        class ColumnInfo
        {
            public string Name { get; set; }
            public string DataType { get; set; }
            public bool IsNullable { get; set; }
        }

        class ForeignKeyInfo
        {
            public string Name { get; set; }
            public string Column { get; set; }
            public string ReferenceTable { get; set; }
            public string ReferenceColumn { get; set; }
        }
    }
}

//using System;
//using System.Collections.Generic;
//using System.Data.SqlClient;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace CodeGenDemo
//{
//    class Program
//    {
//        static async Task Main(string[] args)
//        {
//            string connectionString = "Server=127.0.0.1;Database=MinaCloudAdmin;User Id=sa;Password=t020808;TrustServerCertificate=True;MultipleActiveResultSets=True;";
//            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "Generated");
//            Directory.CreateDirectory(outputDir);

//            using var conn = new SqlConnection(connectionString);
//            await conn.OpenAsync();

//            // Lấy danh sách bảng
//            var tables = new List<string>();
//            using (var cmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'", conn))
//            using (var reader = await cmd.ExecuteReaderAsync())
//            {
//                while (await reader.ReadAsync())
//                {
//                    tables.Add(reader.GetString(0));
//                }
//            }

//            foreach (var table in tables)
//            {
//                // Lấy cột
//                var cols = new List<(string Name, string Type, bool IsNullable)>();
//                using (var cmd = new SqlCommand($"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='{table}'", conn))
//                using (var reader = await cmd.ExecuteReaderAsync())
//                {
//                    while (await reader.ReadAsync())
//                    {
//                        cols.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2) == "YES"));
//                    }
//                }

//                // Lấy FK
//                var fks = new List<ForeignKeyInfo>();
//                using (var cmd = new SqlCommand(@"
//                    SELECT
//                        parent.name AS ParentColumn,
//                        ref.name AS ReferencedColumn,
//                        t.name AS ParentTable,
//                        rt.name AS ReferencedTable
//                    FROM sys.foreign_key_columns fkc
//                    INNER JOIN sys.tables t ON fkc.parent_object_id = t.object_id
//                    INNER JOIN sys.columns parent ON parent.column_id = fkc.parent_column_id AND parent.object_id = t.object_id
//                    INNER JOIN sys.tables rt ON fkc.referenced_object_id = rt.object_id
//                    INNER JOIN sys.columns ref ON ref.column_id = fkc.referenced_column_id AND ref.object_id = rt.object_id
//                    WHERE t.name = @table", conn))
//                {
//                    cmd.Parameters.AddWithValue("@table", table);
//                    using var reader = await cmd.ExecuteReaderAsync();
//                    while (await reader.ReadAsync())
//                    {
//                        fks.Add(new ForeignKeyInfo
//                        {
//                            ParentColumn = reader.GetString(0),
//                            ReferencedColumn = reader.GetString(1),
//                            ParentTable = reader.GetString(2),
//                            ReferencedTable = reader.GetString(3)
//                        });
//                    }
//                }

//                // Gọi generator
//                CodeGenerator.GenerateEntity(table, cols, outputDir);
//                CodeGenerator.GenerateRequest(table, cols, outputDir);
//                CodeGenerator.GenerateResponse(table, cols, outputDir);
//                CodeGenerator.GenerateRepository(table, cols, outputDir);
//                CodeGenerator.GenerateRepositoryImpl(table, cols, fks, outputDir);
//                CodeGenerator.GenerateService(table, cols, outputDir);
//                CodeGenerator.GenerateServiceImpl(table, cols, fks, outputDir);
//                CodeGenerator.GenerateController(table, cols, outputDir);
//            }

//            Console.WriteLine("✅ Code generated successfully!");
//        }
//    }

//    public class ForeignKeyInfo
//    {
//        public string ParentColumn { get; set; } = "";
//        public string ReferencedColumn { get; set; } = "";
//        public string ParentTable { get; set; } = "";
//        public string ReferencedTable { get; set; } = "";
//    }

//    static class CodeGenerator
//    {
//        #region Entity/Request/Response
//        public static void GenerateEntity(string table, List<(string Name, string Type, bool IsNullable)> cols, string outputDir)
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("namespace Generated.Entities");
//            sb.AppendLine("{");
//            sb.AppendLine($"    public class {table}Entity");
//            sb.AppendLine("    {");
//            foreach (var col in cols)
//                sb.AppendLine($"        public {SqlToCSharp(col.Type, col.IsNullable)} {col.Name} {{ get; set; }}");
//            foreach (var fk in cols) { } // Placeholder for FK entity navigation
//            sb.AppendLine("    }");
//            sb.AppendLine("}");
//            WriteToFile(Path.Combine(outputDir, "Entities", $"{table}Entity.cs"), sb.ToString());
//        }

//        public static void GenerateRequest(string table, List<(string Name, string Type, bool IsNullable)> cols, string outputDir)
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("namespace Generated.Requests");
//            sb.AppendLine("{");
//            sb.AppendLine($"    public class {table}Request");
//            sb.AppendLine("    {");
//            foreach (var col in cols)
//                sb.AppendLine($"        public {SqlToCSharp(col.Type, col.IsNullable)} {col.Name} {{ get; set; }}");
//            sb.AppendLine("    }");
//            sb.AppendLine("}");
//            WriteToFile(Path.Combine(outputDir, "Requests", $"{table}Request.cs"), sb.ToString());
//        }

//        public static void GenerateResponse(string table, List<(string Name, string Type, bool IsNullable)> cols, string outputDir)
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("namespace Generated.Responses");
//            sb.AppendLine("{");
//            sb.AppendLine($"    public class {table}Response");
//            sb.AppendLine("    {");
//            foreach (var col in cols)
//                sb.AppendLine($"        public {SqlToCSharp(col.Type, col.IsNullable)} {col.Name} {{ get; set; }}");
//            sb.AppendLine("    }");
//            sb.AppendLine("}");
//            WriteToFile(Path.Combine(outputDir, "Responses", $"{table}Response.cs"), sb.ToString());
//        }
//        #endregion

//        #region Repository Interface
//        public static void GenerateRepository(string table, List<(string Name, string Type, bool IsNullable)> cols, string outputDir)
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("using System.Collections.Generic;");
//            sb.AppendLine("using System.Threading.Tasks;");
//            sb.AppendLine("using Generated.Entities;");
//            sb.AppendLine();
//            sb.AppendLine("namespace Generated.Repositories");
//            sb.AppendLine("{");
//            sb.AppendLine($"    public interface I{table}Repository");
//            sb.AppendLine("    {");
//            sb.AppendLine($"        Task<IEnumerable<{table}Entity>> GetAllAsync();");
//            sb.AppendLine($"        Task<{table}Entity?> GetByIdAsync(int id);");
//            sb.AppendLine($"        Task AddAsync({table}Entity entity);");
//            sb.AppendLine($"        Task UpdateAsync({table}Entity entity);");
//            sb.AppendLine("        Task DeleteAsync(int id);");
//            sb.AppendLine("    }");
//            sb.AppendLine("}");
//            WriteToFile(Path.Combine(outputDir, "Repositories", $"I{table}Repository.cs"), sb.ToString());
//        }
//        #endregion

//        #region Repository Implementation
//        public static void GenerateRepositoryImpl(string table, List<(string Name, string Type, bool IsNullable)> cols, List<ForeignKeyInfo> fks, string outputDir)
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("using System;");
//            sb.AppendLine("using System.Collections.Generic;");
//            sb.AppendLine("using System.Data.SqlClient;");
//            sb.AppendLine("using System.Threading.Tasks;");
//            sb.AppendLine("using Generated.Entities;");
//            sb.AppendLine();
//            sb.AppendLine("namespace Generated.Repositories");
//            sb.AppendLine("{");
//            sb.AppendLine($"    public class {table}Repository : I{table}Repository");
//            sb.AppendLine("    {");
//            sb.AppendLine("        private readonly string _connectionString = \"Server=127.0.0.1;Database=MinaCloudAdmin;User Id=sa;Password=t020808;TrustServerCertificate=True;MultipleActiveResultSets=True\";");
//            sb.AppendLine();

//            string selectCols = "t.*";
//            string joinSql = "";
//            int idx = 1;
//            foreach (var fk in fks)
//            {
//                selectCols += $", r{idx}.*";
//                joinSql += $" LEFT JOIN {fk.ReferencedTable} r{idx} ON t.{fk.ParentColumn} = r{idx}.{fk.ReferencedColumn}";
//                idx++;
//            }

//            // GetAllAsync
//            sb.AppendLine($"        public async Task<IEnumerable<{table}Entity>> GetAllAsync()");
//            sb.AppendLine("        {");
//            sb.AppendLine($"            var list = new List<{table}Entity>();");
//            sb.AppendLine("            using var conn = new SqlConnection(_connectionString);");
//            sb.AppendLine("            await conn.OpenAsync();");
//            sb.AppendLine($"            var sql = \"SELECT {selectCols} FROM {table} t {joinSql}\";");
//            sb.AppendLine("            using var cmd = new SqlCommand(sql, conn);");
//            sb.AppendLine("            using var reader = await cmd.ExecuteReaderAsync();");
//            sb.AppendLine("            while(await reader.ReadAsync())");
//            sb.AppendLine("            {");
//            sb.AppendLine($"                var entity = new {table}Entity();");
//            foreach (var col in cols)
//            {
//                sb.AppendLine($"                entity.{col.Name} = reader[\"{col.Name}\"] == DBNull.Value ? null : ({SqlToCSharp(col.Type, col.IsNullable)})reader[\"{col.Name}\"];");
//            }
//            sb.AppendLine("                list.Add(entity);");
//            sb.AppendLine("            }");
//            sb.AppendLine("            return list;");
//            sb.AppendLine("        }");

//            // GetByIdAsync
//            sb.AppendLine($"        public async Task<{table}Entity?> GetByIdAsync(int id)");
//            sb.AppendLine("        {");
//            sb.AppendLine("            using var conn = new SqlConnection(_connectionString);");
//            sb.AppendLine("            await conn.OpenAsync();");
//            sb.AppendLine($"            var sql = \"SELECT {selectCols} FROM {table} t {joinSql} WHERE t.Id=@Id\";");
//            sb.AppendLine("            using var cmd = new SqlCommand(sql, conn);");
//            sb.AppendLine("            cmd.Parameters.AddWithValue(\"@Id\", id);");
//            sb.AppendLine("            using var reader = await cmd.ExecuteReaderAsync();");
//            sb.AppendLine("            if(await reader.ReadAsync())");
//            sb.AppendLine("            {");
//            sb.AppendLine($"                var entity = new {table}Entity();");
//            foreach (var col in cols)
//            {
//                sb.AppendLine($"                entity.{col.Name} = reader[\"{col.Name}\"] == DBNull.Value ? null : ({SqlToCSharp(col.Type, col.IsNullable)})reader[\"{col.Name}\"];");
//            }
//            sb.AppendLine("                return entity;");
//            sb.AppendLine("            }");
//            sb.AppendLine("            return null;");
//            sb.AppendLine("        }");

//            sb.AppendLine($"        public Task AddAsync({table}Entity entity) => Task.CompletedTask;");
//            sb.AppendLine($"        public Task UpdateAsync({table}Entity entity) => Task.CompletedTask;");
//            sb.AppendLine($"        public Task DeleteAsync(int id) => Task.CompletedTask;");

//            sb.AppendLine("    }");
//            sb.AppendLine("}");
//            WriteToFile(Path.Combine(outputDir, "Repositories", $"{table}Repository.cs"), sb.ToString());
//        }
//        #endregion

//        #region Service Interface
//        public static void GenerateService(string table, List<(string Name, string Type, bool IsNullable)> cols, string outputDir)
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("using System.Collections.Generic;");
//            sb.AppendLine("using System.Threading.Tasks;");
//            sb.AppendLine("using Generated.Requests;");
//            sb.AppendLine("using Generated.Responses;");
//            sb.AppendLine();
//            sb.AppendLine("namespace Generated.Services");
//            sb.AppendLine("{");
//            sb.AppendLine($"    public interface I{table}Service");
//            sb.AppendLine("    {");
//            sb.AppendLine($"        Task<IEnumerable<{table}Response>> GetAllAsync();");
//            sb.AppendLine($"        Task<{table}Response?> GetByIdAsync(int id);");
//            sb.AppendLine($"        Task CreateAsync({table}Request request);");
//            sb.AppendLine($"        Task UpdateAsync(int id, {table}Request request);");
//            sb.AppendLine("        Task DeleteAsync(int id);");
//            sb.AppendLine("    }");
//            sb.AppendLine("}");
//            WriteToFile(Path.Combine(outputDir, "Services", $"I{table}Service.cs"), sb.ToString());
//        }
//        #endregion

//        #region Service Implementation
//        public static void GenerateServiceImpl(string table, List<(string Name, string Type, bool IsNullable)> cols, List<ForeignKeyInfo> fks, string outputDir)
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("using System.Collections.Generic;");
//            sb.AppendLine("using System.Linq;");
//            sb.AppendLine("using System.Threading.Tasks;");
//            sb.AppendLine("using Generated.Entities;");
//            sb.AppendLine("using Generated.Requests;");
//            sb.AppendLine("using Generated.Responses;");
//            sb.AppendLine("using Generated.Repositories;");
//            sb.AppendLine();
//            sb.AppendLine("namespace Generated.Services");
//            sb.AppendLine("{");
//            sb.AppendLine($"    public class {table}Service : I{table}Service");
//            sb.AppendLine("    {");
//            sb.AppendLine($"        private readonly I{table}Repository _repo;");
//            sb.AppendLine($"        public {table}Service(I{table}Repository repo) => _repo = repo;");
//            sb.AppendLine();

//            sb.AppendLine($"        public async Task<IEnumerable<{table}Response>> GetAllAsync()");
//            sb.AppendLine("        {");
//            sb.AppendLine($"            var entities = await _repo.GetAllAsync();");
//            sb.AppendLine($"            return entities.Select(e => new {table}Response");
//            sb.AppendLine("            {");
//            foreach (var col in cols)
//                sb.AppendLine($"                {col.Name} = e.{col.Name},");
//            sb.AppendLine("            });");
//            sb.AppendLine("        }");

//            sb.AppendLine($"        public async Task<{table}Response?> GetByIdAsync(int id)");
//            sb.AppendLine("        {");
//            sb.AppendLine("            var e = await _repo.GetByIdAsync(id);");
//            sb.AppendLine("            if(e == null) return null;");
//            sb.AppendLine($"            return new {table}Response");
//            sb.AppendLine("            {");
//            foreach (var col in cols)
//                sb.AppendLine($"                {col.Name} = e.{col.Name},");
//            sb.AppendLine("            };");
//            sb.AppendLine("        }");

//            sb.AppendLine($"        public async Task CreateAsync({table}Request request)");
//            sb.AppendLine("        {");
//            sb.AppendLine($"            var entity = new {table}Entity");
//            sb.AppendLine("            {");
//            foreach (var col in cols)
//                sb.AppendLine($"                {col.Name} = request.{col.Name},");
//            sb.AppendLine("            };");
//            sb.AppendLine("            await _repo.AddAsync(entity);");
//            sb.AppendLine("        }");

//            sb.AppendLine($"        public async Task UpdateAsync(int id, {table}Request request)");
//            sb.AppendLine("        {");
//            sb.AppendLine($"            var entity = new {table}Entity");
//            sb.AppendLine("            {");
//            foreach (var col in cols)
//                sb.AppendLine($"                {col.Name} = request.{col.Name},");
//            sb.AppendLine("            };");
//            sb.AppendLine("            await _repo.UpdateAsync(entity);");
//            sb.AppendLine("        }");

//            sb.AppendLine($"        public async Task DeleteAsync(int id) => await _repo.DeleteAsync(id);");

//            sb.AppendLine("    }");
//            sb.AppendLine("}");
//            WriteToFile(Path.Combine(outputDir, "Services", $"{table}Service.cs"), sb.ToString());
//        }
//        #endregion

//        #region Controller
//        public static void GenerateController(string table, List<(string Name, string Type, bool IsNullable)> cols, string outputDir)
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
//            sb.AppendLine("using System.Collections.Generic;");
//            sb.AppendLine("using System.Threading.Tasks;");
//            sb.AppendLine("using Generated.Requests;");
//            sb.AppendLine("using Generated.Responses;");
//            sb.AppendLine("using Generated.Services;");
//            sb.AppendLine();
//            sb.AppendLine("namespace Generated.Controllers");
//            sb.AppendLine("{");
//            sb.AppendLine($"    [ApiController]");
//            sb.AppendLine($"    [Route(\"api/[controller]\")]");
//            sb.AppendLine($"    public class {table}Controller : ControllerBase");
//            sb.AppendLine("    {");
//            sb.AppendLine($"        private readonly I{table}Service _service;");
//            sb.AppendLine($"        public {table}Controller(I{table}Service service) => _service = service;");
//            sb.AppendLine();
//            sb.AppendLine("        [HttpGet]");
//            sb.AppendLine($"        public async Task<IEnumerable<{table}Response>> GetAll() => await _service.GetAllAsync();");
//            sb.AppendLine();
//            sb.AppendLine("        [HttpGet(\"{id}\")]");
//            sb.AppendLine($"        public async Task<{table}Response?> GetById(int id) => await _service.GetByIdAsync(id);");
//            sb.AppendLine();
//            sb.AppendLine("        [HttpPost]");
//            sb.AppendLine($"        public async Task<IActionResult> Create([FromBody] {table}Request request)");
//            sb.AppendLine("        { await _service.CreateAsync(request); return Ok(); }");
//            sb.AppendLine();
//            sb.AppendLine("        [HttpPut(\"{id}\")]");
//            sb.AppendLine($"        public async Task<IActionResult> Update(int id, [FromBody] {table}Request request)");
//            sb.AppendLine("        { await _service.UpdateAsync(id, request); return Ok(); }");
//            sb.AppendLine();
//            sb.AppendLine("        [HttpDelete(\"{id}\")]");
//            sb.AppendLine("        public async Task<IActionResult> Delete(int id) { await _service.DeleteAsync(id); return Ok(); }");
//            sb.AppendLine("    }");
//            sb.AppendLine("}");
//            WriteToFile(Path.Combine(outputDir, "Controllers", $"{table}Controller.cs"), sb.ToString());
//        }
//        #endregion
//        #region Utilities
//        private static void WriteToFile(string path, string content)
//        {
//            var dir = Path.GetDirectoryName(path);
//            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
//            File.WriteAllText(path, content);
//        }

//        private static string SqlToCSharp(string sqlType, bool isNullable)
//        {
//            string type = sqlType switch
//            {
//                "int" => "int",
//                "bigint" => "long",
//                "smallint" => "short",
//                "tinyint" => "byte",
//                "bit" => "bool",
//                "float" => "double",
//                "real" => "float",
//                "decimal" or "numeric" => "decimal",
//                "money" or "smallmoney" => "decimal",
//                "datetime" or "smalldatetime" => "DateTime",
//                "date" => "DateTime",
//                "time" => "TimeSpan",
//                "char" or "varchar" or "text" or "nchar" or "nvarchar" or "ntext" => "string",
//                "uniqueidentifier" => "Guid",
//                "binary" or "varbinary" or "image" => "byte[]",
//                _ => "string"
//            };

//            // Nullable cho kiểu giá trị (value types)
//            if (isNullable && type != "string" && type != "byte[]")
//                type += "?";

//            return type;
//        }
//        #endregion
//    }
//}

