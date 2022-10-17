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

namespace ImpulseAIO.Champion.Blitzcrank
{
    internal class Blitzcrank : Base
    {
        private Spell Q, W, E, R;
        private static Menu AntiGapcloserMenu;
        private  HitChance QhitChance => ChampionMenu["HitChance"]["Q"].GetValue<MenuHitChance>().HitChanceIndex;
        private  bool ComboUseQ => ChampionMenu["Combo"]["CQ"].GetValue<MenuBool>().Enabled;
        private bool AutoCQDash => ChampionMenu["Combo"]["CQDash"].GetValue<MenuBool>().Enabled;
        private  bool ComboUseW => ChampionMenu["Combo"]["CW"].GetValue<MenuBool>().Enabled;
        private  bool ComboUseE => ChampionMenu["Combo"]["CE"].GetValue<MenuBool>().Enabled;
        private  bool HarassUseQ => ChampionMenu["Harass"]["HQ"].GetValue<MenuBool>().Enabled;
        private  int HarassMana => ChampionMenu["Harass"]["HMana"].GetValue<MenuSlider>().Value;
        private  int RMode => ChampionMenu["Rset"]["RMode"].GetValue<MenuList>().Index;
        private  bool UseRKill => ChampionMenu["Rset"]["UseRKill"].GetValue<MenuBool>().Enabled;
        private  MenuSliderButton UseRHealth => ChampionMenu["Rset"]["UseRHealth"].GetValue<MenuSliderButton>();
        private  bool UseRSheild => ChampionMenu["Rset"]["UseRSheild"].GetValue<MenuBool>().Enabled;
        private int UseRSheildCount => ChampionMenu["Rset"]["UseRSheildCount"].GetValue<MenuSlider>().Value;
        private int UseRSheildHeath => ChampionMenu["Rset"]["UseRSheildHeath"].GetValue<MenuSlider>().Value;
        private bool UseRInterrupt => ChampionMenu["Rset"]["UseRInterrupt"].GetValue<MenuBool>().Enabled;
        private bool DrawQ => ChampionMenu["Draw"]["DrawQ"].GetValue<MenuBool>().Enabled;
        private bool DrawR => ChampionMenu["Draw"]["DrawR"].GetValue<MenuBool>().Enabled;
        private bool AntiGap => AntiGapcloserMenu["AntiQGap"].GetValue<MenuBool>().Enabled;
        public Blitzcrank()
        {
            Q = new Spell(SpellSlot.Q, 1050f);
            Q.SetSkillshot(0.25f, 70f, 1800f, true, SpellType.Line);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 600f);
            Q.DamageType = E.DamageType = R.DamageType = DamageType.Magical;
            OnMenuLoad();
            Game.OnUpdate += GameOnUpdate;
            AntiGapcloser.OnGapcloser += AntiGapCloser;
            Orbwalker.OnBeforeAttack += (s, g) => {
                var HeroClient = g.Target as AIHeroClient;
                if (HeroClient.IsValidTarget())
                {
                    if(ComboUseE && E.IsReady())
                    {
                        E.Cast();
                    }
                }
            };
            Interrupter.OnInterrupterSpell += (s, g) => {
                if(s.IsValidTarget(R.Range) && UseRInterrupt && R.IsReady())
                {
                    if(g.DangerLevel == Interrupter.DangerLevel.High && IsInterruptUnit(s))
                    {
                        R.Cast();
                    }
                }
            };
            Render.OnEndScene += (s) => {
                if(DrawQ && Q.IsReady())
                {
                    PlusRender.DrawCircle(Player.Position, Q.Range, Color.Red);
                }
                if (DrawR && R.IsReady())
                {
                    PlusRender.DrawCircle(Player.Position, Q.Range, Color.Orange);
                }
            };
        }
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Blitzcrank));
            var HitChance = ChampionMenu.Add(new Menu("HitChance", "HitChance"));
            {
                HitChance.Add(new MenuHitChance("Q", "Q HitChance"));
            }
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo"));
            {
                Combo.Add(new MenuBool("CQ", "Use Q"));
                Combo.Add(new MenuBool("CQDash", "Auto Use Q if Target In Dash"));
                Combo.Add(new MenuBool("CW", "Use W"));
                Combo.Add(new MenuBool("CE", "Use E"));
            }
            var Harass = ChampionMenu.Add(new Menu("Harass", "Harass"));
            {
                Harass.Add(new MenuBool("HQ", "Use Q"));
                Harass.Add(new MenuSlider("HMana", Program.Chinese ? "当蓝量 <= X%时不骚扰" : "Don't Harass if Mana <= X%"));
            }
            var Rset = ChampionMenu.Add(new Menu("Rset", Program.Chinese ? "静电力场" : "R set"));
            {
                Rset.Add(new MenuList("RMode", Program.Chinese ? "使用 R 模式" : "Use R Mode",new string[] { "Only Combo","Always","Disable"},1));
                Rset.Add(new MenuBool("UseRKill", Program.Chinese ? "使用 R 抢头" : "Use R Killable",false));
                Rset.Add(new MenuSliderButton("UseRHealth", Program.Chinese ? "仅当目标血量 <= X%时才使用" : "Only When Target HP% <= X%", 20,0,100));
                Rset.Add(new MenuBool("UseRSheild", Program.Chinese ? "使用 R 摧毁护盾" : "Use R Fuck :) Sheild"));
                Rset.Add(new MenuSlider("UseRSheildCount", Program.Chinese ? "->当周围拥有护盾敌人数 >= X时" : "->When Sheild enemy Count >= X",2,1,5));
                Rset.Add(new MenuSlider("UseRSheildHeath", Program.Chinese ? "->当护盾生命值 >= X时" : "Sheild HP >= X",100,100,400));
                Rset.Add(new MenuBool("UseRInterrupt", Program.Chinese ? "使用 R 终止高危施法" : "Use R Interrupt"));
                var InterruptList = Rset.Add(new Menu("InterruptList", Program.Chinese ? "中断英雄列表" : "Interrupt List"));
                {
                    foreach (var obj in GameObjects.EnemyHeroes)
                    {
                        InterruptList.Add(new MenuBool("rupt." + obj.CharacterName, obj.CharacterName));
                    }
                }
            }
            var Draw = ChampionMenu.Add(new Menu("Draw", "Draw"));
            {
                Draw.Add(new MenuBool("DrawQ", "Draw Q"));
                Draw.Add(new MenuBool("DrawR", "Draw R"));
            }
            AntiGapcloserMenu = AntiGapcloser.Attach(ChampionMenu);
            {
                AntiGapcloserMenu.Add(new MenuBool("AntiQGap", "Use Q AntiGapCloser"));
            }

        }
        private void AntiGapCloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs e)
        {
            if (AntiGap && Q.IsReady())
            {
                if(sender.IsValid && sender.IsEnemy)
                {
                    if (sender.IsDashing())
                    {
                        var pred = Q.GetPrediction(sender, false, -1, new CollisionObjects[] { CollisionObjects.Minions, CollisionObjects.Heroes, CollisionObjects.YasuoWall });
                        if(pred.Hitchance >= HitChance.Dash)
                        {
                            Q.Cast(pred.CastPosition);
                        }
                    }
                }
            }
        }
        private void GameOnUpdate(EventArgs args)
        {
            if (AutoCQDash)
            {
                if (Q.IsReady())
                {
                    foreach(var obj in Cache.EnemyHeroes.Where(x => x.IsValidTarget(Q.Range)))
                    {
                        Q.CastIfHitchanceEquals(obj, HitChance.Dash);
                    }
                }
            }
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
                case OrbwalkerMode.LaneClear:
                    break;
                case OrbwalkerMode.LastHit:
                    break;
                case OrbwalkerMode.Flee:
                    break;
            }
            RLogic();
        }
        private void RLogic()
        {
            if (!R.IsReady() || RMode == 2)
                return;
            if((RMode == 0 && Orbwalker.ActiveMode == OrbwalkerMode.Combo) || RMode == 1)
            {
                if (UseRKill)
                {
                    var KillEnemy = Cache.EnemyHeroes.Where(x => x.IsValidTarget(R.Range) && x.GetRealHeath(DamageType.Magical) < R.GetDamage(x));
                    if (KillEnemy.Any())
                    {
                        R.Cast();
                    }
                }
                if (UseRHealth.Enabled)
                {
                    var HealthLow = Cache.EnemyHeroes.Where(x => x.IsValidTarget(R.Range) && x.HealthPercent <= UseRHealth.Value);
                    if (HealthLow.Any())
                    {
                        R.Cast();
                    }
                }
                if (UseRSheild)
                {
                    var SheildEnemy = Cache.EnemyHeroes.Count(x => x.IsValidTarget(R.Range) && (x.AllShield + x.PhysicalShield + x.MagicalShield) >= UseRSheildHeath);
                    if(SheildEnemy >= UseRSheildCount)
                    {
                        R.Cast();
                    }
                }
            }
        }
        private void Combo()
        {
            var Ret = IMPGetTarGet(Q, false, QhitChance);
            if (Ret.SuccessFlag && Ret.Obj.IsValid)
            {
                if (ComboUseW && W.IsReady() && Player.Mana > W.Instance.ManaCost + Q.Instance.ManaCost)
                {
                    if (Ret.Obj.DistanceToPlayer() >= Player.GetRealAutoAttackRange(Ret.Obj) + 100f)
                    {
                        if (Game.CursorPos.Distance(Ret.Obj) < Ret.Obj.DistanceToPlayer())
                        {
                            W.Cast();
                        }
                    }
                }
                if (ComboUseQ && Q.IsReady())
                {
                    if (Ret.Obj.IsValidTarget())
                    {
                        if (Player.HasBuff("PowerFist") && Orbwalker.GetTarget() != null)
                        {
                            return;
                        }
                        Q.Cast(Ret.CastPosition);
                    }
                }
            }
        }
        private void Harass()
        {
            if (Player.ManaPercent <= HarassMana)
            {
                return;
            }
            if (HarassUseQ)
            {
                var Ret = IMPGetTarGet(Q, false, QhitChance);
                if(Ret.SuccessFlag && Ret.Obj.IsValid)
                {
                    Q.Cast(Ret.CastPosition);
                }
            }
        }
        private bool IsInterruptUnit(AIHeroClient Unit)
        {
            return ChampionMenu["Rset"]["InterruptList"]["rupt." + Unit.CharacterName].GetValue<MenuBool>().Enabled;
        }
    }
}
