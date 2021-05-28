using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using FluentEmail.MailKitSmtp;
using FluentValidation.AspNetCore;
using Lern_API.DataTransferObjects.Requests;
using Lern_API.Helpers;
using Lern_API.Helpers.JWT;
using Lern_API.Helpers.Swagger;
using Lern_API.Services;
using Lern_API.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

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
            // Ajout du système de communication avec la base de données
            services.AddDbContext<LernContext>(options => options.UseNpgsql(Configuration.GetConnectionString()));

            services.AddFluentEmail(Configuration.Get<string>("SenderEmail"), Configuration.Get<string>("SenderName"))
                .AddLiquidRenderer(options =>
                {
                    options.FileProvider =
                        new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Templates"));
                })
                .AddMailKitSender(new SmtpClientOptions
                {
                    Server = Configuration.Get<string>("SmtpServer"),
                    Port = Configuration.Get<int>("SmtpPort"),
                    User = Configuration.Get<string>("SmtpUser"),
                    RequiresAuthentication = !string.IsNullOrEmpty(Configuration.Get<string>("SmtpUser")),
                    UseSsl = Configuration.Get<bool>("SmtpUseSsl")
                });

            // Ajout des contrôleurs applicatifs
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                })
                // Configuration de la validation des modèles
                .AddFluentValidation(fv =>
                {
                    fv.RegisterValidatorsFromAssemblyContaining<UserRequestValidator>();
                    fv.RegisterValidatorsFromAssemblyContaining<LoginRequestValidator>();
                    fv.ImplicitlyValidateChildProperties = true;
                })
                // Configuration de la réponse à un modèle invalide
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = c => new BadRequestObjectResult(new
                    {
                        Errors = c.ModelState.Values.Where(v => v.Errors.Count > 0).SelectMany(v => v.Errors).Select(v => v.ErrorMessage)
                    });
                });

            // Ajout de la compression des réponses
            services.AddResponseCompression(options =>
            {
                options.MimeTypes = Configuration.GetList("GzipSupportedMimeTypes");
            });

            // Ajout de l'auto-génération de Swagger
            services.AddSwaggerGen(options =>
            {
                var info = new OpenApiInfo
                {
                    Title = "Lern. API",
                    Version = "1.0-SNAPSHOT",
                    Description = ""
                };

                options.SwaggerDoc("v1", info);

                options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
                {
                    Description = @"JWT Authorization header using the Bearer scheme.
                                    <br><br>Enter your token in the text input below.",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
                options.IncludeXmlComments(xmlPath);

                options.OperationFilter<AddAuthHeaderOperationFilter>();
                options.SchemaFilter<ReadOnlyPropertiesFilter>();
            });

            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "Content";
            });

            // Ajout des en-têtes CORS
            services.AddCors();

            // Ajout des services
            services.AddSingleton<IMailService, MailService>();
            services.AddScoped(typeof(IDatabaseService<,>), typeof(DatabaseService<,>));
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ISubjectService, SubjectService>();
            services.AddScoped<IQuestionService, QuestionService>();
            services.AddScoped<IProgressionService, ProgressionService>();
            services.AddScoped<IAuthorizationService, AuthorizationService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, LernContext context)
        {
            loggerFactory.AddLog4Net();

            Retry.Do(() => context.Database.Migrate(), TimeSpan.FromSeconds(3), 20);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(options =>
            {
                options.AllowAnyOrigin();
                options.AllowAnyHeader();
                options.AllowAnyMethod();
            });

            if (env.IsDevelopment())
            {
                app.UseSwagger().UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lern. API");
                });
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseResponseCompression();

            app.UseMiddleware<JwtMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSpaStaticFiles();
            app.UseSpa(spa =>
            {
                spa.Options.DefaultPage = "/index.html";
            });
        }
    }
}
