using LibraryService.SharedKernel.Cqrs;
using LibraryService.SharedKernel.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryService.Modules.Libraries.Commands;

public static class DeleteLibrary
{
    public sealed record Command(int Id);

    public sealed class Handler : ICommandHandler<Command, bool>
    {
        private readonly LibraryContext _context;

        public Handler(LibraryContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(Command command, CancellationToken cancellationToken = default)
        {
            var existing = await _context.Libraries.SingleOrDefaultAsync(l => l.Id == command.Id, cancellationToken);
            if (existing is null)
                return false;

            var books = await _context.Books.Where(b => b.LibraryId == command.Id).ToListAsync(cancellationToken);
            _context.Books.RemoveRange(books);
            _context.Libraries.Remove(existing);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
