using System.Reflection;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.UI;

namespace AnimLib.Systems;

internal static class UICharacterSelectExtensions {
  private static readonly MethodInfo UnselectAllCategories = Method("UnselectAllCategories");
  private static readonly MethodInfo UpdateColorPickers = Method("UpdateColorPickers");
  // ReSharper disable once StringLiteralTypo - vanilla typo
  private static readonly MethodInfo MakeHairStylesMenu = Method("MakeHairsylesMenu");

  private static readonly FieldInfo MiddleContainerField = Field("_middleContainer");
  private static readonly FieldInfo ColorPickersField = Field("_colorPickers");
  private static readonly FieldInfo SelectedPickerField = Field("_selectedPicker");

  private static readonly FieldInfo ClothesStyleContainer = Field("_clothStylesContainer");
  private static readonly FieldInfo HairStylesContainer = Field("_hairstylesContainer");
  private static readonly FieldInfo ClothingStylesCategoryButton = Field("_clothingStylesCategoryButton");
  private static readonly FieldInfo HairStylesCategoryButton = Field("_hairStylesCategoryButton");
  private static readonly FieldInfo CharInfoCategoryButton = Field("_charInfoCategoryButton");

  public static void Ex_UnselectAllCategories(this UICharacterCreation self) => UnselectAllCategories.Invoke(self, []);

  public static void Ex_UpdateColorPickers(this UICharacterCreation self) => UpdateColorPickers.Invoke(self, []);

  private static readonly object?[] Args1 = new object?[1];
  public static void Ex_MakeHairStylesMenu(this UICharacterCreation self) {
    Args1[0] = self.GetMiddleContainer();
    MakeHairStylesMenu.Invoke(self, Args1);
    Args1[0] = null;
  }

  public static UIElement GetMiddleContainer(this UICharacterCreation self) =>
    (UIElement)MiddleContainerField.GetValue(self)!;

  public static UIColoredImageButton[] GetColorPickers(this UICharacterCreation self) =>
    (UIColoredImageButton[])ColorPickersField.GetValue(self)!;

  public static void SetSelectedPicker(this UICharacterCreation self, int value) =>
    SelectedPickerField.SetValue(self, value);

  public static UIElement GetClothesStyleContainer(this UICharacterCreation self) =>
    (UIElement)ClothesStyleContainer.GetValue(self)!;

  public static UIElement GetHairStylesContainer(this UICharacterCreation self) =>
    (UIElement)HairStylesContainer.GetValue(self)!;

  public static UIColoredImageButton GetClothingStylesCategoryButton(this UICharacterCreation self) =>
    (UIColoredImageButton)ClothingStylesCategoryButton.GetValue(self)!;

  public static UIColoredImageButton GetHairStylesCategoryButton(this UICharacterCreation self) =>
    (UIColoredImageButton)HairStylesCategoryButton.GetValue(self)!;

  public static UIColoredImageButton GetCharInfoCategoryButton(this UICharacterCreation self) =>
    (UIColoredImageButton)CharInfoCategoryButton.GetValue(self)!;


  private static MethodInfo Method(string name) =>
    typeof(UICharacterCreation).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!;

  private static FieldInfo Field(string name) =>
    typeof(UICharacterCreation).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!;
}

internal static class UIColoredImageButtonExtensions {
  private static readonly FieldInfo TextureField = Field("_texture");
  private static readonly FieldInfo MiddleTextureField = Field("_middleTexture");

  public static Asset<Texture2D> GetTexture(this UIColoredImageButton self) =>
    (Asset<Texture2D>)TextureField.GetValue(self)!;

  public static Asset<Texture2D>? GetMiddleTexture(this UIColoredImageButton self) =>
    MiddleTextureField.GetValue(self) as Asset<Texture2D>;

  private static FieldInfo Field(string name) =>
    typeof(UIColoredImageButton).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!;
}
