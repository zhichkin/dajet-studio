using DaJet.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        [HttpGet("ping")] public ActionResult Ping() { return Ok(); }



        [HttpGet("server")] public async Task<ActionResult> SelectDatabaseServers()
        {
            List<DatabaseServer> servers = new List<DatabaseServer>();
            foreach (DatabaseServer server in Metadata.Servers)
            {
                servers.Add(new DatabaseServer()
                {
                    Name = server.Name,
                    Identity = server.Identity
                });
            }
            JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true };
            string json = JsonSerializer.Serialize(servers, options);
            return Content(json);
        }
        [HttpPost("server")] public async Task<ActionResult> CreateDatabaseServer([FromBody] DatabaseServer server)
        {
            if (Metadata.Servers.Where(s => s.Identity == server.Identity).FirstOrDefault() != null)
            {
                return Conflict();
            }

            string catalogName = GetServerCatalog(server);
            Metadata.Servers.Add(server);
            SaveMetadataSettings();

            return Created(catalogName, server.Identity);
        }
        [HttpPut("server")] public async Task<ActionResult> UpdateDatabaseServer([FromBody] DatabaseServer server)
        {
            DatabaseServer existing = Metadata.Servers.Where(s => s.Identity == server.Identity).FirstOrDefault();
            if (existing == null) { return NotFound(); }

            server.CopyTo(existing);
            SaveMetadataSettings();

            return Ok();
        }
        [HttpDelete("server")] public async Task<ActionResult> DeleteDatabaseServer([FromBody] DatabaseServer server)
        {
            // TODO

            return StatusCode(StatusCodes.Status405MethodNotAllowed);
        }



        [HttpPost("{server}/{database}/{script}")]
        public ActionResult ExecuteScript([FromRoute] string server, [FromRoute] string database, [FromRoute] string script)
        {
            string response = $"{{ \"Server\": \"{server}\", \"Database\": \"{database}\", \"Script\": \"{script}\" }}";
            string input = "";
            Dictionary<string, object> parameters = ParseParameters(HttpContext);
            foreach (var p in parameters)
            {
                input += p.Key + " = " + p.Value.ToString() + Environment.NewLine;
            }
            response += Environment.NewLine + input;
            return Content(response);
        }



        [HttpPut("{server}/{database}/{script}")]
        public async Task<ActionResult> DeployScript([FromRoute] string server, [FromRoute] string database, [FromRoute] string script)
        {
            string content = string.Empty;
            Dictionary<string, object> parameters = ParseParameters(HttpContext);
            foreach (var p in parameters)
            {
                if (p.Key == "script")
                {
                    content = (string)p.Value;
                }
            }
            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest();
            }

            string scriptPath = $"web/{server}/{database}/{script}";

            IFileInfo fileInfo = FileProvider.GetFileInfo($"web/{server}");
            if (!fileInfo.Exists)
            {
                Directory.CreateDirectory(fileInfo.PhysicalPath);
            }

            fileInfo = FileProvider.GetFileInfo($"web/{server}/{database}");
            if (!fileInfo.Exists)
            {
                Directory.CreateDirectory(fileInfo.PhysicalPath);
            }

            fileInfo = FileProvider.GetFileInfo(scriptPath);
            using (var stream = System.IO.File.Create(fileInfo.PhysicalPath))
            {
                await stream.WriteAsync(Convert.FromBase64String(content));
            }

            // TODO: save web, scripts and metadata settings

            return Ok();
        }



        [HttpDelete("{server}/{database}/{script}")]
        public ActionResult DeleteScript([FromRoute] string server, [FromRoute] string database, [FromRoute] string script)
        {
            string scriptPath = $"web/{server}/{database}/{script}";

            IFileInfo fileInfo = FileProvider.GetFileInfo(scriptPath);
            if (fileInfo.Exists)
            {
                System.IO.File.Delete(fileInfo.PhysicalPath);
            }
            
            return Ok();
        }
    }
}

//POST   Creates a new resource.
//GET    Retrieves a resource.
//PUT    Updates an existing resource.
//DELETE Deletes a resource.