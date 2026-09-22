using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Ogasela.Api.OpenApi;

/// <summary>
/// Generates a realistic payload example for every request/response schema in the OpenAPI
/// document (surfaced by /api/v1/bira-doc's "Test Request" panel), driven entirely by property
/// name/type heuristics - so every DTO gets a sample without hand-authoring one per route. Add a
/// new name pattern below if a field's generated example is misleading; the fallback (type-based)
/// still produces something schema-valid even for a name this doesn't recognize.
/// </summary>
public sealed class ExampleSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (schema.Properties is not { Count: > 0 })
        {
            return Task.CompletedTask;
        }

        var example = new JsonObject();
        foreach (var (propertyName, propertySchema) in schema.Properties)
        {
            example[propertyName] = BuildValue(propertyName, propertySchema);
        }

        schema.Examples = [example];
        return Task.CompletedTask;
    }

    private static JsonNode? BuildValue(string propertyName, IOpenApiSchema propertySchema)
    {
        // JsonSchemaType is a [Flags] enum and a nullable property's Type carries the Null flag
        // alongside its real type (e.g. Null|Array for List<string>?), so these must check the
        // flag rather than compare Type for exact equality.
        var type = propertySchema.Type ?? JsonSchemaType.String;

        // Structural types are handled first and recurse with the same property name, so a
        // name-based leaf match (e.g. "mediaUrls" -> a URL) still applies per-item rather than
        // wrongly replacing the whole array/object with a scalar.
        if (type.HasFlag(JsonSchemaType.Array) && propertySchema.Items is not null)
        {
            var item = BuildValue(propertyName, propertySchema.Items);
            var array = new JsonArray();
            if (item is not null)
            {
                array.Add(item);
            }

            return array;
        }

        var byName = ExampleForPropertyName(propertyName);
        if (byName is not null)
        {
            return byName;
        }

        if (propertySchema.Enum is { Count: > 0 } enumValues)
        {
            return JsonNode.Parse(enumValues[0].ToJsonString());
        }

        if (type.HasFlag(JsonSchemaType.String))
        {
            return propertySchema.Format switch
            {
                "uuid" => "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                "date-time" => "2026-01-15T09:30:00Z",
                "date" => "2026-01-15",
                _ => "string"
            };
        }

        if (type.HasFlag(JsonSchemaType.Integer))
        {
            return 1;
        }

        if (type.HasFlag(JsonSchemaType.Number))
        {
            return 1000;
        }

        if (type.HasFlag(JsonSchemaType.Boolean))
        {
            return true;
        }

        return null;
    }

    /// <summary>Ordinal, case-insensitive "ends with"/"contains" matches against common DTO field names across this API.</summary>
    private static JsonNode? ExampleForPropertyName(string name)
    {
        bool Is(string suffix) => name.Equals(suffix, StringComparison.OrdinalIgnoreCase);
        bool Ends(string suffix) => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        bool Has(string part) => name.Contains(part, StringComparison.OrdinalIgnoreCase);

        return name switch
        {
            _ when Is("phone") || Is("phoneNumber") || Is("targetPhone") => "08031234567",
            _ when Is("email") => "ada@example.com",
            _ when Is("password") => "Sup3rSecret!",
            _ when Is("code") => "482913",
            _ when Is("refreshToken") => "8f14e45fceea167a5a36dedd4bea2543",
            _ when Is("accessToken") => "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
            _ when Is("businessName") => "Ada's Fabrics",
            _ when Is("rcNumber") => "RC1234567",
            _ when Is("nin") => "12345678901",
            _ when Is("title") || Is("adCopyTitle") => "iPhone 13 Pro Max - Excellent Condition",
            _ when Is("description") || Is("adCopyDescription") || Is("keywords") => "Barely used, comes with original box and charger.",
            _ when Is("reason") => "This listing looks fraudulent",
            _ when Is("notes") => "Reviewed against the submitted ID photo - looks valid",
            _ when Is("comment") => "Fast response and item exactly as described.",
            _ when Is("content") => "Hi, is this still available?",
            _ when Is("query") => "iPhone 13",
            _ when Is("location") => "Ikeja, Lagos",
            _ when Is("audience") => "Lagos, 18-35, interested in Electronics",
            _ when Is("imageUrl") || Is("imageRef") || Is("mediaUrls") || Is("mediaUrl") => "https://cdn.ogasela.com/listings/photo1.jpg",
            _ when Is("livenessSessionRef") => "liveness-session-9f8c3b",
            _ when Is("rating") => 5,
            _ when Is("page") => 1,
            _ when Is("pageSize") => 20,
            _ when Is("durationDays") => 14,
            _ when Is("attemptNumber") || Is("attributeSchemaVersion") => 1,
            _ when Is("latitude") || Is("lat") => 6.5244m,
            _ when Is("longitude") || Is("lng") => 3.3792m,
            _ when Is("radiusKm") => 10,
            _ when Ends("kobo") => 500000,
            _ when Ends("price") || Is("minPrice") || Is("maxPrice") => 250000,
            _ when Has("percentage") || Has("rate") => 0.75,
            _ => null
        };
    }
}
