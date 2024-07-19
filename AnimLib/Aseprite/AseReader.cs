using System.IO;
using AnimLib.Aseprite.Processors;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.IO;
using log4net;
using ReLogic.Content.Readers;

namespace AnimLib.Aseprite;

/// <summary>
/// AssetReader to read an Aseprite file (*.ase/*.aseprite) into a format usable by Terraria.
/// This Reader uses AsepriteDotNet to load the file into an <see cref="AsepriteFile"/> object,
/// which this reader then uses to create an object that AnimLib and Terraria can use.
/// This class is used to create an <see cref="AnimSpriteSheet"/>.
/// https://github.com/AristurtleDev/AsepriteDotNet
/// </summary>
public class AseReader : IAssetReader {
  public T FromStream<T>(Stream stream) where T : class {
    if (typeof(T) != typeof(AnimSpriteSheet)) {
      throw AssetLoadException.FromInvalidReader<AseReader, T>();
    }

    // AsepriteFileLoader requires the Seek function.
    // We cannot guarantee that the incoming stream supports Seeking (e.g. DeflateStream),
    // So we have to create a new Stream that allows it.
    using MemoryStream newStream = new();
    stream.CopyTo(newStream);
    newStream.Position = 0;

    // We have no access to the file name to name this object.
    AsepriteFile asepriteFile = AsepriteFileLoader.FromStream("", newStream);

    // We have no access to the file name or the mod, so these warnings may just be annoying.
    var warnings = asepriteFile.Warnings;
    if (!warnings.IsEmpty) {
      ILog logger = AnimLibMod.Instance.Logger;
      logger.Warn($"Aseprite file loaded with the following warnings:");
      foreach (string warning in warnings) {
        logger.Warn(warning);
      }
    }

    return AnimSpriteSheetProcessor.Process(asepriteFile) as T;
  }
}
