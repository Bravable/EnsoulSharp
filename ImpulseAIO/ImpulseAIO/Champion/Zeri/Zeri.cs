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

namespace ImpulseAIO.Champion.Zeri
{
    internal class Zeri : Base
    {
        private static Spell NormalQ, FastQ, NonColNormalQ, NonColFastQ,FastW, WallW, E, R;
        private static Dash dash;
        private static Menu AntiGapcloserMenu;
        private int OrbAAMode => ChampionMenu["BasicAttack"]["AAMode"].GetValue<MenuList>().Index;
        private bool DisableAAHP => ChampionMenu["BasicAttack"]["DisableAAHP"].GetValue<MenuBool>().Enabled;
        private bool DisableAACN => ChampionMenu["BasicAttack"]["DisableAACN"].GetValue<MenuBool>().Enabled;
        private int ROrbAAMode => ChampionMenu["BasicAttack"]["RAAMode"].GetValue<MenuList>().Index;
        private bool RDisableAAHP => ChampionMenu["BasicAttack"]["RDisableAAHP"].GetValue<MenuBool>().Enabled;
        private bool RDisableAACN => ChampionMenu["BasicAttack"]["RDisableAACN"].GetValue<MenuBool>().Enabled;
        private bool QAA => ChampionMenu["BasicAttack"]["QAA"].GetValue<MenuBool>().Enabled;
        private bool SAFEQA => ChampionMenu["BasicAttack"]["SAFEQA"].GetValue<MenuBool>().Enabled;
        private bool ComboUseW => ChampionMenu["Combo"]["UseW"].GetValue<MenuBool>().Enabled;
        private int ComboUseE => ChampionMenu["Combo"]["EMode"].GetValue<MenuList>().Index;
        private bool ComboUseWWall => ChampionMenu["Combo"]["UseWWall"].GetValue<MenuBool>().Enabled;
        private bool ComboUseR => ChampionMenu["Combo"]["UseR"].GetValue<MenuBool>().Enabled;
        private int ComboUseRCount => ChampionMenu["Combo"]["UseRCount"].GetValue<MenuSlider>().Value;

        private bool HarassUseW => ChampionMenu["Combo"]["UseW"].GetValue<MenuBool>().Enabled;
        private bool HarassUseWWall => ChampionMenu["Combo"]["UseWWall"].GetValue<MenuBool>().Enabled;

        private bool JumpWall => ChampionMenu["JumpWall"]["Key"].GetValue<MenuKeyBind>().Active;

        private bool DrawQ => ChampionMenu["Draw"]["Q"].GetValue<MenuBool>().Enabled;
        private bool DrawW => ChampionMenu["Draw"]["W"].GetValue<MenuBool>().Enabled;
        private bool DrawR => ChampionMenu["Draw"]["R"].GetValue<MenuBool>().Enabled;
        private bool AntiGapE => AntiGapcloserMenu["antiGapE"].GetValue<MenuBool>().Enabled;

        private bool IsJieZouBuff
        {
            get
            {
                bool HaveLethalBuffs = false;
                foreach (var obj in Player.Buffs)
                {
                    if (obj.Name.Contains("LethalTempo"))
                    {
                        if (obj.Count == 6)
                        {
                            HaveLethalBuffs = true;
                            break;
                        }
                        HaveLethalBuffs = false;
                        break;
                    }
                }
                return HaveLethalBuffs;
            }
        }

        private static List<Vector3> StartVec3 = new List<Vector3>() { new Vector3(1164.776f, 455.3192f, 148.8625f),
            new Vector3(420.0431f, 816.72f, 183.5748f),
            new Vector3(630,4508,95.74805f),
            new Vector3(4776, 694,110.8725f),
            new Vector3(14468.89f, 13948.27f,166.4569f),
            new Vector3(14014, 14512,171.9777f),
            new Vector3(10572, 14356,91.42981f),
            new Vector3(14166, 10326,91.42981f),
            new Vector3(3974, 558,95.74805f),
            new Vector3(525.4004f, 3856.47f,95.74802f),
            new Vector3(10973.24f, 14356f,91.42984f),
            new Vector3(14271.69f, 11206.16f,91.42981f),
            new Vector3(7588, 2988,52.55599f),
            new Vector3(11959.84f, 7753.984f,52.33273f),
            new Vector3(11308, 5328,-57.65408f),
            new Vector3(3504f, 9616f,-33.35656f),
            new Vector3(7228f, 11924f,56.4768f),
            new Vector3(2930, 7094,50.69962f)
        };
        private static List<Vector3> EndVec3 = new List<Vector3>() { new Vector3(4344.15f, 537.4541f, 95.74805f),
            new Vector3(518.7524f, 4716.67f, 93.41431f),
            new Vector3(765.5391f, 10341.43f, 52.8374f),
            new Vector3(10689.7f,767.7703f,49.63037f),
            new Vector3(14256.93f,10190.89f,93.31934f),
            new Vector3(9547.33f, 14319.19f,55.56006f),
            new Vector3(6663.356f, 14085.54f,52.83838f),
            new Vector3(14138.41f, 5897.795f,52.70801f),
            new Vector3(883.776f, 212.4987f,174.2166f),
            new Vector3(188.0797f, 416.1705f,183.5747f),
            new Vector3(13641.23f, 14704.25f,165.5154f),
            new Vector3(14661.37f, 14413.14f,171.9775f),
            new Vector3(10283.4f, 2843.151f,49.19702f),
            new Vector3(11596.24f, 9116.619f,51.27246f),
            new Vector3(12777.13f, 3343.183f,51.36719f),
            new Vector3(2314.208f, 11500.38f,19.47461f),
            new Vector3(4703.729f, 12038.75f,56.43262f),
            new Vector3(3298.77f, 5223.558f,54.00513f)
        };

        public Zeri()
        {
            NormalQ = new Spell(SpellSlot.Q, 825f);
            NormalQ.SetSkillshot(0f, 40f, 2600f, true, SpellType.Line,HitChance.Medium);
            NonColNormalQ = new Spell(SpellSlot.Q, 825f);
            NonColNormalQ.SetSkillshot(0f, 40f, 2600f, true, SpellType.Line, HitChance.Medium);

            FastQ = new Spell(SpellSlot.Q, 825f);
            FastQ.SetSkillshot(0f, 40f, 3400f, false, SpellType.Line, HitChance.Medium);
            NonColFastQ = new Spell(SpellSlot.Q, 825f);
            NonColFastQ.SetSkillshot(0f, 40f, 3400f, false, SpellType.Line, HitChance.Medium);

            FastW = new Spell(SpellSlot.W, 1200f);
            FastW.SetSkillshot(0.6f, 40f, 2200f, true, SpellType.Line);

            WallW = new Spell(SpellSlot.W, 1500f);
            WallW.SetSkillshot(0.75f, 100f, float.MaxValue, false, SpellType.Line);
            E = new Spell(SpellSlot.E, 300f);
            R = new Spell(SpellSlot.R, 825f);
            dash = new Dash(E);
            Orbwalker.OnBeforeAttack += OnOrbwalkerBefore;
            Orbwalker.OnAfterAttack += OnOrbwalkerAfter;
            Orbwalker.OnNonKillableMinion += OnNonKillalbeMinion;
            Game.OnUpdate += OnGameUpdate;
            Render.OnEndScene += OnDraw;
            AntiGapcloser.OnGapcloser += (s, g) => {
                if (!E.IsReady() || !AntiGapE)
                {
                    return;
                }

                if(s.IsEnemy && g.StartPosition.DistanceToPlayer() > g.EndPosition.DistanceToPlayer() && g.EndPosition.DistanceToPlayer() <= 450)
                {
                    var dashPos = dash.CastDash(true);
                    if (dashPos.IsValid())
                    {
                        E.Cast(dashPos);
                    }
                }
            };
            OnMenuLoad();
        }

        private void ResetQRange()
        {
            NormalQ.Range = FastQ.Range = 825f + (IsJieZouBuff ? 75 : 0);
        }
        private bool PassiveReady()
        {
            return Player.HasBuff("zeriqpassiveready");
        }
        private bool IsZeriRBuff()
        {
            return Player.HasBuff("ZeriR");
        }
        private bool LastHitQ(AIBaseClient Unit)
        {
            if (!Unit.IsValidTarget(NormalQ.Range))
                return false;
            bool AoeMode = Player.HasBuff("zeriespecialrounds");
            CollisionObjects[] EBuffColi = new CollisionObjects[] { CollisionObjects.YasuoWall };
            CollisionObjects[] NormalColi = new CollisionObjects[] { CollisionObjects.YasuoWall, CollisionObjects.Minions };
            var col = AoeMode ? EBuffColi : NormalColi;
            NormalQ.Collision = true;
            var predNormal = NormalQ.GetPrediction(Unit, AoeMode, -1, col);
            if (predNormal.Hitchance >= HitChance.Medium)
            {
                return NormalQ.Cast(predNormal.UnitPosition);
            }
            return false;
        }
        private bool CastSmartQ(AIBaseClient Unit)
        {
            if (!Unit.IsValidTarget(NormalQ.Range)) 
                return false;

            bool AoeMode = Player.HasBuff("zeriespecialrounds");
            CollisionObjects[] EBuffColi = new CollisionObjects[] { CollisionObjects.YasuoWall };
            CollisionObjects[] NormalColi = new CollisionObjects[] { CollisionObjects.YasuoWall, CollisionObjects.Minions };
            //目标是野怪时屏蔽掉碰撞
            var Col = (Unit.IsJungle() || Unit.IsMinion()) ? null : AoeMode ? EBuffColi : NormalColi;
            FastQ.Collision = NormalQ.Collision = Col != null;
            if (IsZeriRBuff())
            {
                var preds = FastQ.GetPrediction(Unit, AoeMode, -1, EBuffColi);
                if(preds.Hitchance >= HitChance.Medium)
                {
                    //没有障碍物 直接就Q
                    return FastQ.Cast(preds.UnitPosition);
                }
                else if (preds.Hitchance == HitChance.Collision)
                {
                    var targets = TargetSelector.GetTargets(FastQ.Range, DamageType.Physical);
                    if (targets.Count != 0)
                    {
                        var noncolipreds =
                        targets.Select(i => FastQ.GetPrediction(i, AoeMode, -1, EBuffColi))
                            .Where(
                                i =>
                                i.Hitchance >= HitChance.Medium && i.CastPosition.DistanceToPlayer() <= FastQ.Range)
                            .ToList();
                        if (noncolipreds.Count > 0)
                        {
                            return FastQ.Cast(noncolipreds.MaxOrDefault(i => i.Hitchance).CastPosition);
                        }
                    }
                }
                return false;
            }
            var predNormal = NormalQ.GetPrediction(Unit, AoeMode, -1, Col);
            if (predNormal.Hitchance >= HitChance.Medium)
            {
                return NormalQ.Cast(predNormal.UnitPosition);
            }
            else if (predNormal.Hitchance == HitChance.Collision)
            {
                var targets = TargetSelector.GetTargets(NormalQ.Range, DamageType.Physical);
                if (targets.Count != 0)
                {
                    var noncolipreds =
                    targets.Select(i => NormalQ.GetPrediction(i, AoeMode, -1, Col))
                        .Where(
                            i =>
                            i.Hitchance >= HitChance.Medium && i.CastPosition.DistanceToPlayer() <= FastQ.Range)
                        .ToList();
                    if (noncolipreds.Count > 0)
                    {
                        return NormalQ.Cast(noncolipreds.MaxOrDefault(i => i.Hitchance).CastPosition);
                    }
                }
            }
            return false;
        }
        private void OnGameUpdate(EventArgs args)
        {
            if (JumpWall)
            {
                JumpWallLogic();
            }
            ResetQRange();
            FastW.Delay = 0.6f - Math.Max(0, Math.Min(0.2f, 0.02f * ((Player.AttackSpeedMod - 1) / 0.25f)));
            AutoKill();
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
                case OrbwalkerMode.LaneClear:
                case OrbwalkerMode.LastHit:
                    LastHit();
                    break;
                case OrbwalkerMode.Flee:
                    break;
            }
        }
        private void OnDraw(EventArgs args)
        {
            if(DrawQ && NormalQ.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, NormalQ.Range, Color.Red);
            }
            if (DrawW && FastW.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, NormalQ.Range, Color.Blue);
            }
            if (DrawR && R.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, R.Range, Color.Orange);
            }
            for(int i = 0; i < StartVec3.Count; i++)
            {
                PlusRender.DrawCircle(StartVec3[i], 80f, Color.CadetBlue);
                var pos = Drawing.WorldToScreen(StartVec3[i]);
            }
        }
        private void OnOrbwalkerAfter(object sender, AfterAttackEventArgs Args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                var TARGET = Args.Target as AIBaseClient;
                if (TARGET.IsValidTarget())
                {
                    if (IsZeriRBuff())
                    {
                        if (NormalQ.IsReady() && (!SAFEQA || !dash.InMelleAttackRange(Player.ServerPosition)))
                        {
                            var Target = Args.Target as AIHeroClient ?? TargetSelector.GetTarget(NormalQ.Range,DamageType.Physical);
                            if(Target != null)
                            {
                                CastSmartQ(Target);
                            }
                            
                        }
                    }
                    else
                    {
                        if (NormalQ.IsReady())
                        {
                            var Target = Args.Target as AIHeroClient ?? TargetSelector.GetTarget(NormalQ.Range, DamageType.Physical);
                            if (Target != null)
                            {
                                CastSmartQ(Target);
                            }
                        }
                        
                    }
                }
            }
        }
        private void OnOrbwalkerBefore(object sender,BeforeAttackEventArgs Args)
        {
            var NormalRange = Player.AttackRange + Player.BoundingRadius + Args.Target.BoundingRadius;
            if (IsJieZouBuff)
            {
                NormalRange = NormalRange - Args.Target.BoundingRadius - 20;
            }
            if (Args.Target.DistanceToPlayer() > NormalRange)
            {
                Args.Process = false;
                return;
            }
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                var t = Args.Target as AIBaseClient;
                if (t != null)
                {
                    if (IsZeriRBuff())
                    {
                        if (t.GetRealHeath(DamageType.Magical) <= GetAADamage(t))
                        {
                            Args.Process = true;
                            return;
                        }
                        if (SAFEQA && dash.InMelleAttackRange(Player.ServerPosition))
                        {
                            Args.Process = false;
                            return;
                        }
                        if (ROrbAAMode == 0)
                        {
                            if (!NormalQ.IsReady())
                            {
                                Args.Process = false;
                                return;
                            }
                        }
                        if (ROrbAAMode == 1)
                        {
                            if (t != null && ((RDisableAAHP && t.HealthPercent <= 35) || (RDisableAACN && PassiveReady()) || (t.GetRealHeath(DamageType.Physical) < (GetAADamage(t) + NormalQ.GetDamage(t)))) && NormalQ.IsReady())
                            {
                                Args.Process = true;
                                return;
                            }
                            Args.Process = false;
                        }
                    }
                    else
                    {
                        
                        if (t.GetRealHeath(DamageType.Magical) <= GetAADamage(t))
                        {
                            Args.Process = true;
                            return;
                        }
                        if (OrbAAMode == 0)
                        {
                            if (!NormalQ.IsReady())
                            {
                                Args.Process = false;
                                return;
                            }

                        }
                        if (OrbAAMode == 1)
                        {
                            if (t != null && ((DisableAAHP && t.HealthPercent <= 35) || (DisableAACN && PassiveReady()) || (t.GetRealHeath(DamageType.Physical) < (GetAADamage(t) + NormalQ.GetDamage(t)))) && NormalQ.IsReady())
                            {
                                Args.Process = true;
                                return;
                            }
                            Args.Process = false;
                        }
                    }
                }
            } 
            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear || Orbwalker.ActiveMode == OrbwalkerMode.LastHit || Orbwalker.ActiveMode == OrbwalkerMode.Harass)
            {
                var TARGET = Args.Target as AIBaseClient;
                if (TARGET.IsValidTarget() && TARGET.Type != GameObjectType.AIHeroClient)
                {
                    if (TARGET.Health < GetAADamage(TARGET))
                    {
                        Args.Process = true;
                        return;
                    }
                    if (QAA && NormalQ.IsReady())
                    {
                        Args.Process = true;
                        CastSmartQ(TARGET);
                        return;
                    }
                    if (!NormalQ.IsReady() && QAA)
                    {
                        Args.Process = false;
                        return;
                    }
                }
                var TURRENT = Args.Target as AITurretClient;
                if (TURRENT.IsValidTarget())
                {
                    if (QAA && NormalQ.IsReady())
                    {
                        Args.Process = false;
                        NormalQ.Cast(TURRENT.Position);
                        return;
                    }
                }
            }
        }
        private void CastWToPos(Vector3 Pos)
        {
            Player.Spellbook.CastSpell(SpellSlot.W, Pos, Pos);
        }
        private void Combo()
        {
            if (E.IsReady())
            {
                LogicE();
            }
            if (ComboUseW && FastW.IsReady())
            {
                var Target = TargetSelector.GetTarget(FastW.Range, DamageType.Physical);
                if (Target != null)
                {
                    var Pred = FastW.GetPrediction(Target, false, -1, new CollisionObjects[] { CollisionObjects.Heroes, CollisionObjects.Minions, CollisionObjects.Walls, CollisionObjects.YasuoWall });

                    if (Pred.Hitchance >= HitChance.High)
                    {
                        CastWToPos(Pred.CastPosition);
                        return;
                    }

                    if (ComboUseWWall)
                    {
                        var CollisionObjs = Pred.CollisionObjects.Count;
                        if (CollisionObjs > 0)
                        {
                            WallW.Delay = 0.75f + FastW.Delay;
                            var PREDS = WallW.GetPrediction(Target, true);
                            if (PREDS.Hitchance >= HitChance.High)
                            {
                                var FisrtWall = GetFirstWallPoint(Player.ServerPosition.ToVector2(), PREDS.UnitPosition.ToVector2());
                                if (FisrtWall != Vector2.Zero)
                                {
                                    CastWToPos(PREDS.UnitPosition);
                                }
                            }
                        }
                    }
                }
            }
            if (ComboUseR && R.IsReady())
            {
                if (Player.CountEnemyHerosInRangeFix(R.Range) >= ComboUseRCount)
                {
                    R.Cast();
                }
            }
            if (NormalQ.IsReady())
            {
                var OrbTarget = Orbwalker.GetTarget();
                if(OrbTarget != null)
                {
                    var NewObj = OrbTarget as AIBaseClient;
                    if (!IsZeriRBuff() && NewObj != null && OrbAAMode == 1 && ((DisableAAHP && NewObj.HealthPercent <= 35) || (DisableAACN && PassiveReady()) || (NewObj.GetRealHeath(DamageType.Physical) < (GetAADamage(NewObj) + NormalQ.GetDamage(NewObj)))))
                    {
                        return;
                    }
                    if (IsZeriRBuff() && NewObj != null && ROrbAAMode == 1 && ((RDisableAAHP && NewObj.HealthPercent <= 35) || (RDisableAACN && PassiveReady()) || (NewObj.GetRealHeath(DamageType.Physical) < (GetAADamage(NewObj) + NormalQ.GetDamage(NewObj)))))
                    {
                        return;
                    }
                }

                if ((!IsZeriRBuff() || (!SAFEQA || !dash.InMelleAttackRange(Player.ServerPosition))) && 
                    (IsZeriRBuff() && ROrbAAMode != 0) || (!IsZeriRBuff() && OrbAAMode != 0) ||
                    Orbwalker.GetTarget() == null)
                {
                    
                    var Target = TargetSelector.GetTarget(NormalQ.Range, DamageType.Physical);
                    if (Target != null)
                    {
                       CastSmartQ(Target);
                    }
                }
            }

        }
        private void Harass()
        {
            if (NormalQ.IsReady())
            {
                if (!Player.IsWindingUp)
                {
                    var Target = TargetSelector.GetTarget(NormalQ.Range, DamageType.Physical);
                    if (Target != null)
                    {
                        CastSmartQ(Target);
                    }
                }
            }
            if (HarassUseW && FastW.IsReady())
            {
                var Target = TargetSelector.GetTarget(FastW.Range, DamageType.Physical);
                if (Target != null)
                {
                    var Pred = FastW.GetPrediction(Target, false, -1, new CollisionObjects[] { CollisionObjects.Heroes, CollisionObjects.Minions, CollisionObjects.Walls, CollisionObjects.YasuoWall });

                    if (Pred.Hitchance >= HitChance.High)
                    {
                        CastWToPos(Pred.CastPosition);
                        return;
                    }

                    if (HarassUseWWall)
                    {
                        var CollisionObjs = Pred.CollisionObjects.Count;
                        if (CollisionObjs > 0)
                        {
                            WallW.Delay = 0.75f + FastW.Delay;
                            var PREDS = WallW.GetPrediction(Target, true);
                            if (PREDS.Hitchance >= HitChance.High)
                            {
                                var FisrtWall = GetFirstWallPoint(Player.ServerPosition.ToVector2(), PREDS.UnitPosition.ToVector2());
                                if (FisrtWall != Vector2.Zero)
                                {
                                    CastWToPos(PREDS.UnitPosition);
                                }
                            }
                        }
                    }
                }
            }
        }
        private void OnNonKillalbeMinion(object sender,NonKillableMinionEventArgs args)
        {
            if (!NormalQ.IsReady()) return;

            if(Orbwalker.ActiveMode == OrbwalkerMode.LaneClear || Orbwalker.ActiveMode == OrbwalkerMode.LastHit || Orbwalker.ActiveMode == OrbwalkerMode.Harass)
            {
                if(args.Target.Type == GameObjectType.AIMinionClient || args.Target.IsMinion())
                {
                    var obj = args.Target as AIBaseClient;
                    if (obj == null) return;

                    CastSmartQ(obj);
                }
            }
        }
        private void LogicE()
        {
            if(ComboUseE != 2)
            {
                bool AoeMode = Player.HasBuff("zeriespecialrounds");
                if (ComboUseE == 0 && !AoeMode)
                {
                    
                    var dashPos = dash.CastDash(true);
                    if(dashPos.IsValid())
                    {
                        E.Cast(dashPos);
                    }
                }
                if(ComboUseE == 1)
                {
                    if (!dash.IsGoodPosition(Player.ServerPosition))
                    {
                        var dashPos = dash.CastDash(true);
                        if (dashPos.IsValid())
                        {
                            E.Cast(dashPos);
                        }
                    }
                }
            }
        }
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Zeri));
            var BasicAttack = ChampionMenu.Add(new Menu("BasicAttack", Program.Chinese ? "走砍设置" : "Orbwalker"));
            {
                BasicAttack.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Zeri_Basic_Attack));
                BasicAttack.Add(new MenuSeparator("say1", Program.Chinese ? "常规连招模式" : "Normal Combo Mode"));
                BasicAttack.Add(new MenuList("AAMode", Program.Chinese ? "普攻模式" : "Attack Mode",new string[] { "AQ","Disable"}));
                BasicAttack.Add(new MenuBool("DisableAAHP", Program.Chinese ? "->Disable时 仅当敌人生命<=35%才普攻" : "-> Attack Enemy if Attack Mode is Disable && Enemy Health <= 35%", false));
                BasicAttack.Add(new MenuBool("DisableAACN", Program.Chinese ? "->Disable时 仅当完全充能时才普攻" : "-> Attack Enemy if Attack Mode is Disable && Has Passive", true));
                BasicAttack.Add(new MenuSeparator("say2", Program.Chinese ? "超限爆闪模式" : "R Active Mode"));
                BasicAttack.Add(new MenuList("RAAMode", Program.Chinese ? "普攻模式" : "Attack Mode", new string[] { "AQ", "Disable" }));
                BasicAttack.Add(new MenuBool("SAFEQA", Program.Chinese ? "处于近战英雄攻击范围时 禁止QA" : "Disable AAOrb if Player in Melee hero Attack Range"));
                BasicAttack.Add(new MenuBool("RDisableAAHP", Program.Chinese ? "->从不时 仅当敌人生命<=35%才普攻" : "-> Attack Enemy if Attack Mode is Disable && Enemy Health <= 35%", false));
                BasicAttack.Add(new MenuBool("RDisableAACN", Program.Chinese ? "->从不时 仅当完全充能时才普攻" : "-> Attack Enemy if Attack Mode is Disable && Has Passive", true));
                BasicAttack.Add(new MenuSeparator("say3", Program.Chinese ? "骚扰清线模式" : "Harass && LaneClear && LastHit"));
                BasicAttack.Add(new MenuBool("QAA", Program.Chinese ? "使用Q代替普攻" : "Use Q. Not AA"));
            }
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo"));
            {
                Combo.Add(new MenuBool("UseW", "Use W"));
                Combo.Add(new MenuList("EMode", "Use E", new string[] { "Always", "Only Safe","Disable" }, 1));
                Combo.Add(new MenuBool("UseWWall", "->Extra W Wall Mode"));
                Combo.Add(new MenuBool("UseR", "Use R",false));
                Combo.Add(new MenuSlider("UseRCount", "->When Enemy >= X时",2,1,5));
            }
            var Harass = ChampionMenu.Add(new Menu("Harass", "Harass"));
            {
                Harass.Add(new MenuBool("UseW", "Use W",false));
                Harass.Add(new MenuBool("UseWWall", "->Extra W Wall Mode", false));
            }
            var JumpWall = ChampionMenu.Add(new Menu("JumpWall", "JumpWall"));
            {
                JumpWall.Add(new MenuBool("UseJ", "Use JumpWall"));
                JumpWall.Add(new MenuKeyBind("Key", "JumpWall Key",Keys.H,KeyBindType.Press));
            }
            var Draw = ChampionMenu.Add(new Menu("Draw", "Draw"));
            {
                Draw.Add(new MenuBool("Q", "Draw Q", false));
                Draw.Add(new MenuBool("W", "Draw W", false));
                Draw.Add(new MenuBool("R", "Draw R", false));
            }
            AntiGapcloserMenu = AntiGapcloser.Attach(ChampionMenu);
            {
                AntiGapcloserMenu.Add(new MenuBool("antiGapE", "Use E"));
            }
        }
        private void AutoKill()
        {
            foreach(var obj in Cache.EnemyHeroes.Where(x => x.IsValidTarget()))
            {
                var RealHealth = obj.GetRealHeath(DamageType.Physical);
                if (NormalQ.IsReady() && NormalQ.IsInRange(obj))
                {
                    if(RealHealth < NormalQ.GetDamage(obj))
                    {
                        CastSmartQ(obj);
                    }
                }
                if(FastW.IsReady() && FastW.IsInRange(obj))
                {
                    if(RealHealth < FastW.GetDamage(obj))
                    {
                        var pred = FastW.GetPrediction(obj);
                        if (pred.Hitchance >= HitChance.High)
                        {
                            CastWToPos(pred.CastPosition);
                        }
                    }
                }
            }
        }
        private float GetAADamage(AIBaseClient t)
        {
            if (PassiveReady())
            {
                var LevelDamage = 90 + (110 / 17) * (Player.Level - 1) * (0.7025 + 0.0175 * (Player.Level - 1));
                LevelDamage = Math.Min(LevelDamage, 200);
                var ExtraDamage = 0.8f * Player.TotalMagicalDamage;
                var HealthDamage = (3 + (17 / 17) * (Player.Level - 1) * (0.0725 + 0.0002 * (Player.Level - 1))) / 100f;
                HealthDamage = Math.Min(HealthDamage, 0.15);
                HealthDamage = t.MaxHealth * HealthDamage;
                return (float)Player.CalculateMagicDamage(t, LevelDamage + ExtraDamage + HealthDamage);
            }
            else
            {
                var LevelDamage = 10 + (15 / 17) * (Player.Level - 1) * (0.7025 + 0.0175 * (Player.Level - 1));
                LevelDamage = Math.Min(LevelDamage, 25);
                var ExtraDamage = 0.03 * Player.TotalMagicalDamage;
                var TotalDamage = LevelDamage + ExtraDamage;
                var EndDamage = t.HealthPercent >= 35 ? TotalDamage : TotalDamage + (TotalDamage * 5);
                return (float)Player.CalculateMagicDamage(t, EndDamage);
            }
        }
        private float GetRawQDmg()
        {
            var BaseDamage = 10 + ((NormalQ.Level - 1) * 5);
            return BaseDamage + (Player.TotalAttackDamage * 1.1f);
        }    
        private void JumpWallLogic()
        {
            if (!E.IsReady()) 
                return;
            if (Game.MapId == GameMapId.SummonersRift)
            {
                for (int i = 0; i < StartVec3.Count; i++)
                {
                    if (Game.CursorPos.Distance(StartVec3[i]) <= 50)
                    {
                        Player.IssueOrder(GameObjectOrder.MoveTo, StartVec3[i]);

                        if (Player.Distance(StartVec3[i]) <= 20)
                        {
                            Player.Spellbook.CastSpell(SpellSlot.E, EndVec3[i], EndVec3[i]);

                            break;
                        }
                    }
                }
            }
        }
        private void LastHit()
        {
            if (!Enable_laneclear)
                return;

            var minions = Cache.GetMinions(Player.ServerPosition, NormalQ.Range).Where(x => !x.InAutoAttackRange() && x.GetRealHeath(DamageType.Physical) < NormalQ.GetDamage(x));
            foreach(var obj in minions)
            {
                LastHitQ(obj);
            }

        }
    }
}
