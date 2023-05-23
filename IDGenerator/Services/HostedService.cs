using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IDGenerator.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IDGenerator.Services
{

    class HostedService : IHostedService
    {
        private readonly IOptions<AppOptions> _options;
        private readonly ILogger<HostedService> _logger;

        private readonly IAPIService _apiService;

        private volatile int _totalCount, _errorCount, _processedCount;

        public HostedService(IOptions<AppOptions> options, ILogger<HostedService> logger, IAPIService apiService)
        {
            _options = options;
            _logger = logger;
            _apiService = apiService;
        }

        IEnumerable<IEnumerable<T>> GetCombinations<T>(IEnumerable<T> list, int k)
        {
            if (k == 0) return new[] { new T[0] };
            return list.SelectMany((item, i) =>
            GetCombinations(list, k - 1)
            .Select(combination => new[] { item }.Concat(combination)));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            _logger.LogInformation($"Start generate...");
            var characters = "0123456789";
            var combinations = GetCombinations(characters.ToCharArray(), 6).Select(chars => string.Concat(chars)).ToList();
            _totalCount = combinations.Count();
            _logger.LogInformation($"Begin validate task, task count:{_totalCount}");
            var taskList = new Task<Dictionary<string, bool>>[_options.Value.TaskCount];
            var paramList = combinations.Select((item, index) => new { item, index = index % taskList.Length });
            for (int i = 0; i < taskList.Length; i++)
            {
                var param = paramList.Where(p => p.index == i).Select(s => s.item).ToArray();
                taskList[i] = Task.Factory.StartNew(async param =>
                {
                    var dicResult = new Dictionary<string, bool>();
                    foreach (var item in param as IEnumerable<string>)
                    {
                        dicResult.Add(item, await _apiService.Invoke(item));
                        Interlocked.Increment(ref _processedCount);
                    }
                    return dicResult;
                }, param, TaskCreationOptions.LongRunning).Unwrap();
            }
            Task.WaitAll(taskList);
            var resultList = taskList.SelectMany(s => s.Result);
            var availableList = resultList.Where(p => p.Value).Select(s => s.Key).ToList();
            stopwatch.Stop();
            _logger.LogInformation($"Task completed, Total Count:{_totalCount}, ProcessedCount Count:{_processedCount}, Error Count:{_errorCount}");
            stopwatch.Stop();
            _logger.LogInformation($"End generate, the task takes {stopwatch.Elapsed.ToString(@"hh\:mm\:ss")} in total.");
            await Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
