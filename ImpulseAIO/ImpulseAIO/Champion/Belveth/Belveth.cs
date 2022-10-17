using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;

using SharpDX;
using ImpulseAIO.Common;

namespace ImpulseAIO.Champion.Belveth
{
    internal class Belveth : Base 
    {
        private Spell Q, W, E, R;
        public Belveth()
        {
            //Q = new Spell(SpellSlot.Q, 1050f);
            //Q.SetSkillshot(0.25f, 70f, 1800f, true, SpellType.Line);
            //W = new Spell(SpellSlot.W);
            //E = new Spell(SpellSlot.E);
            //R = new Spell(SpellSlot.R, 600f);
            //Q.DamageType = E.DamageType = R.DamageType = DamageType.Magical;
            //OnMenuLoad();
            //Game.OnUpdate += GameOnUpdate;
            //AntiGapcloser.OnGapcloser += AntiGapCloser;
            //Orbwalker.OnBeforeAttack += (s, g) => {
            //    var HeroClient = g.Target as AIHeroClient;
            //    if (HeroClient.IsValidTarget())
            //    {
            //        if (ComboUseE && E.IsReady())
            //        {
            //            E.Cast();
            //        }
            //    }
            //};
            //Interrupter.OnInterrupterSpell += (s, g) => {
            //    if (s.IsValidTarget(R.Range) && UseRInterrupt && R.IsReady())
            //    {
            //        if (g.DangerLevel == Interrupter.DangerLevel.High && IsInterruptUnit(s))
            //        {
            //            R.Cast();
            //        }
            //    }
            //};
            //Drawing.OnEndScene += (s) => {
            //    if (DrawQ && Q.IsReady())
            //    {
            //        PlusRender.DrawCircle(Player.Position, Q.Range, Color.Red);
            //    }
            //    if (DrawR && R.IsReady())
            //    {
            //        PlusRender.DrawCircle(Player.Position, Q.Range, Color.Orange);
            //    }
            //};
        }
    }
}
