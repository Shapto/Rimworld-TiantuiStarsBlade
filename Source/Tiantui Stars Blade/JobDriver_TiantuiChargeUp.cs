using SingularityFramework.Sequences;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Tiantui_Stars_Blade
{
    public class JobDriver_TiantuiChargeUp : JobDriver_ChargeUp
    {
        private int ticksCharging;
        private int flaresDone;

        private CompTiantuiBlade BladeComp => pawn.equipment?.Primary?.TryGetComp<CompTiantuiBlade>();

        private int ChargeUpTicks
        {
            get
            {
                CompAbilityEffect_Chain chain = job.ability?.CompOfType<CompAbilityEffect_Chain>();
                return Mathf.Max(1, chain?.SettingsForStage(chain.ChargingStage)?.chargeUpTicks ?? 1);
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            bool isChargeToil = true;
            foreach (Toil toil in base.MakeNewToils())
            {
                if (isChargeToil) toil.AddPreTickAction(ChargeTick);
                isChargeToil = false;
                yield return toil;
            }
        }

        private void ChargeTick()
        {
            CompTiantuiBlade bladeComp = BladeComp;
            if (bladeComp == null) return;

            ticksCharging++;
            List<float> flareFractions = bladeComp.Props.chargeFlareFractions;
            while (flaresDone < flareFractions.Count && ticksCharging >= flareFractions[flaresDone] * ChargeUpTicks)
            {
                bladeComp.VentExhausts(bladeComp.Props.chargeFlareIntensity);
                flaresDone++;
            }

            if (ticksCharging % Mathf.Max(1, bladeComp.Props.chargeSmokeIntervalTicks) == 0) bladeComp.VentExhausts(0.6f, false);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksCharging, "ticksCharging");
            Scribe_Values.Look(ref flaresDone, "flaresDone");
        }
    }
}
