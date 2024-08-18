using Consul;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Infinite.Core.Consul
{
    public interface IConsulClientService
    {
        Task<T?> GetValueAsync<T>(string key);
        Task RegisterService(AgentServiceRegistration registration);
        Task DeregisterService(string serviceId);
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
                if (_options.ConsulAddress != null) config.Address = new Uri(_options.ConsulAddress);
            });
        }

        public async Task<T?> GetValueAsync<T>(string key)
        {
            string fullKey = $"{_options.KeyPrefix}/{_options.Environment}/{key}";

            QueryResult<KVPair> result = await _consulClient.KV.Get(fullKey);

            if (result.Response == null)
            {
                return default;
            }

            string json = Encoding.UTF8.GetString(result.Response.Value, 0, result.Response.Value.Length);

            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    // Tentativa de pegar uma propriedade diretamente pelo nome da chave
                    if (document.RootElement.TryGetProperty(key, out JsonElement propertyElement))
                    {
                        return propertyElement.Deserialize<T>();
                    }

                    // Tentativa de pegar uma propriedade com o nome da classe T
                    string propertyName = typeof(T).Name;

                    if (document.RootElement.TryGetProperty(propertyName, out propertyElement))
                    {
                        return propertyElement.Deserialize<T>();
                    }

                    // Se não encontrar uma propriedade, tenta desserializar o JSON inteiro como T
                    return JsonSerializer.Deserialize<T>(json);
                }
            }
            catch (JsonException)
            {
                return default;
            }
        }

        public async Task RegisterService(AgentServiceRegistration registration)
        {
            await _consulClient.Agent.ServiceRegister(registration);
        }

        public async Task DeregisterService(string serviceId)
        {
            await _consulClient.Agent.ServiceDeregister(serviceId);
        }
    }
}
