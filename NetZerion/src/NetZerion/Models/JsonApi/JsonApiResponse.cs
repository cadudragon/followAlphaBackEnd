using System.Text.Json.Serialization;

namespace NetZerion.Models.JsonApi;

/// <summary>
/// Represents a JSON:API response from Zerion.
/// </summary>
/// <typeparam name="T">Type of the data attributes.</typeparam>
public class JsonApiResponse<T>
{
    /// <summary>
    /// Array of resource objects (the primary data).
    /// </summary>
    [JsonPropertyName("data")]
    public List<JsonApiResource<T>> Data { get; set; } = new();

    /// <summary>
    /// Array of related resource objects (included resources).
    /// </summary>
    [JsonPropertyName("included")]
    public List<JsonApiResource<object>>? Included { get; set; }

    /// <summary>
    /// Links for pagination.
    /// </summary>
    [JsonPropertyName("links")]
    public JsonApiLinks? Links { get; set; }

    /// <summary>
    /// Meta information.
    /// </summary>
    [JsonPropertyName("meta")]
    public Dictionary<string, object>? Meta { get; set; }
}

/// <summary>
/// Represents a JSON:API resource object.
/// </summary>
/// <typeparam name="T">Type of the attributes.</typeparam>
public class JsonApiResource<T>
{
    /// <summary>
    /// Resource type.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Resource identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Resource attributes.
    /// </summary>
    [JsonPropertyName("attributes")]
    public T? Attributes { get; set; }

    /// <summary>
    /// Relationships to other resources.
    /// </summary>
    [JsonPropertyName("relationships")]
    public Dictionary<string, JsonApiRelationship>? Relationships { get; set; }
}

/// <summary>
/// Represents a JSON:API relationship.
/// </summary>
public class JsonApiRelationship
{
    /// <summary>
    /// Related resource linkage.
    /// </summary>
    [JsonPropertyName("data")]
    public JsonApiResourceIdentifier? Data { get; set; }

    /// <summary>
    /// Links related to this relationship.
    /// </summary>
    [JsonPropertyName("links")]
    public JsonApiLinks? Links { get; set; }
}

/// <summary>
/// Represents a JSON:API resource identifier.
/// </summary>
public class JsonApiResourceIdentifier
{
    /// <summary>
    /// Resource type.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Resource identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Represents JSON:API links.
/// </summary>
public class JsonApiLinks
{
    /// <summary>
    /// Link to the current page.
    /// </summary>
    [JsonPropertyName("self")]
    public string? Self { get; set; }

    /// <summary>
    /// Link to the next page.
    /// </summary>
    [JsonPropertyName("next")]
    public string? Next { get; set; }

    /// <summary>
    /// Link to the previous page.
    /// </summary>
    [JsonPropertyName("prev")]
    public string? Prev { get; set; }

    /// <summary>
    /// Link to the first page.
    /// </summary>
    [JsonPropertyName("first")]
    public string? First { get; set; }

    /// <summary>
    /// Link to the last page.
    /// </summary>
    [JsonPropertyName("last")]
    public string? Last { get; set; }
}
