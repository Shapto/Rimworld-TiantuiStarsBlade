using RimWorld;
using SingularityFramework.Equipment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Tiantui_Stars_Blade
{
    /// <summary>
    /// The two round types the blade can load. Each has its own magazine.
    /// </summary>
    public enum TigerslayerRoundType
    {
        Tigerslayer,
        SavageTigerslayer
    }

    /// <summary>
    /// XML-tunable numbers for the blade: magazine sizes, heat per round, cooling, and the overheat threshold.
    /// </summary>
    public class CompProperties_TiantuiBlade : CompProperties
    {
        public int tigerslayerMagazineCapacity = 6;
        public int savageTigerslayerMagazineCapacity = 3;

        public float heatPerTigerslayerRound = 10f;
        public float heatPerSavageTigerslayerRound = 22f;
        public float jumpSlamHeatMultiplier = 2.5f;

        public float maximumHeat = 100f;
        public float overheatClearsBelowHeat = 40f;
        public float heatCooledPerSecond = 3f;

        public HediffDef overheatHediff;

        public HediffDef innerStrengthHediff;

        public CompProperties_TiantuiBlade()
        {
            compClass = typeof(CompTiantuiBlade);
        }
    }

    public class CompTiantuiBlade : ThingComp, IEquippedGizmoProvider
    {
        private int tigerslayerRoundsLoaded;
        private int savageTigerslayerRoundsLoaded;
        private TigerslayerRoundType selectedRoundType = TigerslayerRoundType.Tigerslayer;
        private float storedHeat;
        private int heatLastChangedTick;
        private bool isOverheated;

        public CompProperties_TiantuiBlade Props => (CompProperties_TiantuiBlade)props;

        public TigerslayerRoundType SelectedRoundType => selectedRoundType;

        public bool IsSavageModeActive => selectedRoundType == TigerslayerRoundType.SavageTigerslayer;

        public Pawn Wielder => (parent.ParentHolder as Pawn_EquipmentTracker)?.pawn;

        public int RoundsLoadedInSelectedMagazine => IsSavageModeActive ? savageTigerslayerRoundsLoaded : tigerslayerRoundsLoaded;

        public int SelectedMagazineCapacity => IsSavageModeActive ? Props.savageTigerslayerMagazineCapacity : Props.tigerslayerMagazineCapacity;

        public float CurrentHeat
        {
            get
            {
                float secondsSinceChange = (Find.TickManager.TicksGame - heatLastChangedTick) / 60f;
                return Mathf.Max(0f, storedHeat - secondsSinceChange * Props.heatCooledPerSecond);
            }
        }

        public bool IsOverheated
        {
            get
            {
                if (isOverheated && CurrentHeat < Props.overheatClearsBelowHeat) isOverheated = false;
                return isOverheated;
            }
        }

        /// <summary>
        /// A newly made blade comes with both magazines full.
        /// </summary>
        public override void PostPostMake()
        {
            base.PostPostMake();
            tigerslayerRoundsLoaded = Props.tigerslayerMagazineCapacity;
            savageTigerslayerRoundsLoaded = Props.savageTigerslayerMagazineCapacity;
        }

        public bool TrySpendRound(bool isJumpSlam)
        {
            if (IsOverheated || RoundsLoadedInSelectedMagazine <= 0) return false;

            if (IsSavageModeActive) savageTigerslayerRoundsLoaded--;
            else tigerslayerRoundsLoaded--;

            float baseHeat = IsSavageModeActive ? Props.heatPerSavageTigerslayerRound : Props.heatPerTigerslayerRound;
            if (isJumpSlam) baseHeat *= Props.jumpSlamHeatMultiplier;

            AddHeat(baseHeat);
            return true;
        }

        public void SwitchRoundType()
        {
            selectedRoundType = IsSavageModeActive ? TigerslayerRoundType.Tigerslayer : TigerslayerRoundType.SavageTigerslayer;
            UpdateInnerStrengthStage();
        }

        private void AddHeat(float baseHeat)
        {
            storedHeat = CurrentHeat + baseHeat * parent.GetStatValue(TiantuiDefOf.Tiantui_HeatBuildup);
            heatLastChangedTick = Find.TickManager.TicksGame;

            if (storedHeat < Props.maximumHeat) return;

            storedHeat = Props.maximumHeat;
            isOverheated = true;

            Pawn wielder = Wielder;
            if (wielder == null || Props.overheatHediff == null) return;
            if (!wielder.health.hediffSet.HasHediff(Props.overheatHediff)) wielder.health.AddHediff(Props.overheatHediff);
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            if (Props.innerStrengthHediff == null) return;
            if (!pawn.health.hediffSet.HasHediff(Props.innerStrengthHediff)) pawn.health.AddHediff(Props.innerStrengthHediff);
            UpdateInnerStrengthStage();
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            if (Props.innerStrengthHediff == null) return;
            Hediff innerStrength = pawn.health.hediffSet.GetFirstHediffOfDef(Props.innerStrengthHediff);
            if (innerStrength != null) pawn.health.RemoveHediff(innerStrength);
        }

        private void UpdateInnerStrengthStage()
        {
            Pawn wielder = Wielder;
            if (wielder == null || Props.innerStrengthHediff == null) return;
            Hediff innerStrength = wielder.health.hediffSet.GetFirstHediffOfDef(Props.innerStrengthHediff);
            if (innerStrength != null) innerStrength.Severity = IsSavageModeActive ? 2f : 1f;
        }

        public IEnumerable<Gizmo> GetEquippedGizmos(Pawn wielder)
        {
            yield return new Gizmo_TiantuiBladeStatus { bladeComp = this, wielder = wielder };
            yield return new Command_Action
            {
                defaultLabel = IsSavageModeActive ? "Tiantui_LoadTigerslayerRounds".Translate() : "Tiantui_LoadSavageTigerslayerRounds".Translate(),
                defaultDesc = "Tiantui_SwitchRoundTypeDescription".Translate(),
                action = SwitchRoundType
            };
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref tigerslayerRoundsLoaded, "tigerslayerRoundsLoaded");
            Scribe_Values.Look(ref savageTigerslayerRoundsLoaded, "savageTigerslayerRoundsLoaded");
            Scribe_Values.Look(ref selectedRoundType, "selectedRoundType", TigerslayerRoundType.Tigerslayer);
            Scribe_Values.Look(ref storedHeat, "storedHeat");
            Scribe_Values.Look(ref heatLastChangedTick, "heatLastChangedTick");
            Scribe_Values.Look(ref isOverheated, "isOverheated");
        }
    }
}
