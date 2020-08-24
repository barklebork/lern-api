using System.Diagnostics.CodeAnalysis;
using System.IO;
using FluentMigrator.Runner;
using Lern_API.Migrations;
using Lern_API.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nancy.Owin;

namespace Lern_API
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration.Config = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // Ajout du système de migration de bases de données
            services.AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddPostgres()
                    .WithGlobalConnectionString(Configuration.GetConnectionString())
                    .ScanIn(typeof(AddUserTable).Assembly).For.Migrations());
            
            // Ajout de log4net comme logging par défaut
            services.AddLogging(lb => lb.ClearProviders().AddLog4Net());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IMigrationRunner migrationRunner)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles(new StaticFileOptions(new SharedOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Content"))
            }));

            app.UseOwin(x => x.UseNancy(new NancyOptions
            {
                Bootstrapper = new LernBootstrapper(migrationRunner, env.IsProduction())
            }));
        }
    }
}
