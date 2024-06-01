using Consul;
using Microsoft.Extensions.Options;
using System.Text;

namespace Infinite.Core.Consul
{
    public interface IConsulClientService
    {
        Task<string> GetValueAsync(string key);
    }

    public class ConsulClientService : IConsulClientService
    {
        private readonly ConsulOptions _options;
        private readonly IConsulClient _consulClient;

        public ConsulClientService(IOptions<ConsulOptions> options)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _consulClient = new ConsulClient(config =>
            {
                config.Address = new Uri(_options.ConsulAddress);
            });
        }

        public async Task<string> GetValueAsync(string key)
        {
            var fullKey = $"{_options.KeyPrefix}/{key}";
            var result = await _consulClient.KV.Get(fullKey);
            if (result.Response == null)
            {
                throw new Exception($"Key '{fullKey}' not found in Consul.");
            }
            return Encoding.UTF8.GetString(result.Response.Value);
        }
    }
}
