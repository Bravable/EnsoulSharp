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

namespace ImpulseAIO.Champion.Jinx
{
    internal class Jinx : Base
    {
        private static int colorindex = 0;
        private static bool ProtectMode = false;
        private static Menu AntiGapcloserMenu;
        private static Spell Q, W, E, R;
        private static bool ComboUseQ => ChampionMenu["Combo"]["CQ"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseQCHARGE => ChampionMenu["Combo"]["CQCHARGE"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseW => ChampionMenu["Combo"]["CW"].GetValue<MenuBool>().Enabled;
        private static int ComboUseWMode => ChampionMenu["Combo"]["CWAAMode"].GetValue<MenuList>().Index;
        private static bool ComboUseE => ChampionMenu["Combo"]["CE"].GetValue<MenuBool>().Enabled;
        private static int EMode => ChampionMenu["Eset"]["EMode"].GetValue<MenuList>().Index;
        private static int ESafePos => ChampionMenu["Eset"]["SafeSet"].GetValue<MenuList>().Index;
        private static int ESafePosDist => ChampionMenu["Eset"]["SafeSetDist"].GetValue<MenuSlider>().Value;
        private static int EAntiGapdis => ChampionMenu["Eset"]["EAntiGapdis"].GetValue<MenuSlider>().Value;
        private static bool CheckFace => ChampionMenu["Eset"]["CheckFace"].GetValue<MenuBool>().Enabled;
        private static int EcastEnemyCount => ChampionMenu["Eset"]["EcastEnemyCount"].GetValue<MenuSlider>().Value;
        private static int ProtectRange => ChampionMenu["Eset"]["ProtectRange"].GetValue<MenuSlider>().Value;
        private static bool DrawProtectRange => ChampionMenu["Eset"]["DrawProtectRange"].GetValue<MenuBool>().Enabled;
        private static bool autoEDash => ChampionMenu["Eset"]["autoEDash"].GetValue<MenuBool>().Enabled;
        private static int RMode => ChampionMenu["Rset"]["UseR"].GetValue<MenuList>().Index;
        private static int CRRange => ChampionMenu["Rset"]["CRRange"].GetValue<MenuSlider>().Value;
        private static int CRRange2 => ChampionMenu["Rset"]["CRRange2"].GetValue<MenuSlider>().Value;
        private static bool HarassUseQ => ChampionMenu["Harass"]["HQ"].GetValue<MenuBool>().Enabled;
        private static bool HarassUseQPoke => ChampionMenu["Harass"]["HQPoke"].GetValue<MenuBool>().Enabled;
        private static bool HarassUseW => ChampionMenu["Harass"]["HW"].GetValue<MenuBool>().Enabled;
        private static int HarassMana => ChampionMenu["Harass"]["HMana"].GetValue<MenuSlider>().Value;
        private static bool LaneClearUseQ => ChampionMenu["LaneClear"]["LQ"].GetValue<MenuBool>().Enabled;
        private static int LaneClearMana => ChampionMenu["LaneClear"]["LMana"].GetValue<MenuSlider>().Value;
        private static bool autoCC => ChampionMenu["OTHER"]["autoE"].GetValue<MenuBool>().Enabled;
        private static bool opsE => ChampionMenu["OTHER"]["autoE1"].GetValue<MenuBool>().Enabled;
        private static bool SafeCheck => ChampionMenu["OTHER"]["UseSafeCheck"].GetValue<MenuBool>().Enabled;
        private static bool LastHitUseQ => ChampionMenu["LastHit"]["LQ"].GetValue<MenuBool>().Enabled;
        private static int LastHitMana => ChampionMenu["LastHit"]["LQMana"].GetValue<MenuSlider>().Value;
        private static bool DW => ChampionMenu["DRAW"]["DW"].GetValue<MenuBool>().Enabled;
        private static bool DE => ChampionMenu["DRAW"]["DE"].GetValue<MenuBool>().Enabled;
        private static bool DR => ChampionMenu["DRAW"]["DRG1"].GetValue<MenuBool>().Enabled;
        private static bool AntiGap => AntiGapcloserMenu["AntiEGap"].GetValue<MenuBool>().Enabled;
        private bool FishBoneActive
        {
            get
            {
                return Player.HasBuff("JinxQ");
            }
        }
        private float QAddRange
        {
            get { return 50 + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level; }
        }
        public Jinx()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1500f);
            W.SetSkillshot(0.6f, 60f, 3300f, true, SpellType.Line);
            E = new Spell(SpellSlot.E, 925f);
            R = new Spell(SpellSlot.R, 3000f);
            E.SetSkillshot(1.4f, 115f, 1750f, false, SpellType.Circle);
            R.SetSkillshot(0.6f, 140f, 1700f, false, SpellType.Line); // 140 ExtraWidth
            W.DamageType = E.DamageType = R.DamageType = DamageType.Physical;
            OnMenuLoad();
            Common.BaseUlt.BaseUlt.Initialize(ChampionMenu, R);
            Orbwalker.OnBeforeAttack += Orbwalker_OnBeforeAttack;
            Game.OnUpdate += Game_OnUpdate;
            AntiGapcloser.OnGapcloser += AntiGapCloser;
            Render.OnEndScene += OnDraw;
            Interrupter.OnInterrupterSpell += OnInterrupterSpell;
        }
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Jinx));
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo"));
            {
                Combo.Add(new MenuBool("CQ", "Use Q"));
                Combo.Add(new MenuBool("CQCHARGE", Program.Chinese ? "->使用炮如果对方能被最后一A杀死时" : "->Force Use Q if Target HP <= Q Damage",false));
                Combo.Add(new MenuBool("CW", "Use W"));
                Combo.Add(new MenuList("CWAAMode", Program.Chinese ? "-> 目标在走砍范围时W情况" : "if target in Attack Range . So UseW Mode",new string[] { "Impulse","Always","Disable"}));
                Combo.Add(new MenuBool("CE", "Use E"));
            }
            var Eset = ChampionMenu.Add(new Menu("Eset", Program.Chinese ? "嚼火者手雷" : "E set"));
            {
                Eset.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.JinxE));
                Eset.Add(new MenuList("EMode", !Program.Chinese ?"Use E Mode" : "使用E模式",new string[] { !Program.Chinese ? "Always" : "总是","Impulse"},1));
                Eset.Add(new MenuSlider("EAntiGapdis", Program.Chinese ? "反突进当敌我距离 <= X 时使用E" : "-> Anti Gap if Enemy Dash EndPos Dist to Me <= X",300,50, (int)E.Range));
                Eset.Add(new MenuBool("CheckFace", Program.Chinese ? "检测是否面对自己" : "Check Face"));
                Eset.Add(new MenuSlider("EcastEnemyCount", Program.Chinese ? "当自身周围敌人数 >= X时开启保护模式" :"Enable ProtectMode If Player Count Enemy >= X",1,0,5));
                Eset.Add(new MenuSlider("ProtectRange", Program.Chinese ? "保护模式 侦测距离" : "ProtectMode Check Range", 200, 0, 500));
                Eset.Add(new MenuBool("DrawProtectRange", Program.Chinese ? "画出保护模式侦测距离(渐变线圈)" : "Draw ProtectMode Check Range"));
                Eset.Add(new MenuList("SafeSet", Program.Chinese ? "保护模式夹子落点" : "ProtectMode E Pos", new string[] { "EnemyToPlayer", "PlayerToCursor", "Use Prediction" }, 1));
                Eset.Add(new MenuSlider("SafeSetDist", Program.Chinese ? "保护模式夹子延长距离" : "Protect Mode E Extend Distance", 425,100,(int)E.Range));
                Eset.Add(new MenuBool("autoEDash", Program.Chinese ? "自动对敌人飞行路线E(盲僧R等)" : "Auto E Dash"));
            }
            var Rset = ChampionMenu.Add(new Menu("Rset", Program.Chinese ? "超究极死神飞弹" : "R Set"));
            {
                Rset.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.JinxR));
                Rset.Add(new MenuList("UseR", "Use R", new string[] { "Only combo", "Always", "Disable" }, 0));
                Rset.Add(new MenuSlider("CRRange", Program.Chinese ? "大招最远距离限制在 X 米" : "Max Distance", 2000, 0, 5000));
                Rset.Add(new MenuSlider("CRRange2", Program.Chinese ? "敌人距离 >= X 时才使用R" : "Use R if Distance >= X", 525, 0, 3000));
            }
            var Harass = ChampionMenu.Add(new Menu("Harass", "Harass"));
            {
                Harass.Add(new MenuBool("HQ", "Use Q"));
                Harass.Add(new MenuBool("HQPoke", Program.Chinese ? "借助小兵打人" : "Poke Minion"));
                Harass.Add(new MenuBool("HW", "Use W"));
                Harass.Add(new MenuSlider("HMana", Program.Chinese ? "蓝量 <= X%时不技能骚扰" : "Don't Harass if Mana <= X%",40,0,100));
            }
            var LaneClear = ChampionMenu.Add(new Menu("LaneClear", "LaneClear"));
            {
                LaneClear.Add(new MenuBool("LQ", "Use Q"));
                LaneClear.Add(new MenuSlider("LMana", Program.Chinese ? "当蓝量 <= X% 时不清线" : "Dont Laneclear/JungleClear if mana <= X%", 60,0,100));
            }
            var LastHit = ChampionMenu.Add(new Menu("LastHit", "LastHit"));
            {
                LastHit.Add(new MenuBool("LQ", "Use Q"));
                LastHit.Add(new MenuSlider("LQMana", Program.Chinese ? "当蓝量 <= X%时不尾刀" : "Dont LastHit if Mana <= X%", 20));
            }
            var DRAW = ChampionMenu.Add(new Menu("DRAW", "Draw"));
            {
                DRAW.Add(new MenuBool("DW", "Draw W"));
                DRAW.Add(new MenuBool("DE", "Draw E"));
                DRAW.Add(new MenuBool("DRG1", "Draw R Max Range"));
            }
            var Auto = ChampionMenu.Add(new Menu("OTHER", "Misc"));
            {
                Auto.Add(new MenuBool("autoE", Program.Chinese ? "自动 E 如果敌方无法移动" : "Auto E CC"));
                Auto.Add(new MenuBool("autoE1", Program.Chinese ? "自动 E 如果敌方释放高危技能" : "Auto E Interrupt High DangerLevel Spell"));
                Auto.Add(new MenuBool("UseSafeCheck", Program.Chinese ? "->上述条件均启用安全检查" : "Enable ProtectMode Check"));
            }
            AntiGapcloserMenu = AntiGapcloser.Attach(ChampionMenu);
            {
                AntiGapcloserMenu.Add(new MenuBool("AntiEGap", "Use E"));
            }
        }
        private void Orbwalker_OnBeforeAttack(object sender, BeforeAttackEventArgs args)
        {
            if (!Q.IsReady() || !FishBoneActive)
            {
                return;
            }
            //开启大炮模式
            var t = args.Target as AIHeroClient;

            if (t != null)
            {
                var realDistance = GetRealDistance(t);
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && ComboUseQ && 
                    realDistance < GetRealPowPowRange(t) && (t.CountEnemyHerosInRangeFix(250) < 2 || Player.Mana < E.Instance.ManaCost + 40))
                {
                    if (!ComboUseQCHARGE || (GetBonudDamage(t) < t.GetRealHeath(DamageType.Physical)))
                    {
                        //敌人在机枪范围内 如果敌人数小于2或者没蓝了 切回来
                        Q.Cast();
                    }
                }
                else if (Orbwalker.ActiveMode == OrbwalkerMode.Harass && HarassUseQ && ((realDistance <= GetRealPowPowRange(t) && t.CountEnemyHerosInRangeFix(250) < 2) || Player.ManaPercent < HarassMana))
                {
                    Q.Cast();
                }
            }

            var minion = args.Target as AIMinionClient;

            if(minion != null)
            {
                var realDistance = GetRealDistance(minion);
                var czs = Cache.GetMinions(minion.Position, 250f).Count(x => x.GetRealHeath(DamageType.Physical) <= GetBonudDamage(x));
                if (LaneClearUseQ && Enable_laneclear && Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                {
                    if(czs <= 1 || (realDistance < GetRealPowPowRange(minion) && minion.GetRealHeath(DamageType.Physical) < GetPowPowDamage(minion)) || Player.ManaPercent < LaneClearMana)
                    {
                        //如果残血小兵就1个 或者 在机枪距离内 或者 没蓝了 切回
                        Q.Cast();
                    }
                }
                if(LastHitUseQ && Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
                {
                    if (czs <= 1 || (realDistance < GetRealPowPowRange(minion) && minion.GetRealHeath(DamageType.Physical) < GetPowPowDamage(minion)) || Player.ManaPercent < LastHitMana)
                    {
                        //如果残血小兵就1个 或者 在机枪距离内 或者 没蓝了 切回
                        Q.Cast();
                    }
                }
                if(HarassUseQ && Orbwalker.ActiveMode == OrbwalkerMode.Harass)
                {
                    var enemys = minion.CountEnemyHerosInRangeFix(250f);
                    if(enemys == 0 || Player.ManaPercent < HarassMana || (realDistance < GetRealPowPowRange(minion) && minion.GetRealHeath(DamageType.Physical) < GetPowPowDamage(minion)))
                    {
                        Q.Cast();
                    }
                }
            }

        }
        private void Game_OnUpdate(EventArgs args)
        {
            W.Delay = 0.6f - Math.Max(0, Math.Min(0.2f, 0.02f * ((Player.AttackSpeedMod - 1) / 0.25f)));
            AutoKill();
            ResetProtectMode();
            RLogic();
            AutoCCE();

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
                case OrbwalkerMode.LaneClear:
                    LaneClear();
                    break;
                case OrbwalkerMode.LastHit:
                    LastHit();
                    break;
                case OrbwalkerMode.Flee:
                    break;
            }
        }
        private void OnDraw(EventArgs args)
        {
            if (DrawProtectRange)
            {
                colorindex++;
                if (colorindex >= 400)
                    colorindex = 0;
                var colorm = PlusRender.GetFullColorList(400);
                PlusRender.DrawCircle(Player.Position, ProtectRange, colorm[colorindex]);
                var myWorldPos = Drawing.WorldToScreen(Player.Position);
                PlusRender.DrawText((Program.Chinese ? "保护模式:" : "Protect Mode") + (ProtectMode ?( Program.Chinese ? "开启" : "Enable") : (Program.Chinese ? "关闭" : "Disable")),myWorldPos.X - 15,myWorldPos.Y + 20,Color.Yellow);
            }

            if (DW && W.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, W.Range, Color.Green);
            }
            if (DE && E.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, E.Range, Color.Yellow);
            }
            if (DR && R.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, CRRange, Color.Chocolate);
            }
        }
        private void AntiGapCloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (AntiGap && sender.NewIsValidTarget(E.Range) && args.EndPosition.DistanceToPlayer() <= E.Range)
            {
                if (args.EndPosition.DistanceToPlayer() <= EAntiGapdis && args.StartPosition.DistanceToPlayer() > args.EndPosition.DistanceToPlayer())
                {
                    Elogic(sender, true, args.EndPosition);
                }
            }
        }
        private void OnInterrupterSpell(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (!opsE || !E.IsReady()) return;
            if (args.Sender.NewIsValidTarget(E.Range) && args.DangerLevel == Interrupter.DangerLevel.High)
            {
                if (!args.Sender.CanMove)
                {
                    if (HealthPrediction.GetPrediction(args.Sender, 500) > 0)
                    {
                        if (!SafeCheck || IsSafeCastE(args.Sender))
                        {
                            E.Cast(args.Sender);
                            return;
                        }
                    }
                }
            }
        }
        private static int ResetTime = 0;
        private void ResetProtectMode()
        {
            if (Variables.GameTimeTickCount > ResetTime)
            {
                ResetTime = Variables.GameTimeTickCount + 100;
                ProtectMode = Player.CountEnemyHerosInRangeFix(ProtectRange) >= EcastEnemyCount;
            }
        }
        private void RLogic()
        {
            if (Player.CountEnemyHerosInRangeFix(600) != 0)
                return;

            if ((RMode == 0 && Orbwalker.ActiveMode == OrbwalkerMode.Combo || RMode == 1) && R.IsReady())
            {
                var MaxR = CRRange;
                var MinR = CRRange2;

                foreach (var t in Cache.EnemyHeroes.Where(x => x.NewIsValidTarget(MaxR)))
                {
                    var distance = GetRealDistance(t);
                    if (distance > MinR)
                    {
                        var aDamage = Player.GetAutoAttackDamage(t);
                        var wDamage = Player.GetSpellDamage(t, SpellSlot.W);
                        var rDamage = GetRDmg(t, t.DistanceToPlayer() / R.Speed, t.DistanceToPlayer());
                        if(HealthPrediction.GetPrediction(t, (int)(t.DistanceToPlayer() / R.Speed * 1000)) <= 0)
                            continue;
                        if (t.HealthPercent <= 15 && t.CountAllysHerosInRangeFix(400f) - 1 >= 1)
                            continue;
                        var powPowRange = GetRealPowPowRange(t);

                        if (distance < (powPowRange + QAddRange) && !(aDamage * 3.5 > t.GetRealHeath(DamageType.Physical)))
                        {
                            if (!W.IsReady() || !(wDamage > t.GetRealHeath(DamageType.Physical)) || W.GetPrediction(t).CollisionObjects.Count > 0)
                            {
                                if (t.CountAllysHerosInRangeFix(500f) <= 3)
                                {
                                    if (rDamage > t.GetRealHeath(DamageType.Physical) && !Player.Spellbook.IsWindingUp &&
                                        !Player.Spellbook.IsChanneling)
                                    {
                                        var preds = R.GetPrediction(t, true, -1, new CollisionObjects[] { CollisionObjects.YasuoWall,CollisionObjects.Heroes });
                                        if (preds.Hitchance >= HitChance.VeryHigh && R.Cast(preds.UnitPosition))
                                            return;
                                    }
                                }
                            }
                        }
                        else if (distance > (powPowRange + QAddRange))
                        {
                            if (!W.IsReady() || !(wDamage > t.GetRealHeath(DamageType.Physical)) || distance > W.Range ||
                                W.GetPrediction(t).CollisionObjects.Count > 0)
                            {
                                if (t.CountAllysHerosInRangeFix(500f) <= 3)
                                {
                                    if (rDamage > t.GetRealHeath(DamageType.Physical) && !Player.Spellbook.IsWindingUp &&
                                        !Player.Spellbook.IsChanneling)
                                    {
                                        var preds = R.GetPrediction(t, true, -1, new CollisionObjects[] { CollisionObjects.YasuoWall, CollisionObjects.Heroes });
                                        if (preds.Hitchance >= HitChance.VeryHigh && R.Cast(preds.UnitPosition))
                                            return;
                                    }
                                }
                            }
                        }
                    }

                }

            }
        }
        private bool IsSafeCastE(AIHeroClient unit = null)
        {
            //获取周围敌人
            bool Warning = false;

            if (ProtectMode)
            {
                //保护模式开启 自己很危险! 路程 / 时间 = 速度  时间 / 路程 / 速度
                foreach (var enemyobj in Cache.EnemyHeroes.Where(x => x.NewIsValidTarget(E.Range) && x.DistanceToPlayer() <= ProtectRange))
                {
                    if (enemyobj.CombatType == GameObjectCombatType.Melee)
                    {
                        if (enemyobj.DistanceToPlayer() <= enemyobj.AttackRange + Player.BoundingRadius + 30 && (!CheckFace || enemyobj.IsFacing(Player)))
                        {
                            Warning = true;
                            if (enemyobj == unit)
                            {
                                Warning = false;
                            }
                            break;
                        }
                        else
                        {
                            var nextmove = Prediction.GetPrediction(enemyobj, 0.4f).CastPosition;
                            if (nextmove.IsValid())
                            {
                                if (enemyobj.DistanceToPlayer() > nextmove.DistanceToPlayer() && (!CheckFace || enemyobj.IsFacing(Player)))
                                {
                                    Warning = true;
                                    if (enemyobj == unit)
                                    {
                                        Warning = false;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return !Warning;
        }
        private void AutoCCE()
        {
            if (!E.IsReady() || !autoCC) 
                return;
            foreach (var enemy in Cache.EnemyHeroes.Where(e => e.NewIsValidTarget(E.Range)))
            {
                var ccPos = GetCCBuffPos(enemy);
                if (ccPos.IsValid() && HealthPrediction.GetPrediction(enemy, 500) > 0)
                {
                    if (!SafeCheck || IsSafeCastE(enemy))
                    {
                        E.Cast(ccPos);
                        return;
                    }
                }
                if (opsE && enemy.IsCastingImporantSpell())
                {
                    if (!SafeCheck || IsSafeCastE(enemy))
                    {
                        E.Cast(enemy);
                        return;
                    }
                }
            }
        }
        private void AutoKill()
        {
            if (!W.IsReady())
                return;

            if (!InMelleAttackRange(Player.ServerPosition))
            {
                foreach (var enemy in Cache.EnemyHeroes.Where(e => e.NewIsValidTarget(W.Range) && e.GetRealHeath(DamageType.Physical) < W.GetDamage(e)))
                {
                    if(enemy.InAutoAttackRange() && enemy.Health <= Player.GetAutoAttackDamage(enemy) * 2)
                    {
                        continue;
                    }
                    var preds = W.GetPrediction(enemy, false, -1, new CollisionObjects[] { CollisionObjects.YasuoWall, CollisionObjects.Minions,CollisionObjects.Heroes });
                    if (preds.Hitchance >= HitChance.VeryHigh)
                    {
                        W.Cast(preds.CastPosition);
                    }
                }
            }
        }
        private void Elogic(AIHeroClient unit,bool isDash = false,Vector3 enddashpos = new Vector3())
        {
            if (unit == null || !unit.NewIsValidTarget(E.Range))
            {
                return;
            }
               

            bool isCastE = false;
            if (ProtectMode)
            {
                //保护模式开启 自己很危险! 路程 / 时间 = 速度  时间 / 路程 / 速度
                foreach(var enemyobj in Cache.EnemyHeroes.Where(x => x.NewIsValidTarget(E.Range) && x.DistanceToPlayer() <= ProtectRange))
                {
                    if(enemyobj.CombatType == GameObjectCombatType.Melee)
                    {
                        if(enemyobj.DistanceToPlayer() <= enemyobj.AttackRange + enemyobj.BoundingRadius + Player.BoundingRadius + 30 && (!CheckFace || enemyobj.IsFacing(Player)))
                        {
                            var ExtraPos = enemyobj.ServerPosition.Extend(Player.ServerPosition, ESafePosDist);
                            if (ESafePos == 1)
                            {
                                if(Game.CursorPos.Distance(enemyobj) > Player.Distance(enemyobj))
                                {
                                    ExtraPos = Player.ServerPosition.Extend(Game.CursorPos, ESafePosDist);
                                }
                                else
                                {
                                    ExtraPos = Vector3.Zero;
                                }
                            }
                            else if (ESafePos == 2)
                            {
                                var pred = E.GetPrediction(enemyobj);
                                ExtraPos = pred.Hitchance >= HitChance.High ? pred.CastPosition : Vector3.Zero;
                            }
                            if (ExtraPos.IsValid() && ExtraPos.DistanceToPlayer() <= E.Range && E.Cast(ExtraPos))
                            {
                                isCastE = true;
                                break;
                            }
                        }
                        else
                        {
                            var nextmove = Prediction.GetPrediction(enemyobj, 0.4f).CastPosition;
                            if (nextmove.IsValid())
                            {
                                if(enemyobj.DistanceToPlayer() > nextmove.DistanceToPlayer() && (!CheckFace || enemyobj.IsFacing(Player)))
                                {
                                    var ExtraPos = enemyobj.ServerPosition.Extend(Player.ServerPosition, ESafePosDist);
                                    if (ESafePos == 1)
                                    {
                                        if (Game.CursorPos.Distance(enemyobj) > Player.Distance(enemyobj))
                                        {
                                            ExtraPos = Player.ServerPosition.Extend(Game.CursorPos, ESafePosDist);
                                        }
                                        else
                                        {
                                            ExtraPos = Vector3.Zero;
                                        }
                                    }
                                    else if (ESafePos == 2)
                                    {
                                        var pred = E.GetPrediction(enemyobj);
                                        ExtraPos = pred.Hitchance >= HitChance.High ? pred.CastPosition : Vector3.Zero;
                                    }
                                    if (ExtraPos.IsValid() && ExtraPos.DistanceToPlayer() <= E.Range && E.Cast(ExtraPos))
                                    {
                                        isCastE = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (isCastE)
                {
                    return;
                }
            }

            if (isDash && enddashpos.IsValid())
            {
                E.Cast(enddashpos);
                return;
            }
            if (EMode == 0)
            {
                var EPrediction = E.GetPrediction(unit);
                if(EPrediction.Hitchance >= HitChance.High)
                {
                    E.Cast(EPrediction.CastPosition);
                    return;
                }
            }
            if(EMode == 1)
            {
                var pred = E.GetPrediction(unit);
                if (pred.Hitchance < HitChance.Medium)
                {
                    return;
                } 
                var EnemyNextPos = Prediction.GetPrediction(unit, 0.5f).CastPosition;
                if (EnemyNextPos.IsValid())
                {
                    if(EnemyNextPos.DistanceToPlayer() > unit.ServerPosition.DistanceToPlayer())//
                    {
                        if (pred.CastPosition.DistanceToPlayer() <= E.Range)
                        {
                            E.CastIfWillHit(unit, 2);

                            if (unit.HasBuffOfType(BuffType.Slow))
                            {
                                E.Cast(pred.CastPosition);
                            }
                            if (IsMovingInSameDirection(Player, unit))
                            {
                                E.Cast(pred.CastPosition);
                            }
                        }
                    }
                }
            }

            if (autoEDash)
            {
                var obj = Cache.EnemyHeroes.Where(x => x.IsValidTarget(E.Range));
                foreach(var ss in obj)
                {
                    E.CastIfHitchanceEquals(ss, HitChance.Dash);
                }
            }
        }
        private void LastHit()
        {
            if (LastHitUseQ && !FishBoneActive && !Player.IsWindingUp &&
                Orbwalker.GetTarget() == null && Orbwalker.CanAttack() && Player.ManaPercent > LastHitMana)
            {
                foreach (var minion in Cache.GetMinions(Player.ServerPosition, GetBonudRange()).Where(
                minion => !Player.InAutoAttackRange(minion) && GetRealPowPowRange(minion) < GetRealDistance(minion) && GetBonudRange(minion) < GetRealDistance(minion)))
                {
                    var hpPred = HealthPrediction.GetPrediction(minion, 400, 70);
                    if (hpPred < GetBonudDamage(minion) && hpPred > 5)
                    {
                        Q.Cast();
                        if (Orbwalker.CanAttack())
                        {
                            Orbwalker.Attack(minion);
                        }
                        //Orbwalker.ForceTarget = minion;
                        return;
                    }
                }
            }
        }
        private void LaneClear()
        {
            if (!Enable_laneclear || Player.ManaPercent <= LaneClearMana)
            {
                if (FishBoneActive)
                {
                    Q.Cast();
                }
                return;
            }
            

            if (LaneClearUseQ && Q.IsReady() && !Player.IsWindingUp && Orbwalker.CanAttack() && !FishBoneActive)
            {
                foreach (var minion in Cache.GetMinions(Player.ServerPosition, GetBonudRange() + 30).Where(e => !e.InAutoAttackRange() && GetRealPowPowRange(e) < GetRealDistance(e) && GetBonudRange(e) < GetRealDistance(e)))
                {
                    var hpPred = HealthPrediction.GetPrediction(minion, 400);

                    if (hpPred < GetBonudDamage(minion) && hpPred > 5)
                    {
                        Q.Cast();
                        if (Orbwalker.CanAttack())
                        {
                            Orbwalker.Attack(minion);
                        }
                        //Orbwalker.ForceTarget = minion;
                        //Q.Cast();

                        return;
                    }
                }
            }
            else if (FishBoneActive && LaneClearUseQ)
            {
                Q.Cast();
            }
        }
        private void Harass()
        {
            if(Player.ManaPercent <= HarassMana)
            {
                if (FishBoneActive)
                {
                    Q.Cast();
                }
                return;
            }
            if (HarassUseQ && Q.IsReady())
            {
                if (!FishBoneActive)
                {
                    var minion = Cache.GetMinions(Player.ServerPosition,GetBonudRange())
                    .Where(
                        t =>
                            t.GetRealHeath(DamageType.Physical) <= GetBonudDamage(t) &&
                            t.Distance(Player) > GetRealPowPowRange(t))
                    .OrderByDescending(t => t.GetRealHeath(DamageType.Physical));

                    foreach (var m in from minionOutOfRange in minion
                                      where minionOutOfRange != null
                                      select Cache.GetMinions(
                                          Player.ServerPosition,
                                          GetBonudRange())
                                          .Where(
                                              t =>
                                                  t.Distance(minionOutOfRange
                                                      ) <=
                                                  100 && t.GetRealHeath(DamageType.Physical) <= GetBonudDamage(t)).ToArray()
                        into minion2
                                      where minion2.Count() >= 3 
                                      from m in minion2
                                      select m)
                    {
                        Q.Cast();

                        //Q.Cast();
                        if (Orbwalker.CanAttack())
                        {
                            Orbwalker.Attack(m);
                        }
                        //Orbwalker.ForceTarget = m;
                    }

                    // If the player has a minigun
                    var orbT = Orbwalker.GetTarget();

                    var target = TargetSelector.GetTargets(GetBonudRange() + 60, DamageType.Physical).MinOrDefault(x => x.DistanceToPlayer());

                    if (orbT == null && target.NewIsValidTarget())
                    {
                        if (!target.InAutoAttackRange() &&
                            target.DistanceToPlayer() <= GetBonudRange(target))
                        {
                            Q.Cast();
                            if (Orbwalker.CanAttack())
                            {
                                Orbwalker.Attack(target);
                            }
                        }
                    }
                }

                if (HarassUseQPoke)
                {
                    var tOrb = Orbwalker.GetTarget();
                    var t2 = TargetSelector.GetTarget(GetBonudRange() + 200, DamageType.Physical);
                    if(tOrb == null && t2.NewIsValidTarget())
                    {
                        AIBaseClient bestMinion = null;
                        var laneMinions = Cache.GetMinions(Player.ServerPosition, 1000);
                        foreach (var minion in laneMinions)
                        {
                            if (!minion.InAutoAttackRange())
                                continue;

                            float delay = Player.AttackCastDelay + 0.3f;
                            var t2Pred = Prediction.GetPrediction(t2, delay).CastPosition;
                            var minionPred = Prediction.GetPrediction(minion, delay).CastPosition;

                            if (t2Pred.Distance(minionPred) < 250 && t2.Distance(minion) < 250)
                            {
                                if (bestMinion != null)
                                {
                                    if (bestMinion.Distance(t2) > minion.Distance(t2))
                                        bestMinion = minion;
                                }
                                else
                                {
                                    bestMinion = minion;
                                }
                            }
                        }
                        if (bestMinion != null)
                        {
                            if (FishBoneActive)
                            {
                                if (Orbwalker.CanAttack())
                                {
                                    Orbwalker.Attack(bestMinion);
                                }
                                //Orbwalker.ForceTarget = bestMinion;
                            }
                            else
                            {
                                Q.Cast();
                                if (Orbwalker.CanAttack())
                                {
                                    Orbwalker.Attack(bestMinion);
                                }
                                //Orbwalker.ForceTarget = bestMinion;
                            }
                            return;
                        }
                    }
                    //Orbwalker.ForceTarget = null;
                }
            }
            if(HarassUseW && W.IsReady() && !Player.IsWindingUp)
            {
                var Ret = IMPGetTarGet(W, false, HitChance.High);
                if (!Ret.SuccessFlag || !Ret.Obj.IsValid)
                {
                    return;
                }
                W.Cast(Ret.CastPosition);
            }
        }
        private void Combo()
        {
            Orbwalker.ForceTarget = null;
            if(ComboUseE && E.IsReady())
            {
                var t = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                Elogic(t);
            }

            if (ComboUseQ && Q.IsReady())
            {
                var orbT = Orbwalker.GetTarget();

                var t = TargetSelector.GetTarget(GetBonudRange() + 60, DamageType.Physical);

                if (t.NewIsValidTarget())
                {
                    if (!FishBoneActive && (!t.InAutoAttackRange() || t.CountEnemyHerosInRangeFix(250) > 1) && orbT == null)
                    {
                        if (Player.Mana > E.Instance.ManaCost + 20 || (ComboUseQCHARGE && GetBonudDamage(t) > t.GetRealHeath(DamageType.Physical)))
                        {
                            
                            Q.Cast();
                        }

                    }else if(!FishBoneActive  && orbT != null)
                    {
                        if ((Player.Mana > E.Instance.ManaCost + 20 && orbT.Position.CountEnemyHerosInRangeFix(250) > 1) || (ComboUseQCHARGE && GetBonudDamage(t) > t.GetRealHeath(DamageType.Physical)))
                        {
                            Q.Cast();
                        }
                    }
                }
                else if (!FishBoneActive && Player.Mana > E.Instance.ManaCost + 40 && Player.CountEnemyHerosInRangeFix(2000) > 0)
                {
                    Q.Cast();
                }
                else if (FishBoneActive && Player.Mana < 40)
                {
                    Q.Cast();
                }
                else if (FishBoneActive && Player.CountEnemyHerosInRangeFix(2000) == 0)
                {
                    Q.Cast();
                }
            }
            if(ComboUseW && W.IsReady() && !Player.IsWindingUp)
            {
                var orbT = Orbwalker.GetTarget();
                var Ret = IMPGetTarGet(W, false, HitChance.High);
                if (!Ret.SuccessFlag || !Ret.Obj.IsValid)
                {
                    return;
                }
                if (Ret.Obj != null && orbT == null)
                {
                    //如果W范围内有目标但是走砍范围内没有目标时
                    W.Cast(Ret.CastPosition);
                }
                else if (ComboUseWMode != 2 && (orbT != null && Ret.Obj.InAutoAttackRange()) || (orbT == Ret.Obj && orbT.InAutoAttackRange()))
                {
                    if(ComboUseWMode == 0 && !Orbwalker.CanAttack(W.Delay))
                    {
                        //0.6s下一次普攻 释放W
                        if (!InMelleAttackRange(Player.ServerPosition) && (Player.AttackDelay * 0.7 > W.Delay || !(Ret.Obj.GetRealHeath(DamageType.Physical) < GetBonudDamage(Ret.Obj) * 2 && Ret.Obj.GetRealHeath(DamageType.Physical) < Player.GetAutoAttackDamage(Ret.Obj) + W.GetDamage(Ret.Obj))))
                        {
                            W.Cast(Ret.CastPosition);
                        }
                    }
                    else if(ComboUseWMode == 1)
                    {
                        W.Cast(Ret.CastPosition);
                    }
                }
            }
        }
        private float GetBonudRange(AIBaseClient unit = null)
        {
            float ExtraRange = 100 + (25 * (Player.Spellbook.GetSpell(SpellSlot.Q).Level - 1));
            return Player.AttackRange + Player.BoundingRadius + ExtraRange + (unit == null ? 0 : unit.BoundingRadius);
        }
        private float GetRealDistance(AIBaseClient target)
        {
            return Player.Distance(target) + Player.BoundingRadius + target.BoundingRadius;
        }
        private float GetRealPowPowRange(GameObject target)
        {
            return Player.AttackRange + Player.BoundingRadius + target.BoundingRadius;
        }
        private bool IsMovingInSameDirection(AIBaseClient source, AIBaseClient target)
        {
            var sourceLW = source.GetWaypoints().Last();

            if (sourceLW == source.ServerPosition.ToVector2() || !source.IsMoving)
            {
                return false;
            }

            var targetLW = target.GetWaypoints().Last();

            if (targetLW == target.ServerPosition.ToVector2() || !target.IsMoving)
            {
                return false;
            }
            var ensAngle = (sourceLW - source.ServerPosition.ToVector2()).AngleBetween(targetLW - target.ServerPosition.ToVector2());
            if (ensAngle < 40) 
                return true;
            return false;
        }
        private float GetRDmg(AIBaseClient unit, float FlyTime, float distance)
        {
            if (unit == null)
                return 0f;
            int level = Player.Spellbook.GetSpell(SpellSlot.R).Level;
            if (level == 0)
                return 0f;

            var minDamage_Base = new[] { 0, 25, 40, 55 }[level];
            var MaxDamage_Base = new[] { 0, 250, 350, 450 }[level];
            var PercentDamage = new[] { 0, 0.25, 0.3, 0.35 }[level];
            var MinDamage = minDamage_Base + 0.15 * (Player.TotalAttackDamage - Player.BaseAttackDamage);
            var MaxDamage = MaxDamage_Base + 1.5 * (Player.TotalAttackDamage - Player.BaseAttackDamage);

            var ExtraDmg = PercentDamage * (unit.MaxHealth - unit.GetRealHeath(DamageType.Physical));
            if (FlyTime <= 1f) //飞行时间小于1
            {
                //最小伤害值 86 目前30 蓄力0.2秒

                //86 - 30 / 0.75 = 74;
                return (float)EnsoulSharp.SDK.Damage.CalculatePhysicalDamage(Player, unit, MinDamage + ExtraDmg);
            }
            else
            {
                if (distance >= 1500)
                {
                    return (float)EnsoulSharp.SDK.Damage.CalculatePhysicalDamage(Player, unit, MaxDamage + ExtraDmg);
                }
                float DmgPercent = 0.1f + (int)(distance / 100) * 0.06f;
                var endDamage = MaxDamage * DmgPercent + ExtraDmg;
                return (float)EnsoulSharp.SDK.Damage.CalculatePhysicalDamage(Player, unit, endDamage);
            }

        }
        private float GetBonudDamage(AIBaseClient t)
        {
            return (float)Player.CalculatePhysicalDamage(t, Player.TotalAttackDamage * 1.1f);
        }
        private float GetPowPowDamage(AIBaseClient t)
        {
            return (float)Player.CalculatePhysicalDamage(t, Player.TotalAttackDamage * 1.0f);
        }
    }
}
