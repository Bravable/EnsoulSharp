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

namespace ImpulseAIO.Champion.Lucian
{
    internal class Lucian : Base
    {
        private static Spell Q, ExtraQ,HexQ, W, E, R;
        private static bool isDashing => Player.IsDashing();
        private static bool AAPassive;
        private static new Dash Dash;
        private static Menu AntiGapcloserMenu;

        #region 菜单选项
        private static bool Combo_UseQ => ChampionMenu["Combo"]["CQ"].GetValue<MenuBool>().Enabled;
        private static bool Combo_UseW => ChampionMenu["Combo"]["CW"].GetValue<MenuBool>().Enabled;
        private static bool Combo_UseE => ChampionMenu["Combo"]["CE"].GetValue<MenuBool>().Enabled;
        private static int Combo_UseELongMode => ChampionMenu["Combo"]["CELONG"].GetValue<MenuList>().Index;
        private static bool forceR => ChampionMenu["Combo"]["ForceR"].GetValue<MenuKeyBind>().Active;

        private static bool Harass_UseQ => ChampionMenu["Harass"]["HQ"].GetValue<MenuBool>().Enabled;
        private static bool Harass_UseHexQ => ChampionMenu["Harass"]["HEXQ"].GetValue<MenuBool>().Enabled;
        private static bool Harass_UseW => ChampionMenu["Harass"]["HW"].GetValue<MenuBool>().Enabled;
        private static bool Harass_UseE => ChampionMenu["Harass"]["HE"].GetValue<MenuBool>().Enabled;
        private static int Harass_Mana => ChampionMenu["Harass"]["HHMinMana"].GetValue<MenuSlider>().Value;
        private static bool AutoHarassQ => ChampionMenu["Harass"]["HAutoQ"].GetValue<MenuKeyBind>().Active;

        private static bool LaneClear_UseQ => ChampionMenu["LaneClear"]["LQ"].GetValue<MenuBool>().Enabled;
        private static bool LaneClear_UseW => ChampionMenu["LaneClear"]["LW"].GetValue<MenuBool>().Enabled;
        private static bool LaneClear_UseE => ChampionMenu["LaneClear"]["LE"].GetValue<MenuBool>().Enabled;
        private static int LaneClear_Mana => ChampionMenu["LaneClear"]["LMinMana"].GetValue<MenuSlider>().Value;

        private static bool JungleClear_UseQ => ChampionMenu["JungleClear"]["JQ"].GetValue<MenuBool>().Enabled;
        private static bool JungleClear_UseW => ChampionMenu["JungleClear"]["JW"].GetValue<MenuBool>().Enabled;
        private static bool JungleClear_UseE => ChampionMenu["JungleClear"]["JE"].GetValue<MenuBool>().Enabled;

        private static bool DrawHexQ => ChampionMenu["Draw"]["DEQ"].GetValue<MenuBool>().Enabled;
        private static bool DrawQ => ChampionMenu["Draw"]["DQ"].GetValue<MenuBool>().Enabled;
        private static bool DrawW => ChampionMenu["Draw"]["DW"].GetValue<MenuBool>().Enabled;

        private static bool Killable_Q => ChampionMenu["killsteal"]["KillstealQ"].GetValue<MenuBool>().Enabled;
        private static bool Killable_W => ChampionMenu["killsteal"]["KillstealW"].GetValue<MenuBool>().Enabled;

        private static bool AntiGap => AntiGapcloserMenu["AntiEGap"].GetValue<MenuBool>().Enabled;
        #endregion

        #region 初始化
        public Lucian()
        {
            Q = new Spell(SpellSlot.Q, 500f + Player.BoundingRadius);
            ExtraQ = new Spell(SpellSlot.Q, 900f);
            HexQ = new Spell(SpellSlot.Q, 900f);
            W = new Spell(SpellSlot.W, 900f);
            E = new Spell(SpellSlot.E, 460f);
            R = new Spell(SpellSlot.R, 1400);
            Dash = new Dash(E);
            OnMenuLoad();

            Q.SetTargetted(0.4f, 1400f);
            ExtraQ.SetSkillshot(0.4f, 60f, float.MaxValue, true, SpellType.Line);
            HexQ.SetSkillshot(0.4f, 60f, float.MaxValue, false, SpellType.Line);
            W.SetSkillshot(0.25f, 80f, 1600f, true, SpellType.Line);
            R.SetSkillshot(0f, 110f, 2500, true, SpellType.Line);

            AIHeroClient.OnProcessSpellCast += OnDocast_back;
            Game.OnUpdate += Game_OnUpdate;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Orbwalker.OnNonKillableMinion += Orb_NonKillable;
            AntiGapcloser.OnGapcloser += Anti_GapCloser;
            Render.OnEndScene += OnDraw;
        }
        #endregion

        #region 菜单设置
        private static void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Lucian));
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo"));
            {
                Combo.Add(new MenuBool("CQ", "Use Q"));
                Combo.Add(new MenuBool("CW", "Use W"));
                Combo.Add(new MenuBool("CE", "Use E"));
                Combo.Add(new MenuList("CELONG", "E GapDistance", new string[] {"Max","Short"},1));
                Combo.Add(new MenuKeyBind("ForceR", "Smart R", Keys.T, KeyBindType.Press)).AddPermashow();
            }
            var Harass = ChampionMenu.Add(new Menu("Harass", "Harass"));
            {
                Harass.Add(new MenuBool("HQ", "Use Q"));
                Harass.Add(new MenuBool("HEXQ", "Use Extend Q"));
                Harass.Add(new MenuBool("HW", "Use W", false));
                Harass.Add(new MenuBool("HE", "Use E", false));
                Harass.Add(new MenuSlider("HHMinMana", Program.Chinese ? "当蓝量 >= x%时才骚扰" : "Don't Harass if Mana <= X%", 40, 0, 100));
                Harass.Add(new MenuKeyBind("HAutoQ", "Auto Extend Q Harass", Keys.G, KeyBindType.Toggle)).AddPermashow();
            }
            var LC = ChampionMenu.Add(new Menu("LaneClear", "LaneClear"));
            {
                LC.Add(new MenuBool("LQ", "Use Q"));
                LC.Add(new MenuBool("LW", "Use W"));
                LC.Add(new MenuBool("LE", "Use E", false));
                LC.Add(new MenuSlider("LMinMana", Program.Chinese ? "当蓝量 >= x%时才清线" : "Don't LaneClear if Mana <= X%", 40, 0, 100));
            }
            var JC = ChampionMenu.Add(new Menu("JungleClear", "JungleClear"));
            {
                JC.Add(new MenuBool("JQ", "Use Q"));
                JC.Add(new MenuBool("JW", "Use W"));
                JC.Add(new MenuBool("JE", "Use E"));
            }
            var Draw = ChampionMenu.Add(new Menu("Draw", "Draw"));
            {
                Draw.Add(new MenuBool("DEQ", "Draw extend Q Range"));
                Draw.Add(new MenuBool("DQ", "Draw Q",false));
                Draw.Add(new MenuBool("DW", "Draw W",false));
            }
            var killsteal = ChampionMenu.Add(new Menu("killsteal", "killsteal"));
            {
                killsteal.Add(new MenuBool("KillstealQ", "Use Q"));
                killsteal.Add(new MenuBool("KillstealW", "Use W"));
            }
            AntiGapcloserMenu = AntiGapcloser.Attach(ChampionMenu);
            {
                AntiGapcloserMenu.Add(new MenuBool("AntiEGap", "Use E"));
            }
        }
        #endregion

        #region 类方法Hook
        private void Orb_NonKillable(object s,NonKillableMinionEventArgs args)
        {
            if (Enable_laneclear && Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                if (LaneClear_UseE && E.IsReady() && Player.ManaPercent > LaneClear_Mana)
                {
                    var AttackUnit = (AIBaseClient)args.Target;
                    if (AttackUnit != null && AttackUnit.NewIsValidTarget() && args.Target.Health <= AaDamage(AttackUnit) + Player.GetAutoAttackDamage(AttackUnit, false))
                    {
                        E.Cast(Player.ServerPosition.Extend(Game.CursorPos, 30f));
                    }
                }
            }
        }
        private void OnDraw( EventArgs arg)
        {
            if (Q.IsReady())
            {
                if (DrawHexQ)
                {

                    PlusRender.DrawCircle(Player.Position, ExtraQ.Range,Color.Orange);
                }
                if (DrawQ)
                {
                    PlusRender.DrawCircle(Player.Position, Q.Range, Color.Red);
                }
            }
            if (DrawW && W.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, W.Range,Color.Green);
            }
        }
        private void Game_OnUpdate(EventArgs args)
        {
            if (Player.HasBuff("LucianR"))
            {
                Orbwalker.AttackEnabled = false;
                Orbwalker.MoveEnabled = false;
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
            else
            {
                Orbwalker.AttackEnabled = true;
                Orbwalker.MoveEnabled = true;
            }

            ResetQCastTime();
            if (AutoHarassQ)
            {
                ExtraQLogic();
            }
            Killsteal();
            if (forceR)
            {
                UseRTarget();
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
                    Laneclear();
                    JungleClear();
                    break;
                case OrbwalkerMode.LastHit:
                    break;
                case OrbwalkerMode.Flee:
                    break;
            }
        }
        private void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.Q || args.Slot == SpellSlot.W || args.Slot == SpellSlot.E)
            {
                AAPassive = true;
            }
            if (args.Slot == SpellSlot.E)
            {
                Orbwalker.ResetAutoAttackTimer();
            }
        }
        private void OnDocast_back(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (Orbwalker.IsAutoAttack(args.SData.Name))
                {
                    AAPassive = false;
                }
            }
        }
        private void Anti_GapCloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (AntiGap && E.IsReady())
            {
                if (sender.IsEnemy)
                {
                    if(args.StartPosition.DistanceToPlayer() > args.EndPosition.DistanceToPlayer() && args.EndPosition.DistanceToPlayer() <= 240)
                    {
                        //目标朝着自己突进时 寻找安全位置
                        var DashPos = Dash.CastDash();
                        E.Cast(DashPos);
                    }
                }
            }
        }
        #endregion

        #region 方法
        private void Killsteal()
        {
            foreach (var objs in Cache.EnemyHeroes.Where(x => x.IsEnemy && !x.IsDead))
            {
                var predsHealth = HealthPrediction.GetPrediction(objs, (int)(Q.Delay * 1000), 70);

                if (Killable_Q && Q.IsReady())
                {
                    if (predsHealth < Q.GetDamage(objs))
                    {
                        if (objs.NewIsValidTarget(Q.Range))
                        {
                            Q.CastOnUnit(objs);
                        }
                        else
                        {
                            if (!AutoHarassQ)
                            {
                                ExtraQLogic();
                            }
                        }
                    }
                }
                if (Killable_W && W.IsReady())
                {
                    if(predsHealth <= W.GetDamage(objs))
                    {
                        var pred = W.GetPrediction(objs, true, -1, new CollisionObjects[] { CollisionObjects.Minions, CollisionObjects.Heroes, CollisionObjects.YasuoWall });
                        if(pred.Hitchance >= HitChance.Medium)
                        {
                            W.Cast(pred.CastPosition);
                            return;
                        }
                    }
                }
            }
        }
        private void JungleClear()
        {
            if (!Enable_laneclear || Player.ManaPercent <= LaneClear_Mana)
            {
                return;
            }
            if (W.IsReady() && JungleClear_UseW && !AAPassive)
            {
                var minions = Cache.GetJungles(Player.ServerPosition, W.Range);
                var Wfarm = W.GetCircularFarmLocation(minions, 150);
                if (Wfarm.MinionsHit >= 1)
                {
                    W.Cast(Wfarm.Position);
                    return;
                }
            }
            if (E.IsReady() && JungleClear_UseE && !AAPassive)
            {
                var LastUnit = (AIBaseClient)Orbwalker.LastTarget;
                if(LastUnit != null && LastUnit.NewIsValidTarget() && LastUnit.Type == GameObjectType.AIMinionClient && LastUnit.IsJungle() &&
                    LastUnit.GetRealHeath(DamageType.Physical) > AaDamage(LastUnit) + Player.GetAutoAttackDamage(LastUnit))
                {
                    var Epos = Player.ServerPosition.Extend(Game.CursorPos, 75f);
                    E.Cast(Epos);
                    return;
                }

            }
            if (Q.IsReady() && JungleClear_UseQ && !AAPassive)
            {
                var minions = Cache.GetJungles(Player.ServerPosition, Q.Range).MinOrDefault(x => x.GetRealHeath(DamageType.Physical));
                if(minions != null)
                {
                    Q.Cast(minions);
                }
            }
        }
        private void Laneclear()
        {
            if (!Enable_laneclear || Player.ManaPercent <= LaneClear_Mana)
            {
                return;
            }
            if(Q.IsReady() && LaneClear_UseQ && !AAPassive)
            {
                var minions = Cache.GetMinions(Player.ServerPosition, ExtraQ.Range);
                foreach (var minion in minions)
                {
                    var poutput = ExtraQ.GetPrediction(minion,true,-1,new CollisionObjects[] { CollisionObjects.Heroes,CollisionObjects.Minions});
                                
                    var col = poutput.CollisionObjects;

                    if (col.Count > 2)
                    {
                        var minionQ = col.FirstOrDefault(x => x.DistanceToPlayer() <= Q.Range);
                        if (minionQ.NewIsValidTarget(Q.Range))
                        {
                            Q.CastOnUnit(minionQ);
                            return;
                        }
                    }
                }
            }
            if (W.IsReady() && LaneClear_UseW && !AAPassive)
            {
                var minions = Cache.GetMinions(Player.ServerPosition, W.Range);
                var Wfarm = W.GetCircularFarmLocation(minions, 150);
                if (Wfarm.MinionsHit > 3)
                {
                    W.Cast(Wfarm.Position);
                    return;
                }
            }

        }
        private List<AIBaseClient> GetHittableTargets()
        {
            var unitList = new List<AIBaseClient>();
            var minions = Cache.GetMinions(
                Player.ServerPosition,
                Q.Range);
            var jungles = Cache.GetJungles(Player.ServerPosition, Q.Range);

            unitList.AddRange(minions);
            unitList.AddRange(jungles);

            return unitList;
        }
        private void ExtraQLogic()
        {
            if (Q.IsReady())
            {
                var t1 = TargetSelector.GetTarget(HexQ.Range, DamageType.Physical);
                if (t1.NewIsValidTarget(HexQ.Range))
                {
                    var predictionPosition = HexQ.GetPrediction(t1);
                    if (predictionPosition.Hitchance < HitChance.High)
                        return;
                    foreach (var unit in from unit in GetHittableTargets()
                                         let polygon =
                                             new Geometry.Rectangle(
                                             Player.ServerPosition,
                                             Player.ServerPosition.Extend(
                                                 unit.ServerPosition,
                                                 HexQ.Range),
                                             HexQ.Width)
                                         where polygon.IsInside(predictionPosition.CastPosition) && Q.IsInRange(unit)
                                         select unit)
                    {
                        Q.CastOnUnit(unit);
                    }

                }
            }
        }
        private void Harass()    
        {
            if (Player.ManaPercent <= Harass_Mana)
            {
                return;
            }
            if (Harass_UseW && W.IsReady())
            {
                var wt = TargetSelector.GetTarget(W.Range, DamageType.Physical);
                if (wt.NewIsValidTarget(W.Range))
                {
                    if (!wt.InAutoAttackRange() || !AAPassive)
                    {
                        var predW = W.GetPrediction(wt, true, -1, new CollisionObjects[] { CollisionObjects.Minions, CollisionObjects.YasuoWall });
                        if (predW.Hitchance >= HitChance.High)
                        {
                            W.Cast(predW.CastPosition);
                            return;
                        }
                    }

                }
            }
            if (Harass_UseE && E.IsReady() && !AAPassive)
            {
                var Epos = Dash.CastDash();
                if (!Epos.IsZero)
                {
                    Epos = Player.ServerPosition.Extend(Epos, 75f);
                    E.Cast(Epos);
                    return;
                }
            }
            if (Q.IsReady())
            {
                if (Harass_UseQ)
                {
                    CastQ();
                }
                if (!AutoHarassQ && Harass_UseHexQ)
                {
                    ExtraQLogic();
                }
            }
        }
        private void CastQ()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
            if (!target.NewIsValidTarget(Q.Range))
                return;
            Q.Cast(target);
        }
        private void Combo()
        {
            //判断自己普攻范围内的敌人 获取
            var inmy_attack_range_Unit = TargetSelector.GetTarget(Player.GetRealAutoAttackRange(), DamageType.Physical);

            if (inmy_attack_range_Unit != null)
            {
                if (!AAPassive && !isDashing)
                {
                    bool isInMelle = Dash.InMelleAttackRange(Player.ServerPosition);
                    if (Combo_UseW && W.IsReady() && (!isInMelle || !E.IsReady()))
                    {
                        CollisionObjects[] Collisions = inmy_attack_range_Unit.DistanceToPlayer() <= 425 ? null : new CollisionObjects[] { CollisionObjects.Minions, CollisionObjects.YasuoWall };
                        var Wpred = W.GetPrediction(inmy_attack_range_Unit, true, -1, Collisions);
                        if (Wpred.Hitchance >= HitChance.Medium)
                        {
                            W.Cast(Wpred.CastPosition);
                            EnsoulSharp.SDK.Utility.DelayAction.Add(10, () => Player.IssueOrder(GameObjectOrder.MoveTo, inmy_attack_range_Unit.ServerPosition));
                            return;
                        }
                    }
                    if (Combo_UseE && E.IsReady())
                    {
                        var Epos = Dash.CastDash(true);
                        if (!Epos.IsZero)
                        {
                            if (Combo_UseELongMode == 1)
                            {
                                if (!isInMelle)
                                {
                                    //如果自己在近战普攻范围内 长E
                                    Epos = Player.ServerPosition.Extend(Epos, 75f);
                                }
                            }
                            E.Cast(Epos);
                            return;
                        }
                        else
                        {
                            if (Combo_UseQ && Q.IsReady())
                            {
                                CastQ();
                            }
                        }
                    }
                    //如果自己普攻范围内有人
                    if (Combo_UseQ && Q.IsReady() && !E.IsReady())
                    {
                        CastQ();
                    }
                }
            }
            else if (Combo_UseE && E.IsReady())
            {
                var NeedEGap = TargetSelector.GetTarget(E.Range + Player.GetCurrentAutoAttackRange(), DamageType.Physical);
                if (NeedEGap != null && !AAPassive)
                {
                    //如果QW都CD了 而且手上有一个被动
                    if ((!Q.IsReady() && !W.IsReady() && AAPassive) || !AAPassive)
                    {
                        //需要E突进过去的目标
                        var Epos = Dash.CastDash();
                        if (!Epos.IsZero)
                        {
                            //敌我距离 - 普攻距离
                            var fixDist = NeedEGap.DistanceToPlayer() - Player.GetCurrentAutoAttackRange() + 100f;
                            Epos = Player.ServerPosition.Extend(Epos, fixDist);
                            E.Cast(Epos);
                            return;
                        }
                    }
                }
            }
        }
        private void UseRTarget()
        {
            var target = TargetSelector.GetTarget(R.Range, DamageType.Physical);
            if (target != null && R.IsReady() && target.IsValid && target is AIHeroClient && !Player.HasBuff("LucianR"))
            {
                R.Cast(target.ServerPosition);
            }
        }
        private double AaDamage(AIBaseClient target)
        {
            var LvL = Player.Level;
            if(target.Type == GameObjectType.AIHeroClient)
            {
                //如果目标是英雄单位时
                if (LvL >= 1 && LvL <= 6)
                {
                    return Player.CalculateDamage(target, DamageType.Physical, Player.TotalAttackDamage * 0.5f);
                }
                if (LvL >= 7 && LvL <= 12)
                {
                    return Player.CalculateDamage(target, DamageType.Physical, Player.TotalAttackDamage * 0.55f);
                }
                if (LvL >= 13)
                {
                    return Player.CalculateDamage(target, DamageType.Physical, Player.TotalAttackDamage * 0.6f);
                }
            }
            return Player.CalculateDamage(target, DamageType.Physical, Player.TotalAttackDamage);
        }
        private void ResetQCastTime()
        {
            int MyLvL = Player.Level;
            if(MyLvL >= 1 && MyLvL <= 5)
            {
                Q.Delay = 0.4f - (MyLvL * 0.01f);
            }
            if(MyLvL == 6)
            {
                Q.Delay = 0.36f;
            }
            if(MyLvL >= 7 && MyLvL <= 13)
            {
                Q.Delay = 0.42f - (MyLvL * 0.01f);
            }
            if(MyLvL == 14)
            {
                Q.Delay = 0.29f;
            }
            if (MyLvL >= 15 && MyLvL < 18)
            {
                Q.Delay = 0.43f - (MyLvL * 0.01f);
            }
            HexQ.Delay = ExtraQ.Delay = Q.Delay;
        }
        #endregion
    }

}
