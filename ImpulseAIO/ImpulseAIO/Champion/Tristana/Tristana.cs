using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;

using SharpDX;
using SharpDX.Direct3D9;
using ImpulseAIO.Common;
using ImpulseAIO.Common.Evade;

namespace ImpulseAIO.Champion.Tristana
{
    internal class Tristana : Base 
    {
        private static Spell Q, W, E, R;
        private static Menu AntiGapcloserMenu;
        private static bool ComboUseQ => ChampionMenu["Combo"]["CQ"].GetValue<MenuBool>().Enabled;
        private static int ComboUseQDistance => ChampionMenu["Combo"]["CQDIS"].GetValue<MenuSlider>().Value;
        private static bool ComboUseW => ChampionMenu["Combo"]["CW"].GetValue<MenuBool>().Enabled;
        private static int ComboUseWDistance => ChampionMenu["Combo"]["CWDIS"].GetValue<MenuSlider>().Value;
        private static bool ComboUseWC => ChampionMenu["Combo"]["CWCombo"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseE => ChampionMenu["Combo"]["CE"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseEBlock => ChampionMenu["Combo"]["CEB"].GetValue<MenuKeyBind>().Active;

        private static bool ComboUseR => ChampionMenu["Combo"]["CR"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseRE => ChampionMenu["Combo"]["CRE"].GetValue<MenuBool>().Enabled;

        private static bool LaneClearUseQ => ChampionMenu["LaneClear"]["FQ"].GetValue<MenuBool>().Enabled;
        private static int LaneClearMana => ChampionMenu["LaneClear"]["FMana"].GetValue<MenuSlider>().Value;

        private static bool JungleClearUseQ => ChampionMenu["JungleClear"]["FQ"].GetValue<MenuBool>().Enabled;
        private static bool JungleClearUseE => ChampionMenu["JungleClear"]["FE"].GetValue<MenuBool>().Enabled;

        private static bool DQ => ChampionMenu["Draw"]["DQ"].GetValue<MenuBool>().Enabled;
        private static bool DW => ChampionMenu["Draw"]["DW"].GetValue<MenuBool>().Enabled;
        private static bool DE => ChampionMenu["Draw"]["DE"].GetValue<MenuBool>().Enabled;
        private static bool Damage => ChampionMenu["Draw"]["Damage"].GetValue<MenuBool>().Enabled;

        private static bool AntiGapUseR => AntiGapcloserMenu["RAntiGap"].GetValue<MenuBool>().Enabled;
        private static bool RInterrupter => ChampionMenu["Misc"]["RInterrupter"].GetValue<MenuBool>().Enabled;
        private static bool AntiRengar => ChampionMenu["Misc"]["AntiRengar"].GetValue<MenuBool>().Enabled;
        private static bool AttackTurrent => ChampionMenu["FET"].GetValue<MenuBool>().Enabled;
        public Tristana()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 900f);
            E = new Spell(SpellSlot.E, 525f);
            E.SetTargetted(0.1f, 2400f);
            R = new Spell(SpellSlot.R, 525f);
            R.SetTargetted(0.25f, 2000f);
            OnMenuLoad();
            AIBaseClient.OnPlayAnimation += (sender, args) =>
            {
                if (AntiRengar && sender.CharacterName == "Rengar" && args.Animation == "Spell5")
                {
                    if (sender.IsValidTarget(R.Range))
                        R.CastOnUnit(sender);
                }
            };
            Interrupter.OnInterrupterSpell += (s, g) =>
            {
                if (s.IsEnemy && RInterrupter)
                {
                    if (g.DangerLevel == Interrupter.DangerLevel.High)
                    {
                        if (s.IsValidTarget(R.Range))
                            R.Cast(s.Position);
                    }
                }
            };
            AntiGapcloser.OnGapcloser += (sender, args) => {
                if (args.StartPosition.DistanceToPlayer() > args.EndPosition.DistanceToPlayer() && args.EndPosition.DistanceToPlayer() <= 400)
                {
                    if (AntiGapUseR && R.IsReady())
                    {
                        if (sender.IsValidTarget(R.Range))
                            R.CastOnUnit(sender);
                    }
                }
            };
            Orbwalker.OnBeforeAttack += (sender, args) =>
            {
                if (!E.IsReady()) 
                    return;

                var target = args.Target;
                if(Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                {
                    var hero = target as AIHeroClient;
                    if(hero != null && ComboUseE)
                    {
                        var BestE = TargetSelector.GetTargets(E.Range, DamageType.Physical).Where(x => !IsEBlock(x)).MinOrDefault(x => AaIndicator(x));
                        if (BestE != null)
                        {
                            E.CastOnUnit(BestE);
                            args.Target = BestE;
                        }
                    }
                }
                if(Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                {
                    var Turrent = target as AITurretClient;
                    if (Turrent != null)
                    {
                        if (AttackTurrent && Enable_laneclear && Player.Mana > R.Instance.ManaCost + E.Instance.ManaCost + W.Instance.ManaCost + R.Instance.ManaCost)
                        {
                            E.Cast(Turrent);
                        }
                    }
                }
            };
            Orbwalker.OnAfterAttack += (sender, args) => {
                if (!R.IsReady() || !ComboUseR)
                    return;

                var target = args.Target as AIHeroClient;
                if (target.IsValidTarget())
                {
                    var buff = target.GetBuff("tristanaecharge");
                    if(buff != null)
                    {
                        if(buff.Count >= 3)
                        {
                            if(target.GetRealHeath(DamageType.Physical) > GetEDmg(target,4) + Player.GetAutoAttackDamage(target,true,true) &&
                               target.GetRealHeath(DamageType.Physical) < GetEDmg(target,4) + R.GetDamage(target))
                            {
                                R.CastOnUnit(target);
                            }
                        }
                    }
                }
            };
            Render.OnDraw += (s) => {
                if(DQ && Q.IsReady())
                {
                    PlusRender.DrawCircle(Player.Position, ComboUseQDistance, Color.OldLace);
                }
                if (DW && W.IsReady())
                {
                    PlusRender.DrawCircle(Player.Position, W.Range, Color.Orange);
                }
                if (DE && E.IsReady())
                {
                    PlusRender.DrawCircle(Player.Position, E.Range, Color.Red);
                }

                return;
            };
            Game.OnUpdate += GameOnUpdate;
            
            
        }
        
        private void OnMenuLoad()
        {
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo", true));
            {
                Combo.Add(new MenuBool("force", "集中攻击爆炸火花目标"));

                Combo.Add(new MenuSeparator("CQSet", "Q set"));
                {
                    Combo.Add(new MenuBool("CQ", "Use Q"));
                    Combo.Add(new MenuSlider("CQDIS", "当敌人距离 < X时才开Q", 300, 100, 660));
                }
                Combo.Add(new MenuSeparator("CWSet", "W set"));
                {
                    Combo.Add(new MenuBool("CW", "使用 W").SetValue(false));
                    Combo.Add(new MenuSlider("CWDIS", "当敌人距离 > X时才W", 300, 100, (int)W.Range));
                    Combo.Add(new MenuBool("CWCombo", "^-使用W跳跃到可击杀目标"));
                    Combo.Add(new MenuKeyBind("CWUnderTower", "^-使用W越塔", Keys.T, KeyBindType.Toggle)).AddPermashow("脚本释放W时是否越塔");
                }
                Combo.Add(new MenuSeparator("ESet", "E set"));
                {
                    Combo.Add(new MenuBool("CE", "使用 E"));
                    foreach (var enemy in GameObjects.EnemyHeroes)
                    {
                        Combo.Add(new MenuBool("ECast" + enemy.CharacterName, "是否对" + enemy.CharacterName + "释放E技能"));
                    }
                    Combo.Add(new MenuKeyBind("CEB", "对黑名单用户释放E", Keys.E, KeyBindType.Press));
                }
                Combo.Add(new MenuSeparator("RSet", "R set"));
                {
                    Combo.Add(new MenuBool("CR", "Use R"));
                    Combo.Add(new MenuBool("CRE", "^-对于使用E的目标来说 利用R来造成范围爆炸"));
                }
            }
            var LaneClear = ChampionMenu.Add(new Menu("LaneClear", "LaneClear"));
            {
                LaneClear.Add(new MenuBool("FQ", "Use Q"));
                LaneClear.Add(new MenuSlider("FMana", "当蓝量 <= X时不清线野", 40, 0, 100));
            }
            var JungleClear = ChampionMenu.Add(new Menu("JungleClear", "JungleClear"));
            {
                JungleClear.Add(new MenuBool("FQ", "Use Q"));
                JungleClear.Add(new MenuBool("FE", "Use E"));
            }
            var Draw = ChampionMenu.Add(new Menu("Draw", "Draw"));
            {
                Draw.Add(new MenuBool("DQ", "Draw Q Enable Range"));
                Draw.Add(new MenuBool("DW", "Draw W"));
                Draw.Add(new MenuBool("DE", "Draw E"));
                Draw.Add(new MenuBool("Damage", "Draw Damage"));
            }
            var Misc = ChampionMenu.Add(new Menu("Misc", "Misc"));
            {
                Misc.Add(new MenuBool("RInterrupter", "Use R Interrupt Spell"));
                Misc.Add(new MenuBool("AntiRengar", "Anti Ranger"));
            }
            AntiGapcloserMenu = AntiGapcloser.Attach(ChampionMenu);
            {
                AntiGapcloserMenu.Add(new MenuBool("RAntiGap", "Use R",false));
            }
            ChampionMenu.Add(new MenuBool("FET", "Use E Attack EnemyTower"));
        }
        private void GameOnUpdate(EventArgs args)
        {
            E.Range = R.Range = Player.GetCurrentAutoAttackRange();
            ForceTarget();
            SmartE();
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    break;
                case OrbwalkerMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    break;
                case OrbwalkerMode.LastHit:
                    break;
                case OrbwalkerMode.Flee:
                    break;
            }
        }
        private void Combo()
        {
            if (ComboUseW && W.IsReady())
            {
                if (ComboUseWC)
                {
                    var heroList = Cache.EnemyHeroes.Where(y => y.IsEnemy && y.IsValidTarget(W.Range) && !y.IsInvulnerable);
                    var WaitJumpA = heroList.Where(y => y.GetRealHeath(DamageType.Physical) < (GetEDmg(y, 2) + Player.GetAutoAttackDamage(y) * 2)).MinOrDefault(y => y.GetRealHeath(DamageType.Physical));
                    if (WaitJumpA.IsValidTarget())
                    {
                        if (Player.Position.CountEnemyHerosInRangeFix(W.Range) > 0 && WaitJumpA.DistanceToPlayer() > ComboUseWDistance && WaitJumpA.Position.CountEnemyHerosInRangeFix(400) < 3)
                        {
                            var NextPos = Prediction.GetPrediction(WaitJumpA, 0.7f);

                            var jumpPos = new Geometry.Circle(Player.Position, W.Range).Points.Where(x =>  x.CountEnemyHerosInRangeFix(300) <= 1 && x.Distance(WaitJumpA) <= Player.GetCurrentAutoAttackRange()).MinOrDefault(x => x.DistanceToCursor());
                            if (jumpPos.IsValid() && !jumpPos.IsZero)
                            {
                                W.Cast(jumpPos);
                            }
                        }
                    }

                }
            }
            if (ComboUseQ && Q.IsReady())
            {
                var AnyHero = Orbwalker.GetTarget();
                if (AnyHero.IsValidTarget())
                {
                    var toHero = AnyHero as AIHeroClient;
                    if (toHero == null)
                        return;
                    if (toHero.GetRealHeath(DamageType.Physical) <= Player.GetAutoAttackDamage(toHero) * 2 ||
                       toHero.GetRealHeath(DamageType.Physical) <= GetEDmg(toHero, 2) + Player.GetAutoAttackDamage(toHero) * 2)
                    {
                        Q.Cast();
                    }
                    else
                    {
                        AnyHero = TargetSelector.GetTarget(ComboUseQDistance + Player.BoundingRadius, DamageType.Physical);
                        if (AnyHero.IsValidTarget(ComboUseQDistance))
                        {
                            Q.Cast();
                        }
                    }
                }
            }
            if (ComboUseR && !Player.Spellbook.IsWindingUp)
            {
                if (ComboUseRE)
                {
                    var RETarget = Cache.EnemyHeroes.Where(y =>  y.IsValidTarget(R.Range) && y.GetBuffCount("tristanaecharge") >= 3 && y.GetRealHeath(DamageType.Physical) < (W.IsReady() ? GetEDmg(y, 4) + Player.GetAutoAttackDamage(y) * 2 : GetEDmg(y, 4))).FirstOrDefault();
                    if (RETarget.IsValidTarget(R.Range))
                    {
                        if (PosAfterR(RETarget).CountEnemyHerosInRangeFix(300) >= 1)
                        {
                            R.CastOnUnit(RETarget);
                            return;
                        }
                    }
                }
                var target = Cache.EnemyHeroes.Where(y => y.IsValidTarget(R.Range) && y.GetRealHeath(DamageType.Physical) < R.GetDamage(y)).FirstOrDefault();
                if (target.IsValidTarget(R.Range))
                {

                        R.CastOnUnit(target);
                        return;
                    
                }
            }
        }
        private void ForceTarget()
        {
            var eTarget = Cache.EnemyHeroes.Where(y => y.IsValidTarget() && y.InCurrentAutoAttackRange() && y.HasBuff("tristanaechargesound")).FirstOrDefault();
            Orbwalker.ForceTarget = eTarget;
        }
        private void SmartE()
        {
            if (ComboUseEBlock)
            {
                var emwg = TargetSelector.GetTargets(E.Range, DamageType.Physical).Where(x => IsEBlock(x));
                if (emwg.Any())
                {
                    
                    var TempHero = emwg.MinOrDefault(y => AaIndicator(y));
                    if (TempHero.IsValidTarget(E.Range))
                    {
                        E.CastOnUnit(TempHero);
                    }
                }
            }
        }
        private bool IsEBlock(AIBaseClient target)
        {
            return !ChampionMenu["Combo"]["ECast" + target.CharacterName].GetValue<MenuBool>().Enabled;
        }
        private void LaneClear()
        {
            if (!Enable_laneclear || Player.ManaPercent < LaneClearMana)
                return;

            if (LaneClearUseQ && Q.IsReady())
            {
                if(Cache.GetMinions(Player.Position,ComboUseQDistance).Count > 2 && Player.CountEnemyHerosInRangeFix(600f) == 0)
                {
                    Q.Cast();
                }
            }
        }
        private void JungleClear()
        {
            if (!Enable_laneclear || Player.ManaPercent < LaneClearMana)
                return;

            if (JungleClearUseE && E.IsReady())
            {
                var minion = Cache.GetJungles(Player.Position, E.Range).FirstOrDefault(x => x.GetJungleType() >= JungleType.Large && x.GetRealHeath(DamageType.Physical) > Player.GetAutoAttackDamage(x) * 2);
                if (minion != null)
                {
                    E.Cast(minion);
                }
            }
            if (JungleClearUseQ && Q.IsReady())
            {
                if (Cache.GetJungles(Player.Position, ComboUseQDistance).Count(x => x.GetRealHeath(DamageType.Physical) > Player.GetAutoAttackDamage(x)) > 2)
                {
                    Q.Cast();
                }
            }
        }
        private float GetEDmg(AIBaseClient target, int PredAttackCount)
        {
            float BaseDamage = 0;
            float endDamae = 0;
            if (E.IsReady() || target.GetBuffCount("tristanaecharge") > 0)
            {
                int BaseLevel = E.Level - 1;
                BaseDamage = 70 + BaseLevel * 10 + ((Player.TotalAttackDamage - Player.BaseAttackDamage) * (0.5f + (BaseLevel * 0.25f) + (Player.Crit * 0.333f / 2f))) + (Player.TotalMagicalDamage * (0.5f + ((Player.Crit * 0.333f) / 2f)));

                if (PredAttackCount >= 4)
                {
                    PredAttackCount = 4;
                }
                endDamae = BaseDamage + (BaseDamage * 0.3f * PredAttackCount);
            }
            endDamae = (float)Player.CalculatePhysicalDamage(target, (float)endDamae);
            return endDamae;
        }
        private Vector3 PosAfterR(AIBaseClient w)
        {
            return
                Player.Position.Extend(
                    w.ServerPosition,
                     1000f);
        }
    }
}
