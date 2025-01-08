namespace AnimLib.Animations;

/// <summary>
/// This class serves as an alias for <see cref="Dictionary{TKey, TValue}"/>.
/// This instance is created during <see cref="ModContent.Request{T}"/> by
/// <see cref="AnimLib.Aseprite.Processors.TextureDictionaryProcessor"/>,
/// when <see cref="AnimLib.Aseprite.AseReader"/> processes an Aseprite file.
/// </summary>
// Currently we store this after requesting as a Dictionary<string, Asset<Texture2D>>.
// Should we instead store as an Asset<Dictionary<string, Texture2D>> instead?
// Downside is that some fields expect an Asset<Texture2D>, which cannot be used with this.
public class TextureDictionary() : Dictionary<string, Asset<Texture2D>>(StringComparer.OrdinalIgnoreCase);
