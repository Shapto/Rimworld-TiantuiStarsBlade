using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace Tiantui_Stars_Blade
{
    public class JobDriver_SwitchTiantuiRounds : JobDriver
    {
        private CompTiantuiBlade BladeComp => pawn.equipment?.Primary?.TryGetComp<CompTiantuiBlade>();

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            MeleeAnimationCompat.RefreshHeldWeapon(pawn);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(condition => MeleeAnimationCompat.RefreshHeldWeapon(pawn));
            this.FailOn(() => BladeComp == null);

            int switchTicks = BladeComp?.Props.reloadTicks ?? 120;
            yield return Toils_General.Wait(switchTicks).WithSoundsSpreadOver(BladeComp?.Props.reloadSounds, switchTicks);

            Toil switchRounds = ToilMaker.MakeToil("SwitchRounds");
            switchRounds.initAction = () => BladeComp?.SwitchRoundType();
            switchRounds.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return switchRounds;
        }
    }
}
