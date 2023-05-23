using System.Threading.Tasks;
using IDGenerator.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IDGenerator.Services
{
    public interface IAPIService
    {
        Task<bool> Invoke(string id);
    }

    class APIService : IAPIService
    {
        private readonly IOptions<APIOptions> _options;
        private readonly ILogger<APIService> _logger;

        public APIService(IOptions<APIOptions> options, ILogger<APIService> logger)
        {
            _options = options;
            _logger = logger;
        }

        public Task<bool> Invoke(string id)
        {
            var url = _options.Value.URL;
            return Task.FromResult(false);
        }
    }
}
