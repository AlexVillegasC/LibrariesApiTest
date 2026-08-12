using Microsoft.AspNetCore.Routing;

namespace LibraryService.WebAPI.Shared.Endpoints;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
