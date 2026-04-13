using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace TheCity.TheCityCode;

//You're recommended but not required to keep all your code in this package and all your assets in the TheCity folder.
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "TheCity";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);

        harmony.PatchAll();
    }
}
