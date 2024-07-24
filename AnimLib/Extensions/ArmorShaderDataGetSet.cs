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
    UColor = GenerateGetter<Vector3>(type,"_uColor");
    USecondaryColor = GenerateGetter<Vector3>(type,"_uSecondaryColor");
    USaturation = GenerateGetter<float>(type,"_uSaturation");
    UOpacity = GenerateGetter<float>(type,"_uOpacity");
    UTargetPosition = GenerateGetter<Vector2>(type,"_uTargetPosition");
    return;

    Func<ArmorShaderData, TOut> GenerateGetter<TOut>(Type t, string fieldName) {
      const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
      return ClassHacking.GenerateGetter<ArmorShaderData, TOut>(t.GetField(fieldName, bindingFlags)!);
    }
  }

  public static Color GetColor(this ArmorShaderData a) => new(UColor(a));

  public static Vector3 GetUColor(this ArmorShaderData a) => UColor(a);

  public static Color GetSecondaryColor(this ArmorShaderData a) => new(USecondaryColor(a));

  public static Vector3 GetUSecondaryColor(this ArmorShaderData a) => USecondaryColor(a);

  public static float GetSaturation(this ArmorShaderData a) => USaturation(a);

  public static float GetOpacity(this ArmorShaderData a) => UOpacity(a);

  public static Vector2 GetTargetPos(this ArmorShaderData a) => UTargetPosition(a);
}
