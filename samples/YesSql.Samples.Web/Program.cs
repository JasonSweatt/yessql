using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace YesSql.Samples.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = GetConfiguration(args);
            /*
            Log.Logger = new LoggerConfiguration()
                         .Filter.ByExcluding(le => Matching.FromSource("TELEMETRY_LOG").Invoke(le) && le.Level < LogEventLevel.Warning)
                         .Enrich.FromLogContext()
                         .MinimumLevel.Warning()

                         // Exclude debug logs coming from the ASP.NET runtime 
                         .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                         .MinimumLevel.Override("System", LogEventLevel.Warning)
                         .Enrich.FromLogContext()
                         .WriteTo.Console(formatter: new StackDriverJsonFormatter())
                         .WriteTo.Logger(l => l.MinimumLevel.Verbose().WriteTo.Console())
                         /*
                         .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(configuration.GetValue<string>("elasticsearch:uri"))) {
                             IndexFormat = $"{configuration.GetValue<string>("elasticsearch:serilogIndex")}",
                             AutoRegisterTemplate = true,
                             AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6,
                             FailureCallback = e => Console.WriteLine($"Unable to submit event {e.MessageTemplate}"),
                             EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                                                EmitEventFailureHandling.WriteToFailureSink |
                                                EmitEventFailureHandling.RaiseCallback,
                             //FailureSink = new FileSink("./failures.txt", new JsonFormatter(), null),
                             MinimumLogEventLevel = LogEventLevel.Warning,
                             NumberOfShards = configuration.GetValue<int>("elasticsearch:numberOfShards"),
                             NumberOfReplicas = configuration.GetValue<int>("elasticsearch:numberOfReplicas")
                         })
                         .ReadFrom.Configuration(configuration)
                         .CreateLogger();
            */
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>IConfiguration.</returns>
        private static Microsoft.Extensions.Configuration.IConfiguration GetConfiguration(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isDevelopment = environment == Environments.Development;

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddJsonFile("serilog.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"serilog.{environment}.json", optional: true, reloadOnChange: true);

            configurationBuilder.AddCommandLine(args);
            configurationBuilder.AddEnvironmentVariables();
            return configurationBuilder.Build();
        }

        /// <summary>
        /// Creates the host builder.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>IHostBuilder.</returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                //.UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    var configurationRoot = configApp.Build();
                    var env = hostContext.HostingEnvironment;
                    configApp.AddJsonFile("serilog.json", optional: true, reloadOnChange: true);
                    configApp.AddJsonFile($"serilog.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    configApp.AddEnvironmentVariables();
                    configApp.AddCommandLine(args);
                })
                //.ConfigureServices(services => services.AddAutofac())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // TODO for https

                    // webBuilder.ConfigureKestrel(options => options.AddServerHeader = false);
                    webBuilder.UseStartup<Startup>();
                })
                /*
                .UseSerilog((hostContext, loggerConfig) =>
                {
                    loggerConfig
                        .ReadFrom.Configuration(hostContext.Configuration)
                        .Enrich.WithProperty("ApplicationName", hostContext.HostingEnvironment.ApplicationName);
                })
                */;
        
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}