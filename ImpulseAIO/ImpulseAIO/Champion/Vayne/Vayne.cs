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
using ImpulseAIO.Common.Evade.Invisibility;

namespace ImpulseAIO.Champion.Vayne
{
    internal class Vayne : Base
    {
        private static Spell Q, W,E, R;
        private static new Dash Dash;
        private static InvisibilityEvade Evade;
        private static Menu AntiGapcloserMenu;
        private static readonly string[] _jungleMobs =
        {
            "SRU_Blue", "SRU_Red", "SRU_Krug", "SRU_Gromp", "SRU_Murkwolf", "SRU_Razorbeak", "TT_Spiderboss",
            "TTNGolem",
            "TTNWraith", "TTNWolf"
        };
        #region 菜单选项
        private static bool ComboUseQ => ChampionMenu["Combo"]["CQ"].GetValue<MenuBool>().Enabled;
        private static bool CQOnlyWA => ChampionMenu["Combo"]["CQOnlyWA"].GetValue<MenuBool>().Enabled;
        private static bool CQUlt => ChampionMenu["Combo"]["CQUlt"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseE => ChampionMenu["Combo"]["CE"].GetValue<MenuBool>().Enabled;
        private static bool ForceW => ChampionMenu["Combo"]["ForceW"].GetValue<MenuBool>().Enabled;
        private static int Condemndis => ChampionMenu["Condemn"]["condemndis"].GetValue<MenuSlider>().Value;
        private static bool SmartE => ChampionMenu["Condemn"]["smatrE"].GetValue<MenuKeyBind>().Active;
        private static bool ForceATTACK => ChampionMenu["R"]["CRPassForceKill"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseR => ChampionMenu["R"]["CR"].GetValue<MenuBool>().Enabled;
        private static int ComboUseRCount => ChampionMenu["R"]["CRCount"].GetValue<MenuSlider>().Value;
        private static int ComboUseRWaitAA => ChampionMenu["R"]["CRWaitPass"].GetValue<MenuSlider>().Value;
        private static bool HarassUseQ => ChampionMenu["Harass"]["HQ"].GetValue<MenuBool>().Enabled;
        private static bool HarassUseQA => ChampionMenu["Harass"]["HQA"].GetValue<MenuBool>().Enabled;
        private static bool HarassUseE => ChampionMenu["Harass"]["HE"].GetValue<MenuBool>().Enabled;
        private static int HarassMana => ChampionMenu["Harass"]["HMana"].GetValue<MenuSlider>().Value;
        private static int HarassMode => ChampionMenu["Harass"]["HMode"].GetValue<MenuList>().Index;
        private static bool LaneClear_UseQ => ChampionMenu["LaneClear"]["LQ"].GetValue<MenuBool>().Enabled;
        private static int LaneClear_Mana => ChampionMenu["LaneClear"]["LMana"].GetValue<MenuSlider>().Value;
        private static bool JungleClear_UseQ => ChampionMenu["JungleClear"]["JQ"].GetValue<MenuBool>().Enabled;
        private static bool JungleClear_UseE => ChampionMenu["JungleClear"]["JE"].GetValue<MenuBool>().Enabled;
        private static bool DrawQRange => ChampionMenu["Drawing"]["DQ"].GetValue<MenuBool>().Enabled;
        private static bool DrawERange => ChampionMenu["Drawing"]["DE"].GetValue<MenuBool>().Enabled;
        private static bool DrawEPRange => ChampionMenu["Drawing"]["DEP"].GetValue<MenuBool>().Enabled;
        private static bool UseEInterrupt => ChampionMenu["Misc"]["InterruptSpells"].GetValue<MenuBool>().Enabled;
        private static bool AntiGap => AntiGapcloserMenu["AntiEGap"].GetValue<MenuBool>().Enabled;
        #endregion

        #region 初始化
        public Vayne()
        {
            Q = new Spell(SpellSlot.Q, 300);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 550f + GameObjects.Player.BoundingRadius);
            E.SetTargetted(0.25f, 2200f);
            R = new Spell(SpellSlot.R);
            Dash = new Dash(Q);
            Evade = new InvisibilityEvade(new InvsSpell() { spell = Q,IsDashSpell = true},Dash);
            OnMenuLoad();
            Orbwalker.OnBeforeAttack += OnBeforeAttack;
            Orbwalker.OnAfterAttack += OnAfterAttack;
            Game.OnUpdate += Game_OnUpdate;
            Render.OnEndScene += OnDraw;
            Interrupter.OnInterrupterSpell += OnInterruptSpells;
            AntiGapcloser.OnGapcloser += AntiGapCloser;
        }
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Vayne));
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo"));
            {
                Combo.Add(new MenuBool("CQ", "Use Q"));
                Combo.Add(new MenuBool("CQOnlyWA", Program.Chinese ? "-> 仅在二环时使用" : "Only W Count == 2", false));
                Combo.Add(new MenuBool("CQUlt", Program.Chinese ? "额外 Q 安全逻辑" : "Extra Q Safe Logic"));
                Combo.Add(new MenuBool("CE", "Use E"));
                Combo.Add(new MenuBool("ForceW", "Force W Target"));
            }
            var Condemn = ChampionMenu.Add(new Menu("Condemn", "Condemn"));
            {
                Condemn.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.VayneE));
                foreach (var enemy in Cache.EnemyHeroes)
                {
                    Condemn.Add(new MenuBool("condemnset." + enemy.CharacterName, string.Format("Condemn:{0}", enemy.CharacterName)));
                }
                Condemn.Add(new MenuSlider("condemndis", "Condemn Distance", 420, 350, 475));
                Condemn.Add(new MenuKeyBind("smatrE", Program.Chinese ?  "一键E走离自己最近的近战英雄" : "Fast Use Condemn Melee Hero min dist",Keys.E,KeyBindType.Press)).AddPermashow();
            }
            var RSet = ChampionMenu.Add(new Menu("R", Program.Chinese ? "终极时刻" : "R"));
            {
                RSet.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.VayneR));
                RSet.Add(new MenuBool("CR", "Use R", false));
                RSet.Add(new MenuSlider("CRCount", Program.Chinese ? "-> 使用 R 当敌军人数 >= X" : "Use R if Enemy Count >= X", 2, 1, 5));
                RSet.Add(new MenuSlider("CRWaitPass", Program.Chinese ? "Q保持隐身状态的时间" : "Q steal Time", 1000, 0, 1000));
                RSet.Add(new MenuBool("CRPassForceKill", Program.Chinese ? "如果隐身状态下目标能被最后一A杀死则强制取消隐身" : "force kill if target health <= AA  when steal"));
            }
            var Harass = ChampionMenu.Add(new Menu("Harass", "Harass"));
            {
                Harass.Add(new MenuBool("HQ", "Use Q"));
                Harass.Add(new MenuBool("HQA", Program.Chinese ? "使用 先手Q - >A骚扰" : "Use QA harass"));
                Harass.Add(new MenuBool("HE", "Use E"));
                Harass.Add(new MenuSlider("HMana", Program.Chinese ? "当蓝量 <= X 时不用技能骚扰" : "Don't Use Spell Harass if Mana <= X%", 60, 0, 100));
                Harass.Add(new MenuList("HMode", Program.Chinese ? "骚扰模式" : "HarassMode", new string[] { "2Passive + QA", "2Passive + E", "All" }));
            }
            var LaneClear = ChampionMenu.Add(new Menu("LaneClear", "LaneClear"));
            {
                LaneClear.Add(new MenuBool("LQ", "Use Q"));
                LaneClear.Add(new MenuSlider("LMana", Program.Chinese ? "当蓝量 <= X 时不用技能清线野" : "Don't use Spell LaneClear/JungleClear if Mana <= X%", 60, 0, 100));
            }
            var JungleClear = ChampionMenu.Add(new Menu("JungleClear", "JungleClear"));
            {
                JungleClear.Add(new MenuBool("JQ", "Use Q"));
                JungleClear.Add(new MenuBool("JE", "Use E"));
            }
            var Drawing = ChampionMenu.Add(new Menu("Drawing", "Draw"));
            {
                Drawing.Add(new MenuBool("DQ", "Q Range"));
                Drawing.Add(new MenuBool("DE", "E Range"));
                Drawing.Add(new MenuBool("DEP", "E Rect"));
            }
            var Misc = ChampionMenu.Add(new Menu("Misc", "Misc"));
            {
                Misc.Add(new MenuBool("InterruptSpells", Program.Chinese ? "使用E打断技能" : "Use E Interrupt Spell"));
                var InterruptList = Misc.Add(new Menu("InterruptList", Program.Chinese ? "中断英雄列表" : "Interrupt List"));
                {
                    foreach (var obj in GameObjects.EnemyHeroes)
                    {
                        InterruptList.Add(new MenuBool("rupt." + obj.CharacterName, obj.CharacterName));
                    }
                }
            }
            AntiGapcloserMenu = AntiGapcloser.Attach(ChampionMenu);
            {
                AntiGapcloserMenu.Add(new MenuBool("AntiEGap", "Use Q + E AntiGapCloser"));
            }
 
        }
        #endregion
        private void Game_OnUpdate(EventArgs arg)
        {
            Evade.CanEvadeBool(() => Player.HasBuff("vaynetumblefade"));
            if (ForceW)
            {
                var heroList = TargetSelector.GetTargets(Player.GetCurrentAutoAttackRange(), DamageType.Physical);
                Orbwalker.ForceTarget = heroList.FirstOrDefault(x => GetPassiveCount(x) > 0);
            }
            if (SmartE && E.IsReady())
            {
                var waringenemy = Cache.EnemyHeroes.Where(x => x.NewIsValidTarget(E.Range)).MinOrDefault(x => x.DistanceToPlayer());
                if (waringenemy != null)
                {
                    E.CastOnUnit(waringenemy);
                }
            }
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    ELogic();
                    CheckR();
                    break;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
                case OrbwalkerMode.LaneClear:
                    JungleUsage();
                    break;
                case OrbwalkerMode.LastHit:
                    break;
                case OrbwalkerMode.Flee:
                    break;
            }
        }
        private void OnAfterAttack(object Non,AfterAttackEventArgs args)
        {
            if(Orbwalker.ActiveMode == OrbwalkerMode.Combo && Q.IsReady())
            {
                if (ComboUseQ)
                {
                    if (args.Target != null && args.Target is AIHeroClient)
                    {
                        var ToTarget = args.Target as AIHeroClient;
                        if (ToTarget != null && (!CQOnlyWA || GetPassiveCount(ToTarget) == 1))
                        {
                            var pos = Dash.CastDash(true);
                            if (pos.IsValid())
                            {
                                Q.Cast(pos);
                            }
                        }
                    }
                }
            }
            if (Enable_laneclear && Player.ManaPercent > LaneClear_Mana && (Orbwalker.ActiveMode == OrbwalkerMode.LastHit || Orbwalker.ActiveMode == OrbwalkerMode.LaneClear))
            {
                var m = args.Target as AIMinionClient;
                if(m != null && Q.IsReady())
                {
                    var dashPosition = Game.CursorPos;
                    if (m.Team == GameObjectTeam.Neutral && JungleClear_UseQ)
                    {
                        Q.Cast(dashPosition);
                        return;
                    }
                    if (LaneClear_UseQ)
                    {
                        foreach (var minion in Cache.GetMinions(Player.Position,615f).Where(minion => m.NetworkId != minion.NetworkId))
                        {
                            var time = (int)(Player.AttackCastDelay * 1000) + Game.Ping / 2 + 1000 * (int)Math.Max(0, Player.Distance(minion) - Player.BoundingRadius) / (int)Player.BasicAttack.MissileSpeed;
                            var predHealth = HealthPrediction.GetPrediction(minion, time);
                            if (predHealth < Player.GetAutoAttackDamage(minion) + Q.GetDamage(minion) && predHealth > 0)
                            {
                                Q.Cast(dashPosition);
                            }
                        }
                    }
                }
            }   
            if (Orbwalker.ActiveMode == OrbwalkerMode.Harass && Player.ManaPercent > HarassMana)
            {
                var tyh = args.Target.Type == GameObjectType.AIHeroClient ? args.Target as AIHeroClient : null;
                if (GetPassiveCount(tyh) == 1)
                {
                    if (E.IsReady() && HarassUseE && (HarassMode == 1 || HarassMode == 2) && tyh.NewIsValidTarget(E.Range))
                    {
                        E.CastOnUnit(tyh);
                    }
                    if (Q.IsReady() && HarassMode == 0 && HarassUseQ)
                    {
                        Q.Cast(Game.CursorPos);
                    }
                }
                if (GetPassiveCount(tyh) == 0)
                {
                    if (Q.IsReady() && HarassMode == 2 && HarassUseQ)
                    {
                        Q.Cast(Game.CursorPos);
                    }
                }
            }
        }
        private void OnBeforeAttack(object Non, BeforeAttackEventArgs args)
        {
            if(Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                if (ComboUseQ && CQUlt)
                {
                    if (Player.HasBuff("vaynetumblefade") && Dash.InMelleAttackRange(Player.Position))//在近战攻击范围内时
                    {
                        args.Process = false;
                        return;
                    }
                }
                if (args.Target.IsEnemy && Player.HasBuff("vaynetumblefade"))
                {
                    var enemy = args.Target as AIHeroClient;
                    if (enemy == null) 
                        return;
                    var stealthbuff = Player.GetBuff("vaynetumblefade");
                    
                    if (stealthbuff.EndTime - Game.Time < (stealthbuff.EndTime - stealthbuff.StartTime) - (ComboUseRWaitAA / 1000f))
                    {
                        args.Process = true;
                        return;
                    }
                    else
                    {
                        if (ForceATTACK && enemy.GetRealHeath(DamageType.Physical) < GetWADamage(enemy))
                        {
                            args.Process = true;
                            return;
                        }
                        args.Process = false;
                    }
                }
            }
        }
        private void OnInterruptSpells(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (args.Sender.IsEnemy && UseEInterrupt && E.IsReady() && args.DangerLevel == Interrupter.DangerLevel.High)
            {
                if (ChampionMenu["Misc"]["InterruptList"]["rupt." + args.Sender.CharacterName].GetValue<MenuBool>().Enabled)
                {
                    if (!args.Sender.IsInvulnerable && !args.Sender.HaveSpellShield() && args.Sender.NewIsValidTarget(E.Range))
                    {
                        E.Cast(args.Sender);
                    }
                }
            }
        }
        private void AntiGapCloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs e)
        {
            if (AntiGap)
            {
                bool isQ = false;
                if (sender.IsEnemy)
                {
                    if (e.StartPosition.DistanceToPlayer() > e.EndPosition.DistanceToPlayer() && e.EndPosition.DistanceToPlayer() <= 300)
                    {
                        if (Q.IsReady())
                        {
                            if (Q.Cast(Player.Position.Extend(sender.Position, -300f)))
                            {
                                isQ = true;
                            }
                        }
                        if (E.IsReady())
                        {
                            if (isQ)
                            {
                                EnsoulSharp.SDK.Utility.DelayAction.Add(500, () => 
                                {
                                    if (sender.IsDashing() || sender.DistanceToPlayer() < 300)
                                    {
                                        E.CastOnUnit(sender);
                                    }
                                });
                            }
                            else
                            {
                                E.CastOnUnit(sender);
                            }
                        }
                    }
                }
            }
        }
        private void OnDraw(EventArgs args)
        {
            if(DrawQRange && Q.IsReady())
            {
                var pos = Player.Position.Extend(Game.CursorPos, 300f);
                PlusRender.DrawCircle(pos, 30f, Color.Red);
            }
            if (DrawERange && E.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, E.Range, Color.Orange);
            }
            if (DrawEPRange && E.IsReady())
            {
                foreach (var hero in Cache.EnemyHeroes.Where(x => x.NewIsValidTarget(E.Range)))
                {
                    var color = System.Drawing.Color.White;
                    for (var i = 15; i < Condemndis; i += 75)
                    {
                        Vector3 loc3 = hero.ServerPosition.Extend(Player.ServerPosition, -i);
                        var posCF = NavMesh.GetCollisionFlags(loc3);
                        var Gamos = new Geometry.Rectangle(hero.Position, loc3, hero.BoundingRadius);
                        if (posCF.HasFlag(CollisionFlags.Wall) || posCF.HasFlag(CollisionFlags.Building))
                        {
                            color = System.Drawing.Color.Green;
                        }
                        Gamos.Draw(color, 3);
                    }
                }
            }
        }
        private void JungleUsage()
        {
            if (!Enable_laneclear || Player.ManaPercent < LaneClear_Mana)
                return;

            if(JungleClear_UseE && E.IsReady())
            {
                var target = Orbwalker.GetTarget();
                if (target != null && target is AIMinionClient)
                {
                    
                    var minion = (AIMinionClient)target;
                   
                    if (_jungleMobs.Contains(minion.CharacterName))
                        for (var i = 40; i < 425; i += 141)
                        {
                            var flags = NavMesh.GetCollisionFlags(minion.Position.ToVector2().Extend(Player.Position.ToVector2(), -i).ToVector3());
                            if (flags.HasFlag(CollisionFlags.Wall) || flags.HasFlag(CollisionFlags.Building))
                            {
                                E.Cast(minion);
                                return;
                            }
                        }
                }
            }
        }
        private void CheckR()
        {
            if (ComboUseR && R.IsReady() && Player.Position.CountEnemyHerosInRangeFix(800f) >= ComboUseRCount)
            {
                R.Cast();
            }
        }
        private void ELogic()
        {
            var dashPosition = Player.Position.Extend(Game.CursorPos, Q.Range);

            if (ComboUseE && E.IsReady())
            {
                foreach (var obj in Cache.EnemyHeroes.Where(x => x.NewIsValidTarget(E.Range) && ChampionMenu["Condemn"]["condemnset." + x.CharacterName].GetValue<MenuBool>().Enabled))
                {
                    DoCondemn_old(obj);
                }
            }
        }
        private void DoCondemn_old(AIBaseClient hero)
        {
            if (hero == null || !hero.IsValidTarget(E.Range)) 
                return;
            if (hero.HasBuffOfType(BuffType.SpellShield) ||
                hero.HasBuffOfType(BuffType.SpellImmunity) || hero.IsDashing()) 
                return;
            var pred = E.GetPrediction(hero);
            if (pred.Hitchance >= HitChance.High)
            {
                for (var i = 40; i < 425; i += 125)
                {
                    var flags = NavMesh.GetCollisionFlags(
                        pred.UnitPosition.ToVector2().Extend(GameObjects.Player.Position.ToVector2(), -i).ToVector3());
                    if (flags.HasFlag(CollisionFlags.Wall) || flags.HasFlag(CollisionFlags.Building))
                    {
                        E.CastOnUnit(hero);
                    }
                }
            }
        }
        private int GetPassiveCount(AIBaseClient unit)
        {
            return unit.GetBuffCount("vaynesilvereddebuff");
        }
        private float GetWADamage(AIHeroClient unit)
        {
            float BaseDamage = 4f + (W.Level - 1) * 2.5f;
            float Damage = unit.MaxHealth * BaseDamage;
            float AADmg = (float)Player.GetAutoAttackDamage(unit);
            if (GetPassiveCount(unit) == 2)
            {
                //有两层被动时
                return AADmg + Damage;
            }
            return AADmg;
        }
        private void Harass()
        {
            if (Q.IsReady() && HarassUseQA && Player.ManaPercent > HarassMana)
            {
                var enemy = Orbwalker.GetTarget();
                var target = TargetSelector.GetTarget(Q.Range + Player.AttackRange + Player.BoundingRadius, DamageType.Physical);
                if(enemy == null && target != null)
                {
                    var pos = Dash.CastDash();
                    if (pos.IsValid())
                    {
                        Q.Cast(pos);
                    }
                    
                }
            }
        }
    }
}
