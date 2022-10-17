using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using ImpulseAIO.Common.Evade;
using SharpDX;
using ImpulseAIO.Common;

namespace ImpulseAIO.Champion.Thresh
{
    public enum CastState
    {
        NotReady,
        First,
        Second
    }

    internal class Thresh : Base
    {
        private static Spell Q, W, PullE,PushE,R;
        private static Menu AntiGapcloserMenu;

        private AIBaseClient Qedtarget => Cache.EnemyHeroes.Find(e => e.IsEnemy && e.HasBuff("ThreshQ") && e.GetBuff("ThreshQ").Caster.IsMe);

        private static HitChance QhitChance => ChampionMenu["HitChance"]["Q"].GetValue<MenuHitChance>().HitChanceIndex;
        private static bool ComboUseQ => ChampionMenu["Combo"]["CQ"].GetValue<MenuBool>().Enabled;
        private static int ComboUseQ2Mode => ChampionMenu["Combo"]["CQ2Mode"].GetValue<MenuList>().Index;
        private static bool ComboUseW => ChampionMenu["Combo"]["CW"].GetValue<MenuBool>().Enabled;
        private static bool WFarKey => ChampionMenu["Combo"]["WKey"].GetValue<MenuKeyBind>().Active;
        private static bool ComboUseE => ChampionMenu["Combo"]["CE"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseR => ChampionMenu["Combo"]["CR"].GetValue<MenuBool>().Enabled;
        private static int ComboUseRCount => ChampionMenu["Combo"]["CRS"].GetValue<MenuSlider>().Value;

        private static bool PushActive => ChampionMenu["Key"]["Push"].GetValue<MenuKeyBind>().Active;
        private static bool PullActive => ChampionMenu["Key"]["Pull"].GetValue<MenuKeyBind>().Active;
        private static bool DQ => ChampionMenu["Draw"]["DQ"].GetValue<MenuBool>().Enabled;
        private static bool DE => ChampionMenu["Draw"]["DE"].GetValue<MenuBool>().Enabled;
        private static bool AntiGapUseE => AntiGapcloserMenu["EAntiGap"].GetValue<MenuBool>().Enabled;
        private static bool Interrupters => ChampionMenu["Safe"]["EInterrupter"].GetValue<MenuBool>().Enabled;
        private static bool SafeHelp2 => ChampionMenu["Safe"]["DLH"].GetValue<MenuBool>().Enabled;
        public Thresh()
        {
            Q = new Spell(SpellSlot.Q, 1040f);
            Q.SetSkillshot(0.5f, 70f, 1900f, true, SpellType.Line);
            W = new Spell(SpellSlot.W, 970f);
            PushE = new Spell(SpellSlot.E, 537.5f);
            PushE.SetSkillshot(0f, 110f, 2000f, false, SpellType.Line);

            PullE = new Spell(SpellSlot.E, 537.5f);
            PullE.SetSkillshot(0f, 110f, float.MaxValue, false, SpellType.Line);

            R = new Spell(SpellSlot.R, 425f);
            OnMenuLoad();
            Game.OnUpdate += GameOnUpdate;
            MissileManager.Initialize();
            AntiGapcloser.OnGapcloser += AntiGapcloser_OnEnemyGapcloser;
            AIBaseClient.OnDoCast += OnProcessSpellCast;
            Interrupter.OnInterrupterSpell += (s, g) =>
            {
                if (!Interrupters)
                    return;

                if (s.IsEnemy)
                {
                    if (g.DangerLevel == Interrupter.DangerLevel.High)
                    {
                        if (s.IsValidTarget(PullE.Range))
                        {
                            Pull(s);
                        }
                    }
                }
            };
            Render.OnPresent += (args) =>
            {
                if (DQ && Q.IsReady())
                    PlusRender.DrawCircle(Player.Position, Q.Range, Color.Red);
                if (DE && PullE.IsReady())
                    PlusRender.DrawCircle(Player.Position, PullE.Range, Color.Orange);
            };
        }
        private void GameOnUpdate(EventArgs args)
        {
            Orbwalker.AttackEnabled = GetQState() == CastState.Second ? false : true;
            Helper();
            Push_Pull();
            WFarKeyLogic();
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    break;
                case OrbwalkerMode.LaneClear:
                    break;
                case OrbwalkerMode.LastHit:
                    break;
                case OrbwalkerMode.Flee:
                    break;
            }
        }
        private void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs spell)
        {
            if (!SafeHelp2 || !W.IsReady())
                return;

            if (sender == null || !R.IsReady() || Player.IsDead || Player.IsZombie() || sender.IsAlly || sender.IsMe)
            {
                return;
            }

            if (sender.IsEnemy)
            {
                if (Orbwalker.IsAutoAttack(spell.SData.Name)) //如果是普攻
                {
                    var attackmub = spell.Target as AIHeroClient;
                    if (attackmub != null)
                    {
                        if (attackmub.IsMe)
                        {
                            if (sender.GetAutoAttackDamage(Player, true) * 1.2 > Player.Health)
                            {
                                W.Cast(attackmub.Position);
                            }
                        }
                        else if (attackmub.IsAlly && attackmub.DistanceToPlayer() <= W.Range)
                        {
                            if (sender.GetAutoAttackDamage(attackmub, true) * 1.2 > attackmub.Health)
                            {
                                W.Cast(attackmub.Position);
                            }
                        }
                    }
                }
            }
        }
        private void Helper()
        {
            if (!SafeHelp2)
                return;
            foreach (var allyhero in Cache.AlliesHeroes.Where(x => x.IsValid && x.DistanceToPlayer() <= W.Range))
            {
                if (MissileManager.WillHit(allyhero))
                {
                    W.Cast(allyhero.Position);
                }
            }
        }
        private void Push_Pull()
        {
            if (PullActive)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                var T = TargetSelector.GetTarget(PullE.Range, DamageType.Magical);
                if (T.IsValidTarget())
                {
                   Pull(T);
                }
            }
            if (PushActive)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                var T = TargetSelector.GetTarget(PushE.Range, DamageType.Magical);
                if (T.IsValidTarget())
                {
                   Push(T);
                }
            }
        }
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Thresh));
            var HitChance = ChampionMenu.Add(new Menu("HitChance", "HitChance"));
            {
                HitChance.Add(new MenuHitChance("Q", "Q HitChance"));
            }
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo", true));
            {
                Combo.Add(new MenuBool("CQ", "Use Q1"));
                Combo.Add(new MenuList("CQ2Mode", "Use Q2 Mode", new string[] { "总是", "从不", "智能" }, 2));
                Combo.Add(new MenuBool("CW", "Use W"));
                Combo.Add(new MenuKeyBind("WKey", Program.Chinese ? "丢灯笼给距离最远的友军" : "Cast W To MaxDistToPlayer Ally",Keys.T,KeyBindType.Press)).AddPermashow();
                Combo.Add(new MenuBool("CE", "Use E"));
                Combo.Add(new MenuBool("CR", "Use R"));
                Combo.Add(new MenuSlider("CRS", "Use R if hitCount >=X", 2, 1, 5));
            }
            var Key = ChampionMenu.Add(new Menu("Key", "Push && Pull", true));
            {
                Key.Add(new MenuKeyBind("Pull", Program.Chinese ? "将敌人拽回" : "Pull", Keys.A, KeyBindType.Press)).AddPermashow();
                Key.Add(new MenuKeyBind("Push", Program.Chinese ? "将敌人推走" : "Push", Keys.Z, KeyBindType.Press)).AddPermashow();
            }
            AntiGapcloserMenu = AntiGapcloser.Attach(ChampionMenu);
            {
                AntiGapcloserMenu.Add(new MenuBool("EAntiGap", "Use E"));
            }
            var Safe = ChampionMenu.Add(new Menu("Safe", "E && W"));
            {
                Safe.Add(new MenuBool("EInterrupter", Program.Chinese ? "使用 E 打断技能" : "Use E Interrupt"));
                Safe.Add(new MenuBool("DLH", Program.Chinese ? "使用灯笼提供护盾给友军" : "Use W Protect Ally"));
            }
            var Draw = ChampionMenu.Add(new Menu("Draw", "Draw", true));
            {
                Draw.Add(new MenuBool("DQ", "Draw Q"));
                Draw.Add(new MenuBool("DE", "Draw E"));
            }
        }
        private void AntiGapcloser_OnEnemyGapcloser(AIBaseClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (AntiGapUseE && PushE.IsReady() && sender.IsValidTarget(PushE.Range))
            {
                Pull(sender);
            }
        }
        private void WFarKeyLogic()
        {
            if (WFarKey && W.IsReady())
            {
                if(Player.CountAllysHerosInRangeFix(W.Range) - 1 > 0)
                {
                    var FarObj = Cache.AlliesHeroes.Where(x => x.IsAlly && !x.IsMe && x.IsValidTarget(W.Range, false)).MaxOrDefault(x => x.CountEnemyHerosInRangeFix(450f));
                    if(FarObj != null)
                    {
                        W.Cast(FarObj.ServerPosition.Extend(Player.ServerPosition, 200f));
                    }
                }
            }
        }
        private void Combo()
        {
            if (ComboUseE)
            {
                //默认
                var nornalt = TargetSelector.GetTarget(PushE.Range, DamageType.Physical);
                if (nornalt.IsValidTarget() && nornalt != Qedtarget)
                {
                    Pull(nornalt);
                }
            }

            if (ComboUseQ)
            {
                if (GetQState() == CastState.First)
                {
                    var Ret = IMPGetTarGet(Q, false, QhitChance);
                    if (Ret.SuccessFlag && Ret.Obj.IsValid)
                    {
                        Q.Cast(Ret.CastPosition);
                    }
                }
                if (GetQState() == CastState.Second)
                {
                    if (ComboUseQ2Mode != 1)
                    {
                        if (ComboUseQ2Mode == 0)
                        {
                            if (Qedtarget.IsValidTarget() && Qedtarget.Type == GameObjectType.AIHeroClient && GetBuffLaveTime(Qedtarget, "ThreshQ") < 0.4)
                            {
                                Q.Cast();
                            }
                        }
                        if (ComboUseQ2Mode == 2)
                        {
                            if (Qedtarget.IsValidTarget() && Qedtarget.Type == GameObjectType.AIHeroClient && GetBuffLaveTime(Qedtarget, "ThreshQ") < 0.4 &&
                                !Qedtarget.IsUnderEnemyTurret() && (Qedtarget.ServerPosition.CountEnemyHerosInRangeFix(600) <= Player.ServerPosition.CountAllysHerosInRangeFix(1000)) || (Player.ServerPosition.CountAllysHerosInRangeFix(800) > 1 && Qedtarget.GetRealHeath(DamageType.Physical) <= Player.GetAutoAttackDamage(Qedtarget) * 10))
                            {
                                Q.Cast();
                            }
                        }
                    }
                }
            }
            if (ComboUseR && R.IsReady())
            {
                if (Player.ServerPosition.CountEnemyHerosInRangeFix(R.Range) >= ComboUseRCount)
                {
                    R.Cast();
                }
            }
            if (ComboUseW && W.IsReady())
            {
                if (Qedtarget != null && Player.IsDashing())
                {
                    if (Qedtarget.DistanceToPlayer() <= 600)
                    {
                        var FarAlly = Cache.AlliesHeroes.Where(x =>  !x.IsMe && x.IsValidTarget(W.Range, false) && x.DistanceToPlayer() <= W.Range && x.DistanceToPlayer() > W.Range / 3).MaxOrDefault(x => x.DistanceToPlayer());
                        if (FarAlly != null)
                        {
                            W.Cast(FarAlly.ServerPosition);
                        }
                    }
                    else
                    {
                        var FarAlly = Cache.AlliesHeroes.Where(x => !x.IsDead && !x.IsMe && x.IsValidTarget(W.Range, false) && x.DistanceToPlayer() <= W.Range && !(x.GetCurrentAutoAttackRange() > Qedtarget.Distance(x))).MinOrDefault(x => x.DistanceToPlayer());
                        if (FarAlly != null)
                        {
                            W.Cast(FarAlly.ServerPosition);
                        }
                    }
                }
            }
        }
        private void Push(AIBaseClient target)
        {
            if (PushE.IsReady() && target.IsValidTarget(PushE.Range) && target.IsEnemy)
            {
                var pred = PushE.GetPrediction(target);
                if (pred.Hitchance >= HitChance.Medium)
                {
                    PushE.Cast(pred.CastPosition);
                }
            }
        }
        private void Pull(AIBaseClient target)
        {
            if (PullE.IsReady() && target.IsValidTarget(PullE.Range) && target.IsEnemy)
            {
                var pred = PullE.GetPrediction(target);
                if (pred.Hitchance >= HitChance.Medium)
                {
                    var dist = Player.Distance(pred.CastPosition);
                    var ext = Player.Position.Extend(pred.CastPosition, -dist);
                    PullE.Cast(ext);
                }
            }
        }
        private CastState GetQState()
        {
            if (!Q.IsReady())
            {
                return CastState.NotReady;
            }
            if (Q.Instance.Name == "ThreshQ")
            {
                return CastState.First;
            }
            if (Q.Instance.Name == "ThreshQLeap")
            {
                return CastState.Second;
            }
            return CastState.NotReady;
        }
        private float GetBuffLaveTime(AIBaseClient target, string buffName)
        {
            return
                target.Buffs.Where(buff => buff.Name == buffName)
                    .OrderByDescending(buff => buff.EndTime - Game.Time)
                    .Select(buff => buff.EndTime)
                    .FirstOrDefault() - Game.Time;
        }
    }
}
