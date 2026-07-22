using HarmonyLib;
using Verse;

namespace TestMod.HarmonyPatches
{
    [StaticConstructorOnStartup]
    public static class HarmonyInstance
    {

        static HarmonyInstance()
        {
            var harmony = new Harmony("DVeL.TestMod");
            harmony.PatchAll();
        }
    }
}
