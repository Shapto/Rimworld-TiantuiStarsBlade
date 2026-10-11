using RimWorld;
using ShinAndMang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Tiantui_Star_ShinAndMang
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
            if (!Pawn.Spawned || Props.flameMote == null || !ShinMechanics.IsShinActive(Pawn)) return;

            if (flameMote == null || flameMote.Destroyed) flameMote = MoteMaker.MakeAttachedOverlay(Pawn, Props.flameMote, Vector3.zero);
            flameMote.linearScale = Vector3.one * (parent.Severity >= 2f ? Props.extremeScale : 1f);
            flameMote.Maintain();
        }
    }

    /// <summary>
    /// Which textures a flame cycles through, and how fast. Put on the mote's ThingDef.
    /// </summary>
    public class ModExtension_FlameFrames : DefModExtension
    {
        public List<string> framePaths;
        public int ticksPerFrame = 6;
    }

    /// <summary>
    /// A flickering flame drawn just above the Shin (心) glow and below the pawn.
    /// </summary>
    public class Mote_InnerStrengthFlame : MoteAttached
    {
        private const float AltitudeAboveShinGlow = 0.01f;

        private static readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        private static readonly Dictionary<ThingDef, List<Material>> frameMaterialsByDef = new Dictionary<ThingDef, List<Material>>();

        private List<Material> FrameMaterials
        {
            get
            {
                if (frameMaterialsByDef.TryGetValue(def, out List<Material> frameMaterials)) return frameMaterials;

                frameMaterials = new List<Material>();
                List<string> framePaths = def.GetModExtension<ModExtension_FlameFrames>()?.framePaths;
                if (framePaths != null)
                {
                    foreach (string framePath in framePaths)
                        frameMaterials.Add(MaterialPool.MatFrom(framePath, ShaderDatabase.Mote));
                }
                if (frameMaterials.Count == 0) frameMaterials.Add(Graphic.MatSingle);

                frameMaterialsByDef[def] = frameMaterials;
                return frameMaterials;
            }
        }

        protected override void DrawAt(Vector3 drawLocation, bool flip = false)
        {
            float alpha = Alpha;
            if (alpha <= 0f) return;

            List<Material> frameMaterials = FrameMaterials;
            int ticksPerFrame = Mathf.Max(1, def.GetModExtension<ModExtension_FlameFrames>()?.ticksPerFrame ?? 6);
            int frameIndex = (Find.TickManager.TicksGame / ticksPerFrame + thingIDNumber) % frameMaterials.Count;

            Color flameColor = instanceColor;
            flameColor.a *= alpha;
            propertyBlock.SetColor("_Color", flameColor);

            drawLocation.y = AltitudeLayer.MoteLow.AltitudeFor() + AltitudeAboveShinGlow;

            Vector2 drawSize = def.graphicData.drawSize * linearScale.x;
            Matrix4x4 matrix = Matrix4x4.TRS(drawLocation, Quaternion.identity, new Vector3(drawSize.x, 1f, drawSize.y));
            Graphics.DrawMesh(MeshPool.plane10, matrix, frameMaterials[frameIndex], 0, null, 0, propertyBlock);
        }
    }
}
