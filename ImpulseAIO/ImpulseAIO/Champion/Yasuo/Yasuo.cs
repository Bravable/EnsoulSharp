using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Utility;
using System.Text.RegularExpressions;
using SharpDX;
using ImpulseAIO.Common;

namespace ImpulseAIO.Champion.Yasuo
{
    internal class Yasuo : Base
    {
        private static Spell Q, Q3, W, E, R;
        private static float ESpeed => 750 + (Player.MoveSpeed * 0.6f);
        private static bool HaveQ3 => Q.Name == "YasuoQ3Wrapper";
        private static bool HaveQ2 => Player.HasBuff("YasuoQ1");
        private static bool HaveQ1 => (!HaveQ2 && !HaveQ3);
        private static float QDealy => 0.4f * (1 - Math.Min(((Player.AttackSpeedMod - 1) * 100f / 1.67f * 0.01f), 0.67f));

        private static int EDelay = 0;

        private static int RDelay = 0;
        private static int LaneClearDelay = 0;
        private static int QQDelay = 0;
        private List<AIBaseClient> GetDashObj
            =>
                Cache.EnemyHeroes.Cast<AIBaseClient>().Where(x => x.IsEnemy)
                    .Concat(Cache.GetJungles(Player.Position))
                    .Concat(Cache.GetMinions(Player.Position))
                    .Where(i => CanCastE(i))
                    .ToList();
        private static AIHeroClient NearMouseTarget => TargetSelector.GetTarget(250f, DamageType.Physical, true, Game.CursorPos);
        private static AIHeroClient GetQCirTarget => TargetSelector.GetTarget(300f, DamageType.Physical, true, Player.GetDashInfo().EndPos.ToVector3(), null);
        private List<AIHeroClient> GetRTarget => Cache.EnemyHeroes.Where(i => i.IsEnemy && R.IsInRange(i) && CanCastR(i)).ToList();
        private static bool ComboUseE => ChampionMenu["Combo"]["CE"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseEMode => ChampionMenu["Combo"]["CEMode"].GetValue<MenuKeyBind>().Active;
        private static int ComboUseEDIS => ChampionMenu["Combo"]["CEDIS"].GetValue<MenuSlider>().Value;
        private static bool ComboUseEStackQ => ChampionMenu["Combo"]["CEStackQ"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseCEOnlyQ23 => ChampionMenu["Combo"]["CEOnlyQ23"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseBug => ChampionMenu["Combo"]["CEBUG"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseR => ChampionMenu["Combo"]["CR"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseRDelay => ChampionMenu["Combo"]["CRDelay"].GetValue<MenuBool>().Enabled;
        private static int ComboUseRIfHP => ChampionMenu["Combo"]["RHpU"].GetValue<MenuSlider>().Value;
        private static int ComboUseRCount => ChampionMenu["Combo"]["RCountA"].GetValue<MenuSlider>().Value;
        private static bool ComboRSF => ChampionMenu["Combo"]["RSF"].GetValue<MenuBool>().Enabled;
        private static bool ComboCeAllc => ChampionMenu["Combo"]["CeAllc"].GetValue<MenuBool>().Enabled;
        private static bool HarassUseQ3 => ChampionMenu["Harass"]["HQ3"].GetValue<MenuBool>().Enabled;
        private static bool HarassUseQListHit => ChampionMenu["Harass"]["HQLastHit"].GetValue<MenuBool>().Enabled;
        private static bool LaneClearUseQ3 => ChampionMenu["Clear"]["LaneClear"]["LQ3"].GetValue<MenuBool>().Enabled;
        private static bool LaneClearUseE => ChampionMenu["Clear"]["LaneClear"]["LE"].GetValue<MenuBool>().Enabled;
        private static bool LaneClearUseOnlyLastHit => ChampionMenu["Clear"]["LaneClear"]["LELastHit"].GetValue<MenuBool>().Enabled;
        private static bool JungleUseQ3 => ChampionMenu["Clear"]["JungleClear"]["JQ3"].GetValue<MenuBool>().Enabled;
        private static bool JungleUseE => ChampionMenu["Clear"]["JungleClear"]["JE"].GetValue<MenuBool>().Enabled;
        private static bool LastHitUseQ => ChampionMenu["LastHit"]["KQ12"].GetValue<MenuBool>().Enabled;
        private static bool LastHitUseQ3 => ChampionMenu["LastHit"]["KQ3"].GetValue<MenuBool>().Enabled;
        private static bool LastHitUseE => ChampionMenu["LastHit"]["KE"].GetValue<MenuBool>().Enabled;
        private static bool FleeUseQ3 => ChampionMenu["Flee"]["FQ"].GetValue<MenuBool>().Enabled;
        private static bool FleeUseE => ChampionMenu["Flee"]["FE"].GetValue<MenuBool>().Enabled;
        public static bool FleeUseStackQ => ChampionMenu["Flee"]["FEStackQ"].GetValue<MenuBool>().Enabled;
        public static bool DrawQRange => ChampionMenu["Drawing"]["DQ"].GetValue<MenuBool>().Enabled;
        public static bool DrawERange => ChampionMenu["Drawing"]["DE"].GetValue<MenuBool>().Enabled;
        public static bool DrawRRange => ChampionMenu["Drawing"]["DR"].GetValue<MenuBool>().Enabled;
        public static bool DrawTarget => ChampionMenu["Drawing"]["DrawTarget"].GetValue<MenuBool>().Enabled;
        private static bool UseQ3AntiGap => ChampionMenu["Other"]["OEATG"].GetValue<MenuBool>().Enabled;
        private static bool UseQ3Interrupter => ChampionMenu["Other"]["OEInttput"].GetValue<MenuBool>().Enabled;
        private static bool UnderTower => ChampionMenu["Other"]["OQUnderTower"].GetValue<MenuKeyBind>().Active;
        private static bool HideQ => ChampionMenu["Other"]["HideQ"].GetValue<MenuBool>().Enabled;
        private static bool HideR => ChampionMenu["Other"]["HideR"].GetValue<MenuBool>().Enabled;
        private static bool AutoRC => ChampionMenu["Other"]["AutoRC"].GetValue<MenuBool>().Enabled;
        public Yasuo()
        {
            Q = new Spell(SpellSlot.Q, 475f);
            Q.SetSkillshot(0.25f, 55f, 5000f, false, SpellType.Line);
            Q3 = new Spell(SpellSlot.Q, 1070f);
            W = new Spell(SpellSlot.W, 400f);
            E = new Spell(SpellSlot.E, 475f);
            R = new Spell(SpellSlot.R, 1200f);
            Q3.SetSkillshot(0.25f, 90f, 1200f, false, SpellType.Line);
            E.SetTargetted(0f, 1400f);
            R.SetTargetted(0, float.MaxValue);
            Q.DamageType = Q3.DamageType = R.DamageType = DamageType.Physical;

            EvadeTarget.Init();
            OnMenuLoad();

            AIBaseClient.OnDoCast += Game_OnDoCast;
            AIBaseClient.OnDoCast += Game_HideAnimation;
            Game.OnUpdate += Game_OnUpdate;
            GameEvent.OnGameTick += Game_OnTick;
            Render.OnEndScene += Game_OnDraw;
            Render.OnRenderMouseOvers += (args) => {
                if (EnsoulSharp.Hacks.DisableDrawings)
                {
                    return;
                }
                if (DrawTarget && NearMouseTarget != null)
                {
                    if (ComboUseE && ComboCeAllc && ComboUseEMode)
                    {
                        NearMouseTarget.Glow(System.Drawing.Color.Purple, 5, 1);
                    }
                }
            };
            Interrupter.OnInterrupterSpell += (s, g) => { 
                if(!UseQ3Interrupter || !Q.IsReady() || !HaveQ3 || Player.IsDashing())
                {
                    return;
                }
                var Preds = Q3.GetPrediction(s);
                if(Preds.Hitchance >= HitChance.High)
                {
                    Q3.Cast(Preds.CastPosition);
                }
            };
            AntiGapcloser.OnGapcloser += (s, g) => {
                if (!s.IsDashing() || !UseQ3AntiGap || !Q.IsReady() || !HaveQ3 || Player.IsDashing())
                {
                    return;
                }
                var Preds = Q3.GetPrediction(s);
                if (Preds.Hitchance >= HitChance.High)
                {
                    Q3.Cast(Preds.CastPosition);
                }
            };
        }
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Yasuo));
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo"));
            {
                Combo.Add(new MenuBool("CE", "Use E"));
                Combo.Add(new MenuKeyBind("CEMode", Program.Chinese ? "是否跟随鼠标突进" : "Allow Mouse", Keys.A, KeyBindType.Toggle)).AddPermashow();
                Combo.Add(new MenuSlider("CEDIS", Program.Chinese ? "^-当敌我距离 >= X时才用E" : "Use E When EnemyDisMe >= X", 300, 0, (int)E.Range));
                Combo.Add(new MenuBool("CeAllc", Program.Chinese ? "^-如果E跟随鼠标 那么离鼠标范围最近的角色成为击杀目标" : "If E follows the mouse, the character closest to the mouse range becomes the kill target"));
                Combo.Add(new MenuBool("CEStackQ", Program.Chinese ? "^-E中叠Q" : "Stack Q", false));
                Combo.Add(new MenuBool("CEOnlyQ23", Program.Chinese ? "^-仅当有Q2/Q3时才E敌方英雄" : "Use E Only Have Q1 or Q2", false));
                Combo.Add(new MenuBool("CEBUG", Program.Chinese ? "^-使用双凤漏洞" : "Bug eQ", false));
                Combo.Add(new MenuBool("CR", "Use R"));
                Combo.Add(new MenuBool("CRDelay", Program.Chinese ? "延迟释放" : "Delay Cast"));
                Combo.Add(new MenuSlider("RHpU", Program.Chinese ? "^-仅当敌人血量 <= X时释放" : "Only Enemy Health <= X%", 30, 0, 100));
                Combo.Add(new MenuSlider("RCountA", Program.Chinese ? "^-或者当被击飞的人数 >= X" : "Or KnockUp Count >= X", 2, 1, 5));
                Combo.Add(new MenuBool("RSF", Program.Chinese ? "^-尝试落地双凤" : "Try EQ -> R"));
            }
            var Harass = ChampionMenu.Add(new Menu("Harass", "Harass"));
            {
                Harass.Add(new MenuBool("HQ3", Program.Chinese ? "使用 Q3" : "Use Q3"));
                Harass.Add(new MenuBool("HQLastHit", Program.Chinese ? "如果目标未在Q范围内 则用 Q1/Q2补刀" : "UseQ1 / Q2 LastHit if Target Don't in Q Range"));
            }
            var Clear = ChampionMenu.Add(new Menu("Clear", "Lane / JungleClear"));
            {
                var LanceClear = Clear.Add(new Menu("LaneClear", "LaneClear"));
                {
                    LanceClear.Add(new MenuBool("LQ3", Program.Chinese ? "释放 Q3 清线" : "Use Q3"));
                    LanceClear.Add(new MenuBool("LE", Program.Chinese ? "使用 E 清线" : "Use E"));
                    LanceClear.Add(new MenuBool("LELastHit", Program.Chinese ? "只用E进行尾刀" : "Only Lasthit E"));

                }
                var JungleClear = Clear.Add(new Menu("JungleClear", "JungleClear"));
                {
                    JungleClear.Add(new MenuBool("JQ3", Program.Chinese ? "释放 Q3 清野" : "Use Q3"));
                    JungleClear.Add(new MenuBool("JE", Program.Chinese ? "使用 E 清野" : "Use E"));
                }
            }
            var LastHit = ChampionMenu.Add(new Menu("LastHit", "LastHit"));
            {
                LastHit.Add(new MenuSeparator("KQSet", "Q Set"));
                {
                    LastHit.Add(new MenuBool("KQ12", Program.Chinese ? "使用 Q 尾刀" : "Use Q"));
                    LastHit.Add(new MenuBool("KQ3", Program.Chinese ? "使用 Q3 尾刀" : "Use Q3"));
                }
                LastHit.Add(new MenuSeparator("KESet", "E Set"));
                {
                    LastHit.Add(new MenuBool("KE", Program.Chinese ? "使用 E 尾刀" : "Use E"));
                }
            }
            var Flee = ChampionMenu.Add(new Menu("Flee", "Flee"));
            {
                Flee.Add(new MenuBool("FQ", Program.Chinese ? "使用 Q3 技能" : "Use Q3"));
                Flee.Add(new MenuBool("FE", Program.Chinese ? "使用 E 技能" : "Use E"));
                Flee.Add(new MenuBool("FEStackQ", Program.Chinese ? "E中叠Q到Q3" : "Stack Q3"));
            }
            var Drawing = ChampionMenu.Add(new Menu("Drawing", "Draw"));
            {
                Drawing.Add(new MenuBool("DQ", "Draw Q Range"));
                Drawing.Add(new MenuBool("DE", "Draw E Range"));
                Drawing.Add(new MenuBool("DR", "Draw R Range"));
                Drawing.Add(new MenuBool("DrawTarget", Program.Chinese ? "高亮显示欲击杀目标(请关闭屏蔽OBS)" : "Highlight the target to kill(Please Disable Anti-OBS)"));
            }
            var Other = ChampionMenu.Add(new Menu("Other", "Misc"));
            {
                Other.Add(new MenuKeyBind("OQUnderTower", Program.Chinese ? "是否进入塔下" : "UnderTower", EnsoulSharp.SDK.MenuUI.Keys.A, KeyBindType.Toggle)).AddPermashow();
                Other.Add(new MenuBool("OEInttput", Program.Chinese ? "使用Q3中段施法" : "Use Q3 Interrupt"));
                Other.Add(new MenuBool("OEATG", Program.Chinese ? "使用 Q3 反突进" : "Use Q3 Anti GapCloser"));
                Other.Add(new MenuBool("HideQ", Program.Chinese ? "隐藏 Q 施法动作" : "Hide Q CastAnimation"));
                Other.Add(new MenuBool("HideR", Program.Chinese ? "隐藏 R 施法动作" : "Hide R CastAnimation"));
                Other.Add(new MenuBool("AutoRC", Program.Chinese ? "使用 R 自动接狗牌" : "Play Ctrl+6 When R Cast"));
            }
            AntiGapcloser.Attach(ChampionMenu);
            ChampionMenu.Add(new MenuSeparator("DF", "This story is not yet finished..."));
        }
        private void Game_OnDraw(EventArgs args)
        {
            if (DrawQRange && Q.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, HaveQ3 ? Q3.Range : Q.Range, Color.Green);
            }
            if (DrawERange && E.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, E.Range, Color.Orange);
            }
            if (DrawRRange && R.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, R.Range, Color.Red);
            }
        }
        private void Game_OnDoCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.Slot == SpellSlot.E)
                {
                    var To = args.Target as AIBaseClient;
                    if(To != null)
                    {
                        Orbwalker.AttackEnabled = false;
                        var FlyTime = (int)((Player.ServerPosition.Distance(PosAfterE(To)) / E.Speed) * 1000f);
                        DelayAction.Add(FlyTime + 300, () => {
                            Orbwalker.AttackEnabled = true;
                        });
                    }
                }
            }
        }
        private void Game_HideAnimation(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (HideQ && args.Slot == SpellSlot.Q)
                {
                    Game.SendEmote(EmoteId.Dance);
                }
                if (args.Slot == SpellSlot.R)
                {
                    if (AutoRC)
                    {
                        Game.SendMasteryBadge();
                    }
                    if (HideR)
                    {
                        Game.SendEmote(EmoteId.Dance);
                    }
                }
            }
        }
        private void Game_OnUpdate(EventArgs s)
        {
            Q.Delay = Q3.Delay = QDealy;
            E.Speed = ESpeed;
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
                    Flee();
                    break;
            }
        }
        private void Game_OnTick(EventArgs s)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && R.IsReady() && ComboUseR && GetRTarget.Count > 0)
            {
                var hero = (from enemy in GetRTarget
                            let nearEnemy =
                                Cache.EnemyHeroes.Where(i => i.IsEnemy && i.Distance(enemy) < 400 && CanCastR(i)).ToList()
                            where
                                (nearEnemy.Count > 1
                                 && enemy.GetRealHeath(DamageType.Physical) <= Player.GetSpellDamage(enemy, SpellSlot.R))
                                || (nearEnemy.Count > 0
                                    && nearEnemy.Sum(i => i.HealthPercent) / nearEnemy.Count
                                    <= ComboUseRIfHP) || nearEnemy.Count >= ComboUseRCount
                            orderby nearEnemy.Count descending
                            select enemy).ToList();
                if (hero.Count > 0)
                {
                    if (ComboRSF)
                    {
                        if ((E.IsReady() || Player.IsDashing()) && Q.IsReady(500))
                        {
                            var CanEObj = GameObjects.Get<AIBaseClient>().Where(x => x.IsValidTarget(E.Range) && x.IsTargetable && CanCastE(x)).FirstOrDefault();
                            if (CanEObj != null)
                            {
                                E.CastOnUnit(CanEObj);
                                EDelay = Variables.GameTimeTickCount + 120;
                                RDelay = EDelay + 10;
                            }
                        }
                    }
                    if (Variables.GameTimeTickCount > RDelay)
                    {
                        var target = !ComboUseRDelay ? hero.FirstOrDefault() : hero.FirstOrDefault(CanCastDelayR);
                        if (target != null)
                        {
                            R.Cast(target.Position);
                        }
                    }
                }
            }
        }
        private void Combo()
        {
            if (ComboUseE && E.IsReady() && !Player.IsWindingUp)
            {
                if (!ComboUseEMode)
                {
                    var target = Q.GetTarget(75);
                    if (target != null && HaveQ3 && Q.IsReady(500))
                    {
                        var nearObj = GetNearObj(target, true, UnderTower, false);
                        if (nearObj != null
                            && (PosAfterE(nearObj).CountEnemyHerosInRangeFix(300) > 1 || Player.Position.CountEnemyHerosInRangeFix(Q.Range + E.Range / 2) == 1)
                            && E.CastOnUnit(nearObj))
                        {
                            EDelay = Variables.GameTimeTickCount + 250;
                            return;
                        }
                    }
                    target = Q.GetTarget(Q.Width) ?? Q3.GetTarget();
                    if (target != null)
                    {
                        var nearObj = GetNearObj(target, false, UnderTower, false);
                        if (nearObj != null
                            && (nearObj.NetworkId == target.NetworkId
                                    ? !target.InAutoAttackRange()
                                    : Player.Distance(target) > ComboUseEDIS + nearObj.BoundingRadius) && E.CastOnUnit(nearObj))
                        {
                            EDelay = Variables.GameTimeTickCount + 250;
                            return;
                        }
                    }
                }
                else
                {
                    if(ComboCeAllc && NearMouseTarget != null)
                    {
                        var nearObj = GetNearObj(NearMouseTarget, false, UnderTower, false);
                        if (nearObj != null
                            && (nearObj.NetworkId == NearMouseTarget.NetworkId
                                    ? !NearMouseTarget.InAutoAttackRange()
                                    : Player.Distance(NearMouseTarget) > ComboUseEDIS) && E.CastOnUnit(nearObj))
                        {
                            EDelay = Variables.GameTimeTickCount + 250;
                            return;
                        }
                    }
                    else
                    {
                        var nearObj = GetNearObj(null, false, UnderTower);
                        if (nearObj != null && Player.Distance(Game.CursorPos) > ComboUseEDIS
                            && E.CastOnUnit(nearObj))
                        {
                            EDelay = Variables.GameTimeTickCount + 250;
                            return;
                        }
                    }
                }
            }
            if (Q.IsReady() && EDelay < Variables.GameTimeTickCount && QQDelay < Variables.GameTimeTickCount)
            {
                if (RDelay > EDelay && Variables.GameTimeTickCount - RDelay < 120)
                {
                    if(Q.Cast(ComboUseBug ? new Vector3(50000f, 50000f, 50000f) : Player.Position))
                    {
                        QQDelay = Variables.GameTimeTickCount + 50;
                    }
                    R.Cast(GetRTarget.FirstOrDefault().Position);
                    return;
                }
                if (Player.IsDashing())
                {
                    if (GetQCirTarget != null)
                    {
                        var CastPos = Player.Position.Extend(GetQCirTarget.Position, ComboUseBug ? 50000f : Q.Range);
                        if (Q.Cast(CastPos))
                        {
                            QQDelay = Variables.GameTimeTickCount + 50;
                        }
                    }
                    if (!HaveQ3 && ComboUseE && ComboUseEStackQ
                        && Player.Position.CountEnemyHerosInRangeFix(450) == 0 && StackQ())
                    {
                        QQDelay = Variables.GameTimeTickCount + 50;
                        return;
                    }
                }
                else
                {
                    if (!Player.Spellbook.IsWindingUp)
                    {
                        if (!HaveQ3)
                        {
                            var target = Q.GetTarget(Q.Width / 2);
                            if (target.IsValidTarget())
                            {
                                var pred = Q.GetPrediction(target, true);
                                if (pred.Hitchance >= HitChance.High)
                                {
                                    if (Q.Cast(pred.CastPosition))
                                    {
                                        QQDelay = Variables.GameTimeTickCount + 50;
                                    }
                                    return;
                                }
                            }
                        }
                        else if (CastQ3())
                        {
                            return;
                        }
                    }
                }
            }
        }
        private void Harass()
        {
            if (!Q.IsReady() || Player.IsDashing())
                return;

            if (!HaveQ3)
            {
                var state = Q.CastOnBestTarget();
                if (state == CastStates.SuccessfullyCasted)
                    return;
                if ((state == CastStates.InvalidTarget || state == CastStates.NotCasted) && HarassUseQListHit && Q.GetTarget() == null)
                {
                    var minion =
                        Cache.GetMinions(Player.Position).Where(
                            i =>
                            i.IsEnemy && (i.IsMinion() || i.IsPet(false)) && i.IsValidTarget() && GetQHpPred(i) > 0
                            && GetQHpPred(i) <= GetQDmg(i)
                            && (i.IsUnderAllyTurret() || (i.IsUnderEnemyTurret() && !Player.IsUnderEnemyTurret())
                                || i.DistanceToPlayer() > i.GetRealAutoAttackRange() + 50
                                || i.Health > Player.GetAutoAttackDamage(i))).MaxOrDefault(i => i.MaxHealth);
                    if (minion != null)
                    {
                        Q.Cast(minion);
                    }
                }
            }
            else if (HarassUseQ3)
                CastQ3();
        }
        private void LaneClear()
        {
            if (!Enable_laneclear)
                return;

            if ((LaneClearUseE || JungleUseE) && E.IsReady() && !Player.IsDashing())
            {
                var minion = new List<AIBaseClient>();
                minion.AddRange(Cache.GetMinions(Player.Position,E.Range));
                minion.AddRange(Cache.GetJungles(Player.Position,E.Range));
                minion =
                    minion.Where(i => CanCastE(i) && (!PosAfterE(i).IsUnderEnemyTurret() || UnderTower))
                        .OrderByDescending(i => i.MaxHealth)
                        .ToList();
                if (minion.Count > 0)
                {
                    var obj =
                        minion.FirstOrDefault(
                            i => E.GetHealthPrediction(i) > 0 && E.GetHealthPrediction(i) <= GetEDmg(i));
                    if (obj == null && Q.IsReady(500) && (!HaveQ3 || (LaneClearUseQ3 || JungleUseQ3)))
                    {
                        var sub = new List<AIBaseClient>();
                        foreach (var mob in minion)
                        {
                            if (((E.GetHealthPrediction(mob) > 0
                                  && E.GetHealthPrediction(mob) - GetEDmg(mob) <= GetQDmg(mob))
                                 || mob.Team == GameObjectTeam.Neutral) && mob.Distance(PosAfterE(mob)) < 225f)
                            {
                                sub.Add(mob);
                            }
                            if (LaneClearUseOnlyLastHit)
                            {
                                continue;
                            }
                            var nearMinion = new List<AIBaseClient>();
                            nearMinion.AddRange(Cache.GetMinions(Player.Position, E.Range));
                            nearMinion.AddRange(Cache.GetJungles(Player.Position, E.Range));
                            nearMinion =
                                nearMinion.Where(i => i.IsValidTarget(225f, true, PosAfterE(mob).ToVector3()))
                                    .ToList();
                            if (nearMinion.Count > 2
                                || nearMinion.Count(
                                    i => E.GetHealthPrediction(mob) > 0 && E.GetHealthPrediction(mob) <= GetQDmg(mob))
                                > 1)
                            {
                                sub.Add(mob);
                            }
                        }
                        obj = sub.FirstOrDefault();
                    }
                    if (obj.IsValidTarget() && (!obj.IsMinion() || (!LaneClearUseOnlyLastHit || obj.Health < GetEDmg(obj))))
                    {
                        E.CastOnUnit(obj);
                        LaneClearDelay = Variables.GameTimeTickCount + 250;
                        return;
                    }
                }
            }
            if (Q.IsReady() && (LaneClearDelay < Variables.GameTimeTickCount || Player.IsDashing()))
            {
                if (Player.IsDashing())
                {
                    if (!HaveQ1)
                        Q3.Cast(ComboUseBug ? new Vector3(50000f, 50000f, 50000f) : Player.Position);
                    return;
                }
                else
                {
                    var minion = new List<AIBaseClient>();
                    minion.AddRange(Cache.GetMinions(Player.Position, E.Range));
                    minion.AddRange(Cache.GetJungles(Player.Position, E.Range));
                    minion =
                        minion.Where(i => i.IsValidTarget((!HaveQ3 ? Q : Q3).Range - Q.Width))
                            .OrderByDescending(i => i.MaxHealth)
                            .ToList();
                    if (minion.Count == 0)
                    {
                        return;
                    }
                    if (!HaveQ3)
                    {
                        var obj =
                            minion.FirstOrDefault(
                                i => Q.GetHealthPrediction(i) > 0 && Q.GetHealthPrediction(i) <= GetQDmg(i));
                        if (obj != null)
                        {
                            Q.Cast(obj.Position);
                            return;
                        }
                    }
                    if (LaneClearUseQ3)
                    {
                        var pos = Q3.GetLineFarmLocation(minion.Select(i => i.Position.ToVector2()).ToList());
                        if (pos.MinionsHit > 0)
                        {
                            Q3.Cast(pos.Position);
                        }
                    }
                }

            }
        }
        private void LastHit()
        {
            if (Q.IsReady() && !Player.IsDashing())
            {
                if (!HaveQ3 && LastHitUseQ)
                {
                    var minion =
                       Cache.GetMinions(Player.Position,Q.Range).Where(
                           i =>
                           i.IsEnemy && (i.IsMinion() || i.IsPet(false)) && GetQHpPred(i) > 0
                           && GetQHpPred(i) <= GetQDmg(i)
                           && (i.IsUnderAllyTurret() || (i.IsUnderEnemyTurret() && !Player.IsUnderEnemyTurret())
                               || i.DistanceToPlayer() > i.GetRealAutoAttackRange() + 50
                               || i.Health > Player.GetAutoAttackDamage(i))).MaxOrDefault(i => i.MaxHealth);
                    if (minion != null && Q.Cast(minion) == CastStates.SuccessfullyCasted)
                    {
                        return;
                    }
                }
                else if (HaveQ3 && LastHitUseQ3)
                {
                    var minion =
                        Cache.GetMinions(Player.Position, Q3.Range).Where(
                            i =>
                            i.IsEnemy && (i.IsMinion() || i.IsPet(false)) && i.IsValidTarget(Q3.Range - i.BoundingRadius / 2)
                            && Q3.GetHealthPrediction(i) > 0 && Q3.GetHealthPrediction(i) <= GetQDmg(i)
                            && (i.IsUnderAllyTurret() || (i.IsUnderEnemyTurret() && !Player.IsUnderEnemyTurret())
                                || i.DistanceToPlayer() > i.GetRealAutoAttackRange() + 50
                                || i.Health > Player.GetAutoAttackDamage(i))).MaxOrDefault(i => i.MaxHealth);
                    if (minion != null)
                    {
                        Q3.Cast(minion);
                    }
                }
            }
            if (LastHitUseE && E.IsReady() && !Player.Spellbook.IsWindingUp)
            {
                var minion =
                    Cache.GetMinions(Player.Position, E.Range).Where(
                        i =>
                        i.IsEnemy && (i.IsMinion() || i.IsPet(false)) && CanCastE(i) && E.GetHealthPrediction(i) > 0
                        && E.GetHealthPrediction(i) <= GetEDmg(i)
                        && !PosAfterE(i).IsUnderEnemyTurret()
                        && (i.IsUnderAllyTurret() || (i.IsUnderEnemyTurret() && !Player.IsUnderEnemyTurret())
                            || i.DistanceToPlayer() > i.GetRealAutoAttackRange() + 50
                            || i.Health > Player.GetAutoAttackDamage(i))).MaxOrDefault(i => i.MaxHealth);
                if (minion != null)
                {
                    E.CastOnUnit(minion);
                }
            }
        }
        private void Flee()
        {
            if (FleeUseQ3 && Q.IsReady() && HaveQ3 && !Player.IsDashing())
            {
                var target = Cache.EnemyHeroes.Where(x => x.IsEnemy && Q3.IsInRange(x) && !x.IsDead && x.IsValidTarget()).MinOrDefault(x => x.DistanceToPlayer());
                if (target.IsValidTarget())
                {
                    var pred = Q3.GetPrediction(target);
                    if (pred.Hitchance >= HitChance.High)
                    {
                        Q3.Cast(pred.CastPosition);
                    }
                }
            }
            if (!FleeUseE)
            {
                return;
            }
            var obj = GetBestObjToMouse();
            if (obj != null && E.IsReady())
            {
                E.CastOnUnit(obj);
            }
            if (Player.IsDashing() && !HaveQ3 && FleeUseStackQ && Q.IsReady() && QQDelay < Variables.GameTimeTickCount)
            {
                if (Q.Cast(ComboUseBug ? new Vector3(50000f, 50000f, 50000f) : Player.Position))
                {
                    QQDelay = Variables.GameTimeTickCount + 50;
                }
            }
        }
        private AIBaseClient GetBestObjToMouse(bool underTower = true)
        {
            var pos = Game.CursorPos;
            return
                GetDashObj.Where(x => PosAfterE(x).Distance(pos) < Player.Distance(pos))
                    .MinOrDefault(i => PosAfterE(i).Distance(pos));
        }
        private double GetEDmg(AIBaseClient target)
        {
            var stacksPassive = Player.Buffs.Find(b => b.Name.Equals("YasuoDashScalar"));
            var stacks = 1 + 0.25 * ((stacksPassive != null) ? stacksPassive.Count : 0);
            var damage = ((50 + 10 * E.Level) * stacks) + (Player.FlatMagicDamageMod * 0.6) + (Player.FlatPhysicalDamageMod * 0.2); //0.2额外物理
            return Player.CalculateMagicDamage(target, damage) - 15;
        }
        private float GetQHpPred(AIBaseClient minion)
        {
            return HealthPrediction.GetPrediction(minion, (int)(Q.Delay * 1000 - 100));
        }
        private double GetQDmg(AIBaseClient target)
        {
            var dmgItem = 0d;
            if (Player.HasItem((int)ItemId.Sheen) && (Player.CanUseItem((int)ItemId.Sheen) || Player.HasBuff("Sheen")))
            {
                dmgItem = Player.BaseAttackDamage;
            }
            if (Player.HasItem((int)ItemId.Trinity_Force)
                && (Player.CanUseItem((int)ItemId.Trinity_Force) || Player.HasBuff("Sheen")))
            {
                dmgItem = Player.BaseAttackDamage * 2;
            }
            if (dmgItem > 0)
            {
                dmgItem = Player.CalculateDamage(target, DamageType.Physical, dmgItem);
            }
            return Q.GetDamage(target) + dmgItem;
        }
        private bool CastQ3()
        {
            var target = TargetSelector.GetTarget(Q3.Range + (Q3.Width / 2), DamageType.Physical);
            if (target != null)
            {
                var pred = Q3.GetPrediction(target, true, -1, new CollisionObjects[] { CollisionObjects.YasuoWall });

                if (pred.Hitchance >= HitChance.High)
                {
                    if (Q3.Cast(pred.CastPosition))
                    {
                        QQDelay = Variables.GameTimeTickCount + 50;
                    }
                }
            }
            return true;
        }
        private bool StackQ()
        {
            var pos = Player.GetDashInfo().EndPos.ToVector3World();
            var ObjList = new List<AIBaseClient>();
            ObjList.AddRange(Cache.EnemyHeroes.Where(x => x.IsEnemy));
            ObjList.AddRange(Cache.GetMinions(Player.Position));
            ObjList = ObjList.Where(y => y.IsValidTarget(300f, true, pos)).ToList();
            if (ObjList.Count == 0)
            {
                return false;
            }
            var target = ObjList.FirstOrDefault();
            return target != null && Q.Cast(Player.Position.Extend(target.Position, ComboUseBug ? 50000f : Q.Range));
        }
        private bool CanCastR(AIHeroClient target)
        {
            return target.HasBuffOfType(BuffType.Knockback) || target.HasBuffOfType(BuffType.Knockup);
        }
        private bool CanCastDelayR(AIHeroClient target)
        {
            var buff =
                target.Buffs.FirstOrDefault(
                    i => i.IsValid && (i.Type == BuffType.Knockback || i.Type == BuffType.Knockup));
            return buff != null
                   && buff.EndTime - Game.Time
                   <= (buff.EndTime - buff.StartTime) / (buff.EndTime - buff.StartTime <= 0.5 ? 1.5 : 4);
        }
        private static Vector2 PosAfterE(AIBaseClient target)
        {
            return
                Player.Position.Extend(
                    target.ServerPosition,
                     E.Range).ToVector2();
        }
        private static bool CanCastE(AIBaseClient target)
        {
            if (target == null || target.IsDead)
                return false;
            return target.IsValidTarget(E.Range) && (!target.HasBuff("YasuoDashWrapper") && !target.HasBuff("YasuoE"));
        }
        private AIBaseClient GetNearObj(
          AIBaseClient target = null,
          bool inQCir = false,
          bool underTower = true,
          bool checkFace = false)
        {
            var pos = target != null
                          ? target.ServerPosition
                          : Game.CursorPos;
            var obj = new List<AIBaseClient>();


            obj.AddRange(Cache.EnemyHeroes.Where(i => i.IsEnemy && !i.InFountain() && ((R.IsReady() && GetRTarget.Count > 0) || !ComboUseCEOnlyQ23 || ((HaveQ2 || HaveQ3) && Q.IsReady(500)))));
            obj.AddRange(Cache.GetMinions(Player.Position));
            obj.AddRange(Cache.GetJungles(Player.Position));
            return
                obj.Where(
                    i =>
                    CanCastE(i) && (!checkFace || Player.IsFacing(i)) && (underTower || !PosAfterE(i).IsUnderEnemyTurret())
                    && PosAfterE(i).Distance(pos) < (inQCir ? 300 : Player.Distance(pos)))
                    .MinOrDefault(i => PosAfterE(i).Distance(pos));
        }

        internal class EvadeTarget
        {
            #region Static Fields

            private static readonly List<Targets> DetectedTargets = new List<Targets>();

            private static readonly List<SpellData> Spells = new List<SpellData>();

            private static Vector2 wallCastedPos;
            private static Menu EnemysMenu;
            #endregion

            #region Properties

            private static GameObject Wall
            {
                get
                {
                    return GameObjects.Get<MissileClient>().FirstOrDefault(i => i.IsValid &&
                            i.Name == "YasuoW_VisualMis" && i.Team == Player.Team);
                }
            }

            #endregion

            #region Public Methods and Operators

            public static Menu evadeMenu2, championmenu2;

            public static void Init()
            {
                LoadSpellData();

                evadeMenu2 = ChampionMenu.Add(new Menu("EvadeTarget", "风之障壁"));
                {
                    evadeMenu2.Add(new MenuBool("W", "使用 W")); //                                    evadeSpells.Add("ETower", new CheckBox("Under Tower", false));
                    evadeMenu2.Add(new MenuBool("E", "使用E (躲在风墙(W)后)"));
                    evadeMenu2.Add(new MenuBool("ETower", "-> 塔下", false));
                    evadeMenu2.Add(new MenuBool("BAttack", "平A"));
                    evadeMenu2.Add(new MenuSlider("BAttackHpU", "-> 如果血量 <", 35));
                    evadeMenu2.Add(new MenuBool("CAttack", "暴击"));
                    evadeMenu2.Add(new MenuSlider("CAttackHpU", "-> 如果血量 <", 40));
                    championmenu2 = evadeMenu2.Add(new Menu("EvadeTargetList", "躲避目标"));
                    foreach (
                        var spell in Spells.Where(i => GameObjects.EnemyHeroes.Any(a => a.CharacterName == i.ChampionName)))
                    {
                        EnemysMenu = championmenu2.Add(new Menu("scc" + spell.ChampionName, spell.ChampionName));

                        EnemysMenu.Add(new MenuBool(spell.MissileName,
                            spell.MissileName + " (" + spell.Slot + ")",
                            true));

                        /*championmenu2.Add(new MenuBool(spell.MissileName,
                            spell.MissileName + " (" + spell.Slot + ")",
                            true));*/
                    }
                }
                Game.OnUpdate += OnUpdateTarget;
                GameObject.OnCreate += ObjSpellMissileOnCreate;
                GameObject.OnDelete += ObjSpellMissileOnDelete;
                AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            }

            #endregion

            #region Methods

            private static bool GoThroughWall(Vector2 pos1, Vector2 pos2)
            {
                if (Wall == null)
                {
                    return false;
                }
                var wallWidth = 375f;
                var wallDirection = (Wall.Position.ToVector2() - wallCastedPos).Normalized().Perpendicular();
                var wallStart = Wall.Position.ToVector2() + wallWidth / 2f * wallDirection;
                var wallEnd = wallStart - wallWidth * wallDirection;
                var wallPolygon = new Geometry.Rectangle(wallStart, wallEnd, 75);
                var intersections = new List<Vector2>();
                for (var i = 0; i < wallPolygon.Points.Count; i++)
                {
                    var inter =
                        wallPolygon.Points[i].Intersection(
                            wallPolygon.Points[i != wallPolygon.Points.Count - 1 ? i + 1 : 0],
                            pos1,
                            pos2);
                    if (inter.Intersects)
                    {
                        intersections.Add(inter.Point);
                    }
                }
                return intersections.Any();
            }

            private static void LoadSpellData()
            {
                Spells.Add(
                    new SpellData
                    { ChampionName = "Ahri", 
                        SpellNames = new[] { "ahriwdamagemissileback1", "ahriwdamagemissilefront1", "ahriwdamagemissileright1" },
                        Slot = SpellSlot.W }
                    );
                Spells.Add(
                    new SpellData
                    { ChampionName = "Ahri", SpellNames = new[] { "ahritumblemissile" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData
                    { ChampionName = "Akshan", SpellNames = new[] { "akshanrmissile" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData { ChampionName = "Anivia", SpellNames = new[] { "frostbite" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { ChampionName = "Annie", SpellNames = new[] { "annieq" }, Slot = SpellSlot.Q });

                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Brand",
                        SpellNames = new[] { "brandr", "brandrmissile" },
                        Slot = SpellSlot.R
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Caitlyn",
                        SpellNames = new[] { "caitlynrmissile" },
                        Slot = SpellSlot.R
                    });

                Spells.Add(
                    new SpellData { ChampionName = "Elise", SpellNames = new[] { "elisehumanq" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Ezreal",
                        SpellNames = new[] { "ezrealemissile" },
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "FiddleSticks",
                        SpellNames = new[] { "fiddlesticksqmissilefear" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData { ChampionName = "Gangplank", SpellNames = new[] { "gangplankqproceed" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData { ChampionName = "Janna", SpellNames = new[] { "sowthewind" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData { ChampionName = "Kassadin", SpellNames = new[] { "nulllance" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Katarina",
                        SpellNames = new[] { "katarinaq", "katarinaqdaggerarc" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Kindred",
                        SpellNames = new[] { "kindrede" },
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Leblanc",
                        SpellNames = new[] { "leblancq", "leblancrq" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Lillia",
                        SpellNames = new[] { "lilliarexpungemissile" },
                        Slot = SpellSlot.R
                    });
                //-------------
                Spells.Add(new SpellData { ChampionName = "Lulu", SpellNames = new[] { "luluw" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                    { ChampionName = "Malphite", SpellNames = new[] { "seismicshard" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "MissFortune",
                        SpellNames = new[] { "missfortunericochetshot", "missfortunershotextra" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Nami",
                        SpellNames = new[] { "namiwenemy", "namiwmissileenemy" },
                        Slot = SpellSlot.W
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Ryze",
                        SpellNames = new[] { "ryzee" },
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData { ChampionName = "Shaco", SpellNames = new[] { "twoshivpoison" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { ChampionName = "Sona", SpellNames = new[] { "sonaqmissile" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData { ChampionName = "Syndra", SpellNames = new[] { "syndrarspell" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData { ChampionName = "Teemo", SpellNames = new[] { "blindingdart" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    { ChampionName = "Tristana", SpellNames = new[] { "tristanae" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData
                    { ChampionName = "Tristana", SpellNames = new[] { "tristanar" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData
                    { ChampionName = "TwistedFate", SpellNames = new[] { "bluecardattack" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                    { ChampionName = "TwistedFate", SpellNames = new[] { "goldcardattack" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                    { ChampionName = "TwistedFate", SpellNames = new[] { "redcardattack" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Urgot",
                        SpellNames = new[] { "urgotrrecastmissile" },
                        Slot = SpellSlot.R
                    });
                Spells.Add(
                    new SpellData { ChampionName = "Vayne", SpellNames = new[] { "vaynecondemnmissile" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData
                    { ChampionName = "Veigar", SpellNames = new[] { "veigarr" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData
                    { ChampionName = "Viktor", SpellNames = new[] { "viktorpowertransfer" }, Slot = SpellSlot.Q });
            }

            private static void ObjSpellMissileOnCreate(GameObject sender, EventArgs args)
            {
                var missile = sender as MissileClient;
                if (missile == null || !missile.IsValid)
                {
                    return;
                }

                var unit = missile.SpellCaster as AIHeroClient;
                if (unit == null || !unit.IsValid || unit.Team == Player.Team)
                {
                    return;
                }

                var spellData =
                Spells.FirstOrDefault(
                    i =>
                    i.SpellNames.Contains(missile.SData.Name.ToLower())
                    && championmenu2["scc" + i.ChampionName][i.MissileName]!= null && championmenu2["scc" + i.ChampionName][i.MissileName].GetValue<MenuBool>().Enabled);


                if (spellData == null //MenuManager.LaneClearMenu["E"].Cast<CheckBox>().CurrentValue
                    && (!missile.SData.Name.ToLower().Contains("crit")
                            ? evadeMenu2["BAttack"].GetValue<MenuBool>().Enabled
                              && Player.HealthPercent < evadeMenu2["BAttackHpU"].GetValue<MenuSlider>().Value
                            : evadeMenu2["CAttack"].GetValue<MenuBool>().Enabled
                              && Player.HealthPercent < evadeMenu2["CAttackHpU"].GetValue<MenuSlider>().Value))
                {
                    spellData = new SpellData
                    { ChampionName = unit.CharacterName, SpellNames = new[] { missile.SData.Name } };
                }
                if (spellData == null || (missile.Target != null && !missile.Target.IsMe))
                {
                    return;
                }
                DetectedTargets.Add(new Targets { Start = unit.Position, Obj = missile });
            }

            private static void ObjSpellMissileOnDelete(GameObject sender, EventArgs args)
            {
                var missile = sender as MissileClient;
                if (missile == null || !missile.IsValid)
                {
                    return;
                }
                var caster = missile.SpellCaster as AIHeroClient;
                if (caster == null || !caster.IsValid || caster.Team == Player.Team)
                {
                    return;
                }

                DetectedTargets.RemoveAll(i => i.Obj.NetworkId == missile.NetworkId);

            }

            private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
            {
                if (!sender.IsValid || sender.Team != Player.Team || args.SData.Name != "YasuoWMovingWall")
                {
                    return;
                }
                wallCastedPos = sender.Position.ToVector2();
            }

            private static void OnUpdateTarget(EventArgs args)
            {
                if (Player.IsDead)
                {
                    return;
                }
                if (Player.HasBuffOfType(BuffType.SpellImmunity) || Player.HasBuffOfType(BuffType.SpellShield))
                {
                    return;
                }
                if (!W.IsReady(300) && (Wall == null || !E.IsReady(200)))
                {
                    return;
                }
                foreach (var target in
                    DetectedTargets.Where(i => Player.Distance(i.Obj.Position) < 700))
                {
                    if (E.IsReady() && evadeMenu2["E"].GetValue<MenuBool>().Enabled && Wall != null
                        && !GoThroughWall(Player.Position.ToVector2(), target.Obj.Position.ToVector2())
                        && W.IsInRange(target.Obj.Position))
                    {

                        var obj = new List<AIBaseClient>();
                        obj.AddRange(Cache.GetMinions(Player.Position,E.Range));
                        obj.AddRange(Cache.EnemyHeroes.Where(i => i.IsValidTarget(E.Range)));
                        if (
                            obj.Where(
                                i =>
                                CanCastE(i)
                                && (!PosAfterE(i).IsUnderEnemyTurret() || evadeMenu2["ETower"].GetValue<MenuBool>().Enabled)
                                && GoThroughWall(Player.Position.ToVector2(), PosAfterE(i)))
                                .OrderBy(i => PosAfterE(i).Distance(Game.CursorPos))
                                .Any(i => E.CastOnUnit(i)))
                        {
                            return;
                        }
                    }
                    if (W.IsReady() && evadeMenu2["W"].GetValue<MenuBool>().Enabled && W.IsInRange(target.Obj.Position))
                    {
                        W.Cast(Player.Position.Extend(target.Start, 100));
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

                #endregion

                #region Public Properties

                public string MissileName
                {
                    get
                    {
                        return this.SpellNames.FirstOrDefault();
                    }
                }

                #endregion
            }

            private class Targets
            {
                #region Fields

                public MissileClient Obj;

                public Vector3 Start;

                #endregion
            }
        }
    }
}
