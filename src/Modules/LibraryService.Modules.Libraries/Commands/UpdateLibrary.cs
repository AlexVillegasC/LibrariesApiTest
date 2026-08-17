using LibraryService.SharedKernel.Cqrs;
using LibraryService.SharedKernel.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryService.Modules.Libraries.Commands;

public static class UpdateLibrary
{
    public sealed class Request
    {
        public string? Name { get; set; }
        public string? Location { get; set; }
    }

    public sealed record Command(int Id, string? Name, string? Location);

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

            existing.Name = command.Name ?? string.Empty;
            existing.Location = command.Location ?? string.Empty;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
