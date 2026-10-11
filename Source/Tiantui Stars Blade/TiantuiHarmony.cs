using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Tiantui_Stars_Blade
{
    [StaticConstructorOnStartup]
    public static class TiantuiHarmony
    {
        static TiantuiHarmony()
        {
            new Harmony("shapto.tiantuistarsblade").PatchAll();
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.DrawPos), MethodType.Getter)]
    public static class Patch_Pawn_DrawPos_TiantuiStumble
    {
        public static void Postfix(Pawn __instance, ref Vector3 __result)
        {
            if (__instance.jobs?.curDriver is JobDriver_TiantuiDashStumble stumbleDriver) __result = stumbleDriver.CurrentDrawPosition;
        }
    }

    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.BodyAngle))]
    public static class Patch_PawnRenderer_BodyAngle_TiantuiStumble
    {
        public static void Postfix(Pawn ___pawn, ref float __result)
        {
            if (___pawn.jobs?.curDriver is JobDriver_TiantuiDashStumble stumbleDriver) __result = stumbleDriver.LyingBodyAngle;
        }
    }
}
