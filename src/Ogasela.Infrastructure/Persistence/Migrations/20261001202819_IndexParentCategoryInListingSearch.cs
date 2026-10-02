using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ogasela.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Listings now live under subcategories, so the search vector's category text only held the
    /// subcategory name ("Phones &amp; Tablets") and searching the parent ("Electronics") found
    /// nothing. Both triggers now index the category name plus its parent's name, via one shared
    /// helper, and every existing listing is re-indexed.
    /// </summary>
    public partial class IndexParentCategoryInListingSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION listing_category_search_text(category_id uuid) RETURNS text AS $$
                    SELECT COALESCE(c."Name", '') || ' ' || COALESCE(p."Name", '')
                    FROM "Categories" c
                    LEFT JOIN "Categories" p ON p."Id" = c."ParentCategoryId"
                    WHERE c."Id" = category_id;
                $$ LANGUAGE sql STABLE;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION listings_search_vector_update() RETURNS trigger AS $$
                BEGIN
                    NEW."SearchVector" :=
                        setweight(to_tsvector('english', COALESCE(NEW."Title", '')), 'A') ||
                        setweight(to_tsvector('english', COALESCE(NEW."Description", '')), 'B') ||
                        setweight(to_tsvector('english', COALESCE(listing_category_search_text(NEW."CategoryId"), '')), 'C');
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
                """);

            // A renamed parent changes the indexed text of every listing in its subcategories too.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION listings_search_vector_refresh_for_category() RETURNS trigger AS $$
                BEGIN
                    UPDATE "Listings" l
                    SET "SearchVector" =
                        setweight(to_tsvector('english', COALESCE(l."Title", '')), 'A') ||
                        setweight(to_tsvector('english', COALESCE(l."Description", '')), 'B') ||
                        setweight(to_tsvector('english', COALESCE(listing_category_search_text(l."CategoryId"), '')), 'C')
                    WHERE l."CategoryId" = NEW."Id"
                       OR l."CategoryId" IN (SELECT c."Id" FROM "Categories" c WHERE c."ParentCategoryId" = NEW."Id");
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
                """);

            migrationBuilder.Sql("""
                UPDATE "Listings" l
                SET "SearchVector" =
                    setweight(to_tsvector('english', COALESCE(l."Title", '')), 'A') ||
                    setweight(to_tsvector('english', COALESCE(l."Description", '')), 'B') ||
                    setweight(to_tsvector('english', COALESCE(listing_category_search_text(l."CategoryId"), '')), 'C');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS listing_category_search_text(uuid);");
        }
    }
}
