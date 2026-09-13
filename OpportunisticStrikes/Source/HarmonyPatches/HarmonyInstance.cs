using HarmonyLib;
using System.Reflection;
using Verse;

namespace OpportunisticStrikes.HarmonyPatches
{
    [StaticConstructorOnStartup]
    public static class HarmonyInstance
    {

        static HarmonyInstance()
        {
            var harmony = new Harmony("DVeL.OpportunisticStrikes");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
