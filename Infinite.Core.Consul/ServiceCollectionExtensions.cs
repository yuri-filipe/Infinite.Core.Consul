using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infinite.Core.Consul
{
    public static class ServiceCollectionExtensions
    {
        private static void ValidateConsulConfiguration(IConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration["Consul:ConsulAddress"]))
            {
                throw new ArgumentNullException(nameof(configuration), "Consul:ConsulAddress is required");
            }

            if (string.IsNullOrWhiteSpace(configuration["Consul:ServiceId"]))
            {
                throw new ArgumentNullException(nameof(configuration), "Consul:ServiceId is required");
            }

            if (string.IsNullOrWhiteSpace(configuration["Consul:ServiceName"]))
            {
                throw new ArgumentNullException(nameof(configuration), "Consul:ServiceName is required");
            }
        }

        public static IServiceCollection AddConsulClient(this IServiceCollection services, IConfiguration configuration)
        {
            ValidateConsulConfiguration(configuration);

            var consulOptions = new ConsulOptions
            {
                ConsulAddress = configuration["Consul:ConsulAddress"],
                KeyPrefix = configuration["Consul:KeyPrefix"],
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
            };

            services.Configure<ConsulOptions>(options =>
            {
                options.ConsulAddress = consulOptions.ConsulAddress;
                options.KeyPrefix = consulOptions.KeyPrefix;
                options.Environment = consulOptions.Environment;
            });

            services.AddSingleton<IConsulClientService, ConsulClientService>();

            return services;
        }

        public static IApplicationBuilder UseConsul(this IApplicationBuilder app, IConfiguration configuration, string apiName)
        {
            IConsulClientService consulClient = app.ApplicationServices.GetRequiredService<IConsulClientService>();
            ILogger logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger("ServiceCollectionExtensions");
            IHostApplicationLifetime lifeTime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

            lifeTime.ApplicationStarted.Register(async () =>
            {
                try
                {
                    var apiConfig = await consulClient.GetValueAsync<ApiConfiguration>($"apis/{apiName}");

                    if (apiConfig == null)
                    {
                        logger.LogError("Invalid API configuration returned from Consul");
                        throw new Exception("Invalid API configuration returned from Consul");
                    }

                    //int.TryParse(apiConfig.ServicePort, out int port);

                    var registration = new AgentServiceRegistration()
                    {
                        ID = configuration["Consul:ServiceId"],
                        Name = configuration["Consul:ServiceName"],
                        Address = apiConfig.ServiceHost,
                        Port = apiConfig.ServicePort
                    };

                    logger.LogInformation("Registering with Consul");

                    await consulClient.RegisterService(registration).ConfigureAwait(false);
                    logger.LogInformation("Service registered with Consul");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error registering service with Consul");
                }
            });

            lifeTime.ApplicationStopping.Register(async () =>
            {
                try
                {
                    string serviceId = configuration["ApiConfiguration:ServiceId"];
                    logger.LogInformation("Unregistering from Consul");

                    await consulClient.DeregisterService(serviceId).ConfigureAwait(false);
                    logger.LogInformation("Service unregistered from Consul");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error unregistering service from Consul");
                }
            });

            return app;
        }
    }
}
