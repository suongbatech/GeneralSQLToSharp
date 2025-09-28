using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace SqlToCSharpGenerator
{
    class Program
    {
        static string ConnectionString = "Server=127.0.0.1;Database=MinaCloudAdmin;User Id=sa;Password=1;TrustServerCertificate=True;";
        static string OutputPath = @"D:\Temp\GeneratedCode";

        static void Main(string[] args)
        {
            // Create folder structure
            var folders = new[] {
                "Entities","Requests","Responses","IRepositories","Repositories",
                "IServices","Services","Controllers","MappingProfiles","Data"
            };
            foreach (var f in folders)
                Directory.CreateDirectory(Path.Combine(OutputPath, f));

            using var connection = new SqlConnection(ConnectionString);
            connection.Open();

            var tables = GetTables(connection);
            foreach (var table in tables)
            {
                var columns = GetColumns(connection, table);

                WriteFile("Entities", table + ".cs", GenerateEntity(table, columns));
                WriteFile("Requests", table + "Request.cs", GenerateRequest(table, columns));
                WriteFile("Responses", table + "Response.cs", GenerateResponse(table, columns));
                WriteFile("IRepositories", "I" + table + "Repository.cs", GenerateRepositoryInterface(table));
                WriteFile("Repositories", table + "Repository.cs", GenerateRepositoryImpl(table));
                WriteFile("IServices", "I" + table + "Service.cs", GenerateServiceInterface(table));
                WriteFile("Services", table + "Service.cs", GenerateServiceImpl(table));
                WriteFile("Controllers", table + "Controller.cs", GenerateController(table));
                WriteFile("MappingProfiles", table + "Profile.cs", GenerateMappingProfile(table));
            }

            // BaseContext & UnitOfWork
            WriteFile("Data", "BaseContext.cs", GenerateBaseContext(tables));
            WriteFile("Data", "IUnitOfWork.cs", GenerateIUnitOfWork());
            WriteFile("Data", "UnitOfWork.cs", GenerateUnitOfWork());

            // Program.cs
            WriteFile("", "Program.cs", GenerateProgramCs(tables));

            // csproj & appsettings.json
            WriteFile("", "GeneratedApi.csproj", GenerateCsProj());
            WriteFile("", "appsettings.json", GenerateAppSettings());

            Console.WriteLine("✅ Done generating all files!");
        }

        #region Helpers
        static List<string> GetTables(SqlConnection conn)
        {
            var tables = new List<string>();
            using var cmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) tables.Add(reader.GetString(0));
            return tables;
        }

        static List<(string Name, string Type)> GetColumns(SqlConnection conn, string table)
        {
            var columns = new List<(string, string)>();
            using var cmd = new SqlCommand("SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@Table", conn);
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

        static void WriteFile(string folder, string fileName, string content)
        {
            var path = string.IsNullOrEmpty(folder) ? Path.Combine(OutputPath, fileName) : Path.Combine(OutputPath, folder, fileName);
            File.WriteAllText(path, content);
            Console.WriteLine($"Generated: {path}");
        }
        #endregion

        #region Generators
        static string GenerateEntity(string table, List<(string Name, string Type)> columns)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace Entities {");
            sb.AppendLine($"    public class {table} {{");
            foreach (var c in columns)
                sb.AppendLine($"        public {c.Type} {PascalCase(c.Name)} {{ get; set; }}");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        static string GenerateRequest(string table, List<(string Name, string Type)> columns)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace Requests {");
            sb.AppendLine($"    public class {table}Request {{");
            foreach (var c in columns)
                sb.AppendLine($"        public {c.Type} {PascalCase(c.Name)} {{ get; set; }}");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        static string GenerateResponse(string table, List<(string Name, string Type)> columns)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace Responses {");
            sb.AppendLine($"    public class {table}Response {{");
            foreach (var c in columns)
                sb.AppendLine($"        public {c.Type} {PascalCase(c.Name)} {{ get; set; }}");
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

        static string GenerateRepositoryImpl(string table)
        {
            return $@"
using Entities;
using IRepositories;
using Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Repositories
{{
    public class {table}Repository : I{table}Repository
    {{
        private readonly BaseContext _context;
        private readonly DbSet<{table}> _db;

        public {table}Repository(BaseContext context)
        {{
            _context = context;
            _db = _context.Set<{table}>();
        }}

        public async Task<{table}> GetByIdAsync(Guid id) => await _db.FindAsync(id);
        public async Task<IEnumerable<{table}>> GetAllAsync() => await _db.ToListAsync();
        public async Task<{table}> AddAsync({table} entity) {{ _db.Add(entity); await _context.SaveChangesAsync(); return entity; }}
        public async Task UpdateAsync({table} entity) {{ _db.Update(entity); await _context.SaveChangesAsync(); }}
        public async Task DeleteAsync(Guid id)
        {{
            var entity = await GetByIdAsync(id);
            if(entity != null) {{ _db.Remove(entity); await _context.SaveChangesAsync(); }}
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

        static string GenerateServiceImpl(string table)
        {
            return $@"
using Entities;
using IServices;
using IRepositories;
using Requests;
using Responses;
using Data;
using AutoMapper;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{{
    public class {table}Service : I{table}Service
    {{
        private readonly I{table}Repository _repo;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public {table}Service(I{table}Repository repo, IUnitOfWork uow, IMapper mapper)
        {{
            _repo = repo; _uow = uow; _mapper = mapper;
        }}

        public async Task<{table}Response> GetByIdAsync(Guid id)
        {{
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<{table}Response>(entity);
        }}

        public async Task<IEnumerable<{table}Response>> GetAllAsync()
        {{
            var list = await _repo.GetAllAsync();
            return list.Select(x => _mapper.Map<{table}Response>(x));
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
    }}
}}";
        }

        static string GenerateController(string table)
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

        public {table}Controller(I{table}Service service) {{ _service = service; }}

        [HttpGet(""{{id}}"")]
        public async Task<IActionResult> Get(Guid id) => Ok(await _service.GetByIdAsync(id));

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] {table}Request req) => Ok(await _service.CreateAsync(req));

        [HttpPut(""{{id}}"")]
        public async Task<IActionResult> Update(Guid id, [FromBody] {table}Request req) {{ await _service.UpdateAsync(id, req); return Ok(); }}

        [HttpDelete(""{{id}}"")]
        public async Task<IActionResult> Delete(Guid id) {{ await _service.DeleteAsync(id); return Ok(); }}
    }}
}}";
        }

        static string GenerateMappingProfile(string table)
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

        static string GenerateProgramCs(List<string> tables)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine("using Microsoft.OpenApi.Models;");
            sb.AppendLine("using Data;");
            sb.AppendLine("using IRepositories;");
            sb.AppendLine("using Repositories;");
            sb.AppendLine("using IServices;");
            sb.AppendLine("using Services;");
            sb.AppendLine("using AutoMapper;");
            sb.AppendLine("var builder = WebApplication.CreateBuilder(args);");
            sb.AppendLine("builder.Services.AddControllers();");
            sb.AppendLine("builder.Services.AddEndpointsApiExplorer();");
            sb.AppendLine("builder.Services.AddSwaggerGen(c => { c.SwaggerDoc(\"v1\", new OpenApiInfo { Title = \"Generated API\", Version = \"v1\" }); });");
            sb.AppendLine("builder.Services.AddDbContext<BaseContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString(\"DefaultConnection\")));");
            sb.AppendLine("builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();");
            foreach (var t in tables)
            {
                sb.AppendLine($"builder.Services.AddScoped<I{t}Repository, {t}Repository>();");
                sb.AppendLine($"builder.Services.AddScoped<I{t}Service, {t}Service>();");
            }
            sb.AppendLine("builder.Services.AddAutoMapper(typeof(Program));");
            sb.AppendLine("var app = builder.Build();");
            sb.AppendLine("if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }");
            sb.AppendLine("app.UseHttpsRedirection();");
            sb.AppendLine("app.UseAuthorization();");
            sb.AppendLine("app.MapControllers();");
            sb.AppendLine("app.Run();");
            return sb.ToString();
        }

        static string GenerateCsProj()
        {
            return @"<Project Sdk=""Microsoft.NET.Sdk.Web"">
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
    <PackageReference Include=""Swashbuckle.AspNetCore"" Version=""6.5.0"" />
  </ItemGroup>
</Project>";
        }

        static string GenerateAppSettings()
        {
            return @"{
  ""ConnectionStrings"": {
    ""DefaultConnection"": ""Server=127.0.0.1;Database=MinaCloudAdmin;User Id=sa;Password=1;TrustServerCertificate=True;""
  },
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }
  },
  ""AllowedHosts"": ""*""
}";
        }

        static string PascalCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return char.ToUpper(name[0]) + name.Substring(1);
        }
        #endregion
    }
}
