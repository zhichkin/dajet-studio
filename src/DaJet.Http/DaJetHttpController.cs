using DaJet.Metadata;
using DaJet.Scripting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DaJet.Http
{
    [Route("")]
    [ApiController]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class DaJetHttpController : ControllerBase
    {
        private const string SCRIPTS_CATALOG_NAME = "scripts";
        private const string SCRIPT_FILE_EXTENSION = ".qry";
        private MetadataSettings Metadata { get; }
        private IServiceProvider Services { get; }
        private IFileProvider FileProvider { get; }
        public DaJetHttpController(IServiceProvider serviceProvider, IFileProvider fileProvider, IOptions<MetadataSettings> options)
        {
            Metadata = options.Value;
            Services = serviceProvider;
            FileProvider = fileProvider;
        }
        
        private Dictionary<string, object> ParseParameters(HttpContext context)
        {
            object parameters = null;
            Dictionary<string, object> result = new Dictionary<string, object>();

            using (var reader = new StreamReader(context.Request.Body))
            {
                string body = reader.ReadToEndAsync().Result;

                if (!string.IsNullOrWhiteSpace(body))
                {
                    parameters = JsonSerializer.Deserialize<object>(body);
                }
            }

            if (parameters is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Object)
                {
                    var properties = element.EnumerateObject();

                    foreach (var property in properties)
                    {
                        if (property.Value.ValueKind == JsonValueKind.String)
                        {
                            if (property.Value.TryGetDateTime(out DateTime date))
                            {
                                result.Add(property.Name, date);
                            }
                            else if (property.Value.TryGetGuid(out Guid uuid))
                            {
                                result.Add(property.Name, uuid);
                            }
                            else
                            {
                                result.Add(property.Name, property.Value.GetString());
                            }
                        }
                        else if (property.Value.ValueKind == JsonValueKind.True)
                        {
                            result.Add(property.Name, true);
                        }
                        else if (property.Value.ValueKind == JsonValueKind.False)
                        {
                            result.Add(property.Name, false);
                        }
                        else if (property.Value.ValueKind == JsonValueKind.Number)
                        {
                            if (property.Value.TryGetInt32(out int integer))
                            {
                                result.Add(property.Name, integer);
                            }
                            else
                            {
                                result.Add(property.Name, property.Value.GetDecimal());
                            }
                        }
                    }
                }
            }

            return result;
        }

        private void SaveMetadataSettings()
        {
            IFileInfo fileInfo = FileProvider.GetFileInfo($"{Startup.METADATA_SETTINGS_FILE_NAME}");
            JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true };
            string json = JsonSerializer.Serialize(Metadata, options);
            using (StreamWriter writer = new StreamWriter(fileInfo.PhysicalPath, false, Encoding.UTF8))
            {
                writer.Write(json);
            }
        }
        private void CreateCatalogIfNotExists(string catalogName)
        {
            IFileInfo catalog = FileProvider.GetFileInfo(catalogName);
            if (!catalog.Exists) { Directory.CreateDirectory(catalog.PhysicalPath); }
        }
        private string GetScriptsCatalog()
        {
            string catalogName = $"{SCRIPTS_CATALOG_NAME}";
            CreateCatalogIfNotExists(catalogName);
            return catalogName;
        }
        private string GetServerCatalog(DatabaseServer server)
        {
            string catalogName = GetScriptsCatalog();

            catalogName += $"/{server.Identity.ToString().ToLower()}";
            CreateCatalogIfNotExists(catalogName);

            return catalogName;
        }
        private string GetDatabaseCatalog(DatabaseServer server, DatabaseInfo database)
        {
            string catalogName = GetServerCatalog(server);

            catalogName += $"/{database.Identity.ToString().ToLower()}";
            CreateCatalogIfNotExists(catalogName);

            return catalogName;
        }
        private string ReadScriptSourceCode(DatabaseServer server, DatabaseInfo database, MetaScript script)
        {
            string sourceCode;

            string catalogName = GetDatabaseCatalog(server, database);
            string scriptPath = $"{catalogName}/{script.Identity.ToString().ToLower()}{SCRIPT_FILE_EXTENSION}";
            IFileInfo file = FileProvider.GetFileInfo(scriptPath);
            if (!file.Exists)
            {
                throw new FileNotFoundException();
            }

            using (StreamReader reader = new StreamReader(file.PhysicalPath, Encoding.UTF8))
            {
                sourceCode = reader.ReadToEnd();
            }

            return sourceCode;
        }

        [HttpGet("ping")] public ActionResult Ping() { return Ok(); }

        [HttpGet("server/{uuid?}")] public async Task<ActionResult> SelectDatabaseServer([FromRoute] Guid uuid)
        {
            List<DatabaseServer> servers = new List<DatabaseServer>();
            foreach (DatabaseServer server in Metadata.Servers)
            {
                if (uuid == Guid.Empty || server.Identity == uuid)
                {
                    servers.Add(new DatabaseServer()
                    {
                        Name = server.Name,
                        Identity = server.Identity
                    });
                }
            }
            
            if (uuid != Guid.Empty && servers.Count == 0)
            {
                return NotFound();
            }

            JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true };
            string json = JsonSerializer.Serialize(servers, options);
            return Content(json);
        }
        [HttpPost("server/{uuid}")] public async Task<ActionResult> CreateDatabaseServer([FromRoute] Guid uuid, [FromBody] DatabaseServer server)
        {
            if (server.Identity != uuid)
            {
                return BadRequest();
            }
            if (Metadata.Servers.Where(s => s.Identity == uuid).FirstOrDefault() != null)
            {
                return Conflict();
            }

            string catalogName = GetServerCatalog(server);
            Metadata.Servers.Add(server);
            SaveMetadataSettings();

            return Created(catalogName, server.Identity);
        }
        [HttpPut("server/{uuid}")] public async Task<ActionResult> UpdateDatabaseServer([FromRoute] Guid uuid, [FromBody] DatabaseServer server)
        {
            if (server.Identity != uuid)
            {
                return BadRequest();
            }
            DatabaseServer existing = Metadata.Servers.Where(s => s.Identity == uuid).FirstOrDefault();
            if (existing == null)
            {
                return NotFound();
            }

            server.CopyTo(existing);
            SaveMetadataSettings();

            return Ok();
        }
        [HttpDelete("server/{uuid}")] public async Task<ActionResult> DeleteDatabaseServer([FromRoute] Guid uuid)
        {
            DatabaseServer existing = Metadata.Servers.Where(s => s.Identity == uuid).FirstOrDefault();
            if (existing == null) { return NotFound(); }

            string catalogName = GetServerCatalog(existing);
            IFileInfo fileInfo = FileProvider.GetFileInfo(catalogName);
            Directory.Delete(fileInfo.PhysicalPath);

            Metadata.Servers.Remove(existing);
            SaveMetadataSettings();

            return Ok();
        }

        [HttpGet("{server}/database/{uuid?}")] public async Task<ActionResult> SelectDatabase([FromRoute] Guid server, [FromRoute] Guid uuid)
        {
            DatabaseServer srv = Metadata.Servers.Where(s => s.Identity == server).FirstOrDefault();
            if (srv == null)
            {
                return NotFound();
            }

            List<DatabaseInfo> databases = new List<DatabaseInfo>();

            foreach (DatabaseInfo database in srv.Databases)
            {
                if (uuid == Guid.Empty || database.Identity == uuid)
                {
                    databases.Add(new DatabaseInfo()
                    {
                        Name = database.Name,
                        Identity = database.Identity
                    });
                }
            }

            if (uuid != Guid.Empty && databases.Count == 0)
            {
                return NotFound();
            }

            JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true };
            string json = JsonSerializer.Serialize(databases, options);
            return Content(json);
        }
        [HttpPost("{server}/database/{uuid}")] public async Task<ActionResult> CreateDatabase([FromRoute] Guid server, [FromRoute] Guid uuid, [FromBody] DatabaseInfo database)
        {
            if (database.Identity != uuid)
            {
                return BadRequest();
            }
            DatabaseServer srv = Metadata.Servers.Where(s => s.Identity == server).FirstOrDefault();
            if (srv == null)
            {
                return NotFound();
            }
            if (srv.Databases.Where(db => db.Identity == uuid).FirstOrDefault() != null)
            {
                return Conflict();
            }

            string catalogName = GetDatabaseCatalog(srv, database);
            srv.Databases.Add(database);
            SaveMetadataSettings();

            // TODO: initialize database metadata !?

            return Created(catalogName, database.Identity);
        }
        [HttpPut("{server}/database/{uuid}")] public async Task<ActionResult> UpdateDatabase([FromRoute] Guid server, [FromRoute] Guid uuid, [FromBody] DatabaseInfo database)
        {
            if (database.Identity != uuid)
            {
                return BadRequest();
            }
            DatabaseServer srv = Metadata.Servers.Where(s => s.Identity == server).FirstOrDefault();
            if (srv == null)
            {
                return NotFound();
            }
            DatabaseInfo existing = srv.Databases.Where(db => db.Identity == uuid).FirstOrDefault();
            if (existing == null)
            {
                return NotFound();
            }

            database.CopyTo(existing);
            SaveMetadataSettings();

            return Ok();
        }
        [HttpDelete("{server}/database/{uuid}")] public async Task<ActionResult> DeleteDatabase([FromRoute] Guid server, [FromRoute] Guid uuid)
        {
            DatabaseServer srv = Metadata.Servers.Where(s => s.Identity == server).FirstOrDefault();
            if (srv == null)
            {
                return NotFound();
            }
            DatabaseInfo existing = srv.Databases.Where(db => db.Identity == uuid).FirstOrDefault();
            if (existing == null)
            {
                return NotFound();
            }

            string catalogName = GetDatabaseCatalog(srv, existing);
            IFileInfo fileInfo = FileProvider.GetFileInfo(catalogName);
            Directory.Delete(fileInfo.PhysicalPath);

            srv.Databases.Remove(existing);
            SaveMetadataSettings();

            return Ok();
        }

        [HttpGet("{server}/{database}/script/{uuid?}")] public async Task<ActionResult> SelectScript([FromRoute] Guid server, [FromRoute] Guid database, [FromRoute] Guid uuid)
        {
            DatabaseServer srv = Metadata.Servers.Where(s => s.Identity == server).FirstOrDefault();
            if (srv == null)
            {
                return NotFound();
            }
            DatabaseInfo db = srv.Databases.Where(db => db.Identity == database).FirstOrDefault();
            if (db == null)
            {
                return NotFound();
            }

            List<MetaScript> scripts = new List<MetaScript>();

            foreach (MetaScript script in db.Scripts)
            {
                if (uuid == Guid.Empty || script.Identity == uuid)
                {
                    scripts.Add(new MetaScript()
                    {
                        Name = script.Name,
                        Identity = script.Identity
                    });
                }
            }

            if (uuid != Guid.Empty && scripts.Count == 0)
            {
                return NotFound();
            }

            JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true };
            string json = JsonSerializer.Serialize(scripts, options);
            return Content(json);
        }
        [HttpPost("{server}/{database}/script/{uuid}")] public async Task<ActionResult> CreateScript([FromRoute] Guid server, [FromRoute] Guid database, [FromRoute] Guid uuid, [FromBody] MetaScript script)
        {
            if (script.Identity != uuid)
            {
                return BadRequest();
            }
            DatabaseServer srv = Metadata.Servers.Where(s => s.Identity == server).FirstOrDefault();
            if (srv == null)
            {
                return NotFound();
            }
            DatabaseInfo db = srv.Databases.Where(db => db.Identity == database).FirstOrDefault();
            if (db == null)
            {
                return NotFound();
            }
            if (db.Scripts.Where(scr => scr.Identity == uuid).FirstOrDefault() != null)
            {
                return Conflict();
            }

            string catalogName = GetDatabaseCatalog(srv, db);
            string scriptPath = $"{catalogName}/{uuid.ToString().ToLower()}{SCRIPT_FILE_EXTENSION}";
            IFileInfo fileInfo = FileProvider.GetFileInfo(scriptPath);
            using (var stream = System.IO.File.Create(fileInfo.PhysicalPath))
            {
                await stream.WriteAsync(Convert.FromBase64String(script.SourceCode));
            }

            script.SourceCode = string.Empty;
            db.Scripts.Add(script);
            SaveMetadataSettings();

            return Created(scriptPath, script.Identity);
        }
        [HttpPut("{server}/{database}/script/{uuid}")] public async Task<ActionResult> UpdateScript([FromRoute] Guid server, [FromRoute] Guid database, [FromRoute] Guid uuid, [FromBody] MetaScript script)
        {
            if (script.Identity != uuid)
            {
                return BadRequest();
            }
            DatabaseServer srv = Metadata.Servers.Where(s => s.Identity == server).FirstOrDefault();
            if (srv == null)
            {
                return NotFound();
            }
            DatabaseInfo db = srv.Databases.Where(db => db.Identity == database).FirstOrDefault();
            if (db == null)
            {
                return NotFound();
            }
            MetaScript existing = db.Scripts.Where(scr => scr.Identity == uuid).FirstOrDefault();
            if (existing == null)
            {
                return NotFound();
            }

            string catalogName = GetDatabaseCatalog(srv, db);
            string scriptPath = $"{catalogName}/{uuid.ToString().ToLower()}{SCRIPT_FILE_EXTENSION}";
            IFileInfo fileInfo = FileProvider.GetFileInfo(scriptPath);
            using (var stream = System.IO.File.Create(fileInfo.PhysicalPath))
            {
                await stream.WriteAsync(Convert.FromBase64String(script.SourceCode));
            }

            script.CopyTo(existing);
            existing.SourceCode = string.Empty;
            SaveMetadataSettings();

            return Ok();
        }
        [HttpDelete("{server}/{database}/script/{uuid}")] public async Task<ActionResult> DeleteScript([FromRoute] Guid server, [FromRoute] Guid database, [FromRoute] Guid uuid)
        {
            DatabaseServer srv = Metadata.Servers.Where(s => s.Identity == server).FirstOrDefault();
            if (srv == null)
            {
                return NotFound();
            }
            DatabaseInfo db = srv.Databases.Where(db => db.Identity == database).FirstOrDefault();
            if (db == null)
            {
                return NotFound();
            }
            MetaScript existing = db.Scripts.Where(scr => scr.Identity == uuid).FirstOrDefault();
            if (existing == null)
            {
                return NotFound();
            }

            string catalogName = GetDatabaseCatalog(srv, db);
            string scriptPath = $"{catalogName}/{uuid.ToString().ToLower()}{SCRIPT_FILE_EXTENSION}";
            IFileInfo fileInfo = FileProvider.GetFileInfo(scriptPath);
            System.IO.File.Delete(fileInfo.PhysicalPath);

            db.Scripts.Remove(existing);
            SaveMetadataSettings();

            return Ok();
        }



        [HttpPost("{server}/{database}/{script}")]
        public ActionResult ExecuteScript([FromRoute] Guid server, [FromRoute] Guid database, [FromRoute] Guid script)
        {
            DatabaseServer srv = Metadata.Servers.Where(srv => srv.Identity == server).FirstOrDefault();
            if (srv == null) { return NotFound(); }
            DatabaseInfo db = srv.Databases.Where(db => db.Identity == database).FirstOrDefault();
            if (db == null) { return NotFound(); }
            MetaScript scr = db.Scripts.Where(scr => scr.Identity == script).FirstOrDefault();
            if (scr == null) { return NotFound(); }

            // TODO: initialize script with parameters
            //Dictionary<string, object> parameters = ParseParameters(HttpContext);
            //foreach (var p in parameters)
            //{
            //    //input += p.Key + " = " + p.Value.ToString() + Environment.NewLine;
            //}

            string responseJson = "[]";
            string errorMessage = string.Empty;

            string sourceCode = ReadScriptSourceCode(srv, db, scr);

            IMetadataService metadata = Services.GetService<IMetadataService>();
            metadata.AttachDatabase(string.IsNullOrWhiteSpace(srv.Address) ? srv.Name : srv.Address, db);

            IScriptingService scripting = Services.GetService<IScriptingService>();
            string sql = scripting.PrepareScript(sourceCode, out IList<ParseError> parseErrors);
            foreach (ParseError error in parseErrors) { errorMessage += error.Message + Environment.NewLine; }
            if (parseErrors.Count > 0) { return StatusCode(StatusCodes.Status500InternalServerError, errorMessage); }

            try
            {
                responseJson = scripting.ExecuteScript(sql, out IList<ParseError> executeErrors);
                foreach (ParseError error in executeErrors) { errorMessage += error.Message + Environment.NewLine; }
                if (executeErrors.Count > 0) { return StatusCode(StatusCodes.Status500InternalServerError, errorMessage); }
            }
            catch (Exception ex)
            {
                errorMessage = ExceptionHelper.GetErrorText(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }
            
            return Content(responseJson);
        }
    }
}