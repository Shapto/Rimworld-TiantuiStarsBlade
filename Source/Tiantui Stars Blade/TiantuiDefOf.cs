using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Tiantui_Stars_Blade
{
    [DefOf]
    public static class TiantuiDefOf
    {
        public static StatDef Tiantui_HeatBuildup;
        public static JobDef Tiantui_ReloadBlade;
        static TiantuiDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(TiantuiDefOf));
        }
    }
}
