using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ogasela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListingSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Listings",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Listings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Listings",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            // SearchVector is deliberately not part of the EF model (see ListingConfiguration) -
            // it's added and maintained entirely through raw SQL/triggers below.
            migrationBuilder.Sql("""ALTER TABLE "Listings" ADD COLUMN "SearchVector" tsvector;""");

            // Enables similarity()/gin_trgm_ops, used as a fuzzy fallback for short/typo'd
            // keywords that a plain tsvector match would miss.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            // SearchVector combines Title, Description AND the listing's Category name - a
            // native GENERATED column can't reference another table, so it's maintained by
            // triggers instead. Weighted ('A' > 'B' > 'C') so ts_rank favors title matches.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION listings_search_vector_update() RETURNS trigger AS $$
                BEGIN
                    NEW."SearchVector" :=
                        setweight(to_tsvector('english', COALESCE(NEW."Title", '')), 'A') ||
                        setweight(to_tsvector('english', COALESCE(NEW."Description", '')), 'B') ||
                        setweight(to_tsvector('english', COALESCE(
                            (SELECT c."Name" FROM "Categories" c WHERE c."Id" = NEW."CategoryId"), '')), 'C');
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER listings_search_vector_trigger
                BEFORE INSERT OR UPDATE OF "Title", "Description", "CategoryId" ON "Listings"
                FOR EACH ROW EXECUTE FUNCTION listings_search_vector_update();
                """);

            // Keeps existing listings' SearchVector in sync if their category is later renamed.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION listings_search_vector_refresh_for_category() RETURNS trigger AS $$
                BEGIN
                    UPDATE "Listings" l
                    SET "SearchVector" =
                        setweight(to_tsvector('english', COALESCE(l."Title", '')), 'A') ||
                        setweight(to_tsvector('english', COALESCE(l."Description", '')), 'B') ||
                        setweight(to_tsvector('english', COALESCE(NEW."Name", '')), 'C')
                    WHERE l."CategoryId" = NEW."Id";
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER categories_name_change_trigger
                AFTER UPDATE OF "Name" ON "Categories"
                FOR EACH ROW EXECUTE FUNCTION listings_search_vector_refresh_for_category();
                """);

            migrationBuilder.Sql("""CREATE INDEX "IX_Listings_SearchVector" ON "Listings" USING gin ("SearchVector");""");
            migrationBuilder.Sql(
                """CREATE INDEX "IX_Listings_Title_Trgm" ON "Listings" USING gin ("Title" gin_trgm_ops);""");

            // Backfills any rows that already existed before this migration ran (none in a
            // fresh database, but keeps this correct against a populated one).
            migrationBuilder.Sql("""
                UPDATE "Listings" l
                SET "SearchVector" =
                    setweight(to_tsvector('english', COALESCE(l."Title", '')), 'A') ||
                    setweight(to_tsvector('english', COALESCE(l."Description", '')), 'B') ||
                    setweight(to_tsvector('english', COALESCE(
                        (SELECT c."Name" FROM "Categories" c WHERE c."Id" = l."CategoryId"), '')), 'C');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TRIGGER IF EXISTS categories_name_change_trigger ON "Categories";""");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS listings_search_vector_refresh_for_category();");
            migrationBuilder.Sql("""DROP TRIGGER IF EXISTS listings_search_vector_trigger ON "Listings";""");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS listings_search_vector_update();");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Listings_SearchVector";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Listings_Title_Trgm";""");
            migrationBuilder.Sql("""ALTER TABLE "Listings" DROP COLUMN IF EXISTS "SearchVector";""");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Listings");

            // pg_trgm is left installed on Down - other features may depend on it and dropping
            // a shared extension from a "remove one feature" migration is riskier than keeping it.
        }
    }
}
