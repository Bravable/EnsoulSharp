using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using ImpulseAIO.Common;

namespace ImpulseAIO.Champion.Caitlyn
{
    class FlowerlsInfo
    {
        public AIBaseClient Pointer { get; set; }
        public int VaildTime { get; set; }
    }
    internal class Caitlyn : Base
    {
        private static Spell Q, W, E, R;
        private static AIHeroClient LastW = null;
        private static Dash dash = null;
        private static List<FlowerlsInfo> FlowersInfo = new List<FlowerlsInfo>();
        private static Menu AntiGapcloserMenu;

        private float QCastTime = 0;
        private static bool noti => ChampionMenu["Draw"]["noti"].GetValue<MenuBool>().Enabled;
        private static bool qRange => ChampionMenu["Draw"]["qRange"].GetValue<MenuBool>().Enabled;
        private static bool wRange => ChampionMenu["Draw"]["wRange"].GetValue<MenuBool>().Enabled;
        private static bool eRange => ChampionMenu["Draw"]["eRange"].GetValue<MenuBool>().Enabled;
        private static bool rRange => ChampionMenu["Draw"]["rRange"].GetValue<MenuBool>().Enabled;
        private static bool onlyRdy => ChampionMenu["Draw"]["onlyRdy"].GetValue<MenuBool>().Enabled;

        private static bool autoQ2 => ChampionMenu["QConfig"]["autoQ2"].GetValue<MenuBool>().Enabled;
        private static bool autoQ => ChampionMenu["QConfig"]["autoQ"].GetValue<MenuBool>().Enabled;
        private static bool Qaoe => ChampionMenu["QConfig"]["Qaoe"].GetValue<MenuBool>().Enabled;
        private static bool Qslow => ChampionMenu["QConfig"]["Qslow"].GetValue<MenuBool>().Enabled;
        private static bool autoW => ChampionMenu["WConfig"]["autoW"].GetValue<MenuBool>().Enabled;
        private static bool forceW => ChampionMenu["WConfig"]["forceW"].GetValue<MenuBool>().Enabled;
        private static bool Wspell => ChampionMenu["WConfig"]["Wspell"].GetValue<MenuBool>().Enabled;

        private static bool autoE => ChampionMenu["EConfig"]["autoE"].GetValue<MenuBool>().Enabled;
        private static bool Ehitchance => ChampionMenu["EConfig"]["Ehitchance"].GetValue<MenuBool>().Enabled;
        private static bool harrasEQ => ChampionMenu["EConfig"]["harrasEQ"].GetValue<MenuBool>().Enabled;
        private static bool EQks => ChampionMenu["EConfig"]["EQks"].GetValue<MenuBool>().Enabled;
        private static bool useE => ChampionMenu["EConfig"]["useE"].GetValue<MenuKeyBind>().Active;

        private static bool autoR => ChampionMenu["RConfig"]["autoR"].GetValue<MenuBool>().Enabled;
        private static int Rcol => ChampionMenu["RConfig"]["Rcol"].GetValue<MenuSlider>().Value;
        private static int Rrange => ChampionMenu["RConfig"]["Rrange"].GetValue<MenuSlider>().Value;
        private static bool useR => ChampionMenu["RConfig"]["useR"].GetValue<MenuKeyBind>().Active;
        private static bool Rturrent => ChampionMenu["RConfig"]["Rturrent"].GetValue<MenuBool>().Enabled;

        private static bool farmQ => ChampionMenu["Farm"]["farmQ"].GetValue<MenuBool>().Enabled;

        private static bool GapW => AntiGapcloserMenu["GapW"].GetValue<MenuBool>().Enabled;
        private static bool GapE => AntiGapcloserMenu["GapE"].GetValue<MenuBool>().Enabled;
        public Caitlyn()
        {
            Q = new Spell(SpellSlot.Q, 1240f);
            W = new Spell(SpellSlot.W, 800f);
            E = new Spell(SpellSlot.E, 740f);
            R = new Spell(SpellSlot.R, 3000f);

            Q.SetSkillshot(0.625f, 60f, 2200f, false, SpellType.Line);
            W.SetSkillshot(1.25f, 15f, float.MaxValue, false, SpellType.Circle);
            E.SetSkillshot(0.15f, 70f, 1600f, true, SpellType.Line);
            R.SetSkillshot(0.7f, 200f, 1500f, false, SpellType.Circle);
            Q.DamageType = W.DamageType = E.DamageType = R.DamageType = DamageType.Magical;

            dash = new Dash(E);
            OnMenuLoad();
            InitFlowersArray();
            Render.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;

            AntiGapcloser.OnGapcloser += AntiGapcloser_OnEnemyGapcloser;
            AIBaseClient.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Caitlyn));
            var Draw = ChampionMenu.Add(new Menu("Draw", "Draw"));
            {
                Draw.Add(new MenuBool("noti", "画出 大招击杀情况", false));
                Draw.Add(new MenuBool("qRange", "Q range", false));
                Draw.Add(new MenuBool("wRange", "W range", false));
                Draw.Add(new MenuBool("eRange", "E range", false));
                Draw.Add(new MenuBool("rRange", "R range", false));
                Draw.Add(new MenuBool("onlyRdy", "仅画出准备好的技能"));
            }
            var QConfig = ChampionMenu.Add(new Menu("QConfig", "Q Config"));
            {
                QConfig.Add(new MenuBool("autoQ2", "Auto Q", true));
                QConfig.Add(new MenuBool("autoQ", "Reduce Q use", true));
                QConfig.Add(new MenuBool("Qaoe", "Q AOE", true));
                QConfig.Add(new MenuBool("Qslow", "Q Slow / EQ", true));
            }
            var WConfig = ChampionMenu.Add(new Menu("WConfig", "W Config"));
            {
                WConfig.Add(new MenuBool("autoW", "Auto W on hard CC", true));
                WConfig.Add(new MenuBool("forceW", "Force W before E", true));
                WConfig.Add(new MenuBool("Wspell", "W on special spell detection", true));
            }
            var EConfig = ChampionMenu.Add(new Menu("EConfig", "E Config"));
            {
                EConfig.Add(new MenuBool("autoE", "Auto E", true));
                EConfig.Add(new MenuBool("Ehitchance", "Auto E dash and immobile target", true));
                EConfig.Add(new MenuBool("harrasEQ", "TRY E + Q", true));
                EConfig.Add(new MenuBool("EQks", "Ks E + Q + AA", true));
                EConfig.Add(new MenuKeyBind("useE", "Dash E HotKeySmartcast", Keys.T, KeyBindType.Press)).AddPermashow();
            }
            var RConfig = ChampionMenu.Add(new Menu("RConfig", "R Config"));
            {
                RConfig.Add(new MenuBool("autoR", "Auto R KS", true));
                RConfig.Add(new MenuSlider("Rcol", "R collision width [400]", 400, 1, 1000));
                RConfig.Add(new MenuSlider("Rrange", "R minimum range [1000]", 1000, 1, 1500));
                RConfig.Add(new MenuKeyBind("useR", "Semi-manual cast R key", Keys.S, KeyBindType.Press)).AddPermashow();
                RConfig.Add(new MenuBool("Rturrent", "Don't R under turret", true));
            }
            var Farm = ChampionMenu.Add(new Menu("Farm", "Farm"));
            {
                Farm.Add(new MenuBool("farmQ", "Lane clear Q", true));
            }
            AntiGapcloserMenu = AntiGapcloser.Attach(ChampionMenu);
            {
                AntiGapcloserMenu.Add(new MenuBool("GapW", "Use W AntiGapCloser"));
                AntiGapcloserMenu.Add(new MenuBool("GapE", "Use E AntiGapCloser"));
            }
        }
        private void InitFlowersArray()
        {
            foreach (var minion in GameObjects.Get<AIBaseClient>().Where(minion => minion.IsValid))
            {
                Game_OnObjectCreate(minion, null);
            }
            GameObject.OnCreate += Game_OnObjectCreate;
            GameObject.OnDelete += Game_OnObjectDelete;
        }
        private void Game_OnObjectCreate(GameObject obj, EventArgs s)
        {
            var ToBase = obj as AIBaseClient;
            if (ToBase == null)
            {
                return;
            }
            if (ToBase.Name.Equals("Cupcake Trap") && ToBase.MaxHealth == 100 && ToBase.IsAlly)
            {
                FlowersInfo.Add(new FlowerlsInfo() { Pointer = ToBase, VaildTime = Variables.GameTimeTickCount + 180000 });
            }
        }
        private void Game_OnObjectDelete(GameObject obj, EventArgs s)
        {
            var ToBase = obj as AIBaseClient;
            if (ToBase == null)
            {
                return;
            }
            var FindNetwork = FlowersInfo.Find(x => x.Pointer.NetworkId == ToBase.NetworkId);
            if (FindNetwork != null)
            {
                FlowersInfo.RemoveAll(x => x.Pointer.NetworkId == FindNetwork.Pointer.NetworkId);
            }
        }
        private void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsRecalling())
                return;

                AutoWCC();

            if (useR && R.IsReady())
            {
                var t = TargetSelector.GetTargets(R.Range,DamageType.Physical).Where(x => x.GetRealHeath(DamageType.Physical) < R.GetDamage(x)).FirstOrDefault();
                if (t.IsValidTarget())
                {
                    R.CastOnUnit(t);
                }
            }

            R.Range = (500 * R.Level) + 1500;

            if (E.IsReady())
                LogicE();
            if (W.IsReady())
                LogicW();
            if (Q.IsReady() && autoQ2)
                LogicQ();
            if (R.IsReady() && autoR && !Player.IsUnderEnemyTurret() && Game.Time - QCastTime > 1)
                LogicR();
        }
        private void AntiGapcloser_OnEnemyGapcloser(AIBaseClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if(sender.IsValidTarget() && args.StartPosition.DistanceToPlayer() > args.EndPosition.DistanceToPlayer())
            {
                if(GapW && W.IsReady() && args.EndPosition.DistanceToPlayer() <= W.Range)
                {
                    CastW(args.EndPosition);

                    return;
                }
                if (GapE && E.IsReady() && args.EndPosition.DistanceToPlayer() <= 300)
                {
                    var predE = E.GetPrediction(sender);
                    if(predE.Hitchance >= HitChance.High)
                    {
                        var positionT = Player.ServerPosition - (predE.CastPosition - Player.ServerPosition);
                        if(dash.IsGoodPosition(Player.ServerPosition.Extend(positionT, 350f)))
                        {
                            E.Cast(predE.CastPosition);

                            return;
                        }
                    }
                }
            }
        }
        private void Obj_AI_Base_OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if(sender.IsMe && W.IsReady())
            {
                if(forceW && args.SData.Name.Equals("CaitlynEMissile") && LastW != null)
                {
                    DelayAction.Add(30, () =>
                    {
                        if(LastW.IsDead || !LastW.IsValidTarget() || LastW.IsZombie() || HealthPrediction.GetPrediction(LastW,600) <= 0)
                        {
                            LastW = null;
                            return;
                        }
                        var PredPos = W.GetPrediction(LastW);
                        var ExtendPos = LastW.ServerPosition.Extend(Player.ServerPosition, -50f);
                        if (!LastW.IsMoving)
                        {
                            PredPos.CastPosition = ExtendPos;
                        }
                        CastW(PredPos.CastPosition.IsZero || (PredPos.CastPosition == LastW.ServerPosition) ? ExtendPos : PredPos.CastPosition);
                        LastW = null;
                    });
                    return;
                }
                
            }else if (sender.IsMe && !W.IsReady() && LastW != null)
            {
                LastW = null;
            }
            if (sender.IsMe && (args.SData.Name == "CaitlynPiltoverPeacemaker" || args.SData.Name == "CaitlynEntrapment"))
            {
                QCastTime = Game.Time;
            }
        }
        private void Drawing_OnDraw(EventArgs args)
        {

            if (qRange)
            {
                if (onlyRdy || Q.IsReady())
                {
                    PlusRender.DrawCircle(Player.Position, Q.Range, Color.Cyan);
                }
            }
            if (wRange)
            {
                if (onlyRdy || W.IsReady())
                {
                    PlusRender.DrawCircle(Player.Position, W.Range, Color.Orange);
                }

            }
            if (eRange)
            {
                if (onlyRdy || E.IsReady())
                {
                    PlusRender.DrawCircle(Player.Position, E.Range, Color.Yellow);
                }
            }
            if (rRange)
            {
                if (onlyRdy || R.IsReady())
                {
                    PlusRender.DrawCircle(Player.Position, R.Range, Color.Gray);
                }
            }
            if (noti)
            {
                if (R.IsReady())
                {
                    foreach (var obj in Cache.EnemyHeroes.Where(x => x.IsValidTarget()))
                    {
                        var rDamage = R.GetDamage(obj);
                        if (rDamage > obj.GetRealHeath(DamageType.Physical))
                        {
                            PlusRender.DrawText("Ult can kill: " + obj.CharacterName + " have: " + obj.GetRealHeath(DamageType.Physical) + "hp", Drawing.Width * 0.1f, Drawing.Height * 0.5f, Color.Red);
                            var pos1 = Drawing.WorldToScreen(obj.Position);
                            var pos2 = Drawing.WorldToScreen(Player.Position);
                            PlusRender.DrawLine(pos1, pos2, 10, Color.Yellow);
                        }
                    }
                }
            }
        }
        private void LogicR()
        {
            bool cast = false;

            if (Player.IsUnderEnemyTurret() && Rturrent)
                return;

            foreach (var target in Cache.EnemyHeroes.Where(target => target.IsValidTarget(R.Range) && Player.Distance(target.ServerPosition) > Rrange && target.CountEnemyHerosInRangeFix(Rcol) == 1 && target.CountAllysHerosInRangeFix(500) == 0))
            {
                if (R.GetDamage(target) > target.GetRealHeath(DamageType.Physical))
                {
                    cast = true;
                    PredictionOutput output = R.GetPrediction(target);
                    Vector2 direction = output.CastPosition.ToVector2() - Player.ServerPosition.ToVector2();
                    direction.Normalize();
                    List<AIHeroClient> enemies = Cache.EnemyHeroes.Where(x => x.IsValidTarget()).ToList();
                    foreach (var enemy in enemies)
                    {
                        if (enemy.SkinName == target.SkinName || !cast)
                            continue;
                        PredictionOutput prediction = R.GetPrediction(enemy);
                        Vector3 predictedPosition = prediction.CastPosition;
                        Vector3 v = output.CastPosition - Player.ServerPosition;
                        Vector3 w = predictedPosition - Player.ServerPosition;
                        double c1 = Vector3.Dot(w, v);
                        double c2 = Vector3.Dot(v, v);
                        double b = c1 / c2;
                        Vector3 pb = Player.ServerPosition + ((float)b * v);
                        float length = Vector3.Distance(predictedPosition, pb);
                        if (length < (Rcol + enemy.BoundingRadius) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                            cast = false;
                    }
                    if (cast)
                        R.CastOnUnit(target);
                }
            }
        }
        private void LogicW()
        {
            if (Player.Mana > R.Instance.ManaCost + W.Instance.ManaCost && !Player.IsWindingUp)
            {
                if (autoW)
                {
                    foreach (var enemy in Cache.EnemyHeroes.Where(enemy => enemy.IsValidTarget(W.Range) ))
                    {
                        var ccPos = GetCCBuffPos(enemy);
                        if (ccPos.IsValid())
                        {
                            CastW(ccPos);
                        }
                    }
                }
            }
        }
        private void AutoWCC()
        {
            if (!W.IsReady() || !Wspell || Player.IsWindingUp) 
                return;

            foreach(var obj in Cache.EnemyHeroes)
            {
                var ccPos = GetCCBuffPos(obj);
                if (obj.IsValidTarget(W.Range) && obj.IsCastingImporantSpell() && (!obj.CanMove || ccPos.IsValid()))
                {
                    CastW(obj.ServerPosition);
                }
                if(obj.DistanceToPlayer() <= W.Range && obj.HasBuff("zhonyasringshield"))
                {
                    CastW(obj.ServerPosition);
                }
            }
        }
        private void CastDashQ(AIBaseClient unit)
        {
            var pos = Player.IsDashing() ? Player.GetDashInfo().StartPos.ToVector3World() : Player.ServerPosition;
            Q.UpdateSourcePosition(pos, pos);
            var pred = Q.GetPrediction(unit);
            if (pred.Hitchance >= HitChance.High)
            {
                Q.Cast(pred.CastPosition);
            }
        }
        private void LogicQ()
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && Player.IsWindingUp)
                return;
            var t = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
            if (t.IsValidTarget(Q.Range))
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && Player.IsDashing() && Variables.GameTimeTickCount - E.LastCastAttemptTime < 300)
                {
                    CastDashQ(t);
                }
                if (!t.InAutoAttackRange() && Q.GetDamage(t) > t.GetRealHeath(DamageType.Physical) && Player.CountEnemyHerosInRangeFix(400) == 0)
                {
                    CastDashQ(t);
                }
                else if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && Player.Mana > R.Instance.ManaCost + Q.Instance.ManaCost + E.Instance.ManaCost + 10 && Player.CountEnemyHerosInRangeFix(bonusRange() + 100 + t.BoundingRadius) == 0 && !autoQ)
                {
                    CastDashQ(t);
                }
                if ((Orbwalker.ActiveMode == OrbwalkerMode.Combo || Orbwalker.ActiveMode == OrbwalkerMode.Harass) && Player.Mana > R.Instance.ManaCost + Q.Instance.ManaCost && Player.CountEnemyHerosInRangeFix(400) == 0)
                {
                    //Auto Q CC
                    foreach (var enemy in Cache.EnemyHeroes.Where(enemy => enemy.IsValidTarget(Q.Range)))
                    {
                        var ccPos = GetCCBuffPos(enemy);
                        if (ccPos.IsValid())
                        {
                            CastDashQ(enemy);
                        }
                    }
                    //Auto Slow CC
                    if (Player.CountEnemyHerosInRangeFix(bonusRange()) == 0 && !Player.IsWindingUp)
                    {
                        if ((t.HasBuffOfType(BuffType.Slow) && Qslow) || (Variables.GameTimeTickCount - E.LastCastAttemptTime <= 500))
                        {
                            CastDashQ(t);
                        }
                        if (Qaoe)
                            Q.CastIfWillHit(t, 2);
                    }
                }
            }
            else if (Enable_laneclear && farmQ && Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                var minionList = Cache.GetMinions(Player.ServerPosition, Q.Range);
                var farmPosition = Q.GetLineFarmLocation(minionList);
                if (farmPosition.MinionsHit >= 3)
                    Q.Cast(farmPosition.Position);
            }
        }
        private void LogicE()
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && Player.IsWindingUp)
                return;
            if (autoE && (Orbwalker.ActiveMode == OrbwalkerMode.Combo || Orbwalker.ActiveMode == OrbwalkerMode.Harass))
            {
                var Ret = IMPGetTarGet(E, false, HitChance.High);
                if(Ret.SuccessFlag && Ret.Obj.IsValid)
                {
                    var positionT = Player.ServerPosition - (Ret.Obj.ServerPosition - Player.ServerPosition);

                    //目标太远了
                    if (Player.ServerPosition.Extend(positionT, 375f).CountEnemyHerosInRangeFix(700) < 2 && Orbwalker.CanMove())
                    {
                        var eDmg = E.GetDamage(Ret.Obj);
                        var qDmg = Q.GetDamage(Ret.Obj);
                        if (EQks && Q.IsReady() && E.IsReady() && qDmg + eDmg + Player.GetAutoAttackDamage(Ret.Obj) > Ret.Obj.GetRealHeath(DamageType.Physical) && Player.Mana > E.Instance.ManaCost + Q.Instance.ManaCost)
                        {
                            var pred = E.GetPrediction(Ret.Obj);
                            if (pred.Hitchance >= HitChance.High)
                            {
                                if (E.Cast(pred.CastPosition) && W.IsReady())
                                {
                                    LastW = Ret.Obj as AIHeroClient;
                                }
                            }
                        }
                        else if ((Orbwalker.ActiveMode == OrbwalkerMode.Harass || Orbwalker.ActiveMode == OrbwalkerMode.Combo) && harrasEQ && Player.Mana > E.Instance.ManaCost + Q.Instance.ManaCost + R.Instance.ManaCost)
                        {

                            if (!(Orbwalker.ActiveMode == OrbwalkerMode.Combo) || Ret.Obj.InRange(Player.ServerPosition.Extend(positionT, 375f), Player.GetRealAutoAttackRange(Ret.Obj) + 200f))
                            {
                                var pred = E.GetPrediction(Ret.Obj);
                                if (pred.Hitchance >= HitChance.High)
                                {
                                    if (E.Cast(pred.CastPosition) && W.IsReady())
                                    {
                                        LastW = Ret.Obj as AIHeroClient;
                                    }
                                }
                            }
                            if (Ret.Obj.DistanceToPlayer() <= 300)
                            {
                                if (E.CastIfHitchanceEquals(Ret.Obj, HitChance.High) == CastStates.SuccessfullyCasted && W.IsReady())
                                {
                                    LastW = Ret.Obj as AIHeroClient;
                                }
                            }
                        }
                    }

                    if (Player.Mana > R.Instance.ManaCost + E.Instance.ManaCost)
                    {
                        if (Ehitchance)
                        {
                            if ((Orbwalker.CanMove() && !Orbwalker.CanAttack()) || Ret.Obj.IsDashing())
                                E.CastIfHitchanceEquals(Ret.Obj, HitChance.Dash);
                        }
                    }
                }
            }
            if (useE)
            {
                var position = Player.ServerPosition - (Game.CursorPos - Player.ServerPosition);
                E.Cast(position);
            }
        }
        private float GetRealDistance(AIBaseClient target)
        {
            return Player.Position.Distance(target.ServerPosition) + Player.BoundingRadius + target.BoundingRadius;
        }
        private void CastW(Vector3 pos)
        {
            if (FlowersInfo.Any(x => x.Pointer.Distance(pos) <= W.Width))
            {
                return;
            }
            Player.Spellbook.CastSpell(SpellSlot.W, pos);
        }
        public float bonusRange()
        {
            return 650f + Player.BoundingRadius;
        }
    }
}
