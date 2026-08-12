using LibraryService.WebAPI.Data;
using LibraryService.WebAPI.Shared.Endpoints;
using Microsoft.EntityFrameworkCore;

namespace LibraryService.WebAPI.Features.Libraries.DeleteLibrary;

public class DeleteLibraryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/libraries/{libraryId:int}", Handle);
    }

    private static async Task<IResult> Handle(int libraryId, LibraryContext db)
    {
        var library = await db.Libraries.FirstOrDefaultAsync(l => l.Id == libraryId);
        if (library is null)
            return Results.NotFound();

        db.Libraries.Remove(library);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}
