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

namespace ImpulseAIO.Champion.Jhin
{
    class FlowerlsInfo
    {
        public AIBaseClient Pointer { get; set; }
        public int VaildTime { get; set; }
    }
    internal class Jhin : Base
    {
        private bool IsCastingR;
        private bool IsCharging;
        private bool TapKeyPressed;
        private int Stacks;
        private Geometry.Sector LastRCone;
        private Geometry.Sector RPolygon;
        public Dictionary<int, string> TextsInScreen = new Dictionary<int, string>();
        public Dictionary<int, string> TextsInHeroPosition = new Dictionary<int, string>();
        private static List<FlowerlsInfo> FlowersInfo = new List<FlowerlsInfo>();
        private static Spell Q, W, E, R;
        private static Menu AntiGapcloserMenu;
        private bool IsR1
        {
            get { return R.Instance.SData.Name == "JhinR"; }
        }
        private static bool ComboUseQ => ChampionMenu["Combo"]["CQ"].GetValue<MenuBool>().Enabled;
        private static int ComboUseW => ChampionMenu["Combo"]["CW"].GetValue<MenuList>().Index;
        private static bool HarassUseQ => ChampionMenu["Harass"]["Q"].GetValue<MenuBool>().Enabled;
        private static bool HarassUseQPoke => ChampionMenu["Harass"]["HQPoke"].GetValue<MenuBool>().Enabled;
        private static bool HarassUseW => ChampionMenu["Harass"]["W"].GetValue<MenuBool>().Enabled;
        private static int HarassMana => ChampionMenu["Harass"]["ManaPercent"].GetValue<MenuSlider>().Value;
        private static bool LaneClearUseQ => ChampionMenu["LaneClear"]["Q"].GetValue<MenuBool>().Enabled;
        private static int LaneClearUseQMinHits => ChampionMenu["LaneClear"]["MinQ"].GetValue<MenuSlider>().Value;
        private static int LaneClearMana => ChampionMenu["LaneClear"]["LaneClear.ManaPercent"].GetValue<MenuSlider>().Value;
        private static bool LastHitUseQ => ChampionMenu["LastHit"]["LastHit.Q"].GetValue<MenuBool>().Enabled;
        private static int LastHitMana => ChampionMenu["LastHit"]["LastHit.ManaPercent"].GetValue<MenuSlider>().Value;
        private static bool JungleClearUseQ => ChampionMenu["JungleClear"]["JungleClear.Q"].GetValue<MenuBool>().Enabled;
        private static bool AutoW => ChampionMenu["Misc"]["W.AutoEnable"].GetValue<MenuBool>().Enabled;
        private static int AutoWMana => ChampionMenu["Misc"]["W.ManaPercent"].GetValue<MenuSlider>().Value;
        private static bool AutoEImmobile => ChampionMenu["Misc"]["Misc.Immobile"].GetValue<MenuBool>().Enabled;
        private static bool AntiGap => AntiGapcloserMenu["E.Gapcloser"].GetValue<MenuBool>().Enabled;
        public Jhin()
        {
            Q = new Spell(SpellSlot.Q,550f);
            Q.SetTargetted(0.25f, 1800);
            W = new Spell(SpellSlot.W, 2520f);
            W.SetSkillshot(0.75f,45f,float.MaxValue,true,SpellType.Line);

            E = new Spell(SpellSlot.E, 750f);
            E.SetSkillshot(0.25f,160f,1600f,false,SpellType.Circle);
            R = new Spell(SpellSlot.R, 3400f);
            R.SetSkillshot(0.25f, 80f, 5000f, true, SpellType.Line);
            Q.DamageType = W.DamageType = E.DamageType = R.DamageType = DamageType.Physical;
            OnMenuLoad();
            foreach (var enemy in Cache.EnemyHeroes)
            {
                TextsInScreen.Add(enemy.NetworkId, enemy.CharacterName + "R 可击杀");
                TextsInHeroPosition.Add(enemy.NetworkId,"R 可击杀");
            }
            InitFlowersArray();
            AIBaseClient.OnDoCast += OnProcessSpellCast;
            AntiGapcloser.OnGapcloser += AntiGapCloser;
            Game.OnUpdate += GameOnUpdate;
            Render.OnDraw += Draw;
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
            if (ToBase.Name.Equals("Noxious Trap") && ToBase.MaxHealth == 6 && ToBase.IsAlly)
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
            if(FindNetwork != null)
            {
                FlowersInfo.RemoveAll(x => x.Pointer.NetworkId == FindNetwork.Pointer.NetworkId);
            }
        }
        private void Auto()
        {
            if (AutoW)
            {
                if (W.IsReady() && Player.ManaPercent >= AutoWMana && !Player.IsUnderEnemyTurret() && Player.CountEnemyHerosInRangeFix(600) == 0)
                {
                    foreach (var enemy in Cache.EnemyHeroes.Where(x => x.IsValidTarget(W.Range) && TargetHaveEBuff(x))) // 
                    {
                        if (ChampionMenu["Misc"]["AutoW." + enemy.CharacterName].GetValue<MenuBool>().Enabled)
                        {
                            var ccPos = GetCCBuffPos(enemy);

                            if (ccPos.IsValid())
                            {
                                W.Cast(ccPos);
                                break;
                            }
                        }

                        W.CastIfHitchanceEquals(enemy, HitChance.Dash);
                    }
                }
            }
            if (AutoEImmobile)
            {
                if (E.IsReady())
                {
                    foreach (var enemy in Cache.EnemyHeroes.Where(x => x.IsValidTarget(E.Range)))
                    {
                        var time = enemy.GetMovementBlockedDebuffDuration();
                        if ((time > 0 && time * 1000 >= E.Delay) || (enemy.IsCastingImporantSpell() && !enemy.CanMove))
                        {
                            CastE(enemy.ServerPosition);
                        }
                    }
                }
            }
            
        }
        private void Draw(EventArgs args)
        {
            if (ChampionMenu["Ultimate"]["NearMouse.Enabled"].GetValue<MenuBool>().Enabled && ChampionMenu["Ultimate"]["NearMouse.Draw"].GetValue<MenuBool>().Enabled && IsCastingR)
            {
                PlusRender.DrawCircle(Game.CursorPos, ChampionMenu["Ultimate"]["NearMouse.Radius"].GetValue<MenuSlider>().Value, Color.Blue);
            }
            if (R.IsReady() || IsCastingR)
            {
                var count = 0;
                foreach (var enemy in Cache.EnemyHeroes.Where(h => h.IsValidTarget(R.Range) && h.GetRealHeath(DamageType.Physical) < R.GetDamage(h) && TextsInScreen.ContainsKey(h.NetworkId)))
                {
                    var pos = Drawing.WorldToScreen(Player.Position);
                    var Pos = new Vector2(pos.X + 72, (pos.Y - 100) + (45 * count));
                    PlusRender.DrawText(TextsInScreen[enemy.NetworkId], Pos.X, Pos.Y, Color.Red);

                    if (enemy.IsVisibleOnScreen)
                    {
                        var Posz = Drawing.WorldToScreen(enemy.Position);
                        PlusRender.DrawText(TextsInScreen[enemy.NetworkId], Posz.X, Posz.Y, Color.FloralWhite);
                    }
                    count++;
                }
            }
        }
        private void PermaActive()
        {
            if (IsCastingR)
            {
                IsCastingR = R.Instance.Name == "JhinRShot"; //MyHero.Spellbook.IsChanneling;
            }
            IsCharging = Player.HasBuff("JhinPassiveReload");
            Orbwalker.AttackEnabled = !IsCastingR;
            Orbwalker.MoveEnabled = !IsCastingR;
            if (R.IsReady() && !IsCastingR)
            {
                Stacks = 4;
            }

            if (IsCastingR)
            {
                if (TapKeyPressed || ChampionMenu["Ultimate"]["Mode"].GetValue<MenuList>().Index == 2)
                {
                    CastR();
                }
                return;
            }
        }
        private void GameOnUpdate(EventArgs args)
        {
            Killsteal();
            PermaActive();
            Auto();
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
                    JungleClear();
                    break;
                case OrbwalkerMode.LastHit:
                    LastHit();
                    break;
                case OrbwalkerMode.Flee:
                    break;
            }
        }
        private void AntiGapCloser(AIBaseClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (!AntiGap) 
                return;

            if (sender.IsValidTarget())
            {
                if (Player.Distance(args.StartPosition) > Player.Distance(args.EndPosition))
                {
                    if (AntiGapcloserMenu["E.Gapcloser"].GetValue<MenuBool>().Enabled && Player.InRange(args.EndPosition, E.Range))
                    {
                        CastE(args.EndPosition);
                        return;
                    }
                }

                if (TargetHaveEBuff(sender) && W.IsReady())
                {
                    if(Player.CountEnemyHerosInRangeFix(650f) == 0)
                    {
                        if (args.EndPosition.DistanceToPlayer() <= E.Range)
                        {
                            CastW(sender);
                        }
                    }
                }
            }
        }
        private void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                switch (args.Slot)
                {
                    case SpellSlot.R:
                        if (args.SData.Name == "JhinR")
                        {
                            IsCastingR = true;
                            LastRCone = new Geometry.Sector(Player.ServerPosition, Player.ServerPosition.Extend(Game.CursorPos,R.Range), (float)(60f * Math.PI / 180f), R.Range);
                            Stacks = 4;
                        }
                        else if (args.SData.Name == "JhinRShot")
                        {
                            R.LastCastAttemptTime = Variables.GameTimeTickCount;
                            TapKeyPressed = false;
                            Stacks--;
                        }
                        break;
                }
            }
        }
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Jhin));
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo"));
            {
                Combo.Add(new MenuBool("CQ", "Use Q"));
                Combo.Add(new MenuList("CW", "Use W", new[] { "Disable", "Only W/E Buff", "Always" }, 1));
            }
            var Ultimate = ChampionMenu.Add(new Menu("Ultimate", Program.Chinese ? "完美谢幕" : "Ultimate"));
            {
                Ultimate.Add(new MenuList("Mode", "R Aim Mode", new[] { "Disable", "Use Key", "Auto" }, 2));
                Ultimate.Add(new MenuBool("OnlyKillable", Program.Chinese ? "只攻击可击杀目标" : "Only Killable"));
                Ultimate.Add(new MenuSlider("Delay", Program.Chinese ? "R之间的延迟(毫秒)" : "R Delay(ms)", 0, 0, 1500));
                Ultimate.Add(new MenuSeparator("NearMouse", "鼠标附近设置"));
                Ultimate.Add(new MenuBool("NearMouse.Enabled", Program.Chinese ? "只选择鼠标附近目标" : "OnlyNearMouse enemy", false));
                Ultimate.Add(new MenuSlider("NearMouse.Radius", Program.Chinese ? "靠近鼠标半径" : "NearMouse Radius", 500, 100, 1500));
                Ultimate.Add(new MenuBool("NearMouse.Draw", Program.Chinese ? "显示鼠标半径" : "Draw Radius"));
            }
            var Harass = ChampionMenu.Add(new Menu("Harass", "Harass"));
            {
                Harass.Add(new MenuBool("Q", "Use Q"));
                Harass.Add(new MenuBool("HQPoke", "Use Q Minion Poke"));
                Harass.Add(new MenuBool("W", "Use W", false));
                Harass.Add(new MenuSlider("ManaPercent", Program.Chinese ? "当蓝量 <= X%不骚扰" : "Don't Harass if Mana <= X%", 20));
            }
            var LaneClear = ChampionMenu.Add(new Menu("LaneClear", "LaneClear"));
            {
                LaneClear.Add(new MenuBool("Q", "Use Q"));
                LaneClear.Add(new MenuSlider("MinQ", "Use Q Min hit Minion",3,1,5));
                LaneClear.Add(new MenuSlider("LaneClear.ManaPercent", Program.Chinese ? "当蓝量 <= X%时不清线/野" : "Don't Lane/Jungle if Mana <= X%", 50));
            }
            var LastHit = ChampionMenu.Add(new Menu("LastHit","LastHit"));
            {
                LastHit.Add(new MenuBool("LastHit.Q", "Use Q"));
                LastHit.Add(new MenuSlider("LastHit.ManaPercent", Program.Chinese ? "当蓝量 <= X%时不尾刀" : "Don't LastHit if Mana <= X%", 50));
            }
            var JungleClear = ChampionMenu.Add(new Menu("JungleClear", "JungleClear"));
            {
                JungleClear.Add(new MenuBool("JungleClear.Q", "Use Q"));
            }
            var Misc = ChampionMenu.Add(new Menu("Misc", "Misc"));
            {
                Misc.Add(new MenuBool("W.AutoEnable", Program.Chinese ? "自动对有BUFF的无法移动敌人使用W" : "Auto W To E Buff eneies"));
                Misc.Add(new MenuSlider("W.ManaPercent", "当蓝量 <= X%时不自动W", 10));
                foreach (var enemy in GameObjects.EnemyHeroes)
                {
                    Misc.Add(new MenuBool("AutoW." + enemy.CharacterName, enemy.CharacterName));
                }
                Misc.Add(new MenuBool("Misc.Immobile", Program.Chinese ? "自动 E" : "Auto E CC"));
            }
            AntiGapcloserMenu = AntiGapcloser.Attach(ChampionMenu);
            {
                AntiGapcloserMenu.Add(new MenuBool("E.Gapcloser", "Use E AntiGap"));
            }
        }
        private bool TargetHaveEBuff(AIBaseClient target)
        {
            return target.HasBuff("jhinespotteddebuff");
        }
        private void CastW(AIBaseClient target,bool CheckWFlag = false)
        {
            if (IsCastingR)
            {
                return;
            }
            if (W.IsReady() && target != null)
            {
                if(CheckWFlag && ComboUseW == 1)
                {
                    if (!TargetHaveEBuff(target)) 
                        return;
                }
                if(Player.CountEnemyHerosInRangeFix(500) != 0)
                {
                    return;
                }
                if (Cache.EnemyHeroes.Where(x => x.IsValidTarget()).Any(h => h.IsMelee && h.InAutoAttackRange()))
                {
                    return;
                }
                if (target.InAutoAttackRange() && Orbwalker.CanAttack() && target.GetRealHeath(DamageType.Physical) < Player.GetAutoAttackDamage(target))
                {
                    return;
                }
                var hero = target as AIHeroClient;
                if (hero != null)
                {
                    if (hero.IsInvulnerable)
                        return;
                    if (!TargetHaveEBuff(hero))
                    {
                        if (Orbwalker.CanAttack() && Player.InAutoAttackRange(target))
                        {
                            return;
                        }
                    }
                }
                var Pred = W.GetPrediction(target, false, -1, new CollisionObjects[] { CollisionObjects.Heroes,CollisionObjects.YasuoWall });
                if(Pred.Hitchance >= HitChance.High)
                {
                    W.Cast(Pred.CastPosition);
                }
            }
        }
        private bool HavePassiveAttackBuff()
        {
            return Player.HasBuff("jhinpassiveattackbuff");
        }
        private void Combo()
        {
            if (Player.IsWindingUp || IsCastingR) 
                return;
            if (Q.IsReady() && ComboUseQ && !HavePassiveAttackBuff())
            {
                var Ret = TargetSelector.GetTarget(Q.Range,DamageType.Physical);
                if (Ret != null)
                {
                    CastQ(Ret);
                }
            }
            if(W.IsReady() && ComboUseW != 0)
            {
                var Targets = TargetSelector.GetTargets(W.Range, DamageType.Physical);
                foreach(var obj in Targets)
                {
                    CastW(obj,true);
                }
            }
        }
        private AIBaseClient GetMinionsBestQ(AIBaseClient Unit)
        {
            AIBaseClient BestTarget = null;
            if (Unit != null)
            {
                //获取全部可以Q的单位
                
                var AllMinions = GameObjects.Get<AIBaseClient>().Where(x => x.IsValidTarget(GetQRange(x)));
                //获取可以击杀的列表
                var minionUp = AllMinions.Where(x => Q.GetDamage(x) > x.Health && x.Distance(Unit) <= 225).ToList();
                //循环 [击杀列表]
                for (int i = 0; i < minionUp.Count; i++)
                {
                    //在 [取全部可以Q的单位]中排序
                    var minDist = AllMinions.Where(x => x.NetworkId != minionUp[i].NetworkId).OrderBy(x => x.Distance(minionUp[i])).ToList(); //重新排列一个数组

                    //一个手雷能弹跳3次 判断敌方英雄是否在内
                    for (int j = 0; j < minDist.Count; j++)
                    {
                        if (j >= 3)
                            break;

                        if (minDist[j].NetworkId == Unit.NetworkId)
                        {
                            BestTarget = minionUp[i];
                        }
                    }
                }
            }
            return BestTarget;
        }
        private float GetQRange(AIBaseClient unit)
        {
            if (unit == null)
                return Q.Range + Player.BoundingRadius;
            return Q.Range + Player.BoundingRadius + unit.BoundingRadius;
        }
        private void Harass()
        {
            if (Player.ManaPercent <= HarassMana) return;
            if (IsCastingR)
                return;
            if (HarassUseQ && Q.IsReady() && !HavePassiveAttackBuff())
            {
                //获取Q范围敌人
                var Ret = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

                if (Ret!= null)
                {
                    if (Ret.InRange(GetQRange(Ret)))
                    {
                        if (HarassUseQPoke)
                        {
                            var BestQ = GetMinionsBestQ(Ret);
                            if (BestQ != null)
                            {
                                CastQ(BestQ);
                                return;
                            }
                            CastQ(Ret);
                        }
                        else
                        {
                            CastQ(Ret);
                            return;
                        }
                    }
                    else if (HarassUseQPoke)
                    {
                        var BestQ = GetMinionsBestQ(Ret);
                        if (BestQ != null)
                        {
                            CastQ(BestQ);
                            return;
                        }
                    }
                }
            }
            if(HarassUseW && W.IsReady())
            {
                var first = TargetSelector.GetTargets(W.Range, DamageType.Physical);
                foreach(var obj in first)
                {
                    CastW(obj);
                }
            }
        }
        private void LaneClear()
        {
            if (Player.ManaPercent <= LaneClearMana) 
                return;

            if(LaneClearUseQ && Q.IsReady())
            {
                var AllMinions = Cache.GetMinions(Player.ServerPosition, Q.Range).Where(x => x.Health < Q.GetDamage(x)).FirstOrDefault(x => Cache.GetMinions(x.ServerPosition,225f).Count >= LaneClearUseQMinHits) ;
                if(AllMinions != null)
                {
                    CastQ(AllMinions);
                }
            }
        }
        private void JungleClear()
        {
            if (Player.ManaPercent <= LaneClearMana) return;

            if (JungleClearUseQ && Q.IsReady())
            {
                var AllMinions = Cache.GetJungles(Player.ServerPosition, Q.Range).MinOrDefault(x => x.Health);
                if (AllMinions != null)
                {
                    CastQ(AllMinions);
                }
            }
        }
        private void LastHit()
        {
            if (Player.ManaPercent >= LastHitMana)
            {
                if(LastHitUseQ && Q.IsReady())
                {
                    var minion =
                        Cache.GetMinions(Player.ServerPosition, Q.Range).Where(
                            i =>
                            i.IsValidTarget(GetQRange(i))
                            &&  Q.GetDamage(i) > i.Health
                            && (i.IsUnderAllyTurret() || (i.IsUnderEnemyTurret() && !Player.IsUnderEnemyTurret())
                                || i.DistanceToPlayer() > i.GetRealAutoAttackRange() + 50
                                || i.GetRealHeath(DamageType.Physical) > Player.GetAutoAttackDamage(i))).MaxOrDefault(i => i.MaxHealth);
                    if (minion != null)
                    {
                        CastQ(minion);
                    }
                }
            }
        }
        private void CastR()
        {
            if (!IsR1 && Variables.GameTimeTickCount - R.LastCastAttemptTime >= ChampionMenu["Ultimate"]["Delay"].GetValue<MenuSlider>().Value)
            {
                var rTargets = Cache.EnemyHeroes.Where(x => x.IsValidTarget(R.Range)).Where(h => R.IsInRange(h) && (!ChampionMenu["Ultimate"]["OnlyKillable"].GetValue<MenuBool>().Enabled || h.GetRealHeath(DamageType.Physical) > R.GetDamage(h)) && LastRCone.IsInside(h)).ToList();
                var targets = ChampionMenu["Ultimate"]["NearMouse.Enabled"].GetValue<MenuBool>().Enabled
                    ? rTargets.Where(
                        h => h.InRange(Game.CursorPos, ChampionMenu["Ultimate"]["NearMouse.Radius"].GetValue<MenuSlider>().Value)).ToList()
                    : rTargets;
                var target = TargetSelector.GetTarget(targets, DamageType.Physical);
                if (target != null)
                {
                    var pred = R.GetPrediction(target,false,-1,new CollisionObjects[] { CollisionObjects.Heroes,CollisionObjects.YasuoWall});
                    if (pred.Hitchance >= HitChance.VeryHigh)
                    {
                        R.Cast(pred.CastPosition);
                    }
                }
            }
        }
        private void CastQ(AIBaseClient Unit)
        {
            if (IsCastingR)
                return;
            Q.Cast(Unit);
        }
        private void Killsteal()
        {
            if (IsCastingR)
                return;

            foreach(var obj in Cache.EnemyHeroes)
            {
                if (obj.IsInvulnerable || obj.HaveSpellShield())
                    continue;
                if(obj.IsValidTarget(GetQRange(obj)) && obj.GetRealHeath(DamageType.Physical) < Q.GetDamage(obj))
                {
                    CastQ(obj);
                }
                if (obj.IsValidTarget(W.Range) && obj.GetRealHeath(DamageType.Physical) < W.GetDamage(obj))
                {
                    CastW(obj);
                }
            }
        }
        private void CastE(Vector3 pos)
        {
            if (FlowersInfo.Any(x => x.Pointer.Distance(pos) <= E.Width))
            {
                return;
            }
            E.Cast(pos);
        }
    }
}
