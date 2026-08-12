using LibraryService.WebAPI.Data;
using LibraryService.WebAPI.Shared.Endpoints;
using Microsoft.EntityFrameworkCore;

namespace LibraryService.WebAPI.Features.Books.GetBooksByLibrary;

public class GetBooksByLibraryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/libraries/{libraryId:int}/books", Handle)
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(int libraryId, LibraryContext db)
    {
        var libraryExists = await db.Libraries.AnyAsync(l => l.Id == libraryId);
        if (!libraryExists)
            return Results.NotFound();

        var books = await db.Books.Where(b => b.LibraryId == libraryId).ToListAsync();
        return Results.Ok(books);
    }
}
