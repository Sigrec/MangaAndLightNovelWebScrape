using System.Text.Json;
using System.Text.Json.Serialization;
using MangaAndLightNovelWebScrape.Models;

namespace MangaAndLightNovelWebScrape;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for the AniList request/response
/// types used by <see cref="TranslateAPI"/>. Compile-time-generated reader/writer code
/// keeps the JSON path AOT-safe and zero-reflection.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="JsonSourceGenerationOptionsAttribute.PropertyNameCaseInsensitive"/> is set so
/// callers can deserialize AniList responses regardless of incidental case drift on lesser-used
/// fields. The leaf records use explicit <c>[JsonPropertyName]</c> attributes for the exact
/// names that matter (e.g. capital <c>Media</c>).
/// </para>
/// <para>
/// Add a new <c>[JsonSerializable(typeof(T))]</c> line whenever a new request or response
/// shape is introduced — the generator does the rest.
/// </para>
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AniListRequest))]
[JsonSerializable(typeof(AniListResponse))]
internal sealed partial class AniListJsonContext : JsonSerializerContext;
