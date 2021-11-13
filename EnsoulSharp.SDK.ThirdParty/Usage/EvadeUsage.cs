using EnsoulSharp.SDK.Evade;
using SharpDX;
using System;
using System.Linq;

namespace EvadeUsage
{
    public static class EvadeUsage
    {
        // Add your own evade into SDK, and then easy to make other script use it.
        public static void AddEvadeIntoSDK()
        {
            EvadeLibrary.AddEvade("EzEvade");
        }

        // check evade is already add into SDK or not.
        public static void CheckEvadeIsExist()
        {
            if (EvadeLibrary.IsExist("EzEvade"))
            {
                Console.WriteLine("EzEvade load.");
            }
            else
            {
                Console.WriteLine("EzEvade not load.");
            }
        }

        // judge current pos already on skillshot range
        // extraRadius better set unit.BoundingRadius + any value
        public static bool IsDanger(Vector2 position, float extraRadius, bool checkSpellCollision = true)
        {
            foreach (ISkillshot spell in EvadeLibrary.AllSpells["EzEvade"].Values.ToList())
            {
                if (spell.IsInSide(position, extraRadius, checkSpellCollision))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
