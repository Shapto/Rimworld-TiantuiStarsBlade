using RimWorld;
using SingularityFramework.Equipment;
using SingularityFramework.Relics;
using SingularityFramework.Sequences;
using SingularityFramework.Visuals;
using SingularityFramework.WeaponAnchor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

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
        public int tigerslayerMagazineCapacity = 12;
        public int savageTigerslayerMagazineCapacity = 8;

        public float heatPerTigerslayerRound = 10f;
        public float heatPerSavageTigerslayerRound = 15f;

        public float maximumHeat = 100f;
        public float overheatClearsBelowHeat = 40f;
        public float heatCooledPerSecond = 3f;

        public HediffDef overheatHediff;

        public HediffDef innerStrengthHediff;

        public ThingDef tigerslayerAmmoDef;
        public ThingDef savageTigerslayerAmmoDef;
        public int ammoPerTigerslayerRound = 5;
        public int ammoPerSavageTigerslayerRound = 2;

        public int reloadTicks = 120;

        public SoundDef roundFiredSound;
        public SoundDef overheatSound;
        public SoundDef coolOffSound;
        public SoundDef equipSound;
        public List<SoundDef> reloadSounds;

        public float unworthyStumbleChance = 1f;
        public FloatRange stumbleAtDashFraction = new FloatRange(0.3f, 0.6f);
        public IntRange stumbleRollCells = new IntRange(4, 6);
        public int stumbleRollTicks = 30;
        public float stumbleRollEasingPower = 2.5f;
        public float stumbleQuarterTurnsPerCell = 2f;
        public int stumbleBruiseHits = 2;
        public FloatRange stumbleBruiseDamage = new FloatRange(2f, 4f);
        public int stumbleStunTicks = 90;
        public float stumbleDisarmChance = 0.25f;

        public List<FleckDef> exhaustFlecks;
        public FloatRange exhaustFleckScale = new FloatRange(0.25f, 0.35f);
        public FloatRange exhaustFleckSpeed = new FloatRange(0.3f, 0.6f);
        public float exhaustOutwardOffset = 0.1f;
        public float exhaustAngleSpread = 12f;

        public FleckDef exhaustSmokeFleck;
        public FloatRange exhaustSmokeScale = new FloatRange(0.4f, 0.7f);
        public FloatRange exhaustSmokeSpeed = new FloatRange(0.2f, 0.45f);
        public float exhaustSmokeChance = 0.6f;

        public List<float> chargeFlareFractions = new List<float> { 0.35f, 0.7f };
        public float chargeFlareIntensity = 1.6f;
        public int chargeSmokeIntervalTicks = 20;

        public string openedTexPath;

        public CompProperties_TiantuiBlade()
        {
            compClass = typeof(CompTiantuiBlade);
        }
    }

    public class CompTiantuiBlade : ThingComp, IEquippedGizmoProvider, ILandingSlamListener
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

        public ThingDef SelectedAmmoDef => IsSavageModeActive ? Props.savageTigerslayerAmmoDef : Props.tigerslayerAmmoDef;

        public int AmmoPerSelectedRound => IsSavageModeActive ? Props.ammoPerSavageTigerslayerRound : Props.ammoPerTigerslayerRound;

        public int RoundsMissingFromSelectedMagazine => SelectedMagazineCapacity - RoundsLoadedInSelectedMagazine;

        public void Notify_LandingSlamLanded(Pawn jumper) => ForceOverheat();

        public float CurrentHeat
        {
            get
            {
                float secondsSinceChange = (Find.TickManager.TicksGame - heatLastChangedTick) / 60f;
                return Mathf.Max(0f, storedHeat - secondsSinceChange * Props.heatCooledPerSecond);
            }
        }

        public bool WielderIsUnworthy
        {
            get
            {
                Pawn wielder = Wielder;
                CompRelicAptitude aptitudeComp = parent.TryGetComp<CompRelicAptitude>();
                return wielder != null && aptitudeComp != null && RelicAptitude.GetAptitude(wielder, aptitudeComp.Props, parent.def) == AptitudeLevel.Unworthy;
            }
        }

        public bool IsOverheated
        {
            get
            {
                if (isOverheated && CurrentHeat < Props.overheatClearsBelowHeat)
                {
                    isOverheated = false;
                    if (Find.TickManager.TicksGame - TickHeatClearsOverheat <= 120) PlayAtWielder(Props.coolOffSound);
                }
                return isOverheated;
            }
        }

        private int TickHeatClearsOverheat
        {
            get
            {
                if (Props.heatCooledPerSecond <= 0f) return int.MaxValue;
                float secondsToClear = (storedHeat - Props.overheatClearsBelowHeat) / Props.heatCooledPerSecond;
                return heatLastChangedTick + Mathf.CeilToInt(secondsToClear * 60f);
            }
        }

        private Graphic openedGraphic;

        /// <summary>
        /// True while the wielder is reloading or loading the other rounds, when the blade is broken open.
        /// </summary>
        public bool IsOpened
        {
            get
            {
                JobDef currentJob = Wielder?.CurJobDef;
                return currentJob == TiantuiDefOf.Tiantui_ReloadBlade || currentJob == TiantuiDefOf.Tiantui_SwitchRounds;
            }
        }

        /// <summary>
        /// The broken-open texture, sized and shaded like the normal one. Null if no opened texture is set.
        /// </summary>
        public Graphic OpenedGraphic
        {
            get
            {
                if (openedGraphic != null || Props.openedTexPath.NullOrEmpty()) return openedGraphic;

                Graphic closedGraphic = parent.def.graphic;
                openedGraphic = GraphicDatabase.Get<Graphic_Single>(Props.openedTexPath, closedGraphic.Shader, closedGraphic.drawSize, closedGraphic.color);
                return openedGraphic;
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

        /// <summary>
        /// Fires one round of the selected type if one is loaded and the blade isn't overheated.
        /// Normal attacks add heat; ability stages don't (the finisher overheats the blade instead).
        /// The fired sound can be swapped, e.g. the Savage abilities' launch sound in place of the gunshot.
        /// </summary>
        public bool TrySpendRound(bool addsHeat = true, SoundDef firedSound = null)
        {
            if (IsOverheated || RoundsLoadedInSelectedMagazine <= 0) return false;

            if (IsSavageModeActive) savageTigerslayerRoundsLoaded--;
            else tigerslayerRoundsLoaded--;

            if (addsHeat) AddHeat(IsSavageModeActive ? Props.heatPerSavageTigerslayerRound : Props.heatPerTigerslayerRound);
            PlayAtWielder(firedSound ?? Props.roundFiredSound);
            VentExhausts();
            return true;
        }

        public void SwitchRoundType()
        {
            selectedRoundType = IsSavageModeActive ? TigerslayerRoundType.Tigerslayer : TigerslayerRoundType.SavageTigerslayer;
            UpdateInnerStrengthStage();
        }

        private void PlayAtWielder(SoundDef sound)
        {
            Pawn wielder = Wielder;
            if (sound == null || wielder?.Map == null) return;
            sound.PlayOneShot(new TargetInfo(wielder.Position, wielder.Map));
        }

        public void ReloadFrom(List<Thing> ammoStacks)
        {
            ammoStacks = ammoStacks?.Where(stack => stack != null && !stack.Destroyed).ToList();
            if (ammoStacks.NullOrEmpty()) return;

            ThingDef ammoDef = ammoStacks[0].def;
            bool isSavageAmmo = ammoDef == Props.savageTigerslayerAmmoDef;
            if (!isSavageAmmo && ammoDef != Props.tigerslayerAmmoDef) return;

            int ammoPerRound = Mathf.Max(1, isSavageAmmo ? Props.ammoPerSavageTigerslayerRound : Props.ammoPerTigerslayerRound);
            int roundsMissing = isSavageAmmo ? Props.savageTigerslayerMagazineCapacity - savageTigerslayerRoundsLoaded : Props.tigerslayerMagazineCapacity - tigerslayerRoundsLoaded;
            int ammoAvailable = ammoStacks.Where(stack => stack.def == ammoDef).Sum(stack => stack.stackCount);
            int roundsLoaded = Mathf.Min(roundsMissing, ammoAvailable / ammoPerRound);
            if (roundsLoaded <= 0) return;

            int ammoStillToTake = roundsLoaded * ammoPerRound;
            foreach (Thing ammoStack in ammoStacks.Where(stack => stack.def == ammoDef))
            {
                if (ammoStillToTake <= 0) break;
                int takenFromThisStack = Mathf.Min(ammoStillToTake, ammoStack.stackCount);
                ammoStack.SplitOff(takenFromThisStack).Destroy();
                ammoStillToTake -= takenFromThisStack;
            }

            if (isSavageAmmo) savageTigerslayerRoundsLoaded += roundsLoaded;
            else tigerslayerRoundsLoaded += roundsLoaded;
        }

        /// <summary>
        /// Orders the wielder to load the other round type (a short job with the reload sounds).
        /// </summary>
        public void StartSwitchingRoundType(Pawn wielder)
        {
            if (wielder?.jobs == null) return;
            wielder.jobs.TryTakeOrderedJob(JobMaker.MakeJob(TiantuiDefOf.Tiantui_SwitchRounds), JobTag.Misc);
        }

        public void TryStartReload(Pawn wielder)
        {
            ThingDef ammoDef = SelectedAmmoDef;
            if (wielder?.Map == null || ammoDef == null) return;

            int ammoPerRound = AmmoPerSelectedRound;

            List<Thing> carriedAmmo = wielder.inventory?.innerContainer.Where(carriedThing => carriedThing.def == ammoDef).ToList();
            Thing ammoStack = carriedAmmo != null && carriedAmmo.Sum(carriedThing => carriedThing.stackCount) >= ammoPerRound ? carriedAmmo[0] : null;
           
            if (RoundsMissingFromSelectedMagazine <= 0)
            {
                Messages.Message("Tiantui_MagazineFull".Translate(), wielder, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (ammoStack == null)
            {
                ammoStack = GenClosest.ClosestThingReachable(wielder.Position, wielder.Map, ThingRequest.ForDef(ammoDef), PathEndMode.Touch, TraverseParms.For(wielder), 9999f, candidate => candidate.stackCount >= ammoPerRound && !candidate.IsForbidden(wielder) && wielder.CanReserve(candidate));
            }

            if (ammoStack == null)
            {
                Messages.Message("Tiantui_NoAmmoNearby".Translate(ammoDef.label), wielder, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Job reloadJob = JobMaker.MakeJob(TiantuiDefOf.Tiantui_ReloadBlade, ammoStack);
            wielder.jobs.TryTakeOrderedJob(reloadJob, JobTag.Misc);
        }

        private void AddHeat(float baseHeat)
        {
            storedHeat = CurrentHeat + baseHeat * parent.GetStatValue(TiantuiDefOf.Tiantui_HeatBuildup);
            heatLastChangedTick = Find.TickManager.TicksGame;
            if (storedHeat >= Props.maximumHeat) ForceOverheat();
        }

        public void ForceOverheat()
        {
            storedHeat = Props.maximumHeat;
            heatLastChangedTick = Find.TickManager.TicksGame;
            if (isOverheated) return;

            isOverheated = true;
            PlayAtWielder(Props.overheatSound);

            Pawn wielder = Wielder;
            if (wielder == null || Props.overheatHediff == null) return;
            if (!wielder.health.hediffSet.HasHediff(Props.overheatHediff)) wielder.health.AddHediff(Props.overheatHediff);
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            if (pawn.Spawned && Props.equipSound != null) Props.equipSound.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            if (IsOverheated && Props.overheatHediff != null && !pawn.health.hediffSet.HasHediff(Props.overheatHediff)) pawn.health.AddHediff(Props.overheatHediff);
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

            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Fill magazines",
                    defaultDesc = "Fill both magazines to capacity.",
                    action = () =>
                    {
                        tigerslayerRoundsLoaded = Props.tigerslayerMagazineCapacity;
                        savageTigerslayerRoundsLoaded = Props.savageTigerslayerMagazineCapacity;
                    }
                };
            }
        }

        public void VentExhausts(float intensity = 1f, bool withFlames = true)
        {
            Pawn wielder = Wielder;
            if (wielder?.Map == null) return;

            WeaponAnchorGroup exhausts = WeaponEffects.AnchorGroupOf(wielder, "Exhausts", out _);
            if (exhausts == null) return;

            for (int exhaustIndex = 0; exhaustIndex < exhausts.PointCount; exhaustIndex++)
            {
                if (!WeaponEffects.TryGetAnchorInWorld(wielder, "Exhausts", exhaustIndex, out Vector3 worldPosition, out Vector3 worldDirection)) return;

                Vector3 exhaustPosition = worldPosition + worldDirection * Props.exhaustOutwardOffset;
                float outwardAngle = worldDirection.AngleFlat() + Rand.Range(-Props.exhaustAngleSpread, Props.exhaustAngleSpread);

                if (withFlames) ThrowExhaustFlame(wielder.Map, exhaustPosition, outwardAngle, intensity);
                if (!withFlames || Rand.Chance(Props.exhaustSmokeChance * intensity)) ThrowExhaustSmoke(wielder.Map, exhaustPosition, outwardAngle, intensity);
            }
        }

        private void ThrowExhaustFlame(Map map, Vector3 exhaustPosition, float outwardAngle, float intensity)
        {
            if (Props.exhaustFlecks.NullOrEmpty()) return;

            FleckCreationData flameData = FleckMaker.GetDataStatic(exhaustPosition, map, Props.exhaustFlecks.RandomElement(), Props.exhaustFleckScale.RandomInRange * intensity);
            flameData.rotation = outwardAngle;
            flameData.velocityAngle = outwardAngle;
            flameData.velocitySpeed = Props.exhaustFleckSpeed.RandomInRange * intensity;
            map.flecks.CreateFleck(flameData);
        }

        private void ThrowExhaustSmoke(Map map, Vector3 exhaustPosition, float outwardAngle, float intensity)
        {
            if (Props.exhaustSmokeFleck == null) return;

            FleckCreationData smokeData = FleckMaker.GetDataStatic(exhaustPosition, map, Props.exhaustSmokeFleck, Props.exhaustSmokeScale.RandomInRange * intensity);
            smokeData.rotationRate = Rand.Range(-30f, 30f);
            smokeData.velocityAngle = Mathf.LerpAngle(outwardAngle, 0f, 0.5f) + Rand.Range(-15f, 15f);
            smokeData.velocitySpeed = Props.exhaustSmokeSpeed.RandomInRange;
            map.flecks.CreateFleck(smokeData);
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
