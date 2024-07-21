using AnimLib.Utilities;
using System.Reflection;
using Terraria.Graphics.Shaders;

namespace AnimLib.Extensions;

public static class ArmorShaderDataGetSet {
  private static readonly Func<ArmorShaderData, Vector3> UColor;
  private static readonly Func<ArmorShaderData, Vector3> USecondaryColor;
  private static readonly Func<ArmorShaderData, float> USaturation;
  private static readonly Func<ArmorShaderData, float> UOpacity;
  private static readonly Func<ArmorShaderData, Vector2> UTargetPosition;

  static ArmorShaderDataGetSet() {
    Type type = typeof(ArmorShaderData);
    UColor = ClassHacking.GenerateGetter<ArmorShaderData, Vector3>(
      type.GetField("_uColor", BindingFlags.Instance | BindingFlags.NonPublic));
    USecondaryColor = ClassHacking.GenerateGetter<ArmorShaderData, Vector3>(
      type.GetField("_uSecondaryColor", BindingFlags.Instance | BindingFlags.NonPublic));
    USaturation = ClassHacking.GenerateGetter<ArmorShaderData, float>(
      type.GetField("_uSaturation", BindingFlags.Instance | BindingFlags.NonPublic));
    UOpacity = ClassHacking.GenerateGetter<ArmorShaderData, float>(
      type.GetField("_uOpacity", BindingFlags.Instance | BindingFlags.NonPublic));
    UTargetPosition = ClassHacking.GenerateGetter<ArmorShaderData, Vector2>(
      type.GetField("_uTargetPosition", BindingFlags.Instance | BindingFlags.NonPublic));
  }

  public static Color GetColor(this ArmorShaderData a) => new(UColor(a));

  public static Vector3 GetUColor(this ArmorShaderData a) => UColor(a);

  public static Color GetSecondaryColor(this ArmorShaderData a) => new(USecondaryColor(a));

  public static Vector3 GetUSecondaryColor(this ArmorShaderData a) => USecondaryColor(a);

  public static float GetSaturation(this ArmorShaderData a) => USaturation(a);

  public static float GetOpacity(this ArmorShaderData a) => UOpacity(a);

  public static Vector2 GetTargetPos(this ArmorShaderData a) => UTargetPosition(a);
}
