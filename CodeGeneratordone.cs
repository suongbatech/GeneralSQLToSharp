//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace GeneralSQLToSharp
//{
//    static class CodeGeneratordone
//    {
//        public static void GenerateEntity(string table, List<(string Name, string Type)> cols, string outputDir)
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("namespace Generated.Entities");
//            sb.AppendLine("{");
//            sb.AppendLine($"    public class {table}Entity");
//            sb.AppendLine("    {");
//            foreach (var col in cols)
//            {
//                sb.AppendLine($"        public {SqlToCSharp(col.Type)} {col.Name} {{ get; set; }}");
//            }
//            sb.AppendLine("    }");
//            sb.AppendLine("}");
//            WriteToFile(Path.Combine(outputDir, "Entities", $"{table}Entity.cs"), sb.ToString());
//        }

//        public static void GenerateRequest(string table, List<(string Name, string Type)> cols, string outputDir)
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("namespace Generated.Requests");
//            sb.AppendLine("{");
//            sb.AppendLine($"    public class {table}Request");
//            sb.AppendLine("    {");
//            foreach (var col in cols)
//            {
//                sb.AppendLine($"        public {SqlToCSharp(col.Type)} {col.Name} {{ get; set; }}");
//            }
//            sb.AppendLine("    }");
//            sb.AppendLine("}");
//            WriteToFile(Path.Combine(outputDir, "Requests", $"{table}Request.cs"), sb.ToString());
//        }

//        public static void GenerateResponse(string table, List<(string Name, string Type)> cols, string outputDir)
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("namespace Generated.Responses");
//            sb.AppendLine("{");
//            sb.AppendLine($"    public class {table}Response");
//            sb.AppendLine("    {");
//            foreach (var col in cols)
//            {
//                sb.AppendLine($"        public {SqlToCSharp(col.Type)} {col.Name} {{ get; set; }}");
//            }
//            sb.AppendLine("    }");
//            sb.AppendLine("}");
//            WriteToFile(Path.Combine(outputDir, "Responses", $"{table}Response.cs"), sb.ToString());
//        }

//        public static void GenerateRepository(string table, List<(string Name, string Type)> cols, string outputDir)
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

//        public static void GenerateService(string table, List<(string Name, string Type)> cols, string outputDir)
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

//        public static void GenerateRepositoryImpl(string table, List<(string Name, string Type)> cols, string outputDir)
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("using System.Collections.Generic;");
//            sb.AppendLine("using System.Threading.Tasks;");
//            sb.AppendLine("using Generated.Entities;");
//            sb.AppendLine();
//            sb.AppendLine("namespace Generated.Repositories");
//            sb.AppendLine("{");
//            sb.AppendLine($"    public class {table}Repository : I{table}Repository");
//            sb.AppendLine("    {");
//            sb.AppendLine("        public Task<IEnumerable<" + table + "Entity>> GetAllAsync()");
//            sb.AppendLine("        {");
//            sb.AppendLine("            // TODO: implement DB logic");
//            sb.AppendLine("            return Task.FromResult<IEnumerable<" + table + "Entity>>(new List<" + table + "Entity>());");
//            sb.AppendLine("        }");
//            sb.AppendLine();
//            sb.AppendLine("        public Task<" + table + "Entity?> GetByIdAsync(int id)");
//            sb.AppendLine("        {");
//            sb.AppendLine("            // TODO: implement DB logic");
//            sb.AppendLine("            return Task.FromResult<" + table + "Entity?>(null);");
//            sb.AppendLine("        }");
//            sb.AppendLine();
//            sb.AppendLine("        public Task AddAsync(" + table + "Entity entity)");
//            sb.AppendLine("        {");
//            sb.AppendLine("            // TODO: implement DB insert");
//            sb.AppendLine("            return Task.CompletedTask;");
//            sb.AppendLine("        }");
//            sb.AppendLine();
//            sb.AppendLine("        public Task UpdateAsync(" + table + "Entity entity)");
//            sb.AppendLine("        {");
//            sb.AppendLine("            // TODO: implement DB update");
//            sb.AppendLine("            return Task.CompletedTask;");
//            sb.AppendLine("        }");
//            sb.AppendLine();
//            sb.AppendLine("        public Task DeleteAsync(int id)");
//            sb.AppendLine("        {");
//            sb.AppendLine("            // TODO: implement DB delete");
//            sb.AppendLine("            return Task.CompletedTask;");
//            sb.AppendLine("        }");
//            sb.AppendLine("    }");
//            sb.AppendLine("}");
//            WriteToFile(Path.Combine(outputDir, "Repositories", $"{table}Repository.cs"), sb.ToString());
//        }

//        public static void GenerateServiceImpl(string table, List<(string Name, string Type)> cols, string outputDir)
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("using System.Collections.Generic;");
//            sb.AppendLine("using System.Linq;");
//            sb.AppendLine("using System.Threading.Tasks;");
//            sb.AppendLine("using Generated.Repositories;");
//            sb.AppendLine("using Generated.Requests;");
//            sb.AppendLine("using Generated.Responses;");
//            sb.AppendLine("using Generated.Entities;");
//            sb.AppendLine();
//            sb.AppendLine("namespace Generated.Services");
//            sb.AppendLine("{");
//            sb.AppendLine($"    public class {table}Service : I{table}Service");
//            sb.AppendLine("    {");
//            sb.AppendLine($"        private readonly I{table}Repository _repo;");
//            sb.AppendLine();
//            sb.AppendLine($"        public {table}Service(I{table}Repository repo)");
//            sb.AppendLine("        {");
//            sb.AppendLine("            _repo = repo;");
//            sb.AppendLine("        }");
//            sb.AppendLine();
//            sb.AppendLine($"        public async Task<IEnumerable<{table}Response>> GetAllAsync()");
//            sb.AppendLine("        {");
//            sb.AppendLine($"            var entities = await _repo.GetAllAsync();");
//            sb.AppendLine($"            return entities.Select(e => new {table}Response");
//            sb.AppendLine("            {");
//            foreach (var col in cols)
//            {
//                sb.AppendLine($"                {col.Name} = e.{col.Name},");
//            }
//            sb.AppendLine("            });");
//            sb.AppendLine("        }");
//            sb.AppendLine();
//            sb.AppendLine($"        public async Task<{table}Response?> GetByIdAsync(int id)");
//            sb.AppendLine("        {");
//            sb.AppendLine($"            var e = await _repo.GetByIdAsync(id);");
//            sb.AppendLine("            if (e == null) return null;");
//            sb.AppendLine($"            return new {table}Response");
//            sb.AppendLine("            {");
//            foreach (var col in cols)
//            {
//                sb.AppendLine($"                {col.Name} = e.{col.Name},");
//            }
//            sb.AppendLine("            };");
//            sb.AppendLine("        }");
//            sb.AppendLine();
//            sb.AppendLine($"        public async Task CreateAsync({table}Request request)");
//            sb.AppendLine("        {");
//            sb.AppendLine($"            var entity = new {table}Entity");
//            sb.AppendLine("            {");
//            foreach (var col in cols)
//            {
//                sb.AppendLine($"                {col.Name} = request.{col.Name},");
//            }
//            sb.AppendLine("            };");
//            sb.AppendLine("            await _repo.AddAsync(entity);");
//            sb.AppendLine("        }");
//            sb.AppendLine();
//            sb.AppendLine($"        public async Task UpdateAsync(int id, {table}Request request)");
//            sb.AppendLine("        {");
//            sb.AppendLine($"            var entity = new {table}Entity");
//            sb.AppendLine("            {");
//            sb.AppendLine("                // TODO: set Id property here if needed");
//            foreach (var col in cols)
//            {
//                sb.AppendLine($"                {col.Name} = request.{col.Name},");
//            }
//            sb.AppendLine("            };");
//            sb.AppendLine("            await _repo.UpdateAsync(entity);");
//            sb.AppendLine("        }");
//            sb.AppendLine();
//            sb.AppendLine("        public async Task DeleteAsync(int id)");
//            sb.AppendLine("        {");
//            sb.AppendLine("            await _repo.DeleteAsync(id);");
//            sb.AppendLine("        }");
//            sb.AppendLine("    }");
//            sb.AppendLine("}");
//            WriteToFile(Path.Combine(outputDir, "Services", $"{table}Service.cs"), sb.ToString());
//        }


//        public static void GenerateController(string table, List<(string Name, string Type)> cols, string outputDir)
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
//            sb.AppendLine("using System.Threading.Tasks;");
//            sb.AppendLine("using Generated.Services;");
//            sb.AppendLine("using Generated.Requests;");
//            sb.AppendLine();
//            sb.AppendLine("namespace Generated.Controllers");
//            sb.AppendLine("{");
//            sb.AppendLine("    [ApiController]");
//            sb.AppendLine($"    [Route(\"api/[controller]\")]");
//            sb.AppendLine($"    public class {table}Controller : ControllerBase");
//            sb.AppendLine("    {");
//            sb.AppendLine($"        private readonly I{table}Service _service;");
//            sb.AppendLine();
//            sb.AppendLine($"        public {table}Controller(I{table}Service service)");
//            sb.AppendLine("        {");
//            sb.AppendLine("            _service = service;");
//            sb.AppendLine("        }");
//            sb.AppendLine();
//            sb.AppendLine("        [HttpGet]");
//            sb.AppendLine("        public async Task<IActionResult> GetAll()");
//            sb.AppendLine("        {");
//            sb.AppendLine("            var result = await _service.GetAllAsync();");
//            sb.AppendLine("            return Ok(result);");
//            sb.AppendLine("        }");
//            sb.AppendLine();
//            sb.AppendLine("        [HttpGet(\"{id}\")]");
//            sb.AppendLine("        public async Task<IActionResult> GetById(int id)");
//            sb.AppendLine("        {");
//            sb.AppendLine("            var result = await _service.GetByIdAsync(id);");
//            sb.AppendLine("            if (result == null) return NotFound();");
//            sb.AppendLine("            return Ok(result);");
//            sb.AppendLine("        }");
//            sb.AppendLine();
//            sb.AppendLine("        [HttpPost]");
//            sb.AppendLine($"        public async Task<IActionResult> Create({table}Request request)");
//            sb.AppendLine("        {");
//            sb.AppendLine("            await _service.CreateAsync(request);");
//            sb.AppendLine("            return Ok();");
//            sb.AppendLine("        }");
//            sb.AppendLine();
//            sb.AppendLine("        [HttpPut(\"{id}\")]");
//            sb.AppendLine($"        public async Task<IActionResult> Update(int id, {table}Request request)");
//            sb.AppendLine("        {");
//            sb.AppendLine("            await _service.UpdateAsync(id, request);");
//            sb.AppendLine("            return Ok();");
//            sb.AppendLine("        }");
//            sb.AppendLine();
//            sb.AppendLine("        [HttpDelete(\"{id}\")]");
//            sb.AppendLine("        public async Task<IActionResult> Delete(int id)");
//            sb.AppendLine("        {");
//            sb.AppendLine("            await _service.DeleteAsync(id);");
//            sb.AppendLine("            return Ok();");
//            sb.AppendLine("        }");
//            sb.AppendLine("    }");
//            sb.AppendLine("}");
//            WriteToFile(Path.Combine(outputDir, "Controllers", $"{table}Controller.cs"), sb.ToString());
//        }

//        // Helper: SQL → C# mapping
//        public static string SqlToCSharp(string sqlType) => sqlType switch
//        {
//            "int" => "int",
//            "bigint" => "long",
//            "smallint" => "short",
//            "tinyint" => "byte",
//            "bit" => "bool",
//            "float" => "double",
//            "real" => "float",
//            "decimal" or "numeric" or "money" or "smallmoney" => "decimal",
//            "datetime" or "smalldatetime" or "date" or "datetime2" => "DateTime",
//            "datetimeoffset" => "DateTimeOffset",
//            "time" => "TimeSpan",
//            "char" or "varchar" or "text" or "nchar" or "nvarchar" or "ntext" => "string",
//            "uniqueidentifier" => "Guid",
//            "binary" or "varbinary" or "image" => "byte[]",
//            _ => "string"
//        };

//        // Helper: Write file
//        private static void WriteToFile(string path, string content)
//        {
//            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
//            File.WriteAllText(path, content);
//        }
//    }

//}

