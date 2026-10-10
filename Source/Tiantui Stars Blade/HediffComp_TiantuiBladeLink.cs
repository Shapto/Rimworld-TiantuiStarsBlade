using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Tiantui_Stars_Blade
{
    public class HediffCompProperties_TiantuiBladeLink : HediffCompProperties
    {
        public bool onlyWhileOverheated;

        public HediffCompProperties_TiantuiBladeLink()
        {
            compClass = typeof(HediffComp_TiantuiBladeLink);
        }
    }

    /// <summary>
    /// Removes the hediff when the pawn no longer holds the Tiantui blade, and optionally when the blade has cooled.
    /// A safety net for mods that move weapons without telling the weapon it was unequipped.
    /// </summary>
    public class HediffComp_TiantuiBladeLink : HediffComp
    {
        private const int CheckIntervalTicks = 60;

        private bool shouldRemove;

        public HediffCompProperties_TiantuiBladeLink Props => (HediffCompProperties_TiantuiBladeLink)props;

        public override bool CompShouldRemove => shouldRemove;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (!Pawn.IsHashIntervalTick(CheckIntervalTicks)) return;

            CompTiantuiBlade bladeComp = Pawn.equipment?.Primary?.TryGetComp<CompTiantuiBlade>();
            shouldRemove = bladeComp == null || (Props.onlyWhileOverheated && !bladeComp.IsOverheated);
        }
    }
}
