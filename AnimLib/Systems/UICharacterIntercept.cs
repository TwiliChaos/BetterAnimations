using System.Reflection;
using JetBrains.Annotations;
using Terraria.GameContent.UI.Elements;

namespace AnimLib.Systems;

/// <summary>
/// Interception class to inform <see cref="AnimCharacterCollection"/> when a <see cref="UICharacter"/> is being drawn.
/// </summary>
[UsedImplicitly]
public class UICharacterIntercept : ModSystem {
  private static readonly FieldInfo PlayerField = Field<UICharacter>("_player");
  private static readonly FieldInfo AnimationCounterField = Field<UICharacter>("_animationCounter");
  private static readonly FieldInfo HairStylePlayerField = Field<UIHairStyleButton>("_player");

  // This dict exist solely to have AnimCharacter.UICategoryCounterStart be accurate on a per-UICharacter basis
  private static readonly Dictionary<WeakReference<UICharacter>, int> CategoryStartTimers = new();

  private static Player GetPlayer(UICharacter self) => (Player)PlayerField.GetValue(self)!;

  private static Player GetPlayer(UIHairStyleButton self) => (Player)HairStylePlayerField.GetValue(self)!;

  private static int GetAnimationCounter(UICharacter self) => (int)AnimationCounterField.GetValue(self)!;

  public override void Load() {
    Log.Debug("Adding hooks to UICharacter.DrawSelf and UIHairStyleButton.DrawSelf, for updating AnimCharacter UI fields");
    On_UICharacter.DrawSelf += (orig, self, spritebatch) => {
      AnimCharacterCollection collection = GetPlayer(self).GetState<AnimCharacterCollection>();

      bool hasAdded = false;
      foreach (var weakRef in CategoryStartTimers.Keys) {
        if (!weakRef.TryGetTarget(out UICharacter? target)) {
          CategoryStartTimers.Remove(weakRef);
        }
        else if (ReferenceEquals(self, target)) {
          hasAdded = true;
        }
      }

      int animationCounter = GetAnimationCounter(self);
      if (!hasAdded) {
        CategoryStartTimers.Add(new WeakReference<UICharacter>(self), animationCounter);
      }

      int categoryIndex = collection.UICategoryIndex;
      if (categoryIndex != -1) {
        if (collection.UICategoryIndexLastFrame != categoryIndex) {
          collection.UILastCategoryIndex = collection.UICategoryIndexLastFrame;
          collection.UICategoryIndexLastFrame = categoryIndex;
          foreach (var weakRef in CategoryStartTimers.Keys) {
            if (weakRef.TryGetTarget(out UICharacter? uiCharacter)) {
              CategoryStartTimers[weakRef] = GetAnimationCounter(uiCharacter);
            }
          }
        }
      }

      // Set AnimCharacter category start time to the time we have stored
      foreach ((var weakRef, int categoryStartTimer) in CategoryStartTimers) {
        if (weakRef.TryGetTarget(out UICharacter? uiCharacter) && ReferenceEquals(self, uiCharacter)) {
          collection.UICategoryCounterStart = categoryStartTimer;
          break;
        }
      }

      WrapHook(collection, () => orig(self, spritebatch), null, self.IsAnimated, animationCounter);
    };

    On_UIHairStyleButton.DrawSelf += On_UIHairStyleButtonOnDrawSelf;
  }

  private static void On_UIHairStyleButtonOnDrawSelf(On_UIHairStyleButton.orig_DrawSelf orig, UIHairStyleButton self, SpriteBatch spritebatch) {
    AnimCharacterCollection collection = GetPlayer(self).GetState<AnimCharacterCollection>();
    using var t1 = new TempValue<bool>(ref collection.IsDrawingInUI, true);
    using var t2 = new TempValue<bool>(ref collection.UIAnimated, false);
    using var t3 = new TempValue<int>(ref collection.UIAnimationCounter, 0);
    using var t4 = new TempValue<int>(ref collection.UICategoryIndex, 2);
    orig(self, spritebatch);
  }

  internal static void WrapHook(AnimCharacterCollection collection, Action hook,
    int? categoryIndex = null,
    bool? isAnimated = null,
    int animationCounter = 0) {
    using var t1 = new TempValue<bool>(ref collection.IsDrawingInUI, true);
    using var t2 = new TempValue<bool>(ref collection.UIAnimated, isAnimated ?? false);
    using var t3 = new TempValue<int>(ref collection.UIAnimationCounter, animationCounter);
    using var t4 = new TempValue<int>(ref collection.UICategoryIndex, categoryIndex ?? collection.UICategoryIndex);
    hook();
  }

  private readonly ref struct TempValue<T> {
    private readonly T _oldValue;
    private readonly ref T _value;
    private readonly bool _changed;

    public TempValue(ref T value, T? newValue) {
      _oldValue = value;
      _value = ref value;
      if (newValue is null) {
        return;
      }

      _value = newValue;
      _changed = true;
    }

    public void Dispose() {
      if (_changed) {
        _value = _oldValue;
      }
    }
  }

  private static FieldInfo Field<T>(string field) =>
    typeof(T).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!;
}
