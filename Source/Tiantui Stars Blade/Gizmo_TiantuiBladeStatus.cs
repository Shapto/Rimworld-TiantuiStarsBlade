using SingularityFramework.Relics;
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
    /// One status box for the blade: the selected round type and its ammo, the heat, and the wielder's aptitude.
    /// </summary>
    [StaticConstructorOnStartup]
    public class Gizmo_TiantuiBladeStatus : Gizmo
    {
        private static readonly Texture2D EmptyBarTexture = SolidColorMaterials.NewSolidColorTexture(new Color(0.1f, 0.1f, 0.1f));
        private static readonly Texture2D TigerslayerAmmoBarTexture = SolidColorMaterials.NewSolidColorTexture(new Color(0.65f, 0.65f, 0.7f));
        private static readonly Texture2D SavageAmmoBarTexture = SolidColorMaterials.NewSolidColorTexture(new Color(0.45f, 0.8f, 0.3f));
        private static readonly Texture2D HeatBarTexture = SolidColorMaterials.NewSolidColorTexture(new Color(0.95f, 0.55f, 0.15f));
        private static readonly Texture2D OverheatedBarTexture = SolidColorMaterials.NewSolidColorTexture(new Color(0.85f, 0.1f, 0.1f));

        public CompTiantuiBlade bladeComp;
        public Pawn wielder;

        public Gizmo_TiantuiBladeStatus()
        {
            Order = -100f;
        }

        public override float GetWidth(float maximumWidth)
        {
            GameFont previousFont = Text.Font;
            Text.Font = GameFont.Tiny;
            float roundTypeWidth = Mathf.Max(Text.CalcSize("Tiantui_TigerslayerRoundsName".Translate()).x, Text.CalcSize("Tiantui_SavageTigerslayerRoundsName".Translate()).x);
            float aptitudeWidth = Text.CalcSize(RelicAptitude.Label(AptitudeLevel.Partial).CapitalizeFirst()).x;
            Text.Font = previousFont;

            float neededWidth = roundTypeWidth + aptitudeWidth + 12f + 10f;
            return Mathf.Clamp(neededWidth, 180f, maximumWidth);
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maximumWidth, GizmoRenderParms renderParameters)
        {
            Rect outerRect = new Rect(topLeft.x, topLeft.y, GetWidth(maximumWidth), 75f);
            Rect innerRect = outerRect.ContractedBy(6f);
            Widgets.DrawWindowBackground(outerRect);

            float rowHeight = innerRect.height / 3f;
            Rect topRowRect = new Rect(innerRect.x, innerRect.y, innerRect.width, rowHeight);
            Rect ammoBarRect = new Rect(innerRect.x, innerRect.y + rowHeight + 1f, innerRect.width, rowHeight - 2f);
            Rect heatBarRect = new Rect(innerRect.x, innerRect.y + rowHeight * 2f + 1f, innerRect.width, rowHeight - 2f);

            CompRelicAptitude aptitudeComp = bladeComp.parent.TryGetComp<CompRelicAptitude>();

            Text.Font = GameFont.Tiny;
            string roundTypeLabel = bladeComp.IsSavageModeActive ? "Tiantui_SavageTigerslayerRoundsName".Translate() : "Tiantui_TigerslayerRoundsName".Translate();
            Widgets.Label(topRowRect, roundTypeLabel);

            if (aptitudeComp != null && wielder != null)
            {
                AptitudeLevel aptitude = RelicAptitude.GetAptitude(wielder, aptitudeComp.Props, bladeComp.parent.def);
                Text.Anchor = TextAnchor.UpperRight;
                GUI.color = RelicAptitude.TierColor(aptitude);
                Widgets.Label(topRowRect, RelicAptitude.Label(aptitude).CapitalizeFirst());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }

            int roundsLoaded = bladeComp.RoundsLoadedInSelectedMagazine;
            int magazineCapacity = bladeComp.SelectedMagazineCapacity;
            float ammoFraction = magazineCapacity > 0 ? roundsLoaded / (float)magazineCapacity : 0f;
            Texture2D ammoBarTexture = bladeComp.IsSavageModeActive ? SavageAmmoBarTexture : TigerslayerAmmoBarTexture;
            Widgets.FillableBar(ammoBarRect, ammoFraction, ammoBarTexture, EmptyBarTexture, false);

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(ammoBarRect, $"{roundsLoaded} / {magazineCapacity}");

            float currentHeat = bladeComp.CurrentHeat;
            bool isOverheated = bladeComp.IsOverheated;
            float heatFraction = bladeComp.Props.maximumHeat > 0f ? currentHeat / bladeComp.Props.maximumHeat : 0f;
            Texture2D heatBarTexture = isOverheated ? OverheatedBarTexture : HeatBarTexture;
            Widgets.FillableBar(heatBarRect, heatFraction, heatBarTexture, EmptyBarTexture, false);

            string heatLabel = isOverheated ? "Tiantui_Overheated".Translate() : "Tiantui_HeatLabel".Translate(heatFraction.ToStringPercent());
            Widgets.Label(heatBarRect, heatLabel);
            Text.Anchor = TextAnchor.UpperLeft;

            if (Mouse.IsOver(outerRect))
            {
                Widgets.DrawHighlight(outerRect);

                string heatTooltip = "Tiantui_HeatTooltip".Translate(currentHeat.ToString("0"), bladeComp.Props.maximumHeat.ToString("0"), bladeComp.Props.overheatClearsBelowHeat.ToString("0"));
                string aptitudeTooltip = aptitudeComp != null && wielder != null ? RelicAptitude.DescribeAptitude(wielder, aptitudeComp.Props, bladeComp.parent.def) : string.Empty;
                string tooltipText = aptitudeTooltip.NullOrEmpty() ? heatTooltip : aptitudeTooltip + "\n\n" + heatTooltip;
                TooltipHandler.TipRegion(outerRect, tooltipText);
            }

            Text.Font = GameFont.Small;
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
