using LibraryService.SharedKernel.Cqrs;
using LibraryService.SharedKernel.Data;

namespace LibraryService.Modules.Libraries.Commands;

public static class CreateLibrary
{
    public sealed class Request
    {
        public string? Name { get; set; }
        public string? Location { get; set; }
    }

    public sealed record Command(string? Name, string? Location);

    public sealed class LibraryReadModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }

    public sealed class Handler : ICommandHandler<Command, LibraryReadModel>
    {
        private readonly LibraryContext _context;

        public Handler(LibraryContext context)
        {
            _context = context;
        }

        public async Task<LibraryReadModel> Handle(Command command, CancellationToken cancellationToken = default)
        {
            var library = new Library
            {
                Name = command.Name ?? string.Empty,
                Location = command.Location ?? string.Empty
            };

            await _context.Libraries.AddAsync(library, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return new LibraryReadModel
            {
                Id = library.Id,
                Name = library.Name,
                Location = library.Location
            };
        }
    }
}
