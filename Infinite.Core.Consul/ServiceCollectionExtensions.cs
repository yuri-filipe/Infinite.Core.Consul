using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infinite.Core.Consul
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConsulClient(this IServiceCollection services, IConfiguration configuration)
        {
            var consulOptions = new ConsulOptions
            {
                ConsulAddress = Environment.GetEnvironmentVariable("CONSUL_ADDRESS") ?? throw new ArgumentNullException("CONSUL_ADDRESS"),
                KeyPrefix = Environment.GetEnvironmentVariable("CONSUL_KEY_PREFIX") ?? throw new ArgumentNullException("CONSUL_KEY_PREFIX")
            };

            services.Configure<ConsulOptions>(options =>
            {
                options.ConsulAddress = consulOptions.ConsulAddress;
                options.KeyPrefix = consulOptions.KeyPrefix;
            });

            services.AddSingleton<IConsulClientService, ConsulClientService>();

            return services;
        }
    }
}
