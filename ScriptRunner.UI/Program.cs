using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScriptRunner.Library.Extensions;
using ScriptRunner.Library.Repos;
using ScriptRunner.Library.Services;
using ScriptRunner.Library.Settings;
using ScriptRunner.UI.Auth;
using ScriptRunner.UI.Services;
using ScriptRunner.UI.Settings;
using System.Security.Claims;

namespace ScriptRunner.UI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                            .AddNegotiate();

            builder.Services.AddAuthorization(options =>
            {                
                var webSettings = builder.Configuration.GetSection(nameof(WebSettings)).Get<WebSettings>();

                options.AddPolicy("AdminOnly", policy => policy.Requirements.Add(new CheckADGroupRequirement(webSettings.AdminAD)));
                options.FallbackPolicy = options.DefaultPolicy;
            });

            builder.Logging.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
            builder.Logging.AddMemoryLogger();

            // Add services to the container.
            builder.Services.AddRazorPages();

            //builder.Services.AddOptions<RepoSettings>();
            builder.Services.Configure<RepoSettings>(builder.Configuration.GetSection(nameof(RepoSettings)));
            builder.Services.AddTransient<INugetRepo, NugetRepo>();
            builder.Services.AddTransient<IScriptRepo, ScriptRepo>();
            builder.Services.AddTransient<IHistoryRepo, JsonHistoryRepo>();

            builder.Services.Configure<HistorySettings>(builder.Configuration.GetSection(nameof(HistorySettings)));
            builder.Services.AddTransient<IHistoryService, HistoryService>();

            builder.Services.AddTransient<ITransactionService, TransactionService>();
            builder.Services.AddTransient<IPackageRetriever, PackageRetriever>();
            builder.Services.AddTransient<IPackageExecutor, PackageExecutor>();
            builder.Services.AddTransient<ISqlExecutor, SqlExecutor>();
            builder.Services.AddTransient<IPowerShellExecutor, PowerShellExecutor>();
            builder.Services.AddSingleton<IMemoryCache, MemoryCache>();
            
            builder.Services.AddSingleton<IAuthorizationHandler, CheckADGroupHandler>();
            builder.Services.AddMvc(options =>
            {
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            });

            builder.Services.AddControllersWithViews()
                            .AddNewtonsoftJson();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller}/{action=Index}/{id?}");

            app.Run();
        }
    }
}