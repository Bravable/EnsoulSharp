using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.Rendering;
using SharpDX;
using ImpulseAIO.Common;

namespace ImpulseAIO.Champion.Irelia
{
    internal class Irelia : Base
    {
        private static Spell Q, W, E, R;
        private static int ChargingW = 0;
        private static int FirstE = 0;
        private static Vector3 PosE;
        private static int EcastTime;
        private static bool CastRForE = true;
        private static bool CastEForR = true;

        private static bool RecurveBow = false;
        private static bool BladeKing = false;
        private static bool WitsEnd = false;
        private static bool Titanic = false;

        private static bool Divine = false;
        private static float DivineTimer = Variables.TickCount;
        private static bool Sheen = false;
        private static float SheenTimer = Variables.TickCount;
        private static bool Black = false;
        private static bool Trinity = false;
        private static float TrinityTimer = Variables.TickCount;
        private static int last_item_update = 0;
        private static int E1Delay;

        private static bool ComboUseQ => ChampionMenu["Combo"]["CQ"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseQGap => ChampionMenu["Combo"]["CQGap"].GetValue<MenuBool>().Enabled;
        private static int ComboUseQMinionsHealth => ChampionMenu["Combo"]["CQMinionsHealth"].GetValue<MenuSlider>().Value;
        private static bool ComboForceKill => ChampionMenu["Combo"]["ForceQ"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseW => ChampionMenu["Combo"]["CW"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseWQ => ChampionMenu["Combo"]["CWQ"].GetValue<MenuBool>().Enabled;
        private static int ComboUseWTick => ChampionMenu["Combo"]["CWTick"].GetValue<MenuSlider>().Value;
        private static bool ComboUseWOnlyMarks => ChampionMenu["Combo"]["CWOnlyMarks"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseE => ChampionMenu["Combo"]["CE"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseR => ChampionMenu["Combo"]["CR"].GetValue<MenuKeyBind>().Active;
        private static int ComboUseRCheakHealth => ChampionMenu["Combo"]["CRCheakHealth"].GetValue<MenuSlider>().Value;
        private static bool ComboUseROnlyCanKill => ChampionMenu["Combo"]["CROnlyKill"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseRWaitQ => ChampionMenu["Combo"]["CRWaitQ"].GetValue<MenuBool>().Enabled;
        private static bool RActive => ChampionMenu["Combo"]["CRKEY"].GetValue<MenuKeyBind>().Active;

        private static bool HarassUseQ => ChampionMenu["Harass"]["HQ"].GetValue<MenuBool>().Enabled;
        private static bool HarassUseQAA => ChampionMenu["Harass"]["HQAA"].GetValue<MenuBool>().Enabled;
        private static bool HarassUseQFarm => ChampionMenu["Harass"]["HQFarm"].GetValue<MenuBool>().Enabled;
        private static int HarassUseQFarmMagic => ChampionMenu["Harass"]["HQFarmMagic"].GetValue<MenuSlider>().Value;
        private static bool HarassUseE => ChampionMenu["Harass"]["HE"].GetValue<MenuBool>().Enabled;

        private static bool LaneClearUseQ => ChampionMenu["LaneClear"]["LQ"].GetValue<MenuBool>().Enabled;
        private static int LaneClearUseQMana => ChampionMenu["LaneClear"]["LQMagic"].GetValue<MenuSlider>().Value;
        private static bool JungleUseQ => ChampionMenu["JungleClear"]["JQ"].GetValue<MenuBool>().Enabled;
        private static bool JungleUseE => ChampionMenu["JungleClear"]["JE"].GetValue<MenuBool>().Enabled;

        //逃跑
        private static bool FleeUseQ => ChampionMenu["Flee"]["FQ"].GetValue<MenuBool>().Enabled;
        private static bool FleeUseE => ChampionMenu["Flee"]["FE"].GetValue<MenuBool>().Enabled;
        //绘制
        private static bool IsDrawQ => ChampionMenu["Drawing"]["DQ"].GetValue<MenuBool>().Enabled;
        private static bool IsDrawW => ChampionMenu["Drawing"]["DW"].GetValue<MenuBool>().Enabled;
        private static bool IsDrawE => ChampionMenu["Drawing"]["DE"].GetValue<MenuBool>().Enabled;
        private static bool IsDrawR => ChampionMenu["Drawing"]["DR"].GetValue<MenuBool>().Enabled;
        private static bool Interrupters => ChampionMenu["Other"]["EInterrupter"].GetValue<MenuBool>().Enabled;
        private static bool IsUnderTower => ChampionMenu["Other"]["OQUnderTower"].GetValue<MenuKeyBind>().Active;
        public Irelia()
        {
            Q = new Spell(SpellSlot.Q, 600f);
            W = new Spell(SpellSlot.W, 800f);
            E = new Spell(SpellSlot.E, 840f);
            R = new Spell(SpellSlot.R, 900f);
            Q.SetTargetted(0f, 1400 + Player.MoveSpeed);
            W.SetSkillshot(0.25f, 120f, 2300, false, SpellType.Line);
            W.SetCharged("IreliaW", "ireliawdefense", 800, 800, 0);
            E.SetSkillshot(0f, 70f, 2000f, false, SpellType.Line);
            R.SetSkillshot(0.4f, 160f, 2000f, true, SpellType.Line);
            OnMenuLoad();
            UnitDodge.EvadeTarget.Init();

            AIBaseClient.OnPlayAnimation += (s, g) => {
                if (s.IsMe)
                {
                    if (g.Animation.Equals("Spell1") || g.Animation.Equals("Spell3_02"))
                    {
                        g.Process = false;
                    }
                }
            };
            GameEvent.OnGameTick += QLogic;
            Game.OnUpdate += GameOnUpdate;
            AIBaseClient.OnBuffRemove += OnBuffRemove;
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            GameObject.OnCreate += (sender, args) => {
                if (sender.Name.StartsWith("Irelia") && sender.Name.EndsWith("_R_cas"))
                {
                    CastRForE = false;
                }
                if (sender.Name.StartsWith("Irelia") && sender.Name.EndsWith("_E_cas_02"))
                {
                    CastEForR = false;
                }
            };
            GameObject.OnDelete += (sender, args) =>
            {
                if (sender.Name.StartsWith("Irelia") && sender.Name.EndsWith("_R_Mis"))
                {
                    CastRForE = true;
                }
                if (sender.Name.StartsWith("Irelia") && sender.Name.EndsWith("_E_Mis_02"))
                {
                    CastEForR = true;
                }

            };
            Render.OnEndScene += (args) =>
            {
                if (IsDrawQ && Q.IsReady())
                    PlusRender.DrawCircle(Player.Position, Q.Range, Color.Orange);
                if (IsDrawW && W.IsReady())
                    PlusRender.DrawCircle(Player.Position, W.Range, Color.Green);
                if (IsDrawE && E.IsReady())
                    PlusRender.DrawCircle(Player.Position, E.Range, Color.Red);
                if (IsDrawR && R.IsReady())
                    PlusRender.DrawCircle(Player.Position, R.Range, Color.Gainsboro);

                var oos = Cache.GetMinions(Player.Position, Q.Range + 300f);
                oos.AddRange(Cache.EnemyHeroes.Where(x => x.IsValidTarget()));
                foreach (var obj in oos)
                {
                    var Damage = GetQDmg(obj);
                    var draws = Drawing.WorldToScreen(obj.Position);
                    if(obj.GetRealHeath(DamageType.Physical) < Damage)
                    {
                        PlusRender.DrawCircle(obj.Position, obj.BoundingRadius, Q.IsInRange(obj) ? Color.White : Color.Orange);
                    }
                } 
            };
            Interrupter.OnInterrupterSpell += (sender, args) =>
            {
                if (sender.IsEnemy)
                {
                    if (Interrupters && sender.IsValidTarget(E.Range))
                    {
                        if (Player.HasBuff("IreliaE") && PosE.IsValid())
                        {
                            var predPos = GetE2Prediction(sender);
                            if (predPos.Hitchance >= HitChance.High)
                            {
                                if (predPos.CastPosition.DistanceToPlayer() <= E.Range)
                                {
                                    E.Cast(predPos.CastPosition);
                                }
                            }
                        }
                    }
                }
            };
        }
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Irelia));
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo"));
            {
                Combo.Add(new MenuBool("CQ", "Use Q"));
                Combo.Add(new MenuBool("CQGap", Program.Chinese ? "->使用 Q 小兵突进" : "->Use Minion Gap"));
                Combo.Add(new MenuSlider("CQMinionsHealth", Program.Chinese ? "->当自身血量低于X%时Q小兵" : "-> Q Minion if My Health <= X%", 45, 0, 100));
                Combo.Add(new MenuBool("ForceQ", Program.Chinese ? "单挑时允许断Q击杀" : "-> (1v1)if can Kill. ForceQ + A Damage To Kill"));
                Combo.Add(new MenuBool("CW", "Use W"));
                Combo.Add(new MenuBool("CWQ", Program.Chinese ? "尝试 W + Q 击杀小兵" : "Try W + Q Killable Minion"));
                Combo.Add(new MenuSlider("CWTick", Program.Chinese ? "W 蓄力 X秒" : " W Charnge Time", 100, 0, 1500));
                Combo.Add(new MenuBool("CWOnlyMarks", Program.Chinese ? "->仅用于叠被动" : "Only add Passive Count"));
                Combo.Add(new MenuBool("CE", "Use E"));
                Combo.Add(new MenuKeyBind("CR", "Use R", Keys.A, KeyBindType.Toggle)).AddPermashow();
                Combo.Add(new MenuSlider("CRCheakHealth", Program.Chinese ? "->当敌人血量低于X时释放R" : "->When Target HP <= X%", 25, 0, 100));
                Combo.Add(new MenuBool("CROnlyKill", Program.Chinese ? "->仅击杀使用" : "Only Can Killable Cast"));
                Combo.Add(new MenuBool("CRWaitQ", Program.Chinese ? "->Q没好时不要用R" : "-> if Q not Ready. Don't Cast R.", false));
                Combo.Add(new MenuKeyBind("CRKEY", Program.Chinese ? "半自动R热键" : "Smart R Key", EnsoulSharp.SDK.MenuUI.Keys.T, KeyBindType.Press)).AddPermashow();
            }
            var Harass = ChampionMenu.Add(new Menu("Harass", "Harass"));
            {
                Harass.Add(new MenuBool("HQ", "Use Q"));
                Harass.Add(new MenuBool("HQAA", Program.Chinese ? "^-使用Q攻击英雄仅在AA过后(EAQ)" : "-> Attack Hero Use AA (EAQ)"));
                Harass.Add(new MenuBool("HQFarm", Program.Chinese ? "^-使用Q补小兵" : "Use Q Kill Minon"));
                Harass.Add(new MenuSlider("HQFarmMagic", Program.Chinese ? "^-当蓝量 < X% 时不用Q补兵" : "Dont' Kill minion if Mana <= X%", 30, 0, 100));
                Harass.Add(new MenuBool("HE", "Use E"));
            }
            var LanceClear = ChampionMenu.Add(new Menu("LaneClear", "LaneClear"));
            {
                LanceClear.Add(new MenuBool("LQ", "Use Q"));
                LanceClear.Add(new MenuSlider("LQMagic", Program.Chinese ? "当蓝量 < X% 时不清线野" : "Don't Laneclear/JungleClear if Mana <= X%", 20, 0, 100));
            }
            var JungleClear = ChampionMenu.Add(new Menu("JungleClear", "JungleClear"));
            {
                JungleClear.Add(new MenuBool("JQ", "Use Q"));
                JungleClear.Add(new MenuBool("JE", "Use E"));
            }
            var Flee = ChampionMenu.Add(new Menu("Flee", "Flee"));
            {
                Flee.Add(new MenuBool("FQ", "Use Q"));
                Flee.Add(new MenuBool("FE", "Use E"));
            }
            var Drawing = ChampionMenu.Add(new Menu("Drawing", "Draw"));
            {
                Drawing.Add(new MenuBool("DQ", "Draw Q"));
                Drawing.Add(new MenuBool("DW", "Draw W"));
                Drawing.Add(new MenuBool("DE", "Draw E"));
                Drawing.Add(new MenuBool("DR", "Draw R"));
            }
            var Other = ChampionMenu.Add(new Menu("Other", "Misc"));
            {

                Other.Add(new MenuBool("EInterrupter", Program.Chinese ?"使用 E 打断技能":"Use E Interrupt"));
                Other.Add(new MenuKeyBind("OQUnderTower", Program.Chinese ? "是否进入塔下" : "UnterTower", EnsoulSharp.SDK.MenuUI.Keys.A, KeyBindType.Toggle)).AddPermashow();
            }
        }
        private static void OnBuffRemove(AIBaseClient sender, AIBaseClientBuffRemoveEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.Buff.Name == "sheen")
                    SheenTimer = Variables.TickCount + 1800f;
                if (args.Buff.Name == "6632buff")
                    DivineTimer = Variables.TickCount + 1800f;
                if (args.Buff.Name == "3078trinityforce")
                    TrinityTimer = Variables.TickCount + 1800f;
            }
        }
        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name == "IreliaW")
                {
                    ChargingW = Variables.TickCount;
                }
                if (args.SData.Name == "IreliaE")
                {
                    if (!Player.IsWindingUp)
                    {
                        //Game.SendEmote(EmoteId.Dance);
                    }
                    FirstE = 0;
                    PosE = new Vector3(0, 0, 0);
                    FirstE = 1;
                }
                if (args.SData.Name == "IreliaEMissile" && FirstE == 1)
                {
                    PosE = args.End;
                    EcastTime = Variables.TickCount;
                }
                if (args.SData.Name == "IreliaE2")
                {
                    if (!Player.IsWindingUp)
                    {
                        Game.SendEmote(EmoteId.Dance);
                    }
                    FirstE = 0;
                    PosE = new Vector3(0, 0, 0);
                }
            }
        }
        private void GameOnUpdate(EventArgs args)
        {
            if (RActive && R.IsReady())
            {
                CastR();
            }
            CheckItem();
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
                case OrbwalkerMode.LaneClear:
                    JungleClearE();
                    break;
                case OrbwalkerMode.LastHit:
                    break;
                case OrbwalkerMode.Flee:
                    FleeLogic();
                    break;
            }
        }
        private void CastR()
        {
            var target = TargetSelector.GetTarget(R.Range, DamageType.Physical);
            if (target.IsValidTarget(R.Range) && R.IsReady())
            {
                var pos = R.GetPrediction(target);
                if (!pos.CastPosition.IsZero && pos.CastPosition.Distance(target) < R.Range && pos.Hitchance >= HitChance.VeryHigh)
                {
                    R.Cast(pos.CastPosition);
                }
            }
        }
        private void JungleClearE()
        {
            if (!JungleUseE || !Enable_laneclear || Player.ManaPercent <= LaneClearUseQMana)
                return;

            var jungle = Cache.GetJungles(Player.ServerPosition, E.Range).Where(x => x.GetJungleType() >= JungleType.Large).FirstOrDefault();
            if (jungle != null)
            {
                if (E1Delay < Variables.TickCount && E.Name == "IreliaE" && E.IsReady())
                {
                    Vector3 pathStartPos = jungle.GetWaypoints().FirstOrDefault().ToVector3();
                    Vector3 PathEndPos = jungle.GetWaypoints().LastOrDefault().ToVector3();
                    Vector3 pathNorm = (PathEndPos - pathStartPos).Normalized();
                    var tempPred = Prediction.GetPrediction(jungle, 1.2f);

                    if (jungle.Path.Count() == 0 || !jungle.IsMoving)
                    {
                        if (jungle.DistanceToPlayer() <= E.Range)
                        {
                            Vector3 castl = Player.ServerPosition + (jungle.ServerPosition - Player.ServerPosition).Normalized() * 900f;
                            E.Cast(castl);
                            E1Delay = Variables.TickCount + 1500;
                        }
                    }
                    else
                    {
                        if (tempPred != new PredictionOutput())
                        {
                            var distl = Player.Distance(tempPred.CastPosition);
                            if (distl <= E.Range)
                            {
                                var dist2 = Player.Distance(jungle.ServerPosition);
                                if (distl < dist2)
                                {
                                    pathNorm = pathNorm * -1;
                                }
                                var Cast2 = RaySetDist(jungle.ServerPosition, pathNorm, Player.ServerPosition, E.Range);
                                E.Cast(Cast2);
                                E1Delay = Variables.TickCount + 1500;
                            }
                        }
                    }
                }
                if (PosE.IsValid())
                {
                    if (!jungle.HasBuff("ireliamark") && Player.HasBuff("IreliaE"))
                    {
                        var herosPos = GetE2Prediction(jungle);
                        if (herosPos.Hitchance >= HitChance.High)
                        {
                            var EPOS2 = PosE + (herosPos.CastPosition - PosE).Normalized() * (herosPos.CastPosition.Distance(PosE) + 500);
                            E.Cast(EPOS2);
                        }

                    }
                }
            }

        }
        private void QLogic_Minions(AIBaseClient Targets)
        {
            //如果目标没有被控制
            if (CanCastQ(Targets) && Player.ManaPercent < Q.Instance.ManaCost * 2)
            {
                Q.CastOnUnit(Targets);

                return;
            }
            int passiveCount = Player.GetBuffCount("ireliapassivestacks");
            if (Player.HealthPercent <= ComboUseQMinionsHealth || passiveCount < 4)
            {
                var RepardHealth = Cache.GetMinions(Player.ServerPosition, Q.Range).Where(x => x.IsValidTarget(Q.Range) && x.GetRealHeath(DamageType.Physical) < GetQDmg(x) &&
                x.Distance(Targets) < Player.GetRealAutoAttackRange(Targets) + 15f).FirstOrDefault();
                if (RepardHealth != null)
                {
                    Q.CastOnUnit(RepardHealth);
                }
            }
        }
        private void QLogic(EventArgs args)
        {
            if (ComboUseQ && Orbwalker.ActiveMode == OrbwalkerMode.Combo && Q.IsReady(500))
            {
                int passiveCount = Player.GetBuffCount("ireliapassivestacks");
                var CanQHeroObjList = Cache.EnemyHeroes.Where(x => x.IsValidTarget(Q.Range) && CanCastQ(x)).ToList();
                var Targets = CanQHeroObjList.Count > 0 ? CanQHeroObjList.MaxOrDefault(x => TargetSelector.GetPriority(x)) : TargetSelector.GetTarget(1000f, DamageType.Physical);
                if (Targets != null)
                {
                    if (Targets.IsValidTarget(Q.Range) && CanCastQ(Targets) && CheckTower(Targets))
                    {
                        if (Targets.InAutoAttackRange(Targets.BoundingRadius + 50f) && Player.CountEnemyHerosInRangeFix(600) <= 1 && Player.HealthPercent > ComboUseQMinionsHealth)
                        {
                            if (GetStunDuration(Targets) <= 0 || GetPassiveDuration(Targets) <= 200)
                            {
                                Q.CastOnUnit(Targets);
                                return;
                            }
                        }
                        else
                        {
                            Q.CastOnUnit(Targets);
                            return;
                        }
                    }
                    else if (ComboUseQGap && (!Targets.IsValidTarget(Q.Range) || (!CanCastQ(Targets) && !Targets.InAutoAttackRange())))
                    {
                        var gapclosingMinion =
                                        Cache.GetMinions(Player.ServerPosition, Q.Range)
                                            .Where(
                                                m => m.IsValidTarget(Q.Range) && m.GetRealHeath(DamageType.Physical) < GetQDmg(m) &&
                                                    m.Distance(Targets) <
                                                    Targets.DistanceToPlayer() && CheckTower(m));

                        if (gapclosingMinion != null && gapclosingMinion.Count() > 0)
                        {
                            bool isCastQ = false;
                            if ((passiveCount >= 3 || (passiveCount >= 2 && CanCastQ(Targets))) && Player.HealthPercent > ComboUseQMinionsHealth)
                            {
                                Q.Cast(gapclosingMinion.MinOrDefault(m => m.Distance(Targets)));
                                isCastQ = true;
                            }
                            else
                            {
                                Q.Cast(gapclosingMinion.MinOrDefault(m => m.DistanceToPlayer()));
                                isCastQ = true;
                            }
                            if (Player.Mana >= Q.Instance.ManaCost * 2 + E.Instance.ManaCost)
                            {
                                if (isCastQ && ComboUseE && E.IsReady() && !PosE.IsValid() && E1Delay < Variables.TickCount)
                                {
                                    var GrassObj = GameObjects.Get<GrassObject>().Where(x => x.DistanceToPlayer() <= E.Range).MaxOrDefault(x => x.Distance(Targets));
                                    var fastGapMinion = gapclosingMinion.Where(x => x.DistanceToPlayer() > 300).MinOrDefault(m => m.Distance(Targets));
                                    if (GrassObj == null)
                                    {
                                        //获取一个墙体
                                        var firstWall = new Geometry.Circle(Player.ServerPosition, E.Range).Points.Where(x => x.IsWall() || x.IsBuilding()).MaxOrDefault(x => x.Distance(Targets));
                                        if (firstWall == null)
                                        {
                                            //如果没获取到
                                            if(fastGapMinion != null)
                                            {
                                                //如果有最佳突进目标时 放在脚底下
                                                if (E.Cast(fastGapMinion.ServerPosition))
                                                {
                                                    E1Delay = Variables.TickCount + 1500;
                                                    return;
                                                }
                                            }
                                            if (E.Cast(Player.ServerPosition))
                                            {
                                                E1Delay = Variables.TickCount + 1500;
                                                return;
                                            }
                                        }
                                        if(fastGapMinion != null && firstWall.Distance(Targets) > fastGapMinion.Distance(Targets))
                                        {
                                            if (E.Cast(fastGapMinion.ServerPosition))
                                            {
                                                E1Delay = Variables.TickCount + 1500;
                                                return;
                                            }
                                        }
                                        if (E.Cast(firstWall))
                                        {
                                            E1Delay = Variables.TickCount + 1500;
                                            return;
                                        }
                                    }
                                    if (fastGapMinion != null && fastGapMinion.Distance(Targets) < GrassObj.Distance(Targets))
                                    {
                                        if(E.Cast(fastGapMinion.ServerPosition))
                                        {
                                            E1Delay = Variables.TickCount + 1500;
                                            return;
                                        }
                                    }
                                    if (E.Cast(GrassObj.Position))
                                    {
                                        E1Delay = Variables.TickCount + 1500;
                                    }
                                }
                            }
                        }
                    }
                    QLogic_Minions(Targets);
                }
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && Q.IsReady(500) && Player.ManaPercent > LaneClearUseQMana && Enable_laneclear)
            {
                if (LaneClearUseQ)
                {
                    var KillMinion = Cache.GetMinions(Player.ServerPosition, Q.Range).Where(x => x.GetRealHeath(DamageType.Physical) < GetQDmg(x) && CheckTower(x)).FirstOrDefault();
                    if (KillMinion != null)
                    {
                        Q.CastOnUnit(KillMinion);
                    }
                }
                if (JungleUseQ)
                {
                    var KillMinion = Cache.GetJungles(Player.ServerPosition, Q.Range).Where(x => CanCastQ(x) && CheckTower(x)).FirstOrDefault();
                    if (KillMinion != null)
                    {
                        Q.CastOnUnit(KillMinion);
                    }
                }
            }
        }
        private void FleeLogic()
        {
            var target = Cache.EnemyHeroes.Where(x => x.IsEnemy && x.IsValidTarget(E.Range) && !x.HaveSpellShield()).MinOrDefault(z => z.DistanceToPlayer());
            if (FleeUseQ && Q.IsReady(500))
            {
                var minion = Cache.GetMinions(Player.ServerPosition, Q.Range).Where(x => x.GetRealHeath(DamageType.Physical) < GetQDmg(x) && x.DistanceToPlayer() < Game.CursorPos.DistanceToPlayer()).MinOrDefault(x => x.DistanceToCursor());
                if (minion != null)
                {
                    Q.Cast(minion);
                }
                var jungles = Cache.GetJungles(Player.ServerPosition, Q.Range).Where(x => x.GetRealHeath(DamageType.Physical) < GetQDmg(x) && x.DistanceToPlayer() < Game.CursorPos.DistanceToPlayer()).MinOrDefault(x => x.DistanceToCursor());
                if (jungles != null)
                {
                    Q.Cast(jungles);
                }
            }
            if (FleeUseE && target != null)
            {
                var enemy = TargetSelector.GetTargets(E.Range, DamageType.Physical).MinOrDefault(y => y.DistanceToPlayer()); //获取最小单位

                if (enemy.IsValidTarget(E.Range)) //连招E1
                {
                    if (E1Delay < Variables.TickCount && E.Name == "IreliaE" && E.IsReady())
                    {
                        Vector3 pathStartPos = enemy.GetWaypoints().FirstOrDefault().ToVector3();
                        Vector3 PathEndPos = enemy.GetWaypoints().LastOrDefault().ToVector3();
                        Vector3 pathNorm = (PathEndPos - pathStartPos).Normalized();
                        var tempPred = Prediction.GetPrediction(enemy, 0.25f);

                        if (enemy.Path.Count() == 0 || !enemy.IsMoving)
                        {
                            if (enemy.DistanceToPlayer() <= E.Range)
                            {
                                Vector3 castl = Player.ServerPosition + (enemy.ServerPosition - Player.ServerPosition).Normalized() * 900f;
                                E.Cast(castl);
                                E1Delay = Variables.TickCount + 1500;
                            }
                        }
                        else
                        {
                            if (tempPred != new PredictionOutput())
                            {
                                var distl = Player.Distance(tempPred.CastPosition);
                                if (distl <= E.Range)
                                {
                                    var dist2 = Player.Distance(enemy.ServerPosition);
                                    if (distl < dist2)
                                    {
                                        pathNorm = pathNorm * -1;
                                    }
                                    var Cast2 = RaySetDist(enemy.ServerPosition, pathNorm, Player.ServerPosition, E.Range);
                                    E.Cast(Cast2);
                                    E1Delay = Variables.TickCount + 1500;
                                }
                            }
                        }
                    }
                    if (PosE.IsValid())
                    {
                        if (!enemy.HasBuff("ireliamark") && Player.HasBuff("IreliaE"))
                        {
                            var herosPos = GetE2Prediction(enemy);
                            if (herosPos.Hitchance >= HitChance.High)
                            {
                                var Offset = PosE.Distance(herosPos.CastPosition);
                                var HeOffset = E.Range - Player.Distance(herosPos.CastPosition);

                                var Extendd = PosE.Extend(herosPos.CastPosition, Offset + HeOffset);
                                if (PointInE2Circle(Extendd))
                                    E.Cast(Extendd);
                                else
                                    E.Cast(herosPos.CastPosition);
                            }

                        }
                    }

                }
            }
        }
        private void Harass()
        {
            if (HarassUseQ && !Player.IsWindingUp)
            {
                if (HarassUseQFarm)
                {
                    if (HarassUseQFarmMagic < Player.ManaPercent)
                    {
                        var Minions = Cache.GetMinions(Player.ServerPosition, Q.Range).Where(x => x.IsValidTarget(Q.Range) && x.GetRealHeath(DamageType.Physical) < GetQDmg(x) && !x.IsUnderEnemyTurret()).MinOrDefault(x => x.DistanceToPlayer());
                        if (Minions.IsValidTarget())
                        {
                            Q.CastOnUnit(Minions);
                        }
                    }
                }
                if (HarassUseQAA)
                {
                    var hero = Cache.EnemyHeroes.Where(x => x.IsValidTarget(Q.Range) && CanCastQ(x)).FirstOrDefault();
                    if (hero.IsValidTarget() && !hero.IsUnderEnemyTurret())
                    {
                        Q.CastOnUnit(hero);
                    }
                }
            }
            if (HarassUseE)
            {
                var enemy = TargetSelector.GetTargets(E.Range, DamageType.Physical).MinOrDefault(y => y.DistanceToPlayer()); //获取最小单位

                if (enemy.IsValidTarget(E.Range)) //连招E1
                {
                    if (E1Delay < Variables.TickCount && E.Name == "IreliaE" && E.IsReady())
                    {
                        Vector3 pathStartPos = enemy.GetWaypoints().FirstOrDefault().ToVector3();
                        Vector3 PathEndPos = enemy.GetWaypoints().LastOrDefault().ToVector3();
                        Vector3 pathNorm = (PathEndPos - pathStartPos).Normalized();
                        var tempPred = Prediction.GetPrediction(enemy, 0.25f);

                        if (enemy.Path.Count() == 0 || !enemy.IsMoving)
                        {
                            if (enemy.DistanceToPlayer() <= E.Range)
                            {
                                Vector3 castl = Player.ServerPosition + (enemy.ServerPosition - Player.ServerPosition).Normalized() * 900f;
                                E.Cast(castl);
                                E1Delay = Variables.TickCount + 1500;
                            }
                        }
                        else
                        {
                            if (tempPred != new PredictionOutput())
                            {
                                var distl = Player.Distance(tempPred.CastPosition);
                                if (distl <= E.Range)
                                {
                                    var dist2 = Player.Distance(enemy.ServerPosition);
                                    if (distl < dist2)
                                    {
                                        pathNorm = pathNorm * -1;
                                    }
                                    var Cast2 = RaySetDist(enemy.ServerPosition, pathNorm, Player.ServerPosition, E.Range);
                                    E.Cast(Cast2);
                                    E1Delay = Variables.TickCount + 1500;
                                }
                            }
                        }
                    }
                    if (PosE.IsValid())
                    {
                        if (!enemy.HasBuff("ireliamark") && Player.HasBuff("IreliaE"))
                        {
                            var herosPos = GetE2Prediction(enemy);
                            if (herosPos.Hitchance >= HitChance.High)
                            {
                                var Offset = PosE.Distance(herosPos.CastPosition);
                                var HeOffset = E.Range - Player.Distance(herosPos.CastPosition);

                                var Extendd = PosE.Extend(herosPos.CastPosition, Offset + HeOffset);
                                if (PointInE2Circle(Extendd))
                                    E.Cast(Extendd);
                                else
                                    E.Cast(herosPos.CastPosition);
                            }

                        }
                    }

                }
            }
        }
        private bool PointInE2Circle(Vector3 point)
        {
            var Circle = new Geometry.Circle(Player.ServerPosition, E.Range);
            if (Circle.IsInside(point))
            {
                return true;
            }
            return false;
        }
        private void Combo()
        {
            if (ComboUseE && E.IsReady())
            {
                var Eenemy = TargetSelector.GetTarget(E.Range, DamageType.Physical); //获取最小单位
                if (Eenemy != null)
                {
                    if (E1Delay < Variables.TickCount && E.Name == "IreliaE" && !PosE.IsValid())
                    {
                        var way = Eenemy.GetWaypoints();
                        Vector3 pathStartPos = way.FirstOrDefault().ToVector3();
                        Vector3 PathEndPos = way.LastOrDefault().ToVector3();
                        Vector3 pathNorm = (PathEndPos - pathStartPos).Normalized();
                        var tempPred = E.GetPrediction(Eenemy).CastPosition;
                        if (Eenemy.Path.Count() == 0 || !Eenemy.IsMoving)
                        {
                            if (Eenemy.DistanceToPlayer() <= E.Range)
                            {
                                Vector3 castl = Player.ServerPosition.Extend(Eenemy.ServerPosition, -E.Range);
                                if (E.Cast(castl))
                                {
                                    E1Delay = Variables.TickCount + 1500;
                                    return;
                                }
                            }
                        }
                        else
                        {
                            var distl = Player.Distance(tempPred);
                            if (distl <= E.Range)
                            {
                                var dist2 = Player.Distance(Eenemy.ServerPosition);
                                if (distl < dist2)
                                {
                                    pathNorm *= -1;
                                }
                                var Cast2 = RaySetDist(Eenemy.ServerPosition, pathNorm, Player.ServerPosition, E.Range);
                                if (E.Cast(Cast2))
                                {
                                    E1Delay = Variables.TickCount + 1500;
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            if (ComboUseR && R.IsReady() && CastEForR)
            {
                var target = TargetSelector.GetTarget(R.Range, DamageType.Physical); //获取最小单位
                if (target != null && !target.HasBuff("ireliamark"))
                {
                    if (!ComboUseRWaitQ || Q.IsReady(500))
                    {
                        var Health = HealthPrediction.GetPrediction(target, (int)(target.DistanceToPlayer() / R.Speed * 1000), 250);
                        if (ComboUseROnlyCanKill ? GetQDmg(target) + R.GetDamage(target) * 2 + E.GetDamage(target) >= Health : target.HealthPercent <= ComboUseRCheakHealth && GetQDmg(target) < Health)
                        {
                            var pos = R.GetPrediction(target, false, -1, new CollisionObjects[] { CollisionObjects.YasuoWall, CollisionObjects.Heroes });
                            if (!pos.CastPosition.IsZero && pos.CastPosition.Distance(target) < R.Range && pos.Hitchance >= HitChance.VeryHigh)
                            {
                                R.Cast(pos.CastPosition);
                            }
                        }
                    }
                }
            }
            if (ComboUseW)
            {
                var wtarget = TargetSelector.GetTarget(800, DamageType.Physical);
                if (wtarget != null)
                {
                    if (W.IsReady() && !W.IsCharging && (!ComboUseWOnlyMarks || !Player.HasBuff("ireliapassivestacksmax")))
                    {
                        if (!Q.IsReady() || (Q.IsReady() && wtarget.DistanceToPlayer() < (300 + Player.BoundingRadius)))
                        {
                            W.StartCharging();
                        }
                    }
                    if (W.IsCharging) //
                    {
                        var posnext = Prediction.GetPrediction(wtarget, 0.25f);
                        float ChargingWTime = Variables.TickCount - ChargingW;
                        if (posnext != null && (ChargingWTime >= ComboUseWTick ||
                            wtarget.DistanceToPlayer() <= 800 && !(posnext.UnitPosition.DistanceToPlayer() <= 800) ||
                            PosE.IsValid() && Variables.TickCount - EcastTime > 2800))
                        {
                            if (ComboUseWQ && ComboUseQ && Q.IsReady(250))
                            {
                                var CanQWKillableMinions = Cache.GetMinions(Player.ServerPosition, 800).Where(x => x.Distance(wtarget) < wtarget.DistanceToPlayer() && x.GetRealHeath(DamageType.Physical) < (GetWDmg(x, ChargingWTime / 1000) + GetQDmg(x)) && x.GetRealHeath(DamageType.Physical) > GetWDmg(x, ChargingWTime / 1000)).FirstOrDefault();
                                if ((wtarget.GetRealHeath(DamageType.Physical) > GetWDmg(wtarget, ChargingWTime / 1000) + Player.GetAutoAttackDamage(wtarget) || (wtarget.GetRealHeath(DamageType.Physical) > Player.Health && Player.HealthPercent <= ComboUseQMinionsHealth)))
                                {
                                    W.ShootChargedSpell(CanQWKillableMinions.ServerPosition);
                                    return;
                                }
                            }
                            W.ShootChargedSpell(posnext.CastPosition);
                        }
                    }
                }
            }
            if (ComboUseE && E.IsReady())
            {
                var Eenemy = TargetSelector.GetTarget(E.Range, DamageType.Physical); //获取最小单位
                if (Eenemy != null)
                {
                    if (CastRForE && Player.HasBuff("IreliaE") && PosE.IsValid())
                    {
                        bool nonhavebuff = Cache.EnemyHeroes.Where(x => x.IsValidTarget(E.Range) && x.HasBuff("ireliamark") && (Q.IsReady(500) || Player.IsDashing())).Any();
                        if (!nonhavebuff)
                        {
                            var targets = TargetSelector.GetTargets(E.Range, DamageType.Physical);
                            if (targets.Count != 0)
                            {
                                var preds =
                                targets.Select(i => GetE2Prediction(i))
                                    .Where(
                                        i =>
                                        i.Hitchance >= HitChance.High && i.CastPosition.DistanceToPlayer() <= E.Range)
                                    .ToList();
                                if (preds.Count > 0)
                                {
                                    var castPos = preds.MaxOrDefault(i => i.AoeTargetsHitCount).CastPosition;
                                    var Offset = PosE.Distance(castPos);
                                    var HeOffset = E.Range - Player.Distance(castPos);

                                    var Extendd = PosE.Extend(castPos, Offset + HeOffset);
                                    if (PointInE2Circle(Extendd))
                                        E.Cast(Extendd);
                                    else
                                        E.Cast(castPos);
                                }
                            }
                        }
                    }
                }
            }
        }
        private bool CheckTower(AIBaseClient target)
        {
            if (!target.IsUnderEnemyTurret() || IsUnderTower)
            {
                return true;
            }
            return false;
        }
        private PredictionOutput GetE2Prediction(AIBaseClient Unit)
        {
            var SecondE = new Spell(SpellSlot.E, 50000f);
            SecondE.SetSkillshot(0.72f, 70f, int.MaxValue, false, SpellType.Line);
            SecondE.UpdateSourcePosition(PosE, PosE);
            var pred = SecondE.GetPrediction(Unit, true);
            return pred;
        }
        private Vector3 RaySetDist(Vector3 start, Vector3 path, Vector3 center, float dist)
        {
            var a = start.X - center.X;
            var b = start.Y - center.Y;
            var c = start.Z - center.Z;
            var x = path.X;
            var y = path.Y;
            var z = path.Z;
            var nl = a * x + b * y + c * z;
            //var n2 = z * z * dist * dist - a * a * z * z - b * b * z * z + 2 * a * c * x * z + 2 * b * c * y * z + 2 * a * b * x * y + dist * dist * x * x + dist * dist * y * y - a * a * y * y - b * b * x * x - c * c * x * x - c * c * y * y;
            var n2 =
                Math.Pow(z, 2) * Math.Pow(dist, 2) - Math.Pow(a, 2) * Math.Pow(z, 2) - Math.Pow(b, 2) * Math.Pow(z, 2) + 2 * a * c * x * z + 2 * b * c * y * z + 2 * a * b * x * y +
                Math.Pow(dist, 2) * Math.Pow(x, 2) +
                Math.Pow(dist, 2) * Math.Pow(y, 2) -
                Math.Pow(a, 2) * Math.Pow(y, 2) -
                Math.Pow(b, 2) * Math.Pow(x, 2) -
                Math.Pow(c, 2) * Math.Pow(x, 2) -
                Math.Pow(c, 2) * Math.Pow(y, 2);
            var n3 = Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2);
            var r1 = -(nl + Math.Sqrt(n2)) / n3;
            var r2 = -(nl - Math.Sqrt(n2)) / n3;
            var r = Math.Max(r1, r2);
            Vector3 retPos;
            retPos.X = start.X + (float)r * path.X;
            retPos.Y = start.Y + (float)r * path.Y;
            retPos.Z = start.Z + (float)r * path.Z;

            if(retPos.Distance(start) <= 350f)
            {
                retPos = Player.ServerPosition.Extend(retPos, -E.Range);
            }
            return retPos;
        }
        private float GetStunDuration(AIBaseClient target)
        {
            if (target == null || target.IsDead)
            {
                return 0f;
            }

            return (float)(target.Buffs.Where(b => b.IsActive && Game.Time < b.EndTime &&
                                                 (b.Type == BuffType.Charm ||
                                                  b.Type == BuffType.Knockback ||
                                                  b.Type == BuffType.Stun ||
                                                  b.Type == BuffType.Suppression ||
                                                  b.Type == BuffType.Snare)).Aggregate(0f, (current, buff) => Math.Max(current, buff.EndTime)) - Game.Time) * 1000;
        }
        private float GetPassiveDuration(AIBaseClient target)
        {
            if (target == null || target.IsDead)
            {
                return 0f;
            }
            return (float)(target.Buffs.Where(b => b.IsActive && Game.Time < b.EndTime &&
                                                 b.Name == "ireliamark").Aggregate(0f, (current, buff) => Math.Max(current, buff.EndTime)) - Game.Time) * 1000;
        }
        private void CheckItem()
        {
            if (Variables.TickCount > last_item_update)
            {
                RecurveBow = Player.HasItem(ItemId.Recurve_Bow);
                BladeKing = Player.HasItem(ItemId.Blade_of_The_Ruined_King);
                WitsEnd = Player.HasItem(ItemId.Wits_End);
                Titanic = Player.HasItem(ItemId.Titanic_Hydra);
                Divine = Player.HasItem(ItemId.Divine_Sunderer);
                Sheen = Player.HasItem(ItemId.Sheen);
                Black = Player.HasItem(ItemId.Black_Cleaver);
                Trinity = Player.HasItem(ItemId.Trinity_Force);
                last_item_update = Variables.TickCount + 5000;
            }
        }
        private float GetWDmg(AIBaseClient Unit, float Time)
        {
            if (!W.IsReady() && !W.IsCharging)
                return 0f;
            int BaseDamage = 0;
            float ExtraDamage = 0;
            if (Time >= 0.75f)
            {
                BaseDamage = 30 + (W.Level - 1) * 45;
                ExtraDamage = (1.2f * Player.TotalAttackDamage) + (1.2f * Player.TotalMagicalDamage);
                return (float)Player.CalculatePhysicalDamage(Unit, BaseDamage + ExtraDamage);
            }
            else
            {
                BaseDamage = 10 + (W.Level - 1) * 15;
                ExtraDamage = (0.4f * Player.TotalAttackDamage) + (0.4f * Player.TotalMagicalDamage);
                var TimeDamage = (BaseDamage + ExtraDamage) * (1 + (Time / 0.075 * 0.2));
                return (float)Player.CalculatePhysicalDamage(Unit, TimeDamage);
            }
        }
        private float GetQDmg(AIBaseClient target)
        {
            if (target == null || target.IsDead || !target.IsValid)
                return 0f;

            int Qlevel = Player.Spellbook.GetSpell(SpellSlot.Q).Level;
            if (Qlevel == 0)
                return 0f;
            float total = 0;

            int LvL = Player.Level;

            var Passive = Player.HasBuff("ireliapassivestacksmax");

            //满级被动 已修复
            if (Passive)
                total += (float)EnsoulSharp.SDK.Damage.CalculateMagicDamage(Player, target, 10 + (3 * (GameObjects.Player.Level - 1)) + 0.2 * GameObjects.Player.GetBonusPhysicalDamage());

            if (Trinity && (Variables.TickCount >= TrinityTimer || Player.HasBuff("3078trinityforce")))
            {
                //三相修复
                total += (float)EnsoulSharp.SDK.Damage.CalculatePhysicalDamage(Player, target, 2.0f * Player.BaseAttackDamage);
            }
            if (Sheen && !Trinity && (Variables.TickCount >= SheenTimer || Player.HasBuff("sheen")))
            {
                //耀光修复
                total += (float)EnsoulSharp.SDK.Damage.CalculatePhysicalDamage(Player, target, 1.0f * Player.BaseAttackDamage);
            }
            //破败不用动
            if (BladeKing)
            {
                if (target.IsJungle() || target.IsMinion())
                {
                    if (target.Health * 0.1 > 60)
                        total += (float)EnsoulSharp.SDK.Damage.CalculatePhysicalDamage(Player, target, 60f);
                    else
                        total += (float)EnsoulSharp.SDK.Damage.CalculatePhysicalDamage(Player, target, target.Health * 0.1f);
                }
                else
                {
                    total += (float)EnsoulSharp.SDK.Damage.CalculatePhysicalDamage(Player, target, target.Health * 0.1f);
                }

            }
            if (WitsEnd) //智慧末刃修复
            {
                float wedmg = 0f;
                if (LvL >= 1 && LvL <= 8)
                {
                    wedmg = 15;
                }
                else if (LvL == 9)
                {
                    wedmg = 25;
                }
                else if (LvL == 11)
                { 
                    wedmg = 45;
                }
                else if (LvL == 12)
                {
                    wedmg = 55;
                }
                else if (LvL == 13)
                {
                    wedmg = 65;
                }
                else if (LvL == 14)
                {
                    wedmg = 75;
                }
                else if (LvL == 15)
                {
                    wedmg = 76;
                }
                else if (LvL == 16)
                {
                    wedmg = 78;
                }
                else if (LvL == 17)
                {
                    wedmg = 79;
                }
                else if (LvL == 18)
                {
                    wedmg = 80;
                }
                total += (float)EnsoulSharp.SDK.Damage.CalculateMagicDamage(Player, target, wedmg);
            }
            if (RecurveBow)//反曲之弓修复
            {
                total += (float)EnsoulSharp.SDK.Damage.CalculatePhysicalDamage(Player, target, 15);
            }
            if (Titanic)
            {
                //巨型九头蛇修复
                total += (float)EnsoulSharp.SDK.Damage.CalculatePhysicalDamage(Player, target, 4 + (Player.MaxHealth * 0.015));
            }
            //神圣分离者
            if (Divine && (Variables.TickCount >= DivineTimer || Player.HasBuff("6632buff")))
            {
                if (target.Type == GameObjectType.AIHeroClient || (target.IsMinion() && !target.IsJungle()))//英雄单位时
                {
                    var endDmg_t = 0.12 * target.MaxHealth;
                    var minDamage = 1.5 * Player.BaseAttackDamage;
                    if (endDmg_t < minDamage) //如果最小伤害比正常伤害高
                    {
                        endDmg_t = minDamage;
                    }
                    total += (float)EnsoulSharp.SDK.Damage.CalculatePhysicalDamage(Player, target, endDmg_t);
                }
                else
                {
                    if (target.IsJungle())
                    {
                        var endDmg_t = 0.12 * target.MaxHealth;
                        var maxDamage = 2.5 * Player.BaseAttackDamage;
                        if (endDmg_t > maxDamage)
                        {
                            endDmg_t = maxDamage;
                        }
                        total += (float)EnsoulSharp.SDK.Damage.CalculatePhysicalDamage(Player, target, endDmg_t);
                    }
                }

            }

            var normaldmg = 5f + ((Qlevel - 1) * 20f) + (Player.TotalAttackDamage * 0.6f);
            if (target.IsMinion() && !target.IsJungle())
            {
                normaldmg = 5 + ((Qlevel - 1) * 20f) + (Player.TotalAttackDamage * 0.6f) + (55 + (12 * (Player.Level - 1)));

            }
            normaldmg = (float)EnsoulSharp.SDK.Damage.CalculatePhysicalDamage(Player, target, normaldmg);
            return total + normaldmg - 1;
        }
        private bool CanCastQ(AIBaseClient target)
        {
            if (target.IsInvulnerable || (target.HasBuff("UndyingRage") && target.HealthPercent <= 10)) 
                return false;

            if (ComboForceKill && target.Type == GameObjectType.AIHeroClient)
            {
                if (Player.CountEnemyHerosInRangeFix(Q.Range + 100) <= 1)
                {
                    if (target.GetRealHeath(DamageType.Physical) <= GetQDmg(target) + Player.GetAutoAttackDamage(target) * 1)
                    {
                        return true;
                    }
                }
            }

            if (target.HasBuff("ireliamark") || GetQDmg(target) >= target.GetRealHeath(DamageType.Physical))
            {
                return true;
            }
            return false;
        }
        private static class UnitDodge
        {
            public static class EvadeTarget
            {
                #region Static Fields

                public static bool ActiveEvade = false;

                private static readonly List<SpellData> Spells = new List<SpellData>();

                #endregion

                #region Methods
                public static void Init()
                {
                    LoadSpellData();
                    var evadeMenu = ChampionMenu.Add(new Menu("EvadeTarget", Program.Chinese ? "格挡设置" : "W Dodge"));
                    {
                        evadeMenu.Add(new MenuBool("W", "Use W Dodge Spell"));
                        var aaMenu = new Menu("AA", "Attack");
                        {
                            aaMenu.Add(new MenuBool("B", "Basic Attack Dodge"));
                            aaMenu.Add(new MenuSlider("BHpU", "-> When Health < (%)", 35));
                            aaMenu.Add(new MenuBool("C", "Cric Attack Dodge"));
                            aaMenu.Add(new MenuSlider("CHpU", "-> When Health < (%)", 40));
                            evadeMenu.Add(aaMenu);
                        }
                        foreach (var hero in
                            Cache.EnemyHeroes.Where(
                                i => i.IsEnemy &&
                                Spells.Any(
                                    a =>
                                    string.Equals(
                                        a.ChampionName,
                                        i.CharacterName,
                                        StringComparison.InvariantCultureIgnoreCase))))
                        {
                            evadeMenu.Add(new Menu(hero.CharacterName.ToLowerInvariant(), "-> " + hero.CharacterName));
                        }
                        foreach (var spell in
                            Spells.Where(
                                i =>
                                Cache.EnemyHeroes.Any(
                                    a => a.IsEnemy &&
                                    string.Equals(
                                        a.CharacterName,
                                        i.ChampionName,
                                        StringComparison.InvariantCultureIgnoreCase))))
                        {
                            ((Menu)evadeMenu[spell.ChampionName.ToLowerInvariant()]).Add(new MenuBool(
                                spell.MissileName,
                                spell.ChampionName + " (" + spell.Slot + ")",
                                true));
                            ((Menu)evadeMenu[spell.ChampionName.ToLowerInvariant()]).Add(new MenuSlider(
                                spell.MissileName + "H",
                                "^-Dodge Spell When Health =< X%",
                                spell.HealthEvade, 0, 100));
                        }
                    }
                    //Game.OnUpdate += OnUpdateTarget;
                    //AIHeroClient.OnCreate += ObjSpellMissileOnCreate;
                    //GameObject.OnDelete += ObjSpellMissileOnDelete;
                    AIHeroClient.OnDoCast += _OnDoCastBack;
                }
                private static void LoadSpellData()
                {
                    //new Spells

                    //盖伦Q
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Garen", SpellNames = new[] { "garenqattack", "沉默打击" }, Slot = SpellSlot.Q, HealthEvade = 100 });
                    //诺手R + W
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Darius", SpellNames = new[] { "dariusexecute", "诺克萨斯断头台" }, Slot = SpellSlot.R, HealthEvade = 100 });
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Darius", SpellNames = new[] { "dariusnoxiantacticsonhattack", "致残打击" }, Slot = SpellSlot.W, HealthEvade = 100 });
                    //劫 R
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Zed", SpellNames = new[] { "dariusnoxiantacticsonhattack", "瞬狱影杀阵(爆炸时" }, Slot = SpellSlot.R, HealthEvade = 100 });
                    //李青 R
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Leesin", SpellNames = new[] { "blindmonkrkick", "猛龙摆尾" }, Slot = SpellSlot.R, HealthEvade = 100 });
                    //小炮 E + R
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Tristana", SpellNames = new[] { "Tristana_Base_E_explosion".ToLower(), "爆炸火花" }, Slot = SpellSlot.E, HealthEvade = 100 });
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Tristana", SpellNames = new[] { "tristanar", "加农炮" }, Slot = SpellSlot.R, HealthEvade = 100 });
                    //皇子R
                    Spells.Add(
                        new SpellData
                        { ChampionName = "JarvanIV", SpellNames = new[] { "jarvanivcataclysm", "天崩地裂" }, Slot = SpellSlot.R, HealthEvade = 100 });
                    //蝎子R
                    Spells.Add(
                        new SpellData
                        { ChampionName = "skarner", SpellNames = new[] { "detonatingshot", "蝎子R" }, Slot = SpellSlot.R, HealthEvade = 100 });
                    //滑板鞋E
                    Spells.Add(
                        new SpellData
                        { ChampionName = "kalista", SpellNames = new[] { "detonatingshot", "拔矛" }, Slot = SpellSlot.E, HealthEvade = 100 });
                    //螳螂Q
                    Spells.Add(
                        new SpellData
                        { ChampionName = "khazix", SpellNames = new[] { "khazixq", "品尝恐惧" }, Slot = SpellSlot.Q, HealthEvade = 80 });
                    Spells.Add(
                        new SpellData
                        { ChampionName = "khazix", SpellNames = new[] { "khazixqlong", "进化 - 品尝恐惧" }, Slot = SpellSlot.Q, HealthEvade = 80 });
                    //梦魇 E + R
                    Spells.Add(
                        new SpellData
                        { ChampionName = "nocturne", SpellNames = new[] { "NocturneUnspeakableHorror".ToLower(), "无言恐惧" }, Slot = SpellSlot.E, HealthEvade = 100, Spell2Delay = 1800f });
                    Spells.Add(
                        new SpellData
                        { ChampionName = "nocturne", SpellNames = new[] { "NocturneParanoia2".ToLower(), "鬼影重重" }, Slot = SpellSlot.R, HealthEvade = 100 });
                    //狗熊 Q + W
                    Spells.Add(
                        new SpellData
                        { ChampionName = "volibear", SpellNames = new[] { "volibearqattack", "授首一击" }, Slot = SpellSlot.Q, HealthEvade = 100 });
                    Spells.Add(
                        new SpellData
                        { ChampionName = "volibear", SpellNames = new[] { "volibearw", "暴怒撕咬" }, Slot = SpellSlot.W, HealthEvade = 100 });
                    //辛吉德 E
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Singed", SpellNames = new[] { "fling", "举高高" }, Slot = SpellSlot.E, HealthEvade = 100 });
                    //机器人E
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Blitzcrank", SpellNames = new[] { "powerfistattack", "能量铁拳" }, Slot = SpellSlot.E, HealthEvade = 100 });
                    //鳄鱼W
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Renekton", SpellNames = new[] { "renektonexecute", "冷酷追捕" }, Slot = SpellSlot.W, HealthEvade = 100 });
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Renekton", SpellNames = new[] { "renektonsuperexecute", "红怒 - 冷酷追捕" }, Slot = SpellSlot.W, HealthEvade = 100 });
                    //女警R
                    Spells.Add(
                        new SpellData
                        {
                            ChampionName = "Caitlyn",
                            SpellNames = new[] { "caitlynaceintheholemissile", "完美一击" },
                            Slot = SpellSlot.R
                        });
                    //船长Q
                    Spells.Add(
                        new SpellData { ChampionName = "Gangplank", SpellNames = new[] { "parley", "枪火谈判" }, Slot = SpellSlot.Q, HealthEvade = 100 });
                    //女枪Q
                    Spells.Add(
                        new SpellData
                        {
                            ChampionName = "MissFortune",
                            SpellNames = new[] { "missfortunericochetshot", "女枪Q" },
                            Slot = SpellSlot.Q,
                            HealthEvade = 100
                        });
                    //潘森W
                    Spells.Add(
                        new SpellData { ChampionName = "Pantheon", SpellNames = new[] { "pantheonw", "斗盾跃击" }, Slot = SpellSlot.W, HealthEvade = 100 });
                    //卡牌黄牌
                    Spells.Add(
                        new SpellData
                        { ChampionName = "TwistedFate", SpellNames = new[] { "goldcardattack", "黄牌" }, Slot = SpellSlot.W, HealthEvade = 100 });
                    //薇恩E
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Vayne", SpellNames = new[] { "vaynycondemn", "恶魔审判" }, Slot = SpellSlot.E, HealthEvade = 100 });
                    //男刀Q
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Talon", SpellNames = new[] { "talonqattack", "诺克萨斯式外交" }, Slot = SpellSlot.Q, HealthEvade = 100 });
                    //蔚 R
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Vi", SpellNames = new[] { "vir", "天霸横空烈轰" }, Slot = SpellSlot.R, HealthEvade = 100 });
                    //牛头 W
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Alistar", SpellNames = new[] { "headbutt", "野蛮冲撞" }, Slot = SpellSlot.W, HealthEvade = 100 });
                    //乌迪尔 巨熊姿态
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Udyr", SpellNames = new[] { "udyrbearattack", "巨熊姿态 普攻" }, Slot = SpellSlot.E, HealthEvade = 100 });
                    //剑圣Q 双刀
                    Spells.Add(
                        new SpellData
                        { ChampionName = "MasterYi", SpellNames = new[] { "alphastrike", "阿尔法突袭" }, Slot = SpellSlot.Q, HealthEvade = 100 });
                    Spells.Add(
                        new SpellData
                        { ChampionName = "MasterYi", SpellNames = new[] { "masteryidoublestrike", "剑圣被动双刀" }, Slot = SpellSlot.Unknown, HealthEvade = 100 });
                    //狮子狗Q
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Rengar", SpellNames = new[] { "rengarqattack", "残忍无情" }, Slot = SpellSlot.Q, HealthEvade = 100 });
                    //克烈W
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Kled", SpellNames = new[] { "kledwattack", "暴烈秉性 第四下普攻" }, Slot = SpellSlot.Unknown, HealthEvade = 100 });
                    //赵信E+Q
                    Spells.Add(
                        new SpellData
                        { ChampionName = "XinZhao", SpellNames = new[] { "xinzhaoqthrust1", "三重爪击 第一段" }, Slot = SpellSlot.Q, HealthEvade = 100 });
                    Spells.Add(
                        new SpellData
                        { ChampionName = "XinZhao", SpellNames = new[] { "xinzhaoqthrust2", "三重爪击 第二段" }, Slot = SpellSlot.Q, HealthEvade = 100 });
                    Spells.Add(
                        new SpellData
                        { ChampionName = "XinZhao", SpellNames = new[] { "xinzhaoqthrust3", "三重爪击 第三段" }, Slot = SpellSlot.Q, HealthEvade = 100 });
                    Spells.Add(
                        new SpellData
                        { ChampionName = "XinZhao", SpellNames = new[] { "xinzhaoe", "无畏冲锋" }, Slot = SpellSlot.E, HealthEvade = 100 });
                    //奎因E
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Quinn", SpellNames = new[] { "quinne", "旋翔掠杀" }, Slot = SpellSlot.E, HealthEvade = 65 });
                    //奥巴马Q
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Lucian", SpellNames = new[] { "lucianq", "透体圣光" }, Slot = SpellSlot.Q, HealthEvade = 20 });
                    //杰斯Q+E
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Jayce", SpellNames = new[] { "jaycetotheskies", "锤 - 苍穹之跃" }, Slot = SpellSlot.Q, HealthEvade = 100 });
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Jayce", SpellNames = new[] { "jaycethunderingblow", "锤 - 雷霆一击" }, Slot = SpellSlot.E, HealthEvade = 100 });
                    //挖掘机E+R
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Reksai", SpellNames = new[] { "reksaie", "狂野之噬" }, Slot = SpellSlot.E, HealthEvade = 100 });
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Reksai", SpellNames = new[] { "reksair", "虚空猛冲" }, Slot = SpellSlot.R, HealthEvade = 100 });
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Reksai", SpellNames = new[] { "reksaiwburrowed", "出土时的击飞" }, Slot = SpellSlot.W, HealthEvade = 100, SpellRadius = 175f });
                    //凯隐R
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Kayn", SpellNames = new[] { "kaynr", "裂舍影" }, Slot = SpellSlot.R, HealthEvade = 100 });
                    //人马E
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Kayn", SpellNames = new[] { "hecarimrampattack", "毁灭冲锋" }, Slot = SpellSlot.R, HealthEvade = 100 });
                    //千珏E
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Kindred", SpellNames = new[] { "kindrede", "横生惧意" }, Slot = SpellSlot.E, HealthEvade = 100 });
                    //蒙多E
                    Spells.Add(
                        new SpellData
                        { ChampionName = "DrMundo", SpellNames = new[] { "drmundoeattack", "大力行医" }, Slot = SpellSlot.E, HealthEvade = 100 });
                    //猴子Q E
                    Spells.Add(
                        new SpellData
                        { ChampionName = "MonkeyKing", SpellNames = new[] { "monkeykingqattack", "粉碎打击" }, Slot = SpellSlot.Q, HealthEvade = 100 });
                    Spells.Add(
                        new SpellData
                        { ChampionName = "MonkeyKing", SpellNames = new[] { "monkeykingnimbus", "腾云突击" }, Slot = SpellSlot.E, HealthEvade = 100 });
                    //牧魂人 Q
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Yorick", SpellNames = new[] { "yorickqattack", "临终仪式" }, Slot = SpellSlot.Q, HealthEvade = 100 });
                    //波比E
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Poppy", SpellNames = new[] { "poppye", "英勇冲锋" }, Slot = SpellSlot.E, HealthEvade = 100 });
                    //狼人Q
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Warwick", SpellNames = new[] { "warwickQ", "野兽之口" }, Slot = SpellSlot.Q, HealthEvade = 100 });
                    //龙龟E
                    Spells.Add(
                        new SpellData
                        { ChampionName = "Rammus", SpellNames = new[] { "puncturingtaunt", "狂乱嘲讽" }, Slot = SpellSlot.E, HealthEvade = 100 });
                }
                private static void _OnDoCastBack(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
                {
                    if (sender != null && args != null)
                    {
                        var SpellName = args.SData.Name;
                        var HeroName = sender.CharacterName;
                        
                        if (sender is AIHeroClient && sender.IsEnemy)
                        {
                            if (!GameObjects.Player.HasBuff("ireliawdefense") && W.IsReady()) //如果没有在蓄力W
                            {
                                var spellData = Spells.FirstOrDefault(i => i.SpellNames[0].Contains(SpellName.ToLower())); //是否存在技能名
                                if (spellData != null)
                                {
                                    if (ChampionMenu["EvadeTarget"][spellData.ChampionName.ToLower()][spellData.MissileName].GetValue<MenuBool>().Enabled &&
                                        Player.HealthPercent <= ChampionMenu["EvadeTarget"][spellData.ChampionName.ToLower()][spellData.MissileName + "H"].GetValue<MenuSlider>().Value)
                                    {
                                        if (args.Target == null) //如果不是目标型技能
                                        {
                                            if (sender.DistanceToPlayer() <= spellData.SpellRadius) //目标英雄离我距离在技能范围内时
                                            {
                                                ActiveEvade = true;
                                                W.Cast(sender.ServerPosition);
                                                return;
                                            }
                                        }
                                        if (args.Target.IsMe)
                                        {
                                            if (spellData.Spell2Delay != 0) //是否为延迟类控制技能
                                            {
                                                DelayAction.Add((int)spellData.Spell2Delay, () =>
                                                {
                                                    ActiveEvade = true;
                                                    W.Cast(sender.ServerPosition);
                                                });
                                                return;
                                            }
                                            W.Cast(sender.ServerPosition);
                                        }
                                        return;
                                    }
                                }
                                if (spellData == null && Orbwalker.IsAutoAttack(args.SData.Name) && args.Target.IsMe) //如果没找到技能 而且这个是普攻
                                {

                                    if (args.SData.Name.ToLower().Contains("crit") && ChampionMenu["EvadeTarget"]["AA"]["B"].GetValue<MenuBool>().Enabled)
                                    {
                                        if (Player.HealthPercent < ChampionMenu["EvadeTarget"]["AA"]["BHpU"].GetValue<MenuSlider>().Value)
                                        {
                                            ActiveEvade = true;
                                            W.Cast(sender.ServerPosition);
                                            return;
                                        }
                                    }
                                    if (args.SData.Name.ToLower().Contains("basic") && ChampionMenu["EvadeTarget"]["AA"]["C"].GetValue<MenuBool>().Enabled)
                                    {
                                        if (Player.HealthPercent < ChampionMenu["EvadeTarget"]["AA"]["CHpU"].GetValue<MenuSlider>().Value)
                                        {
                                            ActiveEvade = true;
                                            W.Cast(sender.ServerPosition);
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }

                }

                #endregion
                private class SpellData
                {
                    #region Fields

                    public string ChampionName;

                    public SpellSlot Slot;

                    public string[] SpellNames = { };

                    public int HealthEvade;

                    public float SpellRadius;

                    public float Spell2Delay;

                    #endregion

                    #region Public Properties

                    public string MissileName => this.SpellNames.LastOrDefault();

                    #endregion
                }
            }
        }
    }
}
