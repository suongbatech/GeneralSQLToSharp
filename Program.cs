using System;
using System.IO;
using System.Text;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;

namespace SqlToCSharpFullGenerator
{
    class Program
    {
        static string ConnectionString = "Server=127.0.0.1;Database=MinaCloudAdmin;User Id=sa;Password=1;TrustServerCertificate=True;";
        static string OutputPath = @"D:\Temp\Generated";

        static void Main()
        {
            var folders = new[]
            {
                "Entities","Requests","Responses",
                "IRepositories","Repositories",
                "IServices","Services",
                "Controllers","MappingProfiles",
                "Data","SQL", "Helper", "UnitTest"
            };
            foreach (var f in folders)
                Directory.CreateDirectory(Path.Combine(OutputPath, f));

            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            var tables = GetTables(conn);
            foreach (var table in tables)
            {
                var columns = GetColumns(conn, table);
                var pk = columns.First().Name; // tạm lấy cột đầu làm khóa

                WriteFile("Entities", table + ".cs", GenerateEntity(table, columns));
                WriteFile("Requests", table + "Request.cs", GenerateRequest(table, columns));
                WriteFile("Responses", table + "Response.cs", GenerateResponse(table, columns));
                WriteFile("IRepositories", "I" + table + "Repository.cs", GenerateRepositoryInterface(table));
                WriteFile("Repositories", table + "Repository.cs", GenerateRepositoryImpl(table));
                WriteFile("IServices", "I" + table + "Service.cs", GenerateServiceInterface(table));
                WriteFile("Services", table + "Service.cs", GenerateServiceImpl(table));
                WriteFile("Controllers", table + "Controller.cs", GenerateController(table));
                WriteFile("MappingProfiles", table + "Profile.cs", GenerateMappingProfile(table));
                WriteFile("SQL", table + "_CRUD.sql", GenerateSqlCrud(table, columns, pk));
            }

            WriteFile("Controllers", "AuthController.cs", GenerateAuthController());
            WriteFile("Data", "BaseContext.cs", GenerateBaseContext(tables));
            WriteFile("Data", "IUnitOfWork.cs", GenerateIUnitOfWork());
            WriteFile("Data", "UnitOfWork.cs", GenerateUnitOfWork());
            WriteFile("Helper", "ServiceRegistration.cs", GenerateServiceRegistration());
            // sau khi WriteFile AuthController.cs ...
            WriteFile("Helper", "IRefreshTokenStore.cs", GenerateIRefreshTokenStore());
            WriteFile("Helper", "InMemoryRefreshTokenStore.cs", GenerateInMemoryRefreshTokenStore());

            // Tạo UnitTest mẫu cho bảng User
            var tablesUnitTest = new List<string> { "User", "UserRoles" };
            foreach (var tableUnitTest in tablesUnitTest)
            {
                WriteFile("UnitTest", tableUnitTest + "ServiceTests.cs", GenerateUnitTest(tableUnitTest));
            }

            WriteFile("", "Program.cs", GenerateProgramCs());
            WriteFile("", "appsettings.json", GenerateAppSettings());
            WriteFile("", "GeneratedProject.csproj", GenerateCsProj());

            Console.WriteLine("✅ All files generated successfully!");
            Console.WriteLine("👉 Tiếp theo, chạy: dotnet ef migrations add Init && dotnet ef database update");
        }

        #region Helpers
        static List<string> GetTables(SqlConnection conn)
        {
            var list = new List<string>();
            using var cmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(r.GetString(0));
            return list;
        }

        static List<(string Name, string Type)> GetColumns(SqlConnection conn, string table)
        {
            var list = new List<(string, string)>();
            using var cmd = new SqlCommand("SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@Table", conn);
            cmd.Parameters.AddWithValue("@Table", table);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add((r.GetString(0), SqlTypeToCSharp(r.GetString(1))));
            return list;
        }

        static string SqlTypeToCSharp(string type) => type switch
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
            File.WriteAllText(path, content, Encoding.UTF8);
            Console.WriteLine($"Generated: {fileName}");
        }
        #endregion

        #region Generators
        static string GenerateEntity(string table, List<(string Name, string Type)> cols)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("namespace Entities");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {table}");
            sb.AppendLine("    {");
            foreach (var c in cols)
                sb.AppendLine($"        public {c.Type} {c.Name} {{ get; set; }}");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        static string GenerateRequest(string table, List<(string Name, string Type)> cols)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace Requests");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {table}Request");
            sb.AppendLine("    {");
            foreach (var c in cols)
                sb.AppendLine($"        public {c.Type} {c.Name} {{ get; set; }}");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        static string GenerateResponse(string table, List<(string Name, string Type)> cols)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace Responses");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {table}Response");
            sb.AppendLine("    {");
            foreach (var c in cols)
                sb.AppendLine($"        public {c.Type} {c.Name} {{ get; set; }}");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        static string GenerateRepositoryInterface(string table) => $@"
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

        static string GenerateRepositoryImpl(string table) => $@"
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
            _db = context.Set<{table}>();
        }}

        public async Task<{table}> GetByIdAsync(Guid id) => await _db.FindAsync(id);

        public async Task<IEnumerable<{table}>> GetAllAsync() => await _db.ToListAsync();

        public async Task<{table}> AddAsync({table} entity)
        {{
            _db.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }}

        public async Task UpdateAsync({table} entity)
        {{
            _db.Update(entity);
            await _context.SaveChangesAsync();
        }}

        public async Task DeleteAsync(Guid id)
        {{
            var entity = await _db.FindAsync(id);
            if (entity != null)
            {{
                _db.Remove(entity);
                await _context.SaveChangesAsync();
            }}
        }}
    }}
}}";

        static string GenerateServiceInterface(string table) => $@"
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
        Task<{table}Response> CreateAsync({table}Request req);
        Task UpdateAsync(Guid id, {table}Request req);
        Task DeleteAsync(Guid id);
    }}
}}";

        static string GenerateServiceImpl(string table) => $@"
using Entities;
using IServices;
using IRepositories;
using Requests;
using Responses;
using Data;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
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
            _repo = repo;
            _uow = uow;
            _mapper = mapper;
        }}

        public async Task<{table}Response> GetByIdAsync(Guid id) =>
            _mapper.Map<{table}Response>(await _repo.GetByIdAsync(id));

        public async Task<IEnumerable<{table}Response>> GetAllAsync() =>
            (await _repo.GetAllAsync()).Select(_mapper.Map<{table}Response>);

        public async Task<{table}Response> CreateAsync({table}Request req)
        {{
            var entity = _mapper.Map<{table}>(req);
            await _repo.AddAsync(entity);
            await _uow.SaveChangesAsync();
            return _mapper.Map<{table}Response>(entity);
        }}

        public async Task UpdateAsync(Guid id, {table}Request req)
        {{
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) throw new Exception(""NotFound"");
            _mapper.Map(req, entity);
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

        static string GenerateController(string table) => $@"
using Microsoft.AspNetCore.Mvc;
using IServices;
using Requests;
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
        public async Task<IActionResult> Create({table}Request req) => Ok(await _service.CreateAsync(req));

        [HttpPut(""{{id}}"")]
        public async Task<IActionResult> Update(Guid id, {table}Request req)
        {{
            await _service.UpdateAsync(id, req);
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

        static string GenerateMappingProfile(string table) => $@"
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

        static string GenerateSqlCrud(string table, List<(string Name, string Type)> cols, string pk)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"-- CRUD for {table}");
            sb.AppendLine($"SELECT * FROM {table};");
            sb.AppendLine($"SELECT * FROM {table} WHERE {pk}=@Id;");
            sb.AppendLine($"INSERT INTO {table} ({string.Join(",", cols.Select(c => c.Name))}) VALUES ({string.Join(",", cols.Select(c => "@" + c.Name))});");
            sb.AppendLine($"UPDATE {table} SET {string.Join(",", cols.Select(c => c.Name + "=@" + c.Name))} WHERE {pk}=@Id;");
            sb.AppendLine($"DELETE FROM {table} WHERE {pk}=@Id;");
            return sb.ToString();
        }

        static string GenerateBaseContext(List<string> tables)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine("using Entities;");
            sb.AppendLine("namespace Data");
            sb.AppendLine("{");
            sb.AppendLine("    public class BaseContext : DbContext");
            sb.AppendLine("    {");
            sb.AppendLine("        public BaseContext(DbContextOptions<BaseContext> options) : base(options) {}");

            foreach (var t in tables)
                sb.AppendLine($"        public DbSet<{t}> {t}Set {{ get; set; }}");

                        sb.AppendLine("    }"); // đóng class
            sb.AppendLine("}");     // đóng namespace

            return sb.ToString();
        }

        static string GenerateServiceRegistration() => @"
            using Microsoft.Extensions.DependencyInjection;
            using System.Reflection;

            namespace Helper
            {
                public static class ServiceRegistration
                {
                    public static void AddRepositoriesAndServices(this IServiceCollection services)
                    {
                        var asm = Assembly.GetExecutingAssembly();

                        // Repositories
                        var repoInterfaces = asm.GetTypes()
                            .Where(t => t.IsInterface && t.Name.EndsWith(""Repository""));
                        foreach (var iface in repoInterfaces)
                        {
                            var impl = asm.GetTypes().FirstOrDefault(c => c.IsClass && iface.IsAssignableFrom(c));
                            if (impl != null)
                                services.AddScoped(iface, impl);
                        }

                        // Services
                        var svcInterfaces = asm.GetTypes()
                            .Where(t => t.IsInterface && t.Name.EndsWith(""Service""));
                        foreach (var iface in svcInterfaces)
                        {
                            var impl = asm.GetTypes().FirstOrDefault(c => c.IsClass && iface.IsAssignableFrom(c));
                            if (impl != null)
                                services.AddScoped(iface, impl);
                        }
                    }
                }
            }";



        static string GenerateIUnitOfWork() => @"
using System.Threading.Tasks;
namespace Data { public interface IUnitOfWork { Task<int> SaveChangesAsync(); } }";

        static string GenerateUnitOfWork() => @"
using System.Threading.Tasks;
namespace Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly BaseContext _context;
        public UnitOfWork(BaseContext context) => _context = context;
        public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
    }
}";

        static string GenerateProgramCs() => @"
            using Microsoft.AspNetCore.Builder;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Hosting;
            using Microsoft.EntityFrameworkCore;
            using Data;
            using AutoMapper;
            using Helper;
            using Microsoft.AspNetCore.Authentication.JwtBearer;
            using Microsoft.IdentityModel.Tokens;
            using System.Text;

            var builder = WebApplication.CreateBuilder(args);

            // DB
            builder.Services.AddDbContext<BaseContext>(opt =>
                opt.UseSqlServer(builder.Configuration.GetConnectionString(""DefaultConnection"")));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddRepositoriesAndServices();
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            // Auth
            var jwtSection = builder.Configuration.GetSection(""Jwt"");
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSection[""Issuer""],
                        ValidAudience = jwtSection[""Audience""],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSection[""Key""]))
                    };
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(""AdminOnly"", policy => policy.RequireRole(""Admin""));
            });
            builder.Services.AddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Auto migrate DB
            using (var scope = app.Services.CreateScope())
            {
                //var db = scope.ServiceProvider.GetRequiredService<BaseContext>();
                //db.Database.Migrate();
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint(""/swagger/v1/swagger.json"", ""Generated API v1"");
                    c.RoutePrefix = string.Empty; // 👉 Mặc định mở swagger tại http://localhost:xxxx/
                });
            }

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
            ";

        static string GenerateAppSettings() => @"
{
  ""ConnectionStrings"": {
    ""DefaultConnection"": ""Server=127.0.0.1;Database=MinaCloudAdmin;User Id=sa;Password=1;TrustServerCertificate=True;""
  },
  ""Jwt"": {
    ""Key"": ""ThisIsASecretKeyForJwtToken123!"",
    ""Issuer"": ""MyAppIssuer"",
    ""Audience"": ""MyAppAudience""
  },
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }
  },
  ""AllowedHosts"": ""*""
}
";

        static string GenerateCsProj() => @"
            <Project Sdk=""Microsoft.NET.Sdk.Web"">
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
                <PackageReference Include=""Swashbuckle.AspNetCore"" Version=""6.4.0"" />
                <PackageReference Include=""Microsoft.AspNetCore.Authentication.JwtBearer"" Version=""6.0.0"" />

                 <!-- Unit Test -->
                <PackageReference Include=""xunit"" Version=""2.4.2"" />
                <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.4.5"" />
                <PackageReference Include=""Moq"" Version=""4.20.72"" />
                <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.7.2"" />

              </ItemGroup>
            </Project>";
        static string GenerateAuthController() => @"
            using Microsoft.AspNetCore.Mvc;
            using Microsoft.IdentityModel.Tokens;
            using Helper;
            using System.IdentityModel.Tokens.Jwt;
            using System.Security.Claims;
            using System.Text;

            namespace Controllers
            {
                [ApiController]
                [Route(""api/[controller]"")]
                public class AuthController : ControllerBase
                {
                    private readonly IConfiguration _config;
                    private readonly IRefreshTokenStore _tokenStore;

                    public AuthController(IConfiguration config, IRefreshTokenStore tokenStore)
                    {
                        _config = config;
                        _tokenStore = tokenStore;
                    }

                    [HttpPost(""login"")]
                    public IActionResult Login([FromBody] LoginRequest req)
                    {
                        if ((req.Username == ""admin"" && req.Password == ""123"") ||
                            (req.Username == ""user"" && req.Password == ""123""))
                        {
                            var role = req.Username == ""admin"" ? ""Admin"" : ""User"";
                            var accessToken = GenerateJwtToken(req.Username, role);
                            var refreshToken = GenerateRefreshToken();

                            _tokenStore.Save(req.Username, refreshToken, DateTime.UtcNow.AddDays(7));

                            return Ok(new { accessToken, refreshToken });
                        }

                        return Unauthorized();
                    }

                    [HttpPost(""refresh"")]
                    public IActionResult Refresh([FromBody] RefreshRequest req)
                    {
                        if (string.IsNullOrEmpty(req.Username) || string.IsNullOrEmpty(req.RefreshToken))
                            return BadRequest(""Missing fields"");

                        var stored = _tokenStore.Get(req.RefreshToken);
                        if (stored == null || stored.ExpiryDate < DateTime.UtcNow || stored.Username != req.Username)
                            return Unauthorized(""Invalid refresh token"");

                        var newAccessToken = GenerateJwtToken(stored.Username, ""User"");
                        var newRefreshToken = GenerateRefreshToken();

                        _tokenStore.Remove(req.RefreshToken);
                        _tokenStore.Save(stored.Username, newRefreshToken, DateTime.UtcNow.AddDays(7));

                        return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
                    }

                    private string GenerateJwtToken(string username, string role)
                    {
                        var jwtSection = _config.GetSection(""Jwt"");
                        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection[""Key""]));
                        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                        var claims = new[]
                        {
                            new Claim(JwtRegisteredClaimNames.Sub, username),
                            new Claim(ClaimTypes.Role, role),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                        };

                        var token = new JwtSecurityToken(
                            issuer: jwtSection[""Issuer""],
                            audience: jwtSection[""Audience""],
                            claims: claims,
                            expires: DateTime.UtcNow.AddHours(1),
                            signingCredentials: creds
                        );

                        return new JwtSecurityTokenHandler().WriteToken(token);
                    }

                    private string GenerateRefreshToken()
                    {
                        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                    }
                }

                public class LoginRequest
                {
                    public string Username { get; set; }
                    public string Password { get; set; }
                }

                public class RefreshRequest
                {
                    public string Username { get; set; }
                    public string RefreshToken { get; set; }
                }
            }";

        static string GenerateIRefreshTokenStore() => @"
            using System;

            namespace Helper
            {
                public sealed record RefreshTokenInfo(string Username, string Token, DateTime ExpiryDate);

                public interface IRefreshTokenStore
                {
                    void Save(string username, string token, DateTime expiry);
                    RefreshTokenInfo? Get(string token);
                    void Remove(string token);
                }
            }";
        static string GenerateInMemoryRefreshTokenStore() => @"
            using System;
            using System.Collections.Concurrent;

            namespace Helper
            {
                public class InMemoryRefreshTokenStore : IRefreshTokenStore
                {
                    private readonly ConcurrentDictionary<string, RefreshTokenInfo> _store = new();

                    public void Save(string username, string token, DateTime expiry)
                    {
                        var info = new RefreshTokenInfo(username, token, expiry);
                        _store[token] = info;
                    }

                    public RefreshTokenInfo? Get(string token)
                    {
                        return _store.TryGetValue(token, out var info) ? info : null;
                    }

                    public void Remove(string token)
                    {
                        _store.TryRemove(token, out _);
                    }
                }
            }";

        #endregion

        #region UnitTest Generator
        static string GenerateUnitTest(string table)
        {
            return $@"
using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Entities;
using Requests;
using Responses;
using IServices;
using IRepositories;
using AutoMapper;
using Data;
using Services;

namespace UnitTests
{{
    public class {table}ServiceTests
    {{
        private readonly Mock<I{table}Repository> _repoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly {table}Service _service;

        public {table}ServiceTests()
        {{
            _repoMock = new Mock<I{table}Repository>();
            _uowMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();

            _service = new {table}Service(_repoMock.Object, _uowMock.Object, _mapperMock.Object);
        }}

        [Fact]
        public async Task Create_Get_Update_Delete_Workflow()
        {{
            var request = new {table}Request();// TODO: Populate with test data
            var entity = new {table}();
            var response = new {table}Response();// TODO: Populate with test data

            // Setup AutoMapper
            _mapperMock.Setup(m => m.Map<{table}>(It.IsAny<{table}Request>())).Returns(entity);
            _mapperMock.Setup(m => m.Map<{table}Response>(It.IsAny<{table}>())).Returns(response);

            // Setup repository
            _repoMock.Setup(r => r.AddAsync(It.IsAny<{table}>())).ReturnsAsync(entity);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(entity);

            // Create
            var created = await _service.CreateAsync(request);
            Assert.NotNull(created);

            // Get
            var fetched = await _service.GetByIdAsync(Guid.NewGuid());
            Assert.NotNull(fetched);

            // Update
            await _service.UpdateAsync(Guid.NewGuid(), request);
            _repoMock.Verify(r => r.UpdateAsync(entity), Times.Once);

            // Delete
            _repoMock.Setup(r => r.DeleteAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
            await _service.DeleteAsync(Guid.NewGuid());
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Once);
        }}
    }}
}}";
        }

        #endregion

    }
}
