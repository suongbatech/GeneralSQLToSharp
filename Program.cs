using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace GeneralSQLToSharp
{
    #region Helper Models
    public class ColumnInfo
    {
        public string Name { get; set; } = "";
        public string DataType { get; set; } = "";
        public bool IsNullable { get; set; }
    }

    public class ForeignKeyInfo
    {
        public string ColumnName { get; set; } = "";
        public string ReferencedTable { get; set; } = "";
        public string ReferencedColumn { get; set; } = "";
    }
    #endregion

    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Server=127.0.0.1;Database=MinaCloudAdmin;User Id=sa;Password=1;TrustServerCertificate=True;";
            string outputPath = @"D:\Temp\Code\";

            Directory.CreateDirectory(outputPath);

            // csproj
            File.WriteAllText(Path.Combine(outputPath, "GeneralCode.csproj"), @"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.EntityFrameworkCore"" Version=""8.0.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""8.0.0"" />
    <PackageReference Include=""Microsoft.Data.SqlClient"" Version=""6.0.0"" />
    <PackageReference Include=""Microsoft.AspNetCore.OpenApi"" Version=""6.0.14"" />
  </ItemGroup>
</Project>");

            // folders
            string[] folders = { "Entities", "Requests", "Responses", "IRepositories", "Repositories", "IServices", "Services", "Controllers", "Data" };
            foreach (var f in folders) Directory.CreateDirectory(Path.Combine(outputPath, f));

            var tables = GetTables(connectionString);

            foreach (var table in tables)
            {
                var columns = GetColumns(connectionString, table);
                var fks = GetForeignKeys(connectionString, table);

                WriteToFile(Path.Combine(outputPath, "Entities", table + ".cs"), GenerateEntity(table, columns, fks));
                WriteToFile(Path.Combine(outputPath, "Requests", table + "Request.cs"), GenerateRequest(table, columns));
                WriteToFile(Path.Combine(outputPath, "Responses", table + "Response.cs"), GenerateResponse(table, columns));
                WriteToFile(Path.Combine(outputPath, "IRepositories", "I" + table + "Repository.cs"), GenerateRepositoryInterface(table));
                WriteToFile(Path.Combine(outputPath, "Repositories", table + "Repository.cs"), GenerateRepositoryImpl(table));
                WriteToFile(Path.Combine(outputPath, "IServices", "I" + table + "Service.cs"), GenerateServiceInterface(table));
                WriteToFile(Path.Combine(outputPath, "Services", table + "Service.cs"), GenerateServiceImpl(table, columns));
                WriteToFile(Path.Combine(outputPath, "Controllers", table + "Controller.cs"), GenerateController(table));
            }

            // DbContext + UoW
            GenerateDbContextAndUnitOfWork(outputPath, tables);

            // Program.cs
            WriteToFile(Path.Combine(outputPath, "Program.cs"), GenerateProgram(tables));

            Console.WriteLine("✅ Code generation completed!");
        }

        #region SQL Helpers
        private static List<string> GetTables(string connectionString)
        {
            var tables = new List<string>();
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            var cmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) tables.Add(reader.GetString(0));
            return tables;
        }

        private static List<ColumnInfo> GetColumns(string connectionString, string table)
        {
            var columns = new List<ColumnInfo>();
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            var cmd = new SqlCommand($@"
                SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME='{table}'", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                columns.Add(new ColumnInfo
                {
                    Name = reader.GetString(0),
                    DataType = reader.GetString(1),
                    IsNullable = reader.GetString(2) == "YES"
                });
            }
            return columns;
        }

        private static List<ForeignKeyInfo> GetForeignKeys(string connectionString, string table)
        {
            var fks = new List<ForeignKeyInfo>();
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            string query = $@"
SELECT 
    k.COLUMN_NAME, 
    k2.TABLE_NAME AS REFERENCED_TABLE_NAME,
    k2.COLUMN_NAME AS REFERENCED_COLUMN_NAME
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE k ON k.CONSTRAINT_NAME = rc.CONSTRAINT_NAME
JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE k2 ON k2.CONSTRAINT_NAME = rc.UNIQUE_CONSTRAINT_NAME
WHERE k.TABLE_NAME='{table}'";
            using var cmd = new SqlCommand(query, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                fks.Add(new ForeignKeyInfo
                {
                    ColumnName = reader.GetString(0),
                    ReferencedTable = reader.GetString(1),
                    ReferencedColumn = reader.GetString(2)
                });
            }
            return fks;
        }
        #endregion

        #region Generate Files
        private static void WriteToFile(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }

        private static string MapToCSharpType(string sqlType, bool isNullable)
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
                "float" => "double",
                "real" => "float",
                "date" => "DateTime",
                "datetime" => "DateTime",
                "datetime2" => "DateTime",
                "nvarchar" => "string",
                "varchar" => "string",
                "text" => "string",
                _ => "string"
            };
            if (isNullable && type != "string")
                type += "?";
            return type;
        }

        private static string GenerateEntity(string table, List<ColumnInfo> columns, List<ForeignKeyInfo> fks)
        {
            var props = string.Join(Environment.NewLine, columns.Select(c =>
                $"        public {MapToCSharpType(c.DataType, c.IsNullable)} {c.Name} {{ get; set; }}"));
            return $@"namespace Entities
{{
    public class {table}
    {{
{props}
    }}
}}";
        }

        private static string GenerateRequest(string table, List<ColumnInfo> columns)
        {
            var props = string.Join(Environment.NewLine, columns.Select(c =>
                $"        public {MapToCSharpType(c.DataType, c.IsNullable)} {c.Name} {{ get; set; }}"));
            return $@"namespace Requests
{{
    public class {table}Request
    {{
{props}
    }}
}}";
        }

        private static string GenerateResponse(string table, List<ColumnInfo> columns)
        {
            var props = string.Join(Environment.NewLine, columns.Select(c =>
                $"        public {MapToCSharpType(c.DataType, c.IsNullable)} {c.Name} {{ get; set; }}"));
            return $@"namespace Responses
{{
    public class {table}Response
    {{
{props}
    }}
}}";
        }

        private static string GenerateRepositoryInterface(string table)
        {
            return $@"using Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IRepositories
{{
    public interface I{table}Repository
    {{
        Task<{table}> CreateAsync({table} entity);
        Task<{table}> UpdateAsync({table} entity);
        Task DeleteAsync({table} entity);
        Task<{table}?> GetByIdAsync(object id);
        Task<List<{table}>> GetListAsync(int page, int pageSize);
    }}
}}";
        }

        private static string GenerateRepositoryImpl(string table)
        {
            return $@"using Entities;
using Microsoft.EntityFrameworkCore;
using Data;
using IRepositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories
{{
    public class {table}Repository : I{table}Repository
    {{
        private readonly BaseContext _context;
        private readonly DbSet<{table}> _dbSet;

        public {table}Repository(BaseContext context)
        {{
            _context = context;
            _dbSet = _context.Set<{table}>();
        }}

        public async Task<{table}> CreateAsync({table} entity)
        {{
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }}

        public async Task<{table}> UpdateAsync({table} entity)
        {{
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }}

        public async Task DeleteAsync({table} entity)
        {{
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }}

        public async Task<{table}?> GetByIdAsync(object id)
        {{
            return await _dbSet.FindAsync(id);
        }}

        public async Task<List<{table}>> GetListAsync(int page, int pageSize)
        {{
            return await _dbSet.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }}
    }}
}}";
        }

        private static string GenerateServiceInterface(string table)
        {
            return $@"using Requests;
using Responses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IServices
{{
    public interface I{table}Service
    {{
        Task<{table}Response> CreateAsync({table}Request request);
        Task<{table}Response?> UpdateAsync(int id, {table}Request request);
        Task DeleteAsync(int id);
        Task<{table}Response?> GetByIdAsync(int id);
        Task<List<{table}Response>> GetListAsync(int page, int pageSize);
    }}
}}";
        }

        private static string GenerateServiceImpl(string table, List<ColumnInfo> columns)
        {
            var mapToEntity = string.Join(Environment.NewLine, columns.Select(c =>
                $"            entity.{c.Name} = request.{c.Name};"));
            var mapToResponse = string.Join(Environment.NewLine, columns.Select(c =>
                $"                {c.Name} = entity.{c.Name},"));
            return $@"using Entities;
using Requests;
using Responses;
using IRepositories;
using IServices;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{{
    public class {table}Service : I{table}Service
    {{
        private readonly I{table}Repository _repository;

        public {table}Service(I{table}Repository repository)
        {{
            _repository = repository;
        }}

        public async Task<{table}Response> CreateAsync({table}Request request)
        {{
            var entity = new {table}();
{mapToEntity}
            entity = await _repository.CreateAsync(entity);
            return new {table}Response
            {{
{mapToResponse}
            }};
        }}

        public async Task<{table}Response?> UpdateAsync(int id, {table}Request request)
        {{
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;
{mapToEntity}
            entity = await _repository.UpdateAsync(entity);
            return new {table}Response
            {{
{mapToResponse}
            }};
        }}

        public async Task DeleteAsync(int id)
        {{
            var entity = await _repository.GetByIdAsync(id);
            if (entity != null) await _repository.DeleteAsync(entity);
        }}

        public async Task<{table}Response?> GetByIdAsync(int id)
        {{
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;
            return new {table}Response
            {{
{mapToResponse}
            }};
        }}

        public async Task<List<{table}Response>> GetListAsync(int page, int pageSize)
        {{
            var list = await _repository.GetListAsync(page, pageSize);
            return list.Select(entity => new {table}Response
            {{
{mapToResponse}
            }}).ToList();
        }}
    }}
}}";
        }

        private static string GenerateController(string table)
        {
            return $@"using Microsoft.AspNetCore.Mvc;
using Requests;
using IServices;
using System.Threading.Tasks;

namespace Controllers
{{
    [ApiController]
    [Route(""api/[controller]"")]
    public class {table}Controller : ControllerBase
    {{
        private readonly I{table}Service _service;

        public {table}Controller(I{table}Service service)
        {{
            _service = service;
        }}

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] {table}Request request)
        {{
            var response = await _service.CreateAsync(request);
            return Ok(response);
        }}

        [HttpPut(""{{
id}}"")]
        public async Task<IActionResult> Update(int id, [FromBody] {table}Request request)
        {{
            var response = await _service.UpdateAsync(id, request);
            return Ok(response);
        }}

        [HttpDelete(""{{
id}}"")]
        public async Task<IActionResult> Delete(int id)
        {{
            await _service.DeleteAsync(id);
            return NoContent();
        }}

        [HttpGet(""{{
id}}"")]
        public async Task<IActionResult> GetById(int id)
        {{
            var response = await _service.GetByIdAsync(id);
            return Ok(response);
        }}

        [HttpGet]
        public async Task<IActionResult> GetList(int page = 1, int pageSize = 20)
        {{
            var response = await _service.GetListAsync(page, pageSize);
            return Ok(response);
        }}
    }}
}}";
        }

        private static void GenerateDbContextAndUnitOfWork(string outputPath, List<string> tables)
        {
            string dataPath = Path.Combine(outputPath, "Data");
            Directory.CreateDirectory(dataPath);

            var dbContextContent = @"using Microsoft.EntityFrameworkCore;
using Entities;

namespace Data
{
    public class BaseContext : DbContext, IUnitOfWork
    {
        public BaseContext(DbContextOptions<BaseContext> options) : base(options) { }

" + string.Join("\n", tables.Select(t => $"        public DbSet<{t}> {t}s {{ get; set; }}")) + @"

        public async Task<int> SaveChangesAsync()
        {
            return await base.SaveChangesAsync();
        }
    }
}";
            File.WriteAllText(Path.Combine(dataPath, "BaseContext.cs"), dbContextContent);

            var iUnitOfWorkContent = @"using System;
using System.Threading.Tasks;

namespace Data
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync();
    }
}";
            File.WriteAllText(Path.Combine(dataPath, "IUnitOfWork.cs"), iUnitOfWorkContent);
        }

        private static string GenerateProgram(List<string> tables)
        {
            var registrations = string.Join(Environment.NewLine, tables.Select(t =>
                $"builder.Services.AddScoped<I{t}Repository, {t}Repository>();\n    builder.Services.AddScoped<I{t}Service, {t}Service>();"));
            return $@"using Microsoft.EntityFrameworkCore;
using Data;
using IRepositories;
using Repositories;
using IServices;
using Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BaseContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString(""DefaultConnection"")));

{registrations}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{{
    app.UseSwagger();
    app.UseSwaggerUI();
}}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();";
        }
        #endregion
    }
}
