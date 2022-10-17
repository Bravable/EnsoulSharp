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

namespace ImpulseAIO.Champion.Kindred
{
    internal class Kindred : Base
    {
        private static Spell Q, W, E, R;
        private static Dash dash;
        private static Menu AntiGapcloserMenu;
        private static bool ComboUseQ => ChampionMenu["Combo"]["CQ"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseW => ChampionMenu["Combo"]["CW"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseE => ChampionMenu["Combo"]["CE"].GetValue<MenuBool>().Enabled;
        private static bool comboAdvancedE => ChampionMenu["Combo"]["comboAdvancedE"].GetValue<MenuBool>().Enabled;
        private static int comboDistanceW => ChampionMenu["Combo"]["comboDistanceW"].GetValue<MenuSlider>().Value;

        private static bool LaneClearUseQ => ChampionMenu["LaneClear"]["LQ"].GetValue<MenuBool>().Enabled;
        private static int LaneClearUseQCount => ChampionMenu["LaneClear"]["LQC"].GetValue<MenuSlider>().Value;
        private static bool LaneClearUseW => ChampionMenu["LaneClear"]["LW"].GetValue<MenuBool>().Enabled;
        private static int LaneClearMana => ChampionMenu["LaneClear"]["Lmana"].GetValue<MenuSlider>().Value;

        private static bool JungleUseQ => ChampionMenu["JungleClear"]["JQ"].GetValue<MenuBool>().Enabled;
        private static bool JungleUseW => ChampionMenu["JungleClear"]["JW"].GetValue<MenuBool>().Enabled;
        private static bool JungleUseE => ChampionMenu["JungleClear"]["JE"].GetValue<MenuBool>().Enabled;
        private static int JungleMana => ChampionMenu["JungleClear"]["Jmana"].GetValue<MenuSlider>().Value;

        private static bool DrawQ => ChampionMenu["Draw"]["DQ"].GetValue<MenuBool>().Enabled;
        private static bool DrawW => ChampionMenu["Draw"]["DW"].GetValue<MenuBool>().Enabled;
        private static bool DrawE => ChampionMenu["Draw"]["DE"].GetValue<MenuBool>().Enabled;
        private static bool DrawR => ChampionMenu["Draw"]["DR"].GetValue<MenuBool>().Enabled;

        private static bool autoR => ChampionMenu["autoR"].GetValue<MenuBool>().Enabled;
        private static bool AntiGapE => AntiGapcloserMenu["AntiGapE"].GetValue<MenuBool>().Enabled;
        private static bool AntiGapQ => AntiGapcloserMenu["AntiGapQ"].GetValue<MenuBool>().Enabled;
        private static bool AttackE => ChampionMenu["AttackE"].GetValue<MenuBool>().Enabled;
        private static bool FastE => ChampionMenu["FastE"].GetValue<MenuKeyBind>().Active;
        public Kindred()
        {
            Q = new Spell(SpellSlot.Q, 340);
            Q.SetSkillshot(0.25f, 30f, 1400f, false, SpellType.Line);
            W = new Spell(SpellSlot.W, 900);
            E = new Spell(SpellSlot.E, 575f);
            E.SetTargetted(0.1f, float.MaxValue);
            R = new Spell(SpellSlot.R, 500);
            OnMenuLoad();
            dash = new Dash(Q);
            Game.OnUpdate += GameOnUpdate;
            AIBaseClient.OnDoCast += OnProcessSpellCast;
            AntiGapcloser.OnGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Render.OnDraw += Drawing_OnDraw;
        }
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Kindred));
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo"));
            {
                Combo.Add(new MenuBool("CQ", "Use Q"));
                Combo.Add(new MenuBool("CW", "Use W"));
                Combo.Add(new MenuBool("CE", "Use E"));
                var Eset = Combo.Add(new Menu("Eset", "E BlackList"));
                {
                    foreach (var h in GameObjects.EnemyHeroes)
                    {
                        Eset.Add(new MenuBool("notcast." + h.CharacterName, h.CharacterName, false));
                    }
                }
                Combo.Add(new MenuBool("comboAdvancedE", Program.Chinese ? "如果目标能被3次普攻杀死就不E" : "Don't E If Target Health <= 3x Attack Damage"));
                Combo.Add(new MenuSlider("comboDistanceW", Program.Chinese ? "使用W离敌人最小距离" : "Use W Min Distance", (int)W.Range / 2,
                    (int)W.Range / 5,
                    (int)W.Range));
            }
            var LaneClear = ChampionMenu.Add(new Menu("LaneClear", "LaneClear"));
            {
                LaneClear.Add(new MenuBool("LQ", "Use Q"));
                LaneClear.Add(new MenuSlider("LQC", Program.Chinese ? "Use Q 最低小兵数" : "使用 Q 最低小兵数", 3, 1, 3));
                LaneClear.Add(new MenuBool("LW", "Use W", false));
                LaneClear.Add(new MenuSlider("Lmana", Program.Chinese ? "当蓝量 >= x%时才清线" : "Don't LaneClear if Mana <= X%", 40));
            }
            var JungleClear = ChampionMenu.Add(new Menu("JungleClear", "JungleClear"));
            {
                JungleClear.Add(new MenuBool("JQ", "Use Q"));
                JungleClear.Add(new MenuBool("JW", "Use W"));
                JungleClear.Add(new MenuBool("JE", "Use E"));
                JungleClear.Add(new MenuSlider("Jmana", Program.Chinese ? "当蓝量 >= x%时才清野" : "Don't JungleClear if Mana <= X%", 40));
            }
            var Draw = ChampionMenu.Add(new Menu("Draw", "Draw"));
            {
                Draw.Add(new MenuBool("DQ", "Draw Q"));
                Draw.Add(new MenuBool("DW", "Draw W"));
                Draw.Add(new MenuBool("DE", "Draw E"));
                Draw.Add(new MenuBool("DR", "Draw R"));
            }
            AntiGapcloserMenu = AntiGapcloser.Attach(ChampionMenu);
            {
                AntiGapcloserMenu.Add(new MenuBool("AntiGapQ", "AntiGap Q"));
                AntiGapcloserMenu.Add(new MenuBool("AntiGapE", "AntiGap E"));
            }
            ChampionMenu.Add(new MenuBool("autoR", "Auto R"));
            ChampionMenu.Add(new MenuBool("AttackE", Program.Chinese ? "AA聚集三环目标" : "Force Attack E Target"));
            ChampionMenu.Add(new MenuKeyBind("FastE", Program.Chinese ? "快速对黑名单用户使用E" : "Fast E To E Black Target", Keys.E, KeyBindType.Press)).AddPermashow();
        }
        private void GameOnUpdate(EventArgs args)
        {
            if (FastE)
            {
                FastBlackE();
            }
            if (AttackE)
            {
                var ALLHERO = Cache.EnemyHeroes.Where(x => x.IsValidTarget(Player.GetRealAutoAttackRange())).ToList();
                var ATTACKE = ALLHERO.FirstOrDefault(x => x.HasBuff("kindredecharge"));
                //其他玩家可以被2下普攻杀死 但是聚集目标无法被3下普攻打死
                var OTHER = ALLHERO.Where(y => y.NetworkId != ATTACKE.NetworkId &&
                        y.GetRealHeath(DamageType.Physical) <= Player.GetAutoAttackDamage(y) * 2 && ATTACKE.GetRealHeath(DamageType.Physical) > Player.GetAutoAttackDamage(ATTACKE) * 3).FirstOrDefault();
                Orbwalker.ForceTarget = OTHER ?? ATTACKE;
            }
            AutoR();
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    break;
                case OrbwalkerMode.LaneClear:
                    LaneClear();
                    break;
                case OrbwalkerMode.LastHit:
                    break;
                case OrbwalkerMode.Flee:
                    break;
            }
        }
        private static void ClassicUltimate()
        {
            foreach (var ally in Cache.AlliesHeroes.Where(o => o.IsValidTarget(R.Range,false) && !o.IsRecalling() && !o.IsZombie() && o.DistanceToPlayer() < R.Range && !o.InFountain()))
            {
                if (ally.HealthPercent < 10 && Player.ServerPosition.CountEnemyHerosInRangeFix(R.Range + 400) >= 1 && ally.ServerPosition.CountEnemyHerosInRangeFix(675) >= 1)
                {
                    R.Cast();
                }
                if(HealthPrediction.GetPrediction(ally,300) <= 100)
                {
                    R.Cast();
                }
            }
        }
        private void Combo()
        {
            var target = GetTarget(Q.Range + 500f);
            if (ComboUseE && E.IsReady())
            {
                foreach (var enemy in Cache.EnemyHeroes.Where(o => o.IsValidTarget(E.Range)))
                {
                    if (!ChampionMenu["Combo"]["Eset"]["notcast." + enemy.CharacterName].GetValue<MenuBool>().Enabled)
                    {
                        if (comboAdvancedE)
                        {
                            if (enemy.GetRealHeath(DamageType.Physical) <= Player.GetAutoAttackDamage(enemy) * 2)
                            {
                                return;
                            }
                        }
                        E.Cast(enemy);
                    }
                }
            }
            if (ComboUseQ && Q.IsReady() && !Player.Spellbook.IsWindingUp)
            {
                if (Player.InAutoAttackRange(target))
                {
                    var Pos = dash.CastDash();
                    if (Pos.IsValid())
                    {
                        Q.Cast(Pos);
                    }
                }
            }
            if (ComboUseW && W.IsReady())
            {
                foreach (var enemy in Cache.EnemyHeroes.Where(o => o.IsValidTarget(W.Range)))
                {
                    if (enemy.DistanceToPlayer() <= comboDistanceW)
                    {
                        W.Cast(enemy.ServerPosition);
                    }
                }
            }
        }
        private void LaneClear()
        {
            if (!Enable_laneclear)
                return;

            if (Player.ManaPercent >= LaneClearMana)
            {
                var xMinion = Cache.GetMinions(Player.ServerPosition, Player.GetCurrentAutoAttackRange());
                if (xMinion.Count >= LaneClearUseQCount)
                {
                    if (LaneClearUseQ && Q.IsReady())
                    {
                        Q.Cast(Game.CursorPos);
                    }
                    if (LaneClearUseW && W.IsReady())
                    {
                        W.Cast();
                    }
                }
            }
            if (Player.ManaPercent >= JungleMana)
            {
                var mob = Cache.GetJungles(Player.ServerPosition, Player.GetRealAutoAttackRange());
                if (mob == null || mob.Count == 0)
                {
                    return;
                }
                var hasBigMob = mob.Where(x => x.InRange(W.Range) && x.GetJungleType() >= JungleType.Large).Any();
                if (JungleUseE && E.IsReady())
                {
                    var bestEt = mob.Where(x => x.IsValidTarget(E.Range) && x.GetJungleType() >= JungleType.Large && x.Health > Player.GetAutoAttackDamage(x) * 2).FirstOrDefault();
                    if (bestEt != null)
                    {
                        E.CastOnUnit(bestEt);
                    }
                }
                if (JungleUseQ && Q.IsReady())
                {
                    Q.Cast(Game.CursorPos);
                }
                if (JungleUseW && W.IsReady() && (hasBigMob || mob.Count >= 3))
                {
                    W.Cast();
                }
            }
        }
        private void Drawing_OnDraw(EventArgs args)
        {

            if (DrawQ && Q.IsReady())
            {
                
                PlusRender.DrawCircle(Player.Position,
                    Q.Range, Color.White);
            }
            if (DrawW && W.IsReady())
            {
                PlusRender.DrawCircle(Player.Position,
                    comboDistanceW, Color.Gold);
            }
            if (DrawE && E.IsReady())
            {
                PlusRender.DrawCircle(Player.Position,
                    E.Range, Color.DodgerBlue);
            }
            if (DrawR && R.IsReady())
            {
                PlusRender.DrawCircle(Player.Position,
                    R.Range, Color.GreenYellow);
            }
        }
        private void AntiGapcloser_OnEnemyGapcloser(AIBaseClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (AntiGapE && E.IsReady() && sender.IsValidTarget(E.Range) &&
                args.StartPosition.DistanceToPlayer() > args.EndPosition.DistanceToPlayer())
            {
                E.CastOnUnit(sender);
            }
            if (AntiGapQ && Q.IsReady() && sender.IsValidTarget(400f) &&
                args.StartPosition.DistanceToPlayer() > args.EndPosition.DistanceToPlayer())
            {
                var fastDash = dash.CastDash(true);
                if (fastDash.IsValid())
                {
                    Q.Cast(fastDash);
                }
            }
        }
        private void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs spell)
        {
            if (sender == null || !R.IsReady() || Player.IsDead || Player.IsZombie() || sender.IsAlly || sender.IsMe)
            {
                return;
            }

            if (autoR)
            {
                if (R.IsReady() && sender.IsEnemy)
                {
                    #region AA情况
                    if (Orbwalker.IsAutoAttack(spell.SData.Name)) //如果是普攻
                    {
                        var attackmub = spell.Target as AIHeroClient;
                        if (attackmub.IsValidTarget(R.Range,false) && attackmub.IsAlly)
                        {
                            if (attackmub.IsMe)
                            {
                                if (sender.GetAutoAttackDamage(Player, true) * 1.2 > Player.Health)
                                {
                                    R.Cast();
                                }
                            }
                            else if (attackmub.IsAlly && attackmub.DistanceToPlayer() <= R.Range)
                            {
                                if (sender.GetAutoAttackDamage(attackmub, true) * 1.2 > attackmub.Health)
                                {
                                    R.Cast();
                                }
                            }
                        }
                    }
                    #endregion
                }
            }
        }
        private static void AutoR()
        {
            if (autoR)
            {
                ClassicUltimate();
            }
        }
        private void FastBlackE()
        {
            var emwg = TargetSelector.GetTargets(E.Range, DamageType.Physical).Where(x => ChampionMenu["Combo"]["Eset"]["notcast." + x.CharacterName].GetValue<MenuBool>().Enabled);
            if (emwg.Any())
            {
                var TempHero = emwg.MinOrDefault(y => y.GetRealHeath(DamageType.Physical) - Player.GetAutoAttackDamage(y) * 3);
                if (TempHero.IsValidTarget(E.Range))
                {
                    E.CastOnUnit(TempHero);
                }
            }
        }
        private AIHeroClient GetTarget(float range)
        {
            var validEnemies = Cache.EnemyHeroes.Where(o => o.IsValidTarget(range + Player.BoundingRadius + o.BoundingRadius)).ToArray();
            var preferredTarget = validEnemies.FirstOrDefault(o => o.HasBuff("KindredHitTracker") || o.HasBuff("kindredecharge"));

            // Return preferred target with either the passive debuff or the E debuff
            return preferredTarget ?? TargetSelector.GetTarget(validEnemies, DamageType.Physical);
        }
    }
}
