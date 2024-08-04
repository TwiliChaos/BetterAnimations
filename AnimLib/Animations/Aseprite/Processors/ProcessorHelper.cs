using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Aseprite.Types;
using AsepriteDotNet.Common;
using AsepriteDotNet.Processors;
using Rectangle = AsepriteDotNet.Common.Rectangle;

namespace AnimLib.Animations.Aseprite.Processors;

public static class ProcessorHelper {
  // This is an non-nullable array where each element represents a root layer at the specified frame.
  // Some layers may not have pixel data at the specified frame, and would thus be null.
  public static Rgba32[]?[] FlattenFrameToTopLayers(AsepriteFile file, int frameIndex,
    ProcessorOptions options, AsepriteLayer[] rootLayers) {
    ArgumentNullException.ThrowIfNull(file);

    AsepriteFrame frame = file.Frames[frameIndex];

    Rgba32[]?[] result = new Rgba32[rootLayers.Length][];

    for (int i = 0; i < rootLayers.Length; i++) {
      AsepriteLayer rootLayer = rootLayers[i];

      if (rootLayer is AsepriteGroupLayer groupLayer) {
        result[i] = MergeGroupCels(options, frame, groupLayer);
        continue;
      }

      var cels = frame.Cels;
      for (int j = 0; j < cels.Length; j++) {
        AsepriteCel cel = cels[j];
        if (!ReferenceEquals(cel.Layer, rootLayer)) {
          continue;
        }

        Rgba32[]? flattenedLayerPixels = null;
        if (MergeCel(options, frame, cel, ref flattenedLayerPixels)) {
          result[i] = flattenedLayerPixels;
        }

        break;
      }
    }

    return result;
  }

  private static Rgba32[]? MergeGroupCels(ProcessorOptions options, AsepriteFrame frame, AsepriteGroupLayer groupLayer) {
    Rgba32[]? flattenedLayerPixels = null;

    foreach (AsepriteCel cel in GetGroupCels(groupLayer, frame)) {
      MergeCel(options, frame, cel, ref flattenedLayerPixels);
    }

    return flattenedLayerPixels;
  }


  private static List<AsepriteCel> GetGroupCels(AsepriteGroupLayer groupLayer, AsepriteFrame frame) {
    List<AsepriteCel> result = [];

    var children = groupLayer.Children;
    int childrenLength = children.Length;
    var cels = frame.Cels;
    int celsLength = cels.Length;

    int jStart = 0;

    for (int i = 0; i < celsLength; i++) {
      AsepriteCel cel = cels[i];
      AsepriteLayer layer = cel.Layer;

      if (layer.UserData.HasColor) {
        var color = layer.UserData.Color;
        // Ignore Red: not to be imported at all
        // Ignore Green: imported as its own root layer
        // Ignore Yellow: processed to Vector2s
        if (color == UserDataColors.Red ||
            color == UserDataColors.Green ||
            color == UserDataColors.Yellow) {
          continue;
        }
      }

      for (int j = jStart; j < childrenLength; j++) {
        if (!ReferenceEquals(children[j], layer)) {
          continue;
        }

        result.Add(cel);
        jStart = j + 1;
        break;
      }
    }

    return result;
  }

  private static bool MergeCel(ProcessorOptions options, AsepriteFrame frame, AsepriteCel cel,
    ref Rgba32[]? flattenedLayerPixels) {
    cel = cel is AsepriteLinkedCel linkedCel ? linkedCel.Cel : cel;

    // Always import layer with Green userdata
    // Green cel should be excluded in GetGroupCels
    AsepriteUserData userData = cel.Layer.UserData;
    if (!userData.HasColor || userData.Color != UserDataColors.Green) {
      if (options.OnlyVisibleLayers && !cel.Layer.IsVisible) {
        return false;
      }

      if (cel.Layer.IsBackgroundLayer && !options.IncludeBackgroundLayer) {
        return false;
      }
    }

    switch (cel) {
      case AsepriteImageCel imageCel:
        flattenedLayerPixels ??= new Rgba32[frame.Size.Width * frame.Size.Height];
        BlendCel(frame, flattenedLayerPixels, imageCel);
        return true;
      case AsepriteTilemapCel tilemapCel when options.IncludeTilemapLayers:
        flattenedLayerPixels ??= new Rgba32[frame.Size.Width * frame.Size.Height];
        BlendTilemapCel(frame, flattenedLayerPixels, tilemapCel);
        return true;
      default:
        return false;
    }
  }

  private static void BlendCel(AsepriteFrame frame, Rgba32[] flattenedLayerPixels, AsepriteImageCel imageCel) {
    AsepriteFrameExtensions.BlendCel(flattenedLayerPixels, imageCel.Pixels, imageCel.Layer.BlendMode,
      new Rectangle(imageCel.Location, imageCel.Size), frame.Size.Width, imageCel.Opacity,
      imageCel.Layer.Opacity);
  }

  private static void BlendTilemapCel(AsepriteFrame frame, Rgba32[] flattenedLayerPixels, AsepriteTilemapCel tilemapCel) {
    AsepriteFrameExtensions.BlendTilemapCel(flattenedLayerPixels, tilemapCel, frame.Size.Width);
  }

  public static string GetNestedLayerName(ReadOnlySpan<AsepriteLayer> fileLayers, int index) {
    AsepriteLayer targetLayer = fileLayers[index];
    if (targetLayer.ChildLevel == 0) {
      return targetLayer.Name;
    }

    string[] namesToMerge = new string[targetLayer.ChildLevel + 1];
    namesToMerge[targetLayer.ChildLevel] = targetLayer.Name;

    int parentLevel = targetLayer.ChildLevel - 1;
    for (int i = index; i >= 0; i--) {
      AsepriteLayer layer = fileLayers[i];
      if (layer.ChildLevel != parentLevel) {
        continue;
      }

      parentLevel = layer.ChildLevel - 1;
      namesToMerge[layer.ChildLevel] = layer.Name;
      if (layer.ChildLevel == 0) {
        break;
      }
    }

    return string.Join('/', namesToMerge);
  }
}
