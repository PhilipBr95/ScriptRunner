using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScriptRunner.Library.Repos;
using ScriptRunner.Library.Services;
using ScriptRunner.Library.Settings;

namespace ConsoleApp1
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            using IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddOptions<RepoSettings>();
                    services.AddTransient<INugetRepo, NugetRepo>();

                    services.AddTransient<IPackageExecutor, PackageExecutor>();
                    services.AddTransient<ISqlExecutor, SqlExecutor>();
                    services.AddTransient<IPowerShellExecutor, PowerShellExecutor>();
                })
                .Build();

            using IServiceScope scope = host.Services.CreateScope();
            var p = scope.ServiceProvider.GetRequiredService<INugetRepo>();
            var dd = p.GetScriptsAsync();

            //await host.RunAsync();
        }
    }
}