using System.IO;
using AnimLib.Aseprite;
using ReLogic.Content.Sources;
using ReLogic.Utilities;

namespace AnimLib;

public sealed partial class AnimLibMod {
  /// <summary>
  /// Add the Aseprite reader. This will allow mods using AnimLib to request Aseprite files from Assets.
  /// </summary>
  /// <remarks>
  /// Autoload for aseprite files will not work for mods that load earlier than Animlib.
  /// </remarks>
  public override IContentSource CreateDefaultContentSource() {
    Main.instance.Services.Get<AssetReaderCollection>().RegisterReader(new AseReader(), ".ase", ".aseprite");
    return base.CreateDefaultContentSource();
  }

  internal Asset<Texture2D> CreateTexture2DAsset(Stream stream, string filename) {
    return AseAssets.CreateUntracked<Texture2D>(stream, filename, AssetRequestMode.AsyncLoad);
  }

  public readonly AssetRepository AseAssets = new(Main.instance.Services.Get<AssetReaderCollection>());
}
