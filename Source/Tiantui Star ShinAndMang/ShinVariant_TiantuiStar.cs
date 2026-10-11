using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using ShinAndMang;

namespace Tiantui_Stars_Blade.ShinCompat
{
    public class ShinVariant_UnrelentingSpirit : ShinVariant
    {
        private const float HealthMissingForFullBonus = 0.40f;
        private const float NoviceMaximumWoundDamageBonus = 0.10f;
        private const float SovereignMaximumWoundDamageBonus = 0.25f;
        private const float NoviceMaximumWoundHitChanceBonus = 0.05f;
        private const float SovereignMaximumWoundHitChanceBonus = 0.15f;

        private const float NoviceDamageBonusPerCellPerSecond = 0.08f;
        private const float SovereignDamageBonusPerCellPerSecond = 0.12f;
        private const float NoviceMaximumSpeedDamageBonus = 0.15f;
        private const float SovereignMaximumSpeedDamageBonus = 0.25f;

        private const float NoviceStunChance = 0.05f;
        private const float SovereignStunChance = 0.15f;
        private const int StunTicks = 60;

        private int lastStunRollTick = -1;
        private Thing lastStunRollTarget;

        public float MaximumWoundDamageBonus => Mathf.Lerp(NoviceMaximumWoundDamageBonus, SovereignMaximumWoundDamageBonus, MasteryProgress);
        public float MaximumWoundHitChanceBonus => Mathf.Lerp(NoviceMaximumWoundHitChanceBonus, SovereignMaximumWoundHitChanceBonus, MasteryProgress);
        public float DamageBonusPerCellPerSecond => Mathf.Lerp(NoviceDamageBonusPerCellPerSecond, SovereignDamageBonusPerCellPerSecond, MasteryProgress);
        public float MaximumSpeedDamageBonus => Mathf.Lerp(NoviceMaximumSpeedDamageBonus, SovereignMaximumSpeedDamageBonus, MasteryProgress);
        public float StunChance => Mathf.Lerp(NoviceStunChance, SovereignStunChance, MasteryProgress);

        /// <summary>
        /// 0 at full health, 1 once 40% of health is missing.
        /// </summary>
        public float WoundProgress => Mathf.Clamp01((1f - ShinUser.health.summaryHealth.SummaryHealthPercent) / HealthMissingForFullBonus);

        public float WoundDamageBonus => WoundProgress * MaximumWoundDamageBonus;
        public float WoundHitChanceBonus => WoundProgress * MaximumWoundHitChanceBonus;

        /// <summary>
        /// Bonus damage against a target that is fighting the user and is slower than them. 0 otherwise.
        /// </summary>
        public float SpeedDamageBonusAgainst(Thing target)
        {
            if (!(target is Pawn targetPawn) || !IsFighting(targetPawn, ShinUser)) return 0f;

            float speedAdvantage = ShinUser.GetStatValue(StatDefOf.MoveSpeed) - targetPawn.GetStatValue(StatDefOf.MoveSpeed);
            if (speedAdvantage <= 0f) return 0f;
            return Mathf.Min(MaximumSpeedDamageBonus, speedAdvantage * DamageBonusPerCellPerSecond);
        }

        public float TotalDamageBonusAgainst(Thing target) => WoundDamageBonus + SpeedDamageBonusAgainst(target);

        public override void ModifyOutgoingDamage(Thing target, ref DamageInfo damageInfo)
        {
            float damageBonus = TotalDamageBonusAgainst(target);
            if (damageBonus > 0f) damageInfo.SetAmount(damageInfo.Amount * (1f + damageBonus));
        }

        public override void Notify_DamageDealt(Thing target, DamageInfo damageInfo, DamageWorker.DamageResult damageResult)
        {
            if (!(target is Pawn targetPawn) || targetPawn.Dead) return;
            if (damageInfo.Def == null || damageInfo.Def.isRanged || damageInfo.Def.isExplosive) return;

            int currentTick = Find.TickManager.TicksGame;
            if (lastStunRollTick == currentTick && lastStunRollTarget == targetPawn) return;
            lastStunRollTick = currentTick;
            lastStunRollTarget = targetPawn;

            StunHandler stunHandler = targetPawn.stances?.stunner;
            if (stunHandler == null || stunHandler.Stunned || !Rand.Chance(StunChance)) return;
            stunHandler.StunFor(StunTicks, ShinUser);
        }

        public override IEnumerable<StatDef> AffectedStats
        {
            get { yield return StatDefOf.MeleeHitChance; }
        }

        public override float GetStatFactor(StatDef stat) => stat == StatDefOf.MeleeHitChance ? 1f + WoundHitChanceBonus : 1f;

        public override string TargetingReadout(Thing target)
        {
            float damageBonus = TotalDamageBonusAgainst(target);
            if (damageBonus <= 0f) return null;
            return "Tiantui_ShinUnrelentingSpirit_Targeting".Translate(parent.LabelCap, damageBonus.ToStringPercent());
        }
        public override string EffectDescription => "Tiantui_ShinUnrelentingSpirit_Effect".Translate(MaximumWoundDamageBonus.ToStringPercent(), MaximumWoundHitChanceBonus.ToStringPercent(), WoundDamageBonus.ToStringPercent(), DamageBonusPerCellPerSecond.ToStringPercent(), MaximumSpeedDamageBonus.ToStringPercent(), StunChance.ToStringPercent(), (StunTicks / 60f).ToString("0.#"));
    }
}
