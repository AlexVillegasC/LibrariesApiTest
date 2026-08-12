using LibraryService.WebAPI.Data;
using LibraryService.WebAPI.Shared.Endpoints;
using Microsoft.EntityFrameworkCore;

namespace LibraryService.WebAPI.Features.Libraries.GetAllLibraries;

public class GetAllLibrariesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/libraries", Handle);
    }

    private static async Task<IResult> Handle(LibraryContext db)
    {
        var libraries = await db.Libraries.ToListAsync();
        return Results.Ok(libraries);
    }
}
