using RimWorld;
using SingularityFramework.Geometry;
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
    public class JobDriver_TiantuiDashStumble : JobDriver
    {
        private const TargetIndex DashTargetIndex = TargetIndex.A;
        private const TargetIndex DashStartIndex = TargetIndex.B;

        private IntVec3 rollStartCell = IntVec3.Invalid;
        private int rollDistanceCells;
        private int startRotationIndex;
        private int cellsRolled;
        private int ticksRolling;
        private List<IntVec3> rollCells;

        private CompProperties_TiantuiBlade BladeProps => pawn.equipment?.Primary?.TryGetComp<CompTiantuiBlade>()?.Props;

        private int RollTicks => Mathf.Max(1, BladeProps?.stumbleRollTicks ?? 30);

        private float RollProgress
        {
            get
            {
                float timeFraction = Mathf.Clamp01(ticksRolling / (float)RollTicks);
                float easingPower = Mathf.Max(1f, BladeProps?.stumbleRollEasingPower ?? 2.5f);
                return 1f - Mathf.Pow(1f - timeFraction, easingPower);
            }
        }

        public float LyingBodyAngle
        {
            get
            {
                Vector3 rollDirection = (job.GetTarget(DashTargetIndex).Cell - job.GetTarget(DashStartIndex).Cell).ToVector3();
                return rollDirection == Vector3.zero ? 90f : rollDirection.AngleFlat() + 90f;
            }
        }

        public override Rot4 ForcedLayingRotation
        {
            get
            {
                float quarterTurnsPerCell = BladeProps?.stumbleQuarterTurnsPerCell ?? 2f;
                int quarterTurns = Mathf.FloorToInt(RollProgress * RollCells.Count * quarterTurnsPerCell);
                return new Rot4((startRotationIndex + quarterTurns) % 4);
            }
        }

        public override void SetInitialPosture()
        {
            pawn.jobs.posture = PawnPosture.LayingOnGroundNormal;
        }

        private List<IntVec3> RollCells
        {
            get
            {
                if (rollCells != null) return rollCells;

                Map map = pawn.Map;
                Vector3 rollDirection = (job.GetTarget(DashTargetIndex).Cell - job.GetTarget(DashStartIndex).Cell).ToVector3();
                rollDirection.y = 0f;
                if (rollDirection == Vector3.zero || map == null || !rollStartCell.IsValid)
                {
                    rollCells = new List<IntVec3>();
                    return rollCells;
                }

                IntVec3 rollEndCell = (rollStartCell.ToVector3Shifted() + rollDirection.normalized * rollDistanceCells).ToIntVec3();
                rollCells = LineStrike.CellsOnLine(rollStartCell, rollEndCell)
                    .TakeWhile(cell => cell.InBounds(map) && cell.Standable(map))
                    .ToList();
                return rollCells;
            }
        }

        public Vector3 CurrentDrawPosition
        {
            get
            {
                Vector3 startPosition = rollStartCell.ToVector3Shifted();
                Vector3 endPosition = RollCells.Count > 0 ? RollCells[RollCells.Count - 1].ToVector3Shifted() : startPosition;
                Vector3 drawPosition = Vector3.Lerp(startPosition, endPosition, RollProgress);
                drawPosition.y = pawn.Drawer.DrawPos.y;
                return drawPosition;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil rollToil = ToilMaker.MakeToil("TiantuiStumbleRoll");
            rollToil.initAction = StartRolling;
            rollToil.tickAction = RollTick;
            rollToil.handlingFacing = true;
            rollToil.defaultCompleteMode = ToilCompleteMode.Never;
            rollToil.AddFinishAction(() => pawn.Drawer.tweener.ResetTweenedPosToRoot());
            yield return rollToil;

            Toil landToil = ToilMaker.MakeToil("TiantuiStumbleLand");
            landToil.initAction = Land;
            landToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return landToil;

            Toil lieThereToil = Toils_General.Wait(BladeProps?.stumbleStunTicks ?? 90);
            lieThereToil.handlingFacing = true;
            yield return lieThereToil;
        }

        private void StartRolling()
        {
            pawn.pather.StopDead();
            if (rollStartCell.IsValid) return;

            rollStartCell = pawn.Position;
            rollDistanceCells = BladeProps?.stumbleRollCells.RandomInRange ?? 4;
            startRotationIndex = pawn.Rotation.AsInt;
            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Tiantui_StumbleText".Translate(), 2.5f);
        }

        private void RollTick()
        {
            ticksRolling++;
            float rollProgress = RollProgress;
            List<IntVec3> cellsToRoll = RollCells;

            int cellsReached = Mathf.Min(cellsToRoll.Count, Mathf.FloorToInt(rollProgress * cellsToRoll.Count));
            while (cellsRolled < cellsReached)
            {
                IntVec3 nextCell = cellsToRoll[cellsRolled];
                pawn.Position = nextCell;
                pawn.Notify_Teleported(false, false);
                cellsRolled++;
                FleckMaker.ThrowDustPuff(nextCell, pawn.Map, 1.2f);
            }

            if (ticksRolling >= RollTicks) ReadyForNextToil();
        }

        private void Land()
        {
            CompProperties_TiantuiBlade bladeProps = BladeProps;
            if (bladeProps == null) return;

            for (int hitIndex = 0; hitIndex < bladeProps.stumbleBruiseHits; hitIndex++)
            {
                pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, bladeProps.stumbleBruiseDamage.RandomInRange));
                if (pawn.Dead) return;
            }

            if (Rand.Chance(bladeProps.stumbleDisarmChance)) DropBlade();
        }

        private void DropBlade()
        {
            ThingWithComps blade = pawn.equipment?.Primary;
            if (blade == null || blade.TryGetComp<CompTiantuiBlade>() == null) return;

            Map map = pawn.Map;
            if (!CellFinder.TryFindRandomCellNear(pawn.Position, map, 2, cell => cell != pawn.Position && cell.Standable(map), out IntVec3 dropCell)) dropCell = pawn.Position;

            if (pawn.equipment.TryDropEquipment(blade, out ThingWithComps droppedBlade, dropCell, false))
            {
                MoteMaker.ThrowText(droppedBlade.DrawPos, map, "Tiantui_StumbleDisarmed".Translate(), 2.5f);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref rollStartCell, "rollStartCell", IntVec3.Invalid);
            Scribe_Values.Look(ref rollDistanceCells, "rollDistanceCells");
            Scribe_Values.Look(ref startRotationIndex, "startRotationIndex");
            Scribe_Values.Look(ref cellsRolled, "cellsRolled");
            Scribe_Values.Look(ref ticksRolling, "ticksRolling");
        }
    }
}
