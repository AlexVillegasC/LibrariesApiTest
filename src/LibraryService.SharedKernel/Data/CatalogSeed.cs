using Microsoft.EntityFrameworkCore;

namespace LibraryService.SharedKernel.Data;

public static class CatalogSeed
{
    public static readonly (string Name, string Location, (string Name, string Category)[] Books)[] Branches =
    [
        ("Bodleian Library", "Oxford",
        [
            ("Principia Mathematica", "Science"),
            ("The Canterbury Tales", "Poetry"),
            ("Leviathan", "Philosophy")
        ]),
        ("New York Public Library", "New York",
        [
            ("The Great Gatsby", "Fiction"),
            ("A Brief History of Time", "Science"),
            ("Leaves of Grass", "Poetry")
        ]),
        ("Biblioteca Vasconcelos", "Mexico City",
        [
            ("One Hundred Years of Solitude", "Fiction"),
            ("Pedro Páramo", "Fiction"),
            ("The Labyrinth of Solitude", "Essay")
        ])
    ];

    public static async Task EnsureAsync(LibraryContext db, CancellationToken cancellationToken = default)
    {
        foreach (var (name, location, books) in Branches)
        {
            var library = await db.Libraries.FirstOrDefaultAsync(item => item.Name == name, cancellationToken);
            if (library is null)
            {
                library = new Library { Name = name, Location = location };
                db.Libraries.Add(library);
                await db.SaveChangesAsync(cancellationToken);
            }

            foreach (var (bookName, category) in books)
            {
                var exists = await db.Books.AnyAsync(
                    book => book.LibraryId == library.Id && book.Name == bookName,
                    cancellationToken);
                if (exists)
                    continue;

                db.Books.Add(new Book
                {
                    Name = bookName,
                    Category = category,
                    LibraryId = library.Id
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
