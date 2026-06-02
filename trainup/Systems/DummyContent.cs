using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.BigCraftables;

namespace TrainUp.Systems;

/// <summary>
/// Defines the "Training Dummy" big craftable and its crafting recipe by editing the relevant
/// data assets, supplies its texture, and grants the recipe to players. The dummy is a normal
/// placeable big craftable — vanilla handles its placement, saving, and pickup — and hits on it
/// are detected separately by <see cref="Patches.DummyHitPatches"/>.
/// </summary>
public class DummyContent
{
    /// <summary>Unqualified item id / Data/BigCraftables key.</summary>
    public const string BigCraftableId = "skint007.TrainUp_Dummy";

    /// <summary>Qualified id used to match placed objects.</summary>
    public const string QualifiedId = "(BC)skint007.TrainUp_Dummy";

    /// <summary>Key in Data/CraftingRecipes and on the player's known-recipes list.</summary>
    public const string RecipeName = "Train Up Dummy";

    /// <summary>Custom texture asset name.</summary>
    public const string TextureAsset = "Mods/skint007.TrainUp/Dummy";

    private readonly IModHelper _helper;

    public DummyContent(IModHelper helper)
    {
        _helper = helper;
    }

    public void RegisterEvents()
    {
        _helper.Events.Content.AssetRequested += OnAssetRequested;
        _helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(TextureAsset))
        {
            e.LoadFromModFile<Microsoft.Xna.Framework.Graphics.Texture2D>("assets/dummy.png", AssetLoadPriority.Medium);
            return;
        }

        if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, BigCraftableData>().Data;
                data[BigCraftableId] = new BigCraftableData
                {
                    Name = "Training Dummy",
                    DisplayName = _helper.Translation.Get("dummy.name"),
                    Description = _helper.Translation.Get("dummy.description"),
                    Price = 0,
                    Fragility = 0,
                    CanBePlacedIndoors = true,
                    CanBePlacedOutdoors = true,
                    IsLamp = false,
                    Texture = TextureAsset,
                    SpriteIndex = 0
                };
            });
            return;
        }

        if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, string>().Data;
                // ingredients / (unused) / yield(bigCraftable id) / isBigCraftable / unlock / displayName
                data[RecipeName] = $"388 25 390 10/Home/{BigCraftableId}/true/null/{_helper.Translation.Get("dummy.name")}";
            });
        }
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        // Make the recipe known so it shows up in the crafting menu from the start.
        Game1.player.craftingRecipes.TryAdd(RecipeName, 0);
    }
}
