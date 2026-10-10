using SingularityFramework.Strikes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Sound;

namespace Tiantui_Stars_Blade
{
    public class CompProperties_Shatterslash : CompProperties
    {
        /// <summary>
        /// How many swings make one combo. The last swing is the finisher.
        /// </summary>
        public int swingsPerCombo = 3;

        /// <summary>
        /// The combo starts over if the wielder doesn't swing again within this many ticks. 300 ticks is 5 seconds.
        /// </summary>
        public int comboResetTicks = 300;

        /// <summary>
        /// Damage multiplier for a swing that fires a round, per round type.
        /// </summary>
        public float tigerslayerDamageFactor = 1.2f;
        public float savageTigerslayerDamageFactor = 1.4f;

        /// <summary>
        /// Extra effects when a swing that fired a round lands, per round type.
        /// </summary>
        public List<OnHitEffect> tigerslayerOnHitEffects;
        public List<OnHitEffect> savageTigerslayerOnHitEffects;

        /// <summary>
        /// Extra effects when the combo finisher fires a round and lands, on top of the round type's own.
        /// </summary>
        public List<OnHitEffect> finisherOnHitEffects;

        public bool spendRoundsOnMiss = true;

        public SoundDef swingLandedSound;

        public CompProperties_Shatterslash()
        {
            compClass = typeof(CompShatterslash);
        }
    }

    /// <summary>
    /// The blade's enhanced normal attacks. Shatterslash fires a Tigerslayer Round on the third swing of a combo;
    /// Blasting Shatterslash fires a Savage round on every swing. A swing that fires hits harder, adds the round's effects,
    /// and the combo finisher also stuns.
    /// </summary>
    public class CompShatterslash : ThingComp, IStrikeModifier
    {
        private int swingsInCombo;
        private int lastSwingTick = -99999;

        private bool firingThisSwing;
        private bool isFinisherThisSwing;
        private bool savageThisSwing;

        public CompProperties_Shatterslash Props => (CompProperties_Shatterslash)props;

        private CompTiantuiBlade BladeComp => parent.TryGetComp<CompTiantuiBlade>();

        public bool TryJoinStrike(Pawn attacker, Verb attackVerb)
        {
            CompTiantuiBlade bladeComp = BladeComp;
            if (bladeComp == null) return false;

            if (Find.TickManager.TicksGame - lastSwingTick > Props.comboResetTicks) swingsInCombo = 0;

            isFinisherThisSwing = swingsInCombo + 1 >= Props.swingsPerCombo;
            savageThisSwing = bladeComp.IsSavageModeActive;

            bool wantsToFire = savageThisSwing || isFinisherThisSwing;
            firingThisSwing = wantsToFire && !bladeComp.IsOverheated && bladeComp.RoundsLoadedInSelectedMagazine > 0;
            return true;
        }

        public void ModifyDamage(Pawn attacker, Thing target, ref DamageInfo damageInfo)
        {
            if (!firingThisSwing) return;
            float damageFactor = savageThisSwing ? Props.savageTigerslayerDamageFactor : Props.tigerslayerDamageFactor;
            damageInfo.SetAmount(damageInfo.Amount * damageFactor);
        }

        public void Notify_StrikeLanded(Pawn attacker, Thing target)
        {
            if (Props.swingLandedSound != null && attacker.Map != null) Props.swingLandedSound.PlayOneShot(new TargetInfo(attacker.Position, attacker.Map));
            if (firingThisSwing && BladeComp.TrySpendRound())
            {
                OnHitEffect.TryApplyAll(savageThisSwing ? Props.savageTigerslayerOnHitEffects : Props.tigerslayerOnHitEffects, attacker, target);
                if (isFinisherThisSwing) OnHitEffect.TryApplyAll(Props.finisherOnHitEffects, attacker, target);
            }
            AdvanceCombo();
        }

        public void Notify_StrikeMissed(Pawn attacker)
        {
            if (firingThisSwing && Props.spendRoundsOnMiss) BladeComp.TrySpendRound();
            AdvanceCombo();
        }

        private void AdvanceCombo()
        {
            swingsInCombo = isFinisherThisSwing ? 0 : swingsInCombo + 1;
            lastSwingTick = Find.TickManager.TicksGame;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref swingsInCombo, "swingsInCombo");
            Scribe_Values.Look(ref lastSwingTick, "lastSwingTick", -99999);
        }
    }
}
