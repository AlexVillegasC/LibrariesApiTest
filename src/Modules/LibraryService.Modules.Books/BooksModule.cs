using LibraryService.Modules.Books.Commands;
using LibraryService.Modules.Books.Queries;
using LibraryService.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryService.Modules.Books;

public static class BooksModule
{
    public static IServiceCollection AddBooks(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListBooks.Query, IReadOnlyList<ListBooks.BookReadModel>?>, ListBooks.Handler>();
        services.AddScoped<ICommandHandler<CreateBook.Command, CreateBook.BookReadModel?>, CreateBook.Handler>();
        return services;
    }

    public static IEndpointRouteBuilder MapBooks(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/libraries/{libraryId:int}/books")
            .RequireAuthorization();

        group.MapGet("/", async (
            int libraryId,
            IQueryHandler<ListBooks.Query, IReadOnlyList<ListBooks.BookReadModel>?> handler,
            CancellationToken cancellationToken) =>
        {
            var books = await handler.Handle(new ListBooks.Query(libraryId), cancellationToken);
            return books is null ? Results.NotFound() : Results.Ok(books);
        }).WithName("ListBooks");

        group.MapPost("/", async (
            int libraryId,
            CreateBook.Request request,
            ICommandHandler<CreateBook.Command, CreateBook.BookReadModel?> handler,
            CancellationToken cancellationToken) =>
        {
            var book = await handler.Handle(
                new CreateBook.Command(libraryId, request.Name, request.Category),
                cancellationToken);

            return book is null
                ? Results.NotFound()
                : TypedResults.CreatedAtRoute(book, "ListBooks", new { libraryId });
        });

        return app;
    }
}
