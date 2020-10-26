using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DaJet.Http
{
    [Route("")]
    [ApiController]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class DaJetHttpController : ControllerBase
    {
        private readonly ILogger<DaJetHttpController> _logger;
        public DaJetHttpController(ILogger<DaJetHttpController> logger)
        {
            _logger = logger;
        }
        [HttpPost("{server}/{database}/{script}")]
        public ActionResult Post([FromRoute] string server, [FromRoute] string database, [FromRoute] string script)
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
    }
}
