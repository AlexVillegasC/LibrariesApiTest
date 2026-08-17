using System.Data;
using Dapper;
using LibraryService.SharedKernel.Cqrs;
using LibraryService.SharedKernel.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryService.Modules.Books.Queries;

public static class ListBooks
{
    public sealed record Query(int LibraryId);

    public sealed class BookReadModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int LibraryId { get; set; }
    }

    public sealed class Handler : IQueryHandler<Query, IReadOnlyList<BookReadModel>?>
    {
        private readonly LibraryContext _context;

        public Handler(LibraryContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<BookReadModel>?> Handle(Query query, CancellationToken cancellationToken = default)
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            var existsCommand = new CommandDefinition(
                "SELECT 1 FROM \"Libraries\" WHERE \"Id\" = @LibraryId",
                new { query.LibraryId },
                cancellationToken: cancellationToken);

            var libraryExists = await connection.ExecuteScalarAsync<int?>(existsCommand);
            if (libraryExists is null)
                return null;

            var listCommand = new CommandDefinition(
                "SELECT \"Id\", \"Name\", \"Category\", \"LibraryId\" FROM \"Books\" WHERE \"LibraryId\" = @LibraryId",
                new { query.LibraryId },
                cancellationToken: cancellationToken);

            var books = await connection.QueryAsync<BookReadModel>(listCommand);
            return books.ToList();
        }
    }
}
