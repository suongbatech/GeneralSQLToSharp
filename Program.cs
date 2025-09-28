using System;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace GeneralSqlToCSharp
{
    class Program
    {
        static string connectionString = "Server=127.0.0.1;Database=MinaCloudAdmin;User Id=sa;Password=1;TrustServerCertificate=True;";
        static string outputPath = @"D:\\Temp\\Code";

        static void Main(string[] args)
        {
            // Create folder structure
            var folders = new[] { "Entities", "Requests", "Responses", "IRepositories", "Repositories", "IServices", "Services", "Controllers", "MappingProfiles", "Data" };
            foreach (var f in folders)
                Directory.CreateDirectory(Path.Combine(outputPath, f));

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var tables = GetTables(connection);
            foreach (var table in tables)
            {
                var columns = GetColumns(connection, table);

                WriteToFile(Path.Combine(outputPath, "Entities", table + ".cs"), GenerateEntity(table, columns));
                WriteToFile(Path.Combine(outputPath, "Requests", table + "Request.cs"), GenerateRequest(table, columns));
                WriteToFile(Path.Combine(outputPath, "Responses", table + "Response.cs"), GenerateResponse(table, columns));
                WriteToFile(Path.Combine(outputPath, "IRepositories", "I" + table + "Repository.cs"), GenerateRepositoryInterface(table));
                WriteToFile(Path.Combine(outputPath, "Repositories", table + "Repository.cs"), GenerateRepositoryImpl(table, columns));
                WriteToFile(Path.Combine(outputPath, "IServices", "I" + table + "Service.cs"), GenerateServiceInterface(table));
                WriteToFile(Path.Combine(outputPath, "Services", table + "Service.cs"), GenerateServiceImpl(table, columns));
                WriteToFile(Path.Combine(outputPath, "Controllers", table + "Controller.cs"), GenerateController(table, columns));
                WriteToFile(Path.Combine(outputPath, "MappingProfiles", table + "Profile.cs"), GenerateMappingProfile(table, columns));
            }

            // BaseContext + UnitOfWork
            WriteToFile(Path.Combine(outputPath, "Data", "BaseContext.cs"), GenerateBaseContext(tables));
            WriteToFile(Path.Combine(outputPath, "Data", "IUnitOfWork.cs"), GenerateIUnitOfWork());
            WriteToFile(Path.Combine(outputPath, "Data", "UnitOfWork.cs"), GenerateUnitOfWork());

            // csproj
            WriteToFile(Path.Combine(outputPath, "GeneralSqlToCSharp.csproj"), GenerateCsProj());

            Console.WriteLine("✅ Done generating all files!");
        }

        // =================== Helpers ===================
        static List<string> GetTables(SqlConnection connection)
        {
            var tables = new List<string>();
            using var cmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) tables.Add(reader.GetString(0));
            return tables;
        }

        static List<(string Name, string Type)> GetColumns(SqlConnection connection, string table)
        {
            var columns = new List<(string, string)>();
            using var cmd = new SqlCommand(
                "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@Table", connection);
            cmd.Parameters.AddWithValue("@Table", table);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                columns.Add((reader.GetString(0), SqlTypeToCSharp(reader.GetString(1))));
            }
            return columns;
        }

        static string SqlTypeToCSharp(string sqlType) => sqlType switch
        {
            "int" => "int",
            "bigint" => "long",
            "uniqueidentifier" => "Guid",
            "nvarchar" or "varchar" or "text" => "string",
            "datetime" or "smalldatetime" or "date" => "DateTime",
            "bit" => "bool",
            "decimal" or "money" => "decimal",
            _ => "string"
        };

        static void WriteToFile(string path, string content)
        {
            File.WriteAllText(path, content);
            Console.WriteLine($"Generated: {path}");
        }

        // =================== Generators ===================
        static string GenerateEntity(string table, List<(string Name, string Type)> columns)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace Entities");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {table}");
            sb.AppendLine("    {");
            foreach (var col in columns)
                sb.AppendLine($"        public {col.Type} {col.Name} {{ get; set; }}");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        static string GenerateRequest(string table, List<(string Name, string Type)> columns)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace Requests");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {table}Request");
            sb.AppendLine("    {");
            foreach (var col in columns)
                sb.AppendLine($"        public {col.Type} {col.Name} {{ get; set; }}");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        static string GenerateResponse(string table, List<(string Name, string Type)> columns)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace Responses");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {table}Response");
            sb.AppendLine("    {");
            foreach (var col in columns)
                sb.AppendLine($"        public {col.Type} {col.Name} {{ get; set; }}");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        static string GenerateRepositoryInterface(string table)
        {
            return $@"
using Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IRepositories
{{
    public interface I{table}Repository
    {{
        Task<{table}> GetByIdAsync(Guid id);
        Task<IEnumerable<{table}>> GetAllAsync();
        Task<{table}> AddAsync({table} entity);
        Task UpdateAsync({table} entity);
        Task DeleteAsync(Guid id);
    }}
}}";
        }

        static string GenerateRepositoryImpl(string table, List<(string Name, string Type)> columns)
        {
            return $@"
using Entities;
using IRepositories;
using Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{{
    public class {table}Repository : I{table}Repository
    {{
        private readonly BaseContext _context;
        public {table}Repository(BaseContext context) => _context = context;

        public async Task<{table}> GetByIdAsync(Guid id) => await _context.Set<{table}>().FindAsync(id);
        public async Task<IEnumerable<{table}>> GetAllAsync() => await Task.FromResult(_context.Set<{table}>());
        public async Task<{table}> AddAsync({table} entity)
        {{
            _context.Set<{table}>().Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }}
        public async Task UpdateAsync({table} entity)
        {{
            _context.Set<{table}>().Update(entity);
            await _context.SaveChangesAsync();
        }}
        public async Task DeleteAsync(Guid id)
        {{
            var entity = await GetByIdAsync(id);
            if(entity != null)
            {{
                _context.Set<{table}>().Remove(entity);
                await _context.SaveChangesAsync();
            }}
        }}
    }}
}}";
        }

        static string GenerateServiceInterface(string table)
        {
            return $@"
using Requests;
using Responses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IServices
{{
    public interface I{table}Service
    {{
        Task<{table}Response> GetByIdAsync(Guid id);
        Task<IEnumerable<{table}Response>> GetAllAsync();
        Task<{table}Response> CreateAsync({table}Request request);
        Task UpdateAsync(Guid id, {table}Request request);
        Task DeleteAsync(Guid id);
    }}
}}";
        }

        static string GenerateServiceImpl(string table, List<(string Name, string Type)> columns)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using Entities;");
            sb.AppendLine("using IServices;");
            sb.AppendLine("using IRepositories;");
            sb.AppendLine("using Requests;");
            sb.AppendLine("using Responses;");
            sb.AppendLine("using Data;");
            sb.AppendLine("using AutoMapper;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine($"namespace Services {{");
            sb.AppendLine($"    public class {table}Service : I{table}Service {{");
            sb.AppendLine($"        private readonly I{table}Repository _repo;");
            sb.AppendLine($"        private readonly IUnitOfWork _uow;");
            sb.AppendLine($"        private readonly IMapper _mapper;");
            sb.AppendLine($"        public {table}Service(I{table}Repository repo, IUnitOfWork uow, IMapper mapper) => (_repo, _uow, _mapper) = (repo, uow, mapper);");

            sb.AppendLine($@"
        public async Task<{table}Response> GetByIdAsync(Guid id)
        {{
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<{table}Response>(entity);
        }}

        public async Task<IEnumerable<{table}Response>> GetAllAsync()
        {{
            var list = await _repo.GetAllAsync();
            return list.Select(_mapper.Map<{table}Response>);
        }}

        public async Task<{table}Response> CreateAsync({table}Request request)
        {{
            var entity = _mapper.Map<{table}>(request);
            await _repo.AddAsync(entity);
            await _uow.SaveChangesAsync();
            return _mapper.Map<{table}Response>(entity);
        }}

        public async Task UpdateAsync(Guid id, {table}Request request)
        {{
            var entity = await _repo.GetByIdAsync(id);
            if(entity == null) throw new Exception(""Not found"");
            _mapper.Map(request, entity);
            await _repo.UpdateAsync(entity);
            await _uow.SaveChangesAsync();
        }}

        public async Task DeleteAsync(Guid id)
        {{
            await _repo.DeleteAsync(id);
            await _uow.SaveChangesAsync();
        }}
    }} }}");
            return sb.ToString();
        }

        static string GenerateController(string table, List<(string Name, string Type)> columns)
        {
            return $@"
using Microsoft.AspNetCore.Mvc;
using IServices;
using Requests;
using Responses;
using System;
using System.Threading.Tasks;

namespace Controllers
{{
    [ApiController]
    [Route(""api/[controller]"")]
    public class {table}Controller : ControllerBase
    {{
        private readonly I{table}Service _service;
        public {table}Controller(I{table}Service service) => _service = service;

        [HttpGet(""{{id}}"")]
        public async Task<IActionResult> Get(Guid id) => Ok(await _service.GetByIdAsync(id));

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpPost]
        public async Task<IActionResult> Create({table}Request request) => Ok(await _service.CreateAsync(request));

        [HttpPut(""{{id}}"")]
        public async Task<IActionResult> Update(Guid id, {table}Request request)
        {{
            await _service.UpdateAsync(id, request);
            return Ok();
        }}

        [HttpDelete(""{{id}}"")]
        public async Task<IActionResult> Delete(Guid id)
        {{
            await _service.DeleteAsync(id);
            return Ok();
        }}
    }}
}}";
        }

        static string GenerateMappingProfile(string table, List<(string Name, string Type)> columns)
        {
            return $@"
using AutoMapper;
using Entities;
using Requests;
using Responses;

namespace MappingProfiles
{{
    public class {table}Profile : Profile
    {{
        public {table}Profile()
        {{
            CreateMap<{table}Request, {table}>();
            CreateMap<{table}, {table}Response>();
        }}
    }}
}}";
        }

        static string GenerateBaseContext(List<string> tables)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine("using Entities;");
            sb.AppendLine("namespace Data {");
            sb.AppendLine("    public class BaseContext : DbContext {");
            sb.AppendLine("        public BaseContext(DbContextOptions<BaseContext> options) : base(options) { }");
            foreach (var t in tables)
                sb.AppendLine($"        public DbSet<{t}> {t}Set {{ get; set; }}");
            sb.AppendLine("    } }");
            return sb.ToString();
        }

        static string GenerateIUnitOfWork() =>
@"using System.Threading.Tasks;
namespace Data { public interface IUnitOfWork { Task<int> SaveChangesAsync(); } }";

        static string GenerateUnitOfWork() =>
@"using System.Threading.Tasks;
namespace Data { public class UnitOfWork : IUnitOfWork { private readonly BaseContext _context; public UnitOfWork(BaseContext context) => _context = context; public Task<int> SaveChangesAsync() => _context.SaveChangesAsync(); } }";

        static string GenerateCsProj() =>
@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.EntityFrameworkCore"" Version=""6.0.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""6.0.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Tools"" Version=""6.0.0"" />
    <PackageReference Include=""AutoMapper"" Version=""12.0.0"" />
    <PackageReference Include=""AutoMapper.Extensions.Microsoft.DependencyInjection"" Version=""12.0.0"" />
  </ItemGroup>
</Project>";
    }
}
