using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Aseprite.Types;
using AsepriteDotNet.Processors;
using Point = AsepriteDotNet.Common.Point;

namespace AnimLib.Animations.Aseprite.Processors;

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
  public static AnimSpriteSheet Process(AsepriteFile file, ProcessorOptions? options = null) {
    ArgumentNullException.ThrowIfNull(file, nameof(file));
    options ??= ProcessorOptions.Default;

    var fileTags = file.Tags;

    var tags = new AnimTag[fileTags.Length];
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

      tags[i] = AnimTag.FromAse(SpriteSheetProcessor.ProcessTag(aseTag, file.Frames));
    }

    var textureAtlases = AnimTextureAtlasProcessor.Process(file, options);

    var pointDict = ProcessPoints(file, options);

    return new AnimSpriteSheet(textureAtlases, tags, pointDict);
  }

  private static Dictionary<string, Vector2[]> ProcessPoints(AsepriteFile file, ProcessorOptions options) {
    Dictionary<string, Vector2[]> result = [];

    float scale = file.UserData.HasText && file.UserData.Text.Contains("upscale") ? 2 : 1;
    Vector2 center = new Vector2(file.CanvasWidth, file.CanvasHeight) * scale / 2;

    foreach (AsepriteLayer layer in file.Layers) {
      if (layer is { ChildLevel: 0, UserData.HasColor: true } && layer.UserData.Color == UserDataColors.Yellow) {
        var array = new Vector2[file.FrameCount];
        Array.Fill(array, center);
        result.Add(layer.Name, array);
      }
    }

    var frames = file.Frames;
    for (int i = 0; i < frames.Length; i++) {
      foreach (AsepriteCel cel in frames[i].Cels) {
        if (cel is AsepriteImageCel imageCel && result.TryGetValue(cel.Layer.Name, out var array)) {
          Point pos = imageCel.Location;
          array[i] = new Vector2(pos.X + 0.5f, pos.Y + 0.5f) * scale;
        }
      }
    }

    return result;
  }
}
