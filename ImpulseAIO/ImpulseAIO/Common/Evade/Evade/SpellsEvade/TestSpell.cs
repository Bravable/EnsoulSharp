using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using SharpDX;
namespace QSharp.Common.Evade.SpellsEvade
{
    public class TestSpell
    {
        public static void CreateSpell(Vector3 hitTo, AIHeroClient start, string Name = "UFSlash")
        {
            var spellData = SpellDatabase.GetByName(Name);
            SkillshotDetector.TriggerOnDetectSkillshot(
                        DetectionType.ProcessSpell,
                        spellData,
                        Variables.TickCount - Game.Ping / 2,
                        start.Position.ToVector2(),
                        hitTo.ToVector2(),
                        start);
        }
    }
}
