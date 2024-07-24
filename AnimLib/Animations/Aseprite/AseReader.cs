using System.IO;
using AnimLib.Animations.Aseprite.Processors;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.IO;
using ReLogic.Content.Readers;

namespace AnimLib.Animations.Aseprite;

/// <summary>
/// AssetReader to read an Aseprite file (*.ase/*.aseprite) into a format usable by Terraria.
/// This Reader uses AsepriteDotNet to load the file into an <see cref="AsepriteFile"/> object,
/// which this reader then uses to create an object that AnimLib and Terraria can use.
/// This class is used to create an <see cref="AnimSpriteSheet"/>.
/// https://github.com/AristurtleDev/AsepriteDotNet
/// </summary>
public class AseReader : IAssetReader {
  public T FromStream<T>(Stream stream) where T : class {
    // TODO: Allow T to be of AsepriteFile, Texture2D for vanilla texture layout,
    // or of object where a type in the assembly inherits IAsepriteProcessor<T>
    if (typeof(T) != typeof(AnimSpriteSheet)) {
      throw AssetLoadException.FromInvalidReader<AseReader, T>();
    }

    AsepriteFile asepriteFile;
    if (stream.CanSeek) {
      // We have no access to the file name to name this object.
      asepriteFile = AsepriteFileLoader.FromStream("", stream);
    }
    else {
      // AsepriteFileLoader requires the Seek function.
      // We cannot guarantee that the incoming stream supports Seeking (e.g. DeflateStream),
      // So we have to create a new Stream that allows it.
      using MemoryStream newStream = new();
      stream.CopyTo(newStream);
      newStream.Position = 0;
      asepriteFile = AsepriteFileLoader.FromStream("", newStream);
    }

    // We have no access to the file name or the mod, so these warnings may just be annoying.
    var warnings = asepriteFile.Warnings;
    if (!warnings.IsEmpty) {
      Log.Warn($"Aseprite file loaded with the following warnings:");
      foreach (string warning in warnings) {
        Log.Warn(warning);
      }
    }

    return (AnimSpriteSheetProcessor.Process(asepriteFile) as T)!;
  }
}
