using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Tiantui_Stars_Blade
{
    public class JobDriver_ReloadTiantuiBlade : JobDriver
    {
        private const TargetIndex AmmoStackIndex = TargetIndex.A;

        private Thing AmmoStack => job.GetTarget(AmmoStackIndex).Thing;

        private CompTiantuiBlade BladeComp => pawn.equipment?.Primary?.TryGetComp<CompTiantuiBlade>();

        /// <summary>
        /// True when the ammo is in the pawn's own inventory instead of lying on the map.
        /// </summary>
        private bool AmmoIsCarried => AmmoStack?.ParentHolder is Pawn_InventoryTracker;


        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (AmmoIsCarried) return true;
            return pawn.Reserve(AmmoStack, job, 1, -1, null, errorOnFailed);
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            MeleeAnimationCompat.RefreshHeldWeapon(pawn);
            BladeComp?.SpawnMotionTrail();
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(condition =>
            {
                MeleeAnimationCompat.RefreshHeldWeapon(pawn);
                BladeComp?.SpawnMotionTrail();
            });
            this.FailOn(() => BladeComp == null);
            int reloadTicks = BladeComp?.Props.reloadTicks ?? 120;
            List<SoundDef> reloadSounds = BladeComp?.Props.reloadSounds;
            if (AmmoIsCarried)
            {
                this.FailOn(() => AmmoStack == null || AmmoStack.Destroyed);
                yield return Toils_General.Wait(reloadTicks).WithSoundsSpreadOver(reloadSounds, reloadTicks);
            }
            else
            {
                this.FailOnDespawnedNullOrForbidden(AmmoStackIndex);
                yield return Toils_Goto.GotoThing(AmmoStackIndex, PathEndMode.Touch);
                yield return Toils_General.Wait(reloadTicks, AmmoStackIndex).WithProgressBarToilDelay(AmmoStackIndex).WithSoundsSpreadOver(reloadSounds, reloadTicks);
            }

            Toil loadRounds = ToilMaker.MakeToil("LoadRounds");
            loadRounds.initAction = LoadRounds;
            loadRounds.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return loadRounds;
        }

        private void LoadRounds()
        {
            CompTiantuiBlade bladeComp = BladeComp;
            Thing ammoStack = AmmoStack;
            if (bladeComp == null || ammoStack == null) return;

            if (!AmmoIsCarried)
            {
                bladeComp.ReloadFrom(new List<Thing> { ammoStack });
                return;
            }

            ThingDef ammoDef = ammoStack.def;
            bladeComp.ReloadFrom(pawn.inventory.innerContainer.Where(carriedThing => carriedThing.def == ammoDef).ToList());
        }
    }
}
