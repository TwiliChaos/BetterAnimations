using System.Diagnostics;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Aseprite.Types;
using AsepriteDotNet.Common;
using Rectangle = AsepriteDotNet.Common.Rectangle;

namespace AnimLib.Aseprite.Processors;

public static class ProcessorHelper {
  // This is a non-nullable array where each element represents a target layer at the specified frame.
  // Some layers may not have pixel data at the specified frame, and would thus be null.
  public static Rgba32[]?[] FlattenFrameToTopLayers(AsepriteFile file, int frameIndex,
    AnimProcessorOptions options, AsepriteLayer[] targetLayers) {
    ArgumentNullException.ThrowIfNull(file);

    AsepriteFrame frame = file.Frames[frameIndex];

    Rgba32[]?[] result = new Rgba32[targetLayers.Length][];

    for (int i = 0; i < targetLayers.Length; i++) {
      AsepriteLayer targetLayer = targetLayers[i];

      if (targetLayer is AsepriteGroupLayer groupLayer) {
        result[i] = MergeGroupCels(options, frame, groupLayer);
        continue;
      }

      foreach (AsepriteCel cel in frame.Cels) {
        if (!ReferenceEquals(cel.Layer, targetLayer)) {
          continue;
        }

        Rgba32[]? flattenedLayerPixels = null;
        if (MergeCel(options, frame, cel, ref flattenedLayerPixels)) {
          result[i] = flattenedLayerPixels;
        }

        break;
      }
    }

    for (int i = 0; i < targetLayers.Length; i++) {
      ProcessCopyColors(file, frameIndex, targetLayers, i, result);
    }

    return result;
  }

  private static void ProcessCopyColors(AsepriteFile file, int frameIndex, AsepriteLayer[] targetLayers, int layerIndex,
    Rgba32[]?[] result) {
    AsepriteLayer targetLayer = targetLayers[layerIndex];
    if (targetLayer.UserData is not {
          HasColor: true,
          Color.PackedValue: Colors.Blue,
          HasText: true
        }) {
      return;
    }

    if (!targetLayer.UserData.GetOptionsString("copyColor", out var targetLayerName)) {
      return;
    }

    int targetLayerIndex = -1;

    for (int j = 0; j < targetLayers.Length; j++) {
      if (NestedLayerNameMatches(file.Layers, targetLayers[j], targetLayerName)) {
        targetLayerIndex = j;
        break;
      }
    }

    if (targetLayerIndex == -1) {
      if (frameIndex == 0) {
        Log.Warn(
          $"[Aseprite] Layer {GetNestedLayerName(targetLayers, targetLayer)} to copy layer '{targetLayerName}', " +
          $"but the layer was not found.");
      }

      return;
    }

    ref var pixels = ref result[layerIndex];
    if (pixels is not null) {
      var sourcePixels = result[targetLayerIndex];
      if (sourcePixels is null) {
        pixels = null;
        return;
      }

      float alphaMultiplier =
        targetLayer.UserData.GetOptionsBool("ignoreAlpha", out bool? ignoreAlpha) && ignoreAlpha == true
          ? 1
          : targetLayer.Opacity / 255f;

      AsepriteCel? cel = null;
      foreach (AsepriteCel frameCel in file.Frames[frameIndex].Cels) {
        if (ReferenceEquals(frameCel.Layer, targetLayer)) {
          cel = frameCel;
          break;
        }
      }

      Debug.Assert(cel is not null, "Cel should not be null");

      alphaMultiplier *= cel.UserData.GetOptionsBool("ignoreAlpha", out ignoreAlpha) && ignoreAlpha == true
        ? 1
        : cel.Opacity / 255f;

      CopyColor(pixels, sourcePixels, alphaMultiplier);
    }
  }

  private static Rgba32[]? MergeGroupCels(AnimProcessorOptions options, AsepriteFrame frame,
    AsepriteGroupLayer groupLayer) {
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

      // Ignore Red: not to be imported at all
      // Ignore Green: imported as its own target layer
      // Ignore Yellow: processed to Vector2s
      // Ignore Blue: copies RGB values from its tagged layer
      if (layer.UserData is
          { HasColor: true, Color.PackedValue: Colors.Red or Colors.Green or Colors.Yellow or Colors.Blue }) {
        continue;
      }

      for (int j = jStart; j < childrenLength; j++) {
        if (ReferenceEquals(children[j], layer)) {
          result.Add(cel);
          jStart = j + 1;
          break;
        }
      }
    }

    return result;
  }

  private static bool MergeCel(AnimProcessorOptions options, AsepriteFrame frame, AsepriteCel cel,
    ref Rgba32[]? flattenedLayerPixels) {
    cel = cel is AsepriteLinkedCel linkedCel ? linkedCel.Cel : cel;

    // Always import layer with Green or Blue userdata
    // Green and BLue cel should be excluded in GetGroupCels
    if (cel.Layer.UserData is not { HasColor: true, Color.PackedValue: Colors.Green or Colors.Blue }) {
      if (options.OnlyVisibleLayers && !cel.Layer.IsVisible) {
        return false;
      }

      if (!options.IncludeBackgroundLayer && cel.Layer.IsBackgroundLayer) {
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
    int celOpacity = imageCel.UserData.GetOptionsBool("ignoreAlpha", out bool? ignoreAlpha) && ignoreAlpha == true
      ? 255
      : imageCel.Opacity;

    int layerOpacity = imageCel.Layer.UserData.GetOptionsBool("ignoreAlpha", out ignoreAlpha) && ignoreAlpha == true
      ? 255
      : imageCel.Layer.Opacity;

    AsepriteFrameExtensions.BlendCel(flattenedLayerPixels, imageCel.Pixels, imageCel.Layer.BlendMode,
      new Rectangle(imageCel.Location, imageCel.Size), frame.Size.Width, celOpacity,
      layerOpacity);
  }

  private static void BlendTilemapCel(AsepriteFrame frame, Rgba32[] flattenedLayerPixels,
    AsepriteTilemapCel tilemapCel) {
    AsepriteFrameExtensions.BlendTilemapCel(flattenedLayerPixels, tilemapCel, frame.Size.Width);
  }

  private static void CopyColor(Rgba32[] destinationPixels, Rgba32[] sourcePixels, float alphaMultiplier) {
    for (int i = 0; i < destinationPixels.Length; i++) {
      Rgba32 oldDestColor = destinationPixels[i];
      byte oldA = oldDestColor.A;
      if (sourcePixels[i].A == 0 || oldA == 0) {
        destinationPixels[i] = default;
        continue;
      }

      Rgba32 color = sourcePixels[i];
      float multi = (oldA / 255f) * (color.A / 255f) * alphaMultiplier;
      color.R = (byte)(color.R * multi);
      color.G = (byte)(color.G * multi);
      color.B = (byte)(color.B * multi);
      color.A = (byte)(oldA * multi);
      destinationPixels[i] = color;
    }
  }

  private static bool NestedLayerNameMatches(ReadOnlySpan<AsepriteLayer> fileLayers, AsepriteLayer layer,
    ReadOnlySpan<char> name) {
    Span<int> nameIndices = stackalloc int[layer.ChildLevel + 1];

    int i = 0;
    for (; i < fileLayers.Length; i++) {
      if (ReferenceEquals(fileLayers[i], layer)) {
        nameIndices[layer.ChildLevel] = i;
        break;
      }
    }

    int parentLevel = layer.ChildLevel - 1;
    for (; i >= 0; i--) {
      AsepriteLayer currentLayer = fileLayers[i];
      if (currentLayer.ChildLevel != parentLevel) {
        continue;
      }

      parentLevel = currentLayer.ChildLevel - 1;
      nameIndices[currentLayer.ChildLevel] = i;
      if (currentLayer.ChildLevel == 0) {
        break;
      }
    }

    var remainingName = name;
    foreach (int nameIndex in nameIndices) {
      string layerName = fileLayers[nameIndex].Name;
      if (!remainingName.StartsWith(layerName, StringComparison.Ordinal)) {
        return false;
      }

      if (remainingName.Length == layerName.Length) {
        return true;
      }

      remainingName = remainingName[(layerName.Length + 1)..];
    }

    return true;
  }

  public static string GetNestedLayerName(ReadOnlySpan<AsepriteLayer> fileLayers, AsepriteLayer layer) {
    for (int i = 0; i < fileLayers.Length; i++) {
      if (ReferenceEquals(fileLayers[i], layer)) {
        return GetNestedLayerName(fileLayers, i);
      }
    }

    return layer.Name;
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

  public static bool GetOptionsString(this AsepriteUserData userData, string key, out ReadOnlySpan<char> value) {
    value = default;
    if (userData is not { HasText: true }) {
      return false;
    }

    var text = userData.Text.AsSpan();
    ReadOnlySpan<char> keySpan = key;
    int keyIndex = text.IndexOf(keySpan);
    if (keyIndex == -1) {
      return false;
    }

    int rangeStart = keyIndex + keySpan.Length + 1;
    if (text.Length <= rangeStart || text[rangeStart - 1] != ':') {
      return false;
    }

    var subText = text[rangeStart..];
    if (subText.IsEmpty) {
      return false;
    }

    int nextArg = subText.IndexOf(',');
    int rangeEnd = nextArg == -1 ? text.Length : rangeStart + nextArg;
    if (rangeEnd == rangeStart) {
      return false;
    }

    value = text[rangeStart..rangeEnd];
    return true;
  }

  public static bool GetOptionsBool(this AsepriteUserData userData, string key, [NotNullWhen(true)] out bool? value) {
    if (!GetOptionsString(userData, key, out var arg)) {
      if (userData.HasText && userData.Text.AsSpan().Contains(key, StringComparison.Ordinal)) {
        value = true;
        return true;
      }

      value = null;
      return false;
    }

    if (bool.TryParse(arg, out bool result)) {
      value = result;
      return true;
    }

    value = null;
    return false;
  }

  public static bool GetOptionsInt(this AsepriteUserData userData, string key, [NotNullWhen(true)] out int? value) {
    if (!GetOptionsString(userData, key, out var arg)) {
      value = null;
      return false;
    }

    if (!int.TryParse(arg, out int result)) {
      value = null;
      return false;
    }

    value = result;
    return true;
  }
}
