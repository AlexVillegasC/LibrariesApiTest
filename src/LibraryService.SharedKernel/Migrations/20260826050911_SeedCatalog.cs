using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryService.SharedKernel.Migrations
{
    /// <inheritdoc />
    public partial class SeedCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "Libraries" ("Name", "Location")
                SELECT v."Name", v."Location"
                FROM (VALUES
                    ('Bodleian Library', 'Oxford'),
                    ('New York Public Library', 'New York'),
                    ('Biblioteca Vasconcelos', 'Mexico City')
                ) AS v("Name", "Location")
                WHERE NOT EXISTS (
                    SELECT 1 FROM "Libraries" existing
                    WHERE existing."Name" = v."Name"
                );

                INSERT INTO "Books" ("Name", "Category", "LibraryId")
                SELECT v."Name", v."Category", libraries."Id"
                FROM (VALUES
                    ('Bodleian Library', 'Principia Mathematica', 'Science'),
                    ('Bodleian Library', 'The Canterbury Tales', 'Poetry'),
                    ('Bodleian Library', 'Leviathan', 'Philosophy'),
                    ('New York Public Library', 'The Great Gatsby', 'Fiction'),
                    ('New York Public Library', 'A Brief History of Time', 'Science'),
                    ('New York Public Library', 'Leaves of Grass', 'Poetry'),
                    ('Biblioteca Vasconcelos', 'One Hundred Years of Solitude', 'Fiction'),
                    ('Biblioteca Vasconcelos', 'Pedro Páramo', 'Fiction'),
                    ('Biblioteca Vasconcelos', 'The Labyrinth of Solitude', 'Essay')
                ) AS v("LibraryName", "Name", "Category")
                INNER JOIN "Libraries" libraries ON libraries."Name" = v."LibraryName"
                WHERE NOT EXISTS (
                    SELECT 1 FROM "Books" existing
                    WHERE existing."LibraryId" = libraries."Id"
                      AND existing."Name" = v."Name"
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "Books"
                WHERE "LibraryId" IN (
                    SELECT "Id" FROM "Libraries"
                    WHERE "Name" IN (
                        'Bodleian Library',
                        'New York Public Library',
                        'Biblioteca Vasconcelos'
                    )
                );

                DELETE FROM "Libraries"
                WHERE "Name" IN (
                    'Bodleian Library',
                    'New York Public Library',
                    'Biblioteca Vasconcelos'
                );
                """);
        }
    }
}
