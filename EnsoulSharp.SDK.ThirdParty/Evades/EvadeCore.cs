using EnsoulSharp.SDK.MenuUI;
using SharpDX;
using System.Collections.Generic;
using System.Linq;

namespace EnsoulSharp.SDK.ThirdParty.Evades
{
    public static class EvadeCore
    {
        public static List<string> EvadeNames = new List<string> { "EzEvade" };

        public static Dictionary<int, ISkillshot> AllSpells = new Dictionary<int, ISkillshot>();

        public static void AddEvade(string menuName)
        {
            if (!EvadeNames.Contains(menuName))
            {
                EvadeNames.Add(menuName);
            }
        }

        public static bool AlreadyLoad()
        {
            return MenuManager.Instance.Menus.Any(x => x.Root && EvadeNames.Contains(x.Name));
        }

        // judge current pos already on skillshot range
        // extraRadius better set unit.BoundingRadius + any value
        public static bool IsDanger(Vector2 position, float extraRadius, bool checkSpellCollision = true)
        {
            foreach (var spell in AllSpells)
            {
                if (spell.Value.IsInSide(position, extraRadius, checkSpellCollision))
                {
                    return true;
                }
            }

            return false;
        }

        // judge position will hit skillshot
        // example ezreal e evade =>{ if (EvadeCore.WillHit(player.Pos, E.Delay) E.Cast(Game.Cursor) )
        public static bool WillHit(Vector2 position, float delay)
        {
            foreach (var spell in AllSpells)
            {
                if (spell.Value.WillHit(position, delay))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
