using AsepriteDotNet;
using AsepriteDotNet.Aseprite;

namespace AnimLib.Aseprite;

/// <summary>
/// Asset representing an .ase or .aseprite file.
/// This class contains fields for data representing the file,
/// a sprite sheet generated from <see cref="AsepriteDotNet.Processors.SpriteSheetProcessor"/>,
/// and an XNA <see cref="Texture2D"/> asset created from the sprite sheet.
/// </summary>
public sealed class AseAsset : IDisposable {
  public AseAsset(AsepriteFile file, Asset<Texture2D> texture2D, SpriteSheet spriteSheet) {
    this.file = file;
    this.texture2D = texture2D;
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
  public readonly Asset<Texture2D> texture2D;

  /// <summary>
  /// Contains animation data and Rects data from the Aseprite file, mapped to the <see cref="texture2D"/>.
  /// </summary>
  public readonly SpriteSheet spriteSheet;

  public void Dispose() {
    texture2D.Dispose();
  }
}
