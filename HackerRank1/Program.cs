using LibraryService.WebAPI.Data;
using LibraryService.WebAPI.Shared.Auth;
using LibraryService.WebAPI.Shared.Endpoints;
using Microsoft.EntityFrameworkCore;

namespace LibraryService.WebAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddFeatureEndpoints(typeof(Program).Assembly);

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "LibraryService API v1");
            });
        }

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LibraryContext>();
            if (db.Database.IsNpgsql())
                db.Database.Migrate();
            else
                db.Database.EnsureCreated();
        }

        app.UseCors("Frontend");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapFeatureEndpoints();

        app.Run();
    }
}
