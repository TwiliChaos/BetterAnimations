using System.Linq;
using AnimLib.Aseprite.Processors;
using AsepriteDotNet.Aseprite;

namespace AnimLib.Aseprite;

/// <summary>
/// Asset representing an .ase or .aseprite file.
/// This class contains fields for data representing the file,
/// a sprite sheet generated from <see cref="AnimSpriteSheetProcessor"/>,
/// and XNA <see cref="Texture2D"/> assets created from the sprite sheet.
/// </summary>
[PublicAPI]
public sealed class AseAsset : IDisposable {
  public AseAsset(AsepriteFile file, AnimSpriteSheet spriteSheet) {
    this.file = file;
    textures = spriteSheet.Atlases.Select(x => x.Value.Texture).ToArray();
    this.spriteSheet = spriteSheet;
  }

  /// <summary>
  /// Instance of the data class that represents the data from the Aseprite file.
  /// </summary>
  public readonly AsepriteFile file;

  /// <summary>
  /// Texture2D that represents the Spritesheet generated from the Aseprite file.
  /// <seealso cref="AsepriteDotNet.Processors.SpriteSheetProcessor"/>
  /// </summary>
  public readonly Asset<Texture2D>[] textures;

  /// <summary>
  /// Contains animation data and Rects data from the Aseprite file, mapped to the <see cref="textures"/>.
  /// </summary>
  public readonly AnimSpriteSheet spriteSheet;

  public void Dispose() {
    foreach (var textureAsset in textures) {
      textureAsset.Dispose();
    }
  }
}
