using SingularityFramework.Geometry;
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
    public class JobDriver_TiantuiDash : JobDriver_LineDash
    {
        private IntVec3 dashStartCell = IntVec3.Invalid;
        private int stumbleAfterCells = -1;
        private List<IntVec3> dashLineCells;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            foreach (Toil toil in base.MakeNewToils())
            {
                toil.AddPreInitAction(DecideStumble);
                toil.AddPreTickAction(CheckStumble);
                yield return toil;
            }
        }

        private void DecideStumble()
        {
            if (dashStartCell.IsValid) return;
            dashStartCell = pawn.Position;

            CompTiantuiBlade bladeComp = pawn.equipment?.Primary?.TryGetComp<CompTiantuiBlade>();
            if (bladeComp == null || !bladeComp.WielderIsUnworthy || !Rand.Chance(bladeComp.Props.unworthyStumbleChance)) return;

            int lineLength = LineStrike.CellsOnLine(dashStartCell, job.targetA.Cell).Count;
            if (lineLength < 2) return;

            int cellsBeforeFalling = Mathf.RoundToInt(lineLength * bladeComp.Props.stumbleAtDashFraction.RandomInRange);
            stumbleAfterCells = Mathf.Clamp(cellsBeforeFalling, 1, lineLength - 1);
        }

        private void CheckStumble()
        {
            if (stumbleAfterCells < 0 || !dashStartCell.IsValid) return;

            if (dashLineCells == null) dashLineCells = LineStrike.CellsOnLine(dashStartCell, job.targetA.Cell);
            int cellsTravelled = dashLineCells.IndexOf(pawn.Position) + 1;
            if (cellsTravelled < stumbleAfterCells) return;

            Job stumbleJob = JobMaker.MakeJob(TiantuiDefOf.Tiantui_DashStumble, job.targetA.Cell, dashStartCell);
            pawn.jobs.StartJob(stumbleJob, JobCondition.InterruptForced);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref dashStartCell, "dashStartCell", IntVec3.Invalid);
            Scribe_Values.Look(ref stumbleAfterCells, "stumbleAfterCells", -1);
        }
    }
}
