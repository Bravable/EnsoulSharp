using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using SharpDX;
using ImpulseAIO.Common;

namespace ImpulseAIO.Champion.Ezreal
{
    internal class Ezreal : Base
    {
        private static Spell Q, W, E, R;
        private static Dash dash;
        private static Menu AntiGapcloserMenu;
        private static bool ComboUseQ => ChampionMenu["Combo"]["CQ"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseW => ChampionMenu["Combo"]["CW"].GetValue<MenuBool>().Enabled;
        private static bool SemiR => ChampionMenu["Combo"]["SemiR"].GetValue<MenuKeyBind>().Active;
        private static bool KillstealQ => ChampionMenu["Killsteal"]["KQ"].GetValue<MenuBool>().Enabled;
        private static bool KillstealR => ChampionMenu["Killsteal"]["KR"].GetValue<MenuBool>().Enabled;
        private static bool HarassUseQ => ChampionMenu["Harass"]["HQ"].GetValue<MenuBool>().Enabled;
        private static bool HarassUseW => ChampionMenu["Harass"]["HW"].GetValue<MenuBool>().Enabled;
        private static int HarassMana => ChampionMenu["Harass"]["HMana"].GetValue<MenuSlider>().Value;
        private static bool WaveUseQ => ChampionMenu["LaneClear"]["LQ"].GetValue<MenuBool>().Enabled;
        private static bool AutoHarass => ChampionMenu["LaneClear"]["AQH"].GetValue<MenuKeyBind>().Active;
        private static int WaveUseQMana => ChampionMenu["LaneClear"]["LQMana"].GetValue<MenuSlider>().Value;
        private static bool JungleUseQ => ChampionMenu["JungleClear"]["JQ"].GetValue<MenuBool>().Enabled;
        private static bool JungleUseW => ChampionMenu["JungleClear"]["JW"].GetValue<MenuBool>().Enabled;
        private static bool LastHitUseQ => ChampionMenu["LastHit"]["LQ"].GetValue<MenuBool>().Enabled;
        private static int LastHitUseQMana => ChampionMenu["LastHit"]["LQMana"].GetValue<MenuSlider>().Value;
        private static bool TurrentW => ChampionMenu["Turrent"]["TW"].GetValue<MenuBool>().Enabled;
        private static MenuSliderButton safecheck => ChampionMenu["Turrent"]["safe"].GetValue<MenuSliderButton>();
        private static MenuSliderButton overrideCheck => ChampionMenu["Turrent"]["allies"].GetValue<MenuSliderButton>();
        private static int TWMana => ChampionMenu["Turrent"]["TWMana"].GetValue<MenuSlider>().Value;
        private static bool DQ => ChampionMenu["Draw"]["DQ"].GetValue<MenuBool>().Enabled;
        private static bool DE => ChampionMenu["Draw"]["DE"].GetValue<MenuBool>().Enabled;
        private static bool AntiGap => AntiGapcloserMenu["EAntiGap"].GetValue<MenuBool>().Enabled;
        private static int RRange => ChampionMenu["RRange"].GetValue<MenuSlider>().Value;
        public Ezreal()
        {
            Q = new Spell(SpellSlot.Q, 1200f);
            Q.SetSkillshot(0.25f, 60f, 2000f, true, SpellType.Line);
            W = new Spell(SpellSlot.W, 1200f);
            W.SetSkillshot(0.25f, 80f, 1700f, false, SpellType.Line);
            E = new Spell(SpellSlot.E, 475f) { Delay = 0.65f };
            R = new Spell(SpellSlot.R, 20000f);
            R.SetSkillshot(1f, 160f, 2000f, false, SpellType.Line);
            Q.DamageType = W.DamageType = E.DamageType = R.DamageType = DamageType.Physical;

            OnMenuLoad();
            dash = new Dash(E);
            Common.BaseUlt.BaseUlt.Initialize(ChampionMenu, R);
            Render.OnEndScene += OnDraw;
            Game.OnUpdate += GameOnUpdate;
            AIBaseClient.OnBuffAdd += OnBuffAdd;
            AntiGapcloser.OnGapcloser += OnAntiGapCloser;
            Orbwalker.OnNonKillableMinion += (s, g) => {
                var Minions = g.Target as AIBaseClient;
                if(Minions != null && Enable_laneclear && Player.ManaPercent > WaveUseQMana && WaveUseQ)
                {
                    if(Q.GetHealthPrediction(Minions) > 0 && Q.GetHealthPrediction(Minions) < Q.GetDamage(Minions))
                    {
                        var pred = Q.GetPrediction(Minions, false, -1, new CollisionObjects[] { CollisionObjects.YasuoWall, CollisionObjects.Minions });
                        if (pred.Hitchance >= HitChance.Medium && Q.Cast(pred.CastPosition))
                        {
                            return;
                        }
                    }
                }
            };
            Orbwalker.OnBeforeAttack += (s, g) => { 
                if(Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                {
                    var Turrent = g.Target as AITurretClient;
                    if (Turrent.IsValidTarget(W.Range))
                    {
                        if (!TurrentW || !W.IsReady())
                        {
                            return;
                        }
                        if (TWMana >= Player.ManaPercent)
                        {
                            return;
                        }
                        var alliesCount = Cache.AlliesHeroes.Count(x => x.IsValidTarget(900, false) && !x.IsMe);

                        if (safecheck.Enabled && GameObjects.Player.CountEnemyHeroesInRange(safecheck.Value) != 0 &&
                           (!overrideCheck.Enabled || alliesCount < overrideCheck.Value))
                        {
                            return;
                        }

                        if (safecheck.Enabled && GameObjects.Player.CountEnemyHeroesInRange(safecheck.Value) != 0 &&
                            overrideCheck.Enabled && alliesCount < overrideCheck.Value)
                        {
                            return;
                        }

                        W.Cast(Turrent.ServerPosition);
                    }
                }
            };
        }
        private void GameOnUpdate(EventArgs args)
        {
            Killsteal();
            Orbwalker.ForceTarget = Cache.EnemyHeroes.Where(x => x.IsValidTarget() && HaveWBuff(x)).FirstOrDefault();
            if(R.IsReady() && SemiR)
            {
                SemiRCast();
            }
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    if (!Player.IsWindingUp)
                    {
                        CastW();
                        CastQ();
                    }
                    break;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
                case OrbwalkerMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    break;
                case OrbwalkerMode.LastHit:
                    LastHit();
                    break;
                case OrbwalkerMode.Flee:
                    break;
            }
        }
        private void OnAntiGapCloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs e)
        {
            if(AntiGap && E.IsReady() && sender.IsEnemy)
            {
                if(e.Type == AntiGapcloser.GapcloserType.Targeted)
                {
                    if (e.Target != null && e.Target.IsMe)
                    {
                        var dashPos = dash.CastDash(true);
                        if (dashPos.IsValid())
                        {
                            E.Cast(dashPos);
                        }
                    }
                }
                else if(e.Type == AntiGapcloser.GapcloserType.SkillShot && e.EndPosition.DistanceToPlayer() < e.StartPosition.DistanceToPlayer() && e.EndPosition.DistanceToPlayer() <= 500)
                {
                    var dashPos = dash.CastDash(true);
                    if (dashPos.IsValid())
                    {
                        E.Cast(dashPos);
                    }
                }
            }
        }
        private void OnBuffAdd(AIBaseClient sender,AIBaseClientBuffAddEventArgs args)
        {
            if (sender.IsMe && E.IsReady())
            {
                if (args.Buff.Name == "ThreshQ" || args.Buff.Name == "rocketgrab2" || args.Buff.Name == "PykeQ")
                {
                    var dashPos = dash.CastDash(true);
                    if (dashPos.IsValid())
                    {
                        E.Cast(dashPos);
                    }
                }
            }
        }
        private void OnDraw(EventArgs args)
        {
            if (DQ && Q.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, Q.Range, Color.Red);
            }
            if (DE && E.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, E.Range, Color.Red);
            }
        }
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Ezreal));
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo"));
            {
                Combo.Add(new MenuBool("CQ", "Use Q"));
                Combo.Add(new MenuBool("CW", "Use W"));
                Combo.Add(new MenuKeyBind("SemiR", "Semi - R Cast",Keys.T,KeyBindType.Press));
            }
            var Harass = ChampionMenu.Add(new Menu("Harass", "Harass"));
            {
                Harass.Add(new MenuBool("HQ", "Use Q"));
                Harass.Add(new MenuBool("HW", "Use W",false));
                Harass.Add(new MenuSlider("HMana", Program.Chinese ? "蓝量 < X% 时不骚扰" : "Don't Harass if Mana <= X%", 30, 0, 100));
            }
            var Killsteal = ChampionMenu.Add(new Menu("Killsteal", "Killsteal"));
            {
                Killsteal.Add(new MenuBool("KQ", "Use Q"));
                Killsteal.Add(new MenuBool("KR", "Use R"));
            }
            var LaneClear = ChampionMenu.Add(new Menu("LaneClear", "LaneClear"));
            {
                LaneClear.Add(new MenuBool("LQ", "Use Q"));
                LaneClear.Add(new MenuKeyBind("AQH", "LaneClear Auto Q Harass",Keys.G,KeyBindType.Toggle)).AddPermashow();
                LaneClear.Add(new MenuSlider("LQMana", Program.Chinese ? "蓝量 < X 时不清线野" : "Don't Laneclear/JungleClear if Mana <= X%", 30, 0, 100));
            }
            var JungleClear = ChampionMenu.Add(new Menu("JungleClear", "JungleClear"));
            {
                JungleClear.Add(new MenuBool("JQ", "Use Q"));
                JungleClear.Add(new MenuBool("JW", "Use W"));
            }
            var LastHit = ChampionMenu.Add(new Menu("LastHit", "LastHit"));
            {
                LastHit.Add(new MenuBool("LQ", "Use Q"));
                LastHit.Add(new MenuSlider("LQMana", Program.Chinese ? "蓝量 < X 时不尾刀" : "Dont' LastHit if Mana <= X%", 70, 0, 100));
            }
            var Turrent = ChampionMenu.Add(new Menu("Turrent", "W Turrent"));
            {
                Turrent.Add(new MenuBool("TW", "Use W"));
                Turrent.Add(new MenuSliderButton("safe", "^ Only if no enemies in range", 1400, 1200, 2000));
                Turrent.Add(new MenuSliderButton("allies", "^ Despite allies count nearby >? x", 1, 1, 4));
                Turrent.Add(new MenuSlider("TWMana", Program.Chinese ? "蓝量 < X 时不对防御塔用W" : "Dont' W Turrent if Mana <= X%", 70, 0, 100));
            }
            var Draw = ChampionMenu.Add(new Menu("Draw", "Draw"));
            {
                Draw.Add(new MenuBool("DQ", "Draw Q"));
                Draw.Add(new MenuBool("DE", "Draw E"));
            }
            AntiGapcloserMenu = AntiGapcloser.Attach(ChampionMenu);
            {
                AntiGapcloserMenu.Add(new MenuBool("EAntiGap", "Use E"));
            }

            ChampionMenu.Add(new MenuSlider("RRange", "R Range", 1400, 0, 3000));
        }
        private void LastHit()
        {
            if (Player.ManaPercent < LastHitUseQMana)
                return;

            if (LastHitUseQ && Q.IsReady())
            {
                var minion =
                        Cache.GetMinions(Player.ServerPosition, Q.Range).Where(
                            i =>
                            i.IsValidTarget(Q.Range)
                            && Q.GetHealthPrediction(i) > 0 && Q.GetHealthPrediction(i) <= Q.GetDamage(i)
                            && (i.IsUnderAllyTurret() || (i.IsUnderEnemyTurret() && !Player.IsUnderEnemyTurret())
                                || i.DistanceToPlayer() > i.GetRealAutoAttackRange() + 50
                                || i.GetRealHeath(DamageType.Physical) > Player.GetAutoAttackDamage(i))).MaxOrDefault(i => i.MaxHealth);
                if (minion != null)
                {
                    var pred = Q.GetPrediction(minion, false, -1, new CollisionObjects[] { CollisionObjects.YasuoWall, CollisionObjects.Minions });
                    if (pred.Hitchance >= HitChance.Medium && Q.Cast(pred.CastPosition))
                    {
                        return;
                    }
                }
            }
        }
        private void JungleClear()
        {
            if (!Enable_laneclear || Player.ManaPercent < WaveUseQMana)
                return;
            var target = Cache.GetJungles(Player.ServerPosition, Q.Range);
            foreach(var obj in target)
            {
                if(JungleUseW && W.IsReady() && obj.IsValidTarget(W.Range))
                {
                    if (obj.GetJungleType() >= JungleType.Large)
                    {
                        var predpos = W.GetPrediction(obj, false, -1, new CollisionObjects[] { CollisionObjects.YasuoWall, CollisionObjects.Heroes });
                        if (predpos.Hitchance >= HitChance.High)
                        {
                            W.Cast(predpos.CastPosition);
                        }
                    }
                }
                if(JungleUseQ && Q.IsReady() && obj.IsValidTarget(Q.Range))
                {
                    var predpos = Q.GetPrediction(obj, false, -1, new CollisionObjects[] { CollisionObjects.YasuoWall, CollisionObjects.Heroes, CollisionObjects.Minions });
                    if (predpos.Hitchance >= HitChance.High)
                    {
                        Q.Cast(predpos.CastPosition);
                    }
                }
            }
        }
        private void LaneClear()
        {
            if (!Enable_laneclear || Player.ManaPercent < WaveUseQMana)
                return;

            if (Q.IsReady())
            {
                if (AutoHarass)
                {
                    Harass();
                }
                if (WaveUseQ)
                {
                    var preds = Cache.GetMinions(Player.Position, Q.Range).Where(i => Q.GetHealthPrediction(i) > 0 && Q.GetHealthPrediction(i) <= Q.GetDamage(i) && (i.IsUnderAllyTurret() || (i.IsUnderEnemyTurret() && !Player.IsUnderEnemyTurret())
                || i.DistanceToPlayer() > i.GetRealAutoAttackRange() + 50
                || i.Health > Player.GetAutoAttackDamage(i))).Select(y => Q.GetPrediction(y, false, -1, new CollisionObjects[] { CollisionObjects.Minions })).Where(i => i.Hitchance >= HitChance.High && i.CastPosition.DistanceToPlayer() <= Q.Range).ToList();
                    if (preds.Count > 0)
                    {
                        Q.Cast(preds.FirstOrDefault().CastPosition);
                    }
                }
            }
        }
        private void Harass()
        {
            if (Player.ManaPercent < HarassMana)
                return;

            if (HarassUseW && W.IsReady())
            {
                if (!(!Q.IsReady() && Player.CountEnemyHeroesInRange(Player.GetRealAutoAttackRange()) == 0))
                {
                    var wtarget = W.GetTarget();
                    if (wtarget == null)
                    {
                        return;
                    }

                    var winput = W.GetPrediction(wtarget);
                    if (winput.Hitchance >= HitChance.High)
                    {
                        W.Cast(winput.CastPosition);
                        return;
                    }
                }
            }
            if (HarassUseQ && Q.IsReady())
            {
                var priorityTarget = Cache.EnemyHeroes.FirstOrDefault(x => x.IsValidTarget(Q.Range) && HaveWBuff(x));
                if (priorityTarget != null)
                {
                    var qinpupt = Q.GetPrediction(priorityTarget);
                    if (qinpupt.Hitchance >= HitChance.High)
                    {
                        Q.Cast(qinpupt.CastPosition);
                        return;
                    }
                }
                var Ret = IMPGetTarGet(Q, false, HitChance.High);
                if(!Ret.SuccessFlag || !Ret.Obj.IsValid)
                {
                    return;
                }
                Q.Cast(Ret.CastPosition);
            }
        }
        private bool HaveWBuff(AIBaseClient target)
        {
            return target.HasBuff("ezrealwattach");
        }
        private void SemiRCast()
        {
            var Ret = IMPGetTarGet(R, false, HitChance.High);
            if(Ret.SuccessFlag && Ret.Obj.IsValid && R.IsInRange(Ret.CastPosition))
            {
                R.Cast(Ret.CastPosition);
            }
        }
        private void CastW()
        {
            if (!ComboUseW || !W.IsReady())
            {
                return;
            }
            var Ret = IMPGetTarGet(W, false, HitChance.High);

            if (!Ret.SuccessFlag || !Ret.Obj.IsValid)
            {
                return;
            }

            if (W.IsInRange(Ret.CastPosition))
            {
                W.Cast(Ret.CastPosition);
            }
        }
        private void CastQ()
        {
            if (!ComboUseQ || !Q.IsReady())
            {
                return;
            }
            var Ret = IMPGetTarGet(Q, false, HitChance.High);

            if (!Ret.SuccessFlag || !Ret.Obj.IsValid)
            {
                return;
            }
            var priorityTarget = Cache.EnemyHeroes.FirstOrDefault(x => x.IsValidTarget(Q.Range) && HaveWBuff(x));
            if (priorityTarget != null)
            {
                var qinpupt = Q.GetPrediction(priorityTarget);
                if (qinpupt.Hitchance >= HitChance.High && Q.IsInRange(qinpupt.CastPosition))
                {
                    Q.Cast(qinpupt.CastPosition);
                }
            }
            else
            {
                if (Q.IsInRange(Ret.CastPosition))
                {
                    Q.Cast(Ret.CastPosition);
                }
            }
        }
        private void Killsteal()
        {
            if (!KillstealQ || !Q.IsReady()) return;

            foreach (var target in Cache.EnemyHeroes.Where(x =>
                x.IsValidTarget(Q.Range) &&
                Q.GetDamage(x) >= x.GetRealHeath(DamageType.Physical) &&
                !x.IsInvulnerable))
            {
                var qinput = Q.GetPrediction(target);
                if (qinput.Hitchance >= HitChance.High)
                {
                    Q.Cast(qinput.CastPosition);
                }

            }

            if (!KillstealR || !R.IsReady())
            {
                return;
            }

            if(Player.CountEnemyHerosInRangeFix(800) > 0)
            {
                return;
            }

            foreach (var target in Cache.EnemyHeroes.Where(x =>
                x.IsValidTarget(RRange) &&
                R.GetDamage(x) >= x.GetRealHeath(DamageType.Magical) &&
                !x.IsInvulnerable))
            {
                var rinput = R.GetPrediction(target);
                if (rinput.Hitchance >= HitChance.High)
                {
                    R.Cast(rinput.CastPosition);
                }

            }
        }
    }
}
