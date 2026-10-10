using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiantui_Stars_Blade
{
    [DefOf]
    public static class TiantuiDefOf
    {
        public static StatDef Tiantui_HeatBuildup;

        static TiantuiDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(TiantuiDefOf));
        }
    }
}
