using System.Diagnostics;

namespace AnimLib;

internal static class Log {
  internal static void Error(string message, Exception ex) {
    AnimLibMod.Instance.Logger.Error(message, ex);
  }

  internal static void Warn(string message) {
    AnimLibMod.Instance.Logger.Warn(message);
  }

  [Conditional("DEBUG")]
  internal static void Debug(string message) {
    AnimLibMod.Instance.Logger.Debug(message);
    Main.NewText(message, Color.Gray);
  }

  internal static void Info(string message) {
    AnimLibMod.Instance.Logger.Info(message);
  }
}
