using LibraryService.WebAPI.Data;
using LibraryService.WebAPI.Shared.Endpoints;
using Microsoft.EntityFrameworkCore;

namespace LibraryService.WebAPI.Features.Libraries.UpdateLibrary;

public record UpdateLibraryRequest(string Name, string Location);

public class UpdateLibraryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/libraries/{libraryId:int}", Handle);
    }

    private static async Task<IResult> Handle(int libraryId, UpdateLibraryRequest request, LibraryContext db)
    {
        var existing = await db.Libraries.FirstOrDefaultAsync(l => l.Id == libraryId);
        if (existing is null)
            return Results.NotFound();

        existing.Name = request.Name;
        existing.Location = request.Location;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}
