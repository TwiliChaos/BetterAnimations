using JetBrains.Annotations;

namespace AnimLib;

/// <summary>
/// Class used to hold a single static reference to an instance of <typeparamref name="T"/>.
/// <para>Classes inheriting from this should use a private constructor.</para>
/// </summary>
/// <typeparam name="T">The type to make Singleton.</typeparam>
[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract class SingleInstance<T> where T : SingleInstance<T>
{
  private static T CreateInstance() => (T)Activator.CreateInstance(typeof(T), true)!;

  /// <summary>
  /// The singleton instance of this type.
  /// </summary>
  public static T Instance => _instance ??= CreateInstance();

  private static T? _instance;

  public static void Initialize() {
    _instance ??= CreateInstance();
  }

  public static void Unload() => _instance = null;
}
