using AsepriteDotNet;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Aseprite.Types;
using AsepriteDotNet.Processors;

namespace AnimLib.Aseprite.Processors;

/// <summary>
/// Defines a processor for processing an <see cref="AnimLibMod"/> <see cref="AnimSpriteSheet"/> from an <see cref="AsepriteFile"/>.
/// </summary>
public static class AnimSpriteSheetProcessor {
  /// <summary>
  /// Processes an <see cref="AnimLibMod"/> <see cref="AnimSpriteSheet"/>
  /// </summary>
  /// <param name="file">The <see cref="AsepriteFile"/> to process.</param>
  /// <param name="options">Optional <see cref="ProcessorOptions"/> used in processing the <see cref="AsepriteFile"/>.</param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  public static AnimSpriteSheet Process(AsepriteFile file, [CanBeNull] ProcessorOptions options = null) {
    ArgumentNullException.ThrowIfNull(file, nameof(file));
    options ??= ProcessorOptions.Default;

    var fileTags = file.Tags;

    var tags = new AnimationTag[fileTags.Length];
    var tagHashes = fileTags.Length < 256 ? stackalloc int[fileTags.Length] : new int[fileTags.Length];
    for (int i = 0; i < tagHashes.Length; i++) {
      tagHashes[i] = 0;
    }

    for (int i = 0; i < fileTags.Length; i++) {
      AsepriteTag aseTag = fileTags[i];
      int hash = aseTag.Name.GetHashCode();
      if (tagHashes.Contains(hash)) {
        throw new InvalidOperationException("Duplicate tag name '" + aseTag.Name +
          "' found.  Tags must have unique names for a sprite sheet");
      }

      tagHashes[i] = hash;

      tags[i] = SpriteSheetProcessor.ProcessTag(aseTag, file.Frames);
    }

    var textureAtlases = AnimTextureAtlasProcessor.Process(file, options);

    return new AnimSpriteSheet(textureAtlases, tags);
  }
}
