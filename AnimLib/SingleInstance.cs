using JetBrains.Annotations;

namespace AnimLib;

/// <summary>
/// Class used to hold a single static reference to an instance of <typeparamref name="T"/>.
/// <para>Classes inheriting from this should use a private constructor.</para>
/// </summary>
/// <typeparam name="T">The type to make Singleton.</typeparam>
[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract class SingleInstance<T> where T : SingleInstance<T> {
  private static readonly Lazy<T> LazyInstance = new(Initialize);

  /// <summary>
  /// The singleton instance of this type.
  /// </summary>
  public static T Instance => LazyInstance.Value;

  private static T Initialize() {
    AnimLibMod.OnUnload += Unload;
    return Activator.CreateInstance<T>();
  }

  private static void Unload() {
    if (!LazyInstance.IsValueCreated) {
      return;
    }

    // ReSharper disable once SuspiciousTypeConversion.Global
    if (LazyInstance.Value is IDisposable disposable) {
      disposable.Dispose();
    }
  }
}
