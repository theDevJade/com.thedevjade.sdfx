using System;
using System.Collections.Generic;
using System.Linq;

namespace SDFX.VectorTextureCompiler.Core.Modules
{
    internal static class ModuleConflicts
    {
        public static readonly string[] DiffuseLighting =
        {
            "shading", "toon", "cel", "flat", "lightmodes", "shadow", "pbr"
        };

        public static IReadOnlyList<string> DiffuseLightingExcept(string selfId)
        {
            return DiffuseLighting
                .Where(id => !string.Equals(id, selfId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }
}
