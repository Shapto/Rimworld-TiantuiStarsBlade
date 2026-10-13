using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Tiantui_Stars_Blade
{
    public static class MeleeAnimationCompat
    {
        private static readonly Type IdleControllerType = AccessTools.TypeByName("AM.Idle.IdleControllerComp");
        private static readonly MethodInfo ClearAnimationMethod = IdleControllerType == null ? null : AccessTools.Method(IdleControllerType, "ClearAnimation");

        public static void RefreshHeldWeapon(Pawn pawn)
        {
            if (ClearAnimationMethod == null || pawn?.AllComps == null) return;

            foreach (ThingComp comp in pawn.AllComps)
            {
                if (!IdleControllerType.IsInstanceOfType(comp)) continue;
                ClearAnimationMethod.Invoke(comp, null);
                return;
            }
        }
    }
}
