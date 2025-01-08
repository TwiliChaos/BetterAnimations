using AnimLib.Animations;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Aseprite.Types;
using AsepriteDotNet.Processors;
using Point = AsepriteDotNet.Common.Point;

namespace AnimLib.Aseprite.Processors;

/// <summary>
/// Defines a processor for processing an <see cref="AnimLibMod"/> <see cref="AsepriteFile"/> from an <see cref="AnimSpriteSheet"/>.
/// </summary>
public class AnimSpriteSheetProcessor : IAsepriteProcessor<AnimSpriteSheet> {
  /// <summary>
  /// Processes an <see cref="AnimLibMod"/> <see cref="AnimSpriteSheet"/>
  /// </summary>
  /// <param name="file">The <see cref="AsepriteFile"/> to process.</param>
  /// <param name="options">Optional <see cref="ProcessorOptions"/> used in processing the <see cref="AsepriteFile"/>.</param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  public AnimSpriteSheet Process(AsepriteFile file, AnimProcessorOptions options) {
    ArgumentNullException.ThrowIfNull(file, nameof(file));

    var tags = GetTags(file);

    var textureAtlases = AnimTextureAtlasProcessor.Process(file, options);

    var pointDict = ProcessPoints(file, options);

    return new AnimSpriteSheet(textureAtlases, tags, pointDict);
  }

  private static AnimTag[] GetTags(AsepriteFile file) {
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

    return tags;
  }

  private static Dictionary<string, Vector2[]> ProcessPoints(AsepriteFile file, AnimProcessorOptions options) {
    Dictionary<string, Vector2[]> result = [];

    float scale = options.Upscale ? 2 : 1;
    Vector2 center = new Vector2(file.CanvasWidth, file.CanvasHeight) * scale / 2;

    foreach (AsepriteLayer layer in file.Layers) {
      if (layer is { ChildLevel: 0, UserData: { HasColor: true, Color.PackedValue: Colors.Yellow } }) {
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
