using LibraryService.SharedKernel.Cqrs;
using LibraryService.SharedKernel.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryService.Modules.Books.Commands;

public static class CreateBook
{
    public sealed class Request
    {
        public string? Name { get; set; }
        public string? Category { get; set; }
    }

    public sealed record Command(int LibraryId, string? Name, string? Category);

    public sealed class BookReadModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int LibraryId { get; set; }
    }

    public sealed class Handler : ICommandHandler<Command, BookReadModel?>
    {
        private readonly LibraryContext _context;

        public Handler(LibraryContext context)
        {
            _context = context;
        }

        public async Task<BookReadModel?> Handle(Command command, CancellationToken cancellationToken = default)
        {
            var libraryExists = await _context.Libraries.AnyAsync(l => l.Id == command.LibraryId, cancellationToken);
            if (!libraryExists)
                return null;

            var book = new Book
            {
                Name = command.Name ?? string.Empty,
                Category = command.Category ?? string.Empty,
                LibraryId = command.LibraryId
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync(cancellationToken);

            return new BookReadModel
            {
                Id = book.Id,
                Name = book.Name,
                Category = book.Category,
                LibraryId = book.LibraryId
            };
        }
    }
}
