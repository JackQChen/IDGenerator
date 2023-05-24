using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IDGenerator.Config;
using IDGenerator.Properties;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDGenerator.Services
{
    public interface IAPIService
    {
        Task<bool> Invoke(string id);
    }

    public class APIService : IAPIService
    {
        private readonly IOptions<APIOptions> _options;
        private readonly ILogger<APIService> _logger;

        public APIService(IOptions<APIOptions> options, ILogger<APIService> logger)
        {
            _options = options;
            _logger = logger;
        }
        public async Task<bool> Invoke(string id)
        {
            return await Invoke(id, true);
        }

        public async Task<bool> Invoke(string id, bool retry)
        {
            var url = _options.Value.URL;
            var valid = false;
            using (var client = new HttpClient())
            {
                try
                {
                    //var content = new MultipartFormDataContent();
                    //content.Add(new StringContent("authenticity_token"), "");
                    //content.Add(new StringContent("value"), id);
                    var data = Resources.JsonData.Replace("$id", id);
                    var content = new StringContent(data, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var jsonObject = JsonConvert.DeserializeObject<JObject>(responseContent);
                        var result = jsonObject["input01"];
                        valid = result["Valid"].Value<bool>();
                        if (valid)
                            _logger.LogTrace($"Id:{id} is valid");
                        else
                        {
                            var message = result["ErrorMessage"].Value<string>();
                            if (message != "That username is taken. Try another.")
                                _logger.LogError("Other error: " + message);
                        }
                    }
                    else
                        _logger.LogError("Request failed: " + response.StatusCode);
                }
                catch (Exception ex)
                {
                    if (retry)
                        valid = await Invoke(id, false);
                    _logger.LogError($"An error occurred(Id->{id}): " + ex.Message);
                }
                return valid;
            }
        }
    }
}
