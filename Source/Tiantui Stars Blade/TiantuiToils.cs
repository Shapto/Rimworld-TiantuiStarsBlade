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
    public static class TiantuiToils
    {
        public static Toil WithSoundsSpreadOver(this Toil toil, List<SoundDef> sounds, int durationTicks)
        {
            if (sounds.NullOrEmpty()) return toil;

            int ticksElapsed = 0;
            int soundsPlayed = 0;
            toil.AddPreTickAction(() =>
            {
                Pawn actor = toil.actor;
                int lastSoundIndex = sounds.Count - 1;
                while (soundsPlayed < sounds.Count)
                {
                    int playAtTick = lastSoundIndex == 0 ? 0 : soundsPlayed * (durationTicks - 1) / lastSoundIndex;
                    if (ticksElapsed < playAtTick) break;
                    if (actor?.Map != null) sounds[soundsPlayed]?.PlayOneShot(new TargetInfo(actor.Position, actor.Map));
                    soundsPlayed++;
                }
                ticksElapsed++;
            });
            return toil;
        }
    }
}
