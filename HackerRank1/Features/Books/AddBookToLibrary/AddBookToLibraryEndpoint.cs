using LibraryService.WebAPI.Data;
using LibraryService.WebAPI.Shared.Endpoints;
using Microsoft.EntityFrameworkCore;

namespace LibraryService.WebAPI.Features.Books.AddBookToLibrary;

public record AddBookRequest(string Name, string? Category);

public class AddBookToLibraryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/libraries/{libraryId:int}/books", Handle);
    }

    private static async Task<IResult> Handle(int libraryId, AddBookRequest request, LibraryContext db)
    {
        var libraryExists = await db.Libraries.AnyAsync(l => l.Id == libraryId);
        if (!libraryExists)
            return Results.NotFound();

        var book = new Book
        {
            Name = request.Name,
            Category = request.Category ?? string.Empty,
            LibraryId = libraryId
        };

        await db.Books.AddAsync(book);
        await db.SaveChangesAsync();
        return Results.Created($"/api/libraries/{libraryId}/books/{book.Id}", book);
    }
}
