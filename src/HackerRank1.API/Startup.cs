using LibraryService.Modules.Auth;
using LibraryService.Modules.Books;
using LibraryService.Modules.Libraries;
using LibraryService.SharedKernel.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace LibraryService.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<LibraryContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 1,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                })
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));

            services.AddAuth();
            services.AddLibraries();
            services.AddBooks();
            services.AddControllers();
            services.AddCors(o => o.AddPolicy("Frontend", p => p
                .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()));
            services.AddOpenApi(options =>
            {
                options.CreateSchemaReferenceId = info =>
                {
                    var type = info.Type;
                    if (type.IsNested && type.DeclaringType is { } declaring)
                        return $"{declaring.Name}.{type.Name}";

                    return OpenApiOptions.CreateDefaultSchemaReferenceId(info);
                };
                options.AddDocumentTransformer((document, _, _) =>
                {
                    document.Info.Title = "LibraryService API";
                    document.Info.Version = "v1";
                    document.Info.Description = "A simple example ASP.NET Core Web API for LibraryService";
                    return Task.CompletedTask;
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var isLocalDev = env.IsDevelopment() || env.IsEnvironment("Local");

            if (isLocalDev)
            {
                app.UseDeveloperExceptionPage();
            }

            using (var scope = app.ApplicationServices.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LibraryContext>();
                if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
                    db.Database.EnsureCreated();
                else
                    db.Database.Migrate();
                var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                IdentitySeed.EnsureRolesAsync(roles).GetAwaiter().GetResult();
                CatalogSeed.EnsureAsync(db).GetAwaiter().GetResult();
            }

            app.UseRouting();
            app.UseCors("Frontend");
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapLibraries();
                endpoints.MapBooks();

                if (isLocalDev)
                {
                    endpoints.MapOpenApi();
                    endpoints.MapScalarApiReference();
                }
            });
        }
    }
}
