using TeraSyncV2Services.Discord;
using TeraSyncV2Shared.Data;
using TeraSyncV2Shared.Metrics;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using TeraSyncV2Shared.Utils;
using TeraSyncV2Shared.Services;
using StackExchange.Redis;
using TeraSyncV2Shared.Utils.Configuration;

namespace TeraSyncV2Services;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var config = app.ApplicationServices.GetRequiredService<IConfigurationService<TeraSyncConfigurationBase>>();

        var metricServer = new KestrelMetricServer(config.GetValueOrDefault<int>(nameof(TeraSyncConfigurationBase.MetricsPort), 4982));
        metricServer.Start();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var teraConfig = Configuration.GetSection("TeraSyncV2");

        services.AddDbContextPool<TeraDbContext>(options =>
        {
            options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), builder =>
            {
                builder.MigrationsHistoryTable("_efmigrationshistory", "public");
            }).UseSnakeCaseNamingConvention();
            options.EnableThreadSafetyChecks(false);
        }, Configuration.GetValue(nameof(TeraSyncConfigurationBase.DbContextPoolSize), 1024));
        services.AddDbContextFactory<TeraDbContext>(options =>
        {
            options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), builder =>
            {
                builder.MigrationsHistoryTable("_efmigrationshistory", "public");
                builder.MigrationsAssembly("TeraSyncShared");
            }).UseSnakeCaseNamingConvention();
            options.EnableThreadSafetyChecks(false);
        });

        services.AddSingleton(m => new TeraMetrics(m.GetService<ILogger<TeraMetrics>>(), new List<string> { },
        new List<string> { }));

        var redis = teraConfig.GetValue(nameof(ServerConfiguration.RedisConnectionString), string.Empty);
        var options = ConfigurationOptions.Parse(redis);
        options.ClientName = "Tera Sync V2";
        options.ChannelPrefix = "UserData";
        ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(options);
        services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);

        services.Configure<ServicesConfiguration>(Configuration.GetRequiredSection("TeraSyncV2"));
        services.Configure<ServerConfiguration>(Configuration.GetRequiredSection("TeraSyncV2"));
        services.Configure<TeraSyncConfigurationBase>(Configuration.GetRequiredSection("TeraSyncV2"));
        services.AddSingleton(Configuration);
        services.AddSingleton<ServerTokenGenerator>();
        services.AddSingleton<DiscordBotServices>();
        services.AddHostedService<DiscordBot>();
        services.AddSingleton<IConfigurationService<ServicesConfiguration>, TeraConfigurationServiceServer<ServicesConfiguration>>();
        services.AddSingleton<IConfigurationService<ServerConfiguration>, TeraConfigurationServiceClient<ServerConfiguration>>();
        services.AddSingleton<IConfigurationService<TeraSyncConfigurationBase>, TeraConfigurationServiceClient<TeraSyncConfigurationBase>>();

        services.AddHostedService(p => (TeraConfigurationServiceClient<TeraSyncConfigurationBase>)p.GetService<IConfigurationService<TeraSyncConfigurationBase>>());
        services.AddHostedService(p => (TeraConfigurationServiceClient<ServerConfiguration>)p.GetService<IConfigurationService<ServerConfiguration>>());
    }
}