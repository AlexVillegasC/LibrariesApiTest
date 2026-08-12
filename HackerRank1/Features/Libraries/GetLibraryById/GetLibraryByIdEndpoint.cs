using LibraryService.WebAPI.Data;
using LibraryService.WebAPI.Shared.Endpoints;
using Microsoft.EntityFrameworkCore;

namespace LibraryService.WebAPI.Features.Libraries.GetLibraryById;

public class GetLibraryByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/libraries/{libraryId:int}", Handle);
    }

    private static async Task<IResult> Handle(int libraryId, LibraryContext db)
    {
        var library = await db.Libraries.FirstOrDefaultAsync(l => l.Id == libraryId);
        return library is null ? Results.NotFound() : Results.Ok(library);
    }
}
