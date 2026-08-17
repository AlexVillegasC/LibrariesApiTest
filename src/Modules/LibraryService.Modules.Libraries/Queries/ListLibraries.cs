using System.Data;
using Dapper;
using LibraryService.SharedKernel.Cqrs;
using LibraryService.SharedKernel.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryService.Modules.Libraries.Queries;

public static class ListLibraries
{
    public sealed record Query;

    public sealed class LibraryReadModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }

    public sealed class Handler : IQueryHandler<Query, IReadOnlyList<LibraryReadModel>>
    {
        private readonly LibraryContext _context;

        public Handler(LibraryContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<LibraryReadModel>> Handle(Query query, CancellationToken cancellationToken = default)
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            var command = new CommandDefinition(
                "SELECT \"Id\", \"Name\", \"Location\" FROM \"Libraries\"",
                cancellationToken: cancellationToken);

            var libraries = await connection.QueryAsync<LibraryReadModel>(command);
            return libraries.ToList();
        }
    }
}
