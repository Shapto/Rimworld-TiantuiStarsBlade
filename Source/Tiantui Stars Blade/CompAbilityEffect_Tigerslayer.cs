using RimWorld;
using SingularityFramework.Sequences;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Tiantui_Stars_Blade
{
    public class CompProperties_AbilityTigerslayer : CompProperties_AbilityEffect
    {

        public TigerslayerRoundType roundType = TigerslayerRoundType.Tigerslayer;

        public SoundDef roundFiredSound;

        public CompProperties_AbilityTigerslayer()
        {
            compClass = typeof(CompAbilityEffect_Tigerslayer);
        }
    }
    public class CompAbilityEffect_Tigerslayer : CompAbilityEffect
    {
        public new CompProperties_AbilityTigerslayer Props => (CompProperties_AbilityTigerslayer)props;

        private CompTiantuiBlade BladeComp => parent.pawn?.equipment?.Primary?.TryGetComp<CompTiantuiBlade>();

        public override bool ShouldHideGizmo => BladeComp == null || BladeComp.SelectedRoundType != Props.roundType;

        public override bool GizmoDisabled(out string reason)
        {
            CompTiantuiBlade bladeComp = BladeComp;
            if (bladeComp != null && bladeComp.IsOverheated)
            {
                reason = "Tiantui_AbilityOverheated".Translate();
                return true;
            }
            if (bladeComp != null && bladeComp.RoundsLoadedInSelectedMagazine <= 0)
            {
                reason = "Tiantui_AbilityNoRounds".Translate();
                return true;
            }
            return base.GizmoDisabled(out reason);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            CompTiantuiBlade bladeComp = BladeComp;
            if (bladeComp == null) return;

            bladeComp.TrySpendRound(false, Props.roundFiredSound);
        }
    }
}
