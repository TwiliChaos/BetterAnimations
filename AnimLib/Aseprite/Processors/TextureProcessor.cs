using System.Buffers;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Common;
using AsepriteDotNet.Processors;
using Texture = AsepriteDotNet.Texture;

namespace AnimLib.Aseprite.Processors;

/// <summary>
/// Basic processor to process an <see cref="AsepriteFile"/> into a <see cref="Texture2D"/>.
/// </summary>
public class TextureProcessor : IAsepriteProcessor<Texture2D> {
  public Texture2D Process(AsepriteFile file, AnimProcessorOptions options) {
    ProcessorOptions aseOptions = options.ToAseprite();
    AsepriteDotNet.TextureAtlas atlas = TextureAtlasProcessor.Process(file, aseOptions);
    Texture texture = atlas.Texture;
    if (!options.Upscale) {
      return AseReader.CreateTexture2DAsset(texture, AssetRequestMode.ImmediateLoad).Value;
    }

    Size size = texture.Size;
    int width = size.Width * 2;
    int height = size.Height * 2;
    var array = ArrayPool<Rgba32>.Shared.Rent(width * height);
    try {
      Span<Rgba32> upscaledPixels = new(array, 0, width * height);
      WriteScaledPixels(texture.Pixels, upscaledPixels, width);
      // Upscale(texture.Pixels, size.Width, upscaledPixels);
      return AseReader.CreateTexture2DAsset(texture.Name, width, height, upscaledPixels, AssetRequestMode.ImmediateLoad).Value;
    } finally {
      ArrayPool<Rgba32>.Shared.Return(array);
    }
  }

  private static void WriteScaledPixels(ReadOnlySpan<Rgba32> source, Span<Rgba32> destination, int destinationWidth) {
    int length = source.Length;
    for (int p = 0; p < length; p++) {
      int p2 = p * 2;
      int px = p2 % destinationWidth; // increase x by 2 per pixel
      int py = p2 / destinationWidth * 2; // increase y by 2 per row of pixels
      int row1 = py * destinationWidth + px;
      int row2 = row1 + destinationWidth;

      Rgba32 pixel = source[p];
      destination[row1] = pixel;
      destination[row1 + 1] = pixel;
      destination[row2] = pixel;
      destination[row2 + 1] = pixel;
    }
  }
}
