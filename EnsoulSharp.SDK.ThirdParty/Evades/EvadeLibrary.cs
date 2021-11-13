using System.Collections.Generic;

namespace EnsoulSharp.SDK.ThirdParty.Evades
{
    /// <summary>
    ///    The Evade Library Class.
    /// </summary>
    public static class EvadeLibrary
    {
        private static List<string> EvadeNames = new List<string>();

        /// <summary>
        ///    Gets or sets all skillshots.
        /// </summary>
        public static Dictionary<string, Dictionary<int, ISkillshot>> AllSpells = new Dictionary<string, Dictionary<int, ISkillshot>>();

        /// <summary>
        ///     Add Evade into Library.
        /// </summary>
        /// <param name="evadeName">The Evade Name.</param>
        public static void AddEvade(string evadeName)
        {
            if (!EvadeNames.Contains(evadeName))
            {
                EvadeNames.Add(evadeName);
            }
        }

        /// <summary>
        ///     Judge evade is init.
        /// </summary>
        /// <returns></returns>
        public static bool IsExist(string evadeName)
        {
            return EvadeNames.Contains(evadeName);
        }
    }
}
