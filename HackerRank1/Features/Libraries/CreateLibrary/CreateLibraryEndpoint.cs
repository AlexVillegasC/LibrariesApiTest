using LibraryService.WebAPI.Data;
using LibraryService.WebAPI.Shared.Endpoints;

namespace LibraryService.WebAPI.Features.Libraries.CreateLibrary;

public record CreateLibraryRequest(string Name, string Location);

public class CreateLibraryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/libraries", Handle);
    }

    private static async Task<IResult> Handle(CreateLibraryRequest request, LibraryContext db)
    {
        var library = new Library
        {
            Name = request.Name,
            Location = request.Location
        };

        await db.Libraries.AddAsync(library);
        await db.SaveChangesAsync();
        return Results.Ok(library);
    }
}
