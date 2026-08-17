using LibraryService.Modules.Libraries.Commands;
using LibraryService.Modules.Libraries.Queries;
using LibraryService.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading;

namespace LibraryService.Modules.Libraries;

public static class LibrariesModule
{
    public static IServiceCollection AddLibraries(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListLibraries.Query, IReadOnlyList<ListLibraries.LibraryReadModel>>, ListLibraries.Handler>();
        services.AddScoped<IQueryHandler<GetLibrary.Query, GetLibrary.LibraryReadModel?>, GetLibrary.Handler>();
        services.AddScoped<ICommandHandler<CreateLibrary.Command, CreateLibrary.LibraryReadModel>, CreateLibrary.Handler>();
        services.AddScoped<ICommandHandler<UpdateLibrary.Command, bool>, UpdateLibrary.Handler>();
        services.AddScoped<ICommandHandler<DeleteLibrary.Command, bool>, DeleteLibrary.Handler>();
        return services;
    }

    public static IEndpointRouteBuilder MapLibraries(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/libraries");

        group.MapGet("/", async (
            IQueryHandler<ListLibraries.Query, IReadOnlyList<ListLibraries.LibraryReadModel>> handler,
            CancellationToken cancellationToken) =>
        {
            var libraries = await handler.Handle(new ListLibraries.Query(), cancellationToken);
            return Results.Ok(libraries);
        });

        group.MapGet("/{libraryId:int}", async (
            int libraryId,
            IQueryHandler<GetLibrary.Query, GetLibrary.LibraryReadModel?> handler,
            CancellationToken cancellationToken) =>
        {
            var library = await handler.Handle(new GetLibrary.Query(libraryId), cancellationToken);
            return library is null ? Results.NotFound() : Results.Ok(library);
        });

        group.MapPost("/", async (
            CreateLibrary.Request request,
            ICommandHandler<CreateLibrary.Command, CreateLibrary.LibraryReadModel> handler,
            CancellationToken cancellationToken) =>
        {
            var created = await handler.Handle(new CreateLibrary.Command(request.Name, request.Location), cancellationToken);
            return Results.Ok(created);
        });

        group.MapPut("/{libraryId:int}", async (
            int libraryId,
            UpdateLibrary.Request request,
            ICommandHandler<UpdateLibrary.Command, bool> handler,
            CancellationToken cancellationToken) =>
        {
            var updated = await handler.Handle(new UpdateLibrary.Command(libraryId, request.Name, request.Location), cancellationToken);
            return updated ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{libraryId:int}", async (
            int libraryId,
            ICommandHandler<DeleteLibrary.Command, bool> handler,
            CancellationToken cancellationToken) =>
        {
            var deleted = await handler.Handle(new DeleteLibrary.Command(libraryId), cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }
}
