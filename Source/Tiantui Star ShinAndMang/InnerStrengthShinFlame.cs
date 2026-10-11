using RimWorld;
using ShinAndMang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Tiantui_Stars_Blade.ShinCompat
{
    public class HediffCompProperties_InnerStrengthShinFlame : HediffCompProperties
    {
        public ThingDef flameMote;

        /// <summary>
        /// How much bigger the flame burns with Extreme Inner Strength.
        /// </summary>
        public float extremeScale = 1.25f;

        public HediffCompProperties_InnerStrengthShinFlame()
        {
            compClass = typeof(HediffComp_InnerStrengthShinFlame);
        }
    }

    /// <summary>
    /// On Inner Strength: while the pawn also has Shin (心) active, a red flame burns inside the golden aura.
    /// </summary>
    public class HediffComp_InnerStrengthShinFlame : HediffComp
    {
        private Mote flameMote;

        public HediffCompProperties_InnerStrengthShinFlame Props => (HediffCompProperties_InnerStrengthShinFlame)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (!Pawn.Spawned || !ShinMechanics.IsShinActive(Pawn)) return;

            if (Props.flameMote != null)
            {
                if (flameMote == null || flameMote.Destroyed) flameMote = MoteMaker.MakeAttachedOverlay(Pawn, Props.flameMote, Vector3.zero);
                flameMote.linearScale = Vector3.one * (parent.Severity >= 2f ? Props.extremeScale : 1f);
                flameMote.Maintain();
            }
        }

        public override string CompTipStringExtra
        {
            get
            {
                bool isExtreme = parent.Severity >= 2f;
                float damageTakenFactor = isExtreme ? InnerStrengthShinStats.ExtremeInnerStrengthDamageTakenFactor : InnerStrengthShinStats.InnerStrengthDamageTakenFactor;
                float painShockThresholdOffset = isExtreme ? InnerStrengthShinStats.ExtremeInnerStrengthPainShockThresholdOffset : InnerStrengthShinStats.InnerStrengthPainShockThresholdOffset;
                return "Tiantui_InnerStrengthShinBonus".Translate(damageTakenFactor.ToStringPercent(), painShockThresholdOffset.ToStringPercent());
            }
        }
    }

    /// <summary>
    /// While Shin (心) is active, Inner Strength strengthens it: a stat change credited to Inner Strength itself.
    /// </summary>
    public class StatPart_InnerStrengthShin : StatPart
    {
        public HediffDef innerStrengthHediff;
        public float innerStrengthValue;
        public float extremeInnerStrengthValue;
        public bool isFactor;

        /// <summary>
        /// The value for this pawn's current Inner Strength stage, if they have it and Shin (心) is active.
        /// </summary>
        private bool TryGetValue(StatRequest request, out float value, out Hediff innerStrength)
        {
            value = isFactor ? 1f : 0f;
            innerStrength = null;
            if (!(request.Thing is Pawn pawn) || innerStrengthHediff == null || !ShinMechanics.IsShinActive(pawn)) return false;

            innerStrength = pawn.health.hediffSet.GetFirstHediffOfDef(innerStrengthHediff);
            if (innerStrength == null) return false;

            value = innerStrength.Severity >= 2f ? extremeInnerStrengthValue : innerStrengthValue;
            return true;
        }

        public override void TransformValue(StatRequest request, ref float value)
        {
            if (!TryGetValue(request, out float partValue, out _)) return;
            if (isFactor) value *= partValue;
            else value += partValue;
        }

        public override string ExplanationPart(StatRequest request)
        {
            if (!TryGetValue(request, out float partValue, out Hediff innerStrength)) return null;
            string valueText = isFactor ? "x" + partValue.ToStringPercent() : parentStat.ValueToString(partValue, ToStringNumberSense.Offset);
            return innerStrength.LabelCap + " (Shin (心)): " + valueText;
        }
    }

    /// <summary>
    /// At startup, adds Inner Strength's Shin (心) bonus to the stats it changes.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class InnerStrengthShinStats
    {
        public const float InnerStrengthDamageTakenFactor = 0.95f;
        public const float ExtremeInnerStrengthDamageTakenFactor = 0.90f;
        public const float InnerStrengthPainShockThresholdOffset = 0.03f;
        public const float ExtremeInnerStrengthPainShockThresholdOffset = 0.06f;

        static InnerStrengthShinStats()
        {
            HediffDef innerStrengthHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Tiantui_InnerStrength");
            if (innerStrengthHediff == null) return;

            AddPart(StatDefOf.IncomingDamageFactor, innerStrengthHediff, InnerStrengthDamageTakenFactor, ExtremeInnerStrengthDamageTakenFactor, true);
            AddPart(StatDefOf.PainShockThreshold, innerStrengthHediff, InnerStrengthPainShockThresholdOffset, ExtremeInnerStrengthPainShockThresholdOffset, false);
        }

        private static void AddPart(StatDef stat, HediffDef innerStrengthHediff, float innerStrengthValue, float extremeInnerStrengthValue, bool isFactor)
        {
            if (stat.parts == null) stat.parts = new List<StatPart>();
            stat.parts.Add(new StatPart_InnerStrengthShin { parentStat = stat, innerStrengthHediff = innerStrengthHediff, innerStrengthValue = innerStrengthValue, extremeInnerStrengthValue = extremeInnerStrengthValue, isFactor = isFactor });
        }
    }

    /// <summary>
    /// A red glow drawn just above the Shin (心) glow and below the pawn.
    /// </summary>
    public class Mote_InnerStrengthFlame : MoteAttached
    {
        private const float AltitudeAboveShinGlow = 0.01f;

        protected override void DrawAt(Vector3 drawLocation, bool flip = false)
        {
            // Same layer as the Shin (心) glow, nudged up so it draws over the glow but under the pawn.
            drawLocation.y = AltitudeLayer.MoteLow.AltitudeFor() + AltitudeAboveShinGlow;
            base.DrawAt(drawLocation, flip);
        }
   
    }
}
