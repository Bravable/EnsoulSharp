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
namespace ImpulseAIO.Champion.Kaisa
{
    internal class Kaisa : Base
    {
        private static Spell Q, W, E, R;
        private static InvisibilityEvade Evade;
        private static new Dash Dash;
        #region 菜单选项
        private static bool ComboUseQ => ChampionMenu["Combo"]["CQ"].GetValue<MenuBool>().Enabled;
        private static int ComboUseQCount => ChampionMenu["Combo"]["CQCount"].GetValue<MenuSlider>().Value;
        private static bool ComboUseW => ChampionMenu["Combo"]["CW"].GetValue<MenuBool>().Enabled;
        private static int ComboUseWCount => ChampionMenu["Combo"]["CWCount"].GetValue<MenuSlider>().Value;
        private static bool ComboUseE => ChampionMenu["Combo"]["CE"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseR => ChampionMenu["Combo"]["CR"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseRForce => ChampionMenu["Combo"]["CRForce"].GetValue<MenuBool>().Enabled;
        private static bool HarassUseQ => ChampionMenu["Harass"]["HQ"].GetValue<MenuBool>().Enabled;
        private static int HarassUseQCount => ChampionMenu["Harass"]["HQCount"].GetValue<MenuSlider>().Value;
        private static bool HarassUseW => ChampionMenu["Harass"]["HW"].GetValue<MenuBool>().Enabled;
        private static int HarassUseWCount => ChampionMenu["Harass"]["HWCount"].GetValue<MenuSlider>().Value;
        private static int HarassMana => ChampionMenu["Harass"]["HMana"].GetValue<MenuSlider>().Value;
        private static bool LaneClearUseQ => ChampionMenu["LaneClear"]["LQ"].GetValue<MenuBool>().Enabled;
        private static int LaneClearUseQCount => ChampionMenu["LaneClear"]["LQNum"].GetValue<MenuSlider>().Value;
        private static int LaneClearMana => ChampionMenu["LaneClear"]["LMana"].GetValue<MenuSlider>().Value;
        private static bool JungleUseQ => ChampionMenu["JungleClear"]["JQ"].GetValue<MenuBool>().Enabled;
        private static bool JungleUseW => ChampionMenu["JungleClear"]["JW"].GetValue<MenuBool>().Enabled;
        private static bool Fast => ChampionMenu["FastA"].GetValue<MenuKeyBind>().Active;

        private static bool IsUpdateQ
        {
            get
            {
                return Player.HasBuff("KaisaQEvolved");
            }
        }
        private static bool IsUpdateW
        {
            get
            {
                return Player.HasBuff("KaisaWEvolved");
            }
        }
        private static bool IsUpdateE
        {
            get
            {
                return Player.HasBuff("KaisaEEvolved");
            }
        }
        #endregion

        #region 初始化
        public Kaisa()
        {
            Q = new Spell(SpellSlot.Q, 600f + Player.BoundingRadius);
            W = new Spell(SpellSlot.W, 3000f);
            W.SetSkillshot(0.4f, 100f, 1750f, true, SpellType.Line);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 1500f);
            Q.DamageType = W.DamageType = DamageType.Physical;
            OnMenuLoad();
            Evade = new InvisibilityEvade(new InvsSpell() { spell = E,IsDashSpell = false},null);
            Dash = new Dash(R);
            Orbwalker.OnAfterAttack += OnOrbwalkerAfter;
            Game.OnUpdate += Game_OnUpdate;
            AIBaseClient.OnDoCast += OnDoCast;
            Render.OnEndScene += OnDrawEndScene;
        }
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Kaisa));
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo"));
            {
                Combo.Add(new MenuBool("CQ", "Use Q"));
                Combo.Add(new MenuSlider("CQCount", Program.Chinese ? "当周围小兵 <= X时才用Q" : "When QRange Minion <= X", 2, 1, 12));
                Combo.Add(new MenuBool("CW", "Use W"));
                Combo.Add(new MenuSlider("CWCount", Program.Chinese ? "当英雄身上电浆数 >=X 时才释放" : "Only When Passive Count >= X", 3, 0, 5));
                Combo.Add(new MenuBool("CE", "Use E"));
                Combo.Add(new MenuBool("CR", "Use R"));
                Combo.Add(new MenuBool("CRForce", Program.Chinese ? "->濒死强制R" : "If Will die.force R"));
            }
            var Harass = ChampionMenu.Add(new Menu("Harass", "Harass", true));
            {
                Harass.Add(new MenuBool("HQ", "Use Q"));
                Harass.Add(new MenuSlider("HQCount", Program.Chinese ? "当周围小兵 <= X时才用Q" : "When QRange Minion <= X", 2, 1, 12));
                Harass.Add(new MenuBool("HW", "Use W"));
                Harass.Add(new MenuSlider("HWCount", Program.Chinese ? "当英雄身上电浆数 >=X 时才释放" : "Only When Passive Count >= X", 3, 0, 5));
                Harass.Add(new MenuSlider("HMana", Program.Chinese ? "当蓝量 < X% 时不骚扰" : "Don't Harass if Mana <= X%", 25, 0, 100));
            }
            var LaneClear = ChampionMenu.Add(new Menu("LaneClear", "LaneClear", true));
            {
                LaneClear.Add(new MenuBool("LQ", "Use Q"));
                LaneClear.Add(new MenuSlider("LQNum", Program.Chinese ? "当周围小兵 >= X时才用Q" : "When QRange Minion >= X", 3, 1, 6));
                LaneClear.Add(new MenuSlider("LMana", Program.Chinese ? "当蓝量 < X% 时不清线野" : "Don't Laneclear/JungleClear if Mana <= X%", 30, 0, 100));
            }
            var JungleClear = ChampionMenu.Add(new Menu("JungleClear", "JungleClear"));
            {
                JungleClear.Add(new MenuBool("JQ", "Use Q"));
                JungleClear.Add(new MenuBool("JW", "Use W"));
            }
            ChampionMenu.Add(new MenuKeyBind("FastA", Program.Chinese ? "快速进化技能" : "Fast Update Spell", Keys.Alt, KeyBindType.Press)).AddPermashow("快速进化技能热键");
        }
        #endregion

        #region 类方法hook
        private void OnOrbwalkerAfter(object obj, AfterAttackEventArgs Args)
        {
            if(ComboUseW && W.IsReady())
            {
                var Unit = Args.Target as AIHeroClient;
                if (Unit.NewIsValidTarget(W.Range) && GetPassiveBuffCount(Unit) >= ComboUseWCount - 1)
                {
                    CastW(Unit);
                }
            }
        }
        private void Game_OnUpdate(EventArgs arg)
        {

            R.Range = 1500 + (R.Level - 1) * 750;

            Evade.CanEvadeBool(() => IsUpdateE);
            Orbwalker.AttackEnabled = !Player.HasBuff("KaisaE");
            if (Fast)
            {
                if (Player.EvolvePoints != 0)
                {
                    Player.Spellbook.CastSpell(SpellSlot.Recall);
                    Player.Spellbook.EvolveSpell(SpellSlot.E);
                    Player.Spellbook.EvolveSpell(SpellSlot.Q);
                    Player.Spellbook.EvolveSpell(SpellSlot.W);
                }
            }
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
                    LaneClear();
                    JungleClear();
                    break;
                case OrbwalkerMode.LastHit:
                    break;
                case OrbwalkerMode.Flee:
                    break;
            }
            //在近战英雄攻击范围内时E
            if (IsUpdateE && E.IsReady() && ComboUseE)
            {
                if (Dash.InMelleAttackRange(Player.ServerPosition))
                {
                    E.Cast();
                }
            }
        }
        private void OnDoCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs Args)
        {
            if (sender.IsMe)
            {
                if (Args.Slot == SpellSlot.W)
                {
                    Game.SendEmote(EmoteId.Joke);
                }
            }
        }
        private void OnDrawEndScene(EventArgs args)
        {
            if (R.IsReady())
            {
                PlusRender.DrawCircle(Player.Position, R.Range, Color.Purple);
            }
        }
        #endregion

        #region Methods
        private void AutoKill()
        {
            if (W.IsReady())
            {
                var target = TargetSelector.GetTargets(W.Range, DamageType.Magical).Where(x => x.GetRealHeath(DamageType.Magical) <= GetWDmg(x));
                foreach (var tt in target)
                {
                    var preds = W.GetPrediction(tt, false, -1, new CollisionObjects[] { CollisionObjects.Heroes, CollisionObjects.Minions, CollisionObjects.YasuoWall });
                    if (preds.Hitchance >= HitChance.High)
                    {
                        W.Cast(preds.CastPosition);
                    }
                }
            }
        }
        private void JungleClear()
        {
            if (!Enable_laneclear || Player.ManaPercent < LaneClearMana)
                return;

            if (JungleUseQ && Q.IsReady())
            {
                var jung = Cache.GetJungles(Player.ServerPosition, Q.Range);
                if (jung.Count > 0)
                {
                    Q.Cast();
                }
            }
            if (JungleUseW && W.IsReady())
            {
                var jung = GameObjects.GetJungles(Player.GetCurrentAutoAttackRange()).MaxOrDefault(y => GetPassiveBuffCount(y));
                if (jung.NewIsValidTarget())
                {
                    var preds = W.GetPrediction(jung);
                    if (preds.Hitchance >= HitChance.High)
                    {
                        W.Cast(preds.CastPosition);
                    }
                }
            }
        }
        private void LaneClear()
        {
            if (!Enable_laneclear || Player.ManaPercent < LaneClearMana)
                return;
            if (LaneClearUseQ && Q.IsReady())
            {
                if (Player.ServerPosition.CountMinionsInRangeFix(Q.Range) >= LaneClearUseQCount)
                {
                    var MiniNum = Player.ServerPosition.CountMinionsInRangeFix(Q.Range);
                    var dmg = MiniNum / (IsUpdateQ ? 12 : 6);
                    var em = Cache.GetMinions(Player.ServerPosition, Q.Range).Where(x => x.GetRealHeath(DamageType.Physical) <= GetQDmg(x, dmg));
                    if (em.Any())
                    {
                        Q.Cast();
                    }
                }
            }
        }
        private void Harass()
        {
            if (Player.ManaPercent <= HarassMana)
                return;

            if (HarassUseQ && Q.IsReady())
            {
                if (Player.ServerPosition.CountMinionsInRangeFix(Q.Range) < HarassUseQCount && Player.ServerPosition.CountEnemyHerosInRangeFix(Q.Range) > 0)
                {
                    Q.Cast();
                }
            }
            if (HarassUseW && W.IsReady())
            {
                var wwtarget = TargetSelector.GetTargets(W.Range, DamageType.Physical).Where(x => GetPassiveBuffCount(x) >= HarassUseWCount).FirstOrDefault();
                if (wwtarget.NewIsValidTarget())
                {
                    var pred = W.GetPrediction(wwtarget, false, -1, new CollisionObjects[] { CollisionObjects.Heroes, CollisionObjects.Minions, CollisionObjects.YasuoWall });
                    if (pred.Hitchance >= HitChance.High)
                    {
                        W.Cast(pred.CastPosition);
                    }
                }
            }
        }
        private int GetWCollision(Vector3 from,Vector3 to)
        {
            var List = new List<Vector2>() { to.ToVector2()};
            var WCollisionList = W.GetCollision(from.ToVector2(),List);
            return WCollisionList.Count;
        }
        private void Combo()
        {
            if (ComboUseR && R.IsReady())
            {
                //获取电浆
                var Rtarget = TargetSelector.GetTargets(R.Range, DamageType.Physical).Where(x => x.HasBuff("kaisapassivemarkerr"));
                var EnemyObjs = Cache.EnemyHeroes.Where(x => x.NewIsValidTarget()).ToList();
                //循环电浆角色
                foreach(var obj in Rtarget)
                {
                    if (W.IsReady() && ComboUseW && Player.Mana > R.Instance.ManaCost + W.Instance.ManaCost) // RW逻辑
                    {
                        foreach (var killwobj in EnemyObjs.Where(x => x.NewIsValidTarget(W.Range,true,obj.ServerPosition) && x.GetRealHeath(DamageType.Magical) < GetWDmg(x)))//以电浆角色为圆心 获取可W击杀对象
                        {
                            //获取坐标   点到击杀对象没有碰撞;
                            var Point = new Geometry.Circle(obj.ServerPosition, 470f,7).Points.Where(x => GetWCollision(x.ToVector3(), killwobj.ServerPosition) == 0 && x.Distance(killwobj) < W.Range - 50 &&
                                                                                             Dash.IsGoodPosition(x.ToVector3())).MinOrDefault(x => x.DistanceToCursor());
                            if (Point.IsValid())
                            {
                                bool isCast = R.Cast(Point);
                                if (isCast)
                                {
                                    CastW(killwobj);
                                }
                            }
                        }
                    }
                    if(obj.GetRealHeath(DamageType.Magical) <= GetQDmg(obj, 4) + GetWDmg(obj) + Player.GetAutoAttackDamage(obj) * 2 && !obj.InAutoAttackRange())
                    {
                        var Point = new Geometry.Circle(obj.ServerPosition, 470,7).Points.Where(x => Dash.IsGoodPosition(x.ToVector3())).ToList();

                        var bestPoint = Point.Where(x => !W.IsReady() || GetWCollision(x.ToVector3(), obj.ServerPosition) == 0).MinOrDefault(x => x.Distance(obj));
                        if (bestPoint.IsValid())
                        {
                            bool isCast = R.Cast(bestPoint);

                            if (ComboUseE && E.IsReady() && isCast)
                            {
                                if (bestPoint.CountEnemyHerosInRangeFix(400) >= 1 && bestPoint.CountAllysHerosInRangeFix(400) <= 3)
                                {
                                    if (IsUpdateE)
                                    {
                                        E.Cast();
                                    }
                                }
                            }

                        }
                    }
                    if (ComboUseRForce && Player.HealthPercent <= 10 && (Player.CountEnemyHerosInRangeFix(450) != 0 || InMelleAttackRange(Player.ServerPosition) || HealthPrediction.GetPrediction(Player,400) <= 0))
                    {
                        var Point = new Geometry.Circle(obj.ServerPosition, 470,7).Points.Where(x => Dash.IsGoodPosition(x.ToVector3())).MaxOrDefault(x => x.Distance(obj));
                        if (Point.IsValid())
                        {
                            bool isCast = R.Cast(Point);

                            if (ComboUseE && E.IsReady() && isCast)
                            {
                                if (IsUpdateE)
                                {
                                    E.Cast();
                                }
                            }

                        }
                    }

                }
            }
            if (ComboUseQ && Q.IsReady())
            {
                if (Player.ServerPosition.CountMinionsInRangeFix(Q.Range) <= ComboUseQCount && Player.ServerPosition.CountEnemyHerosInRangeFix(Q.Range) > 0)
                {
                    bool isCast = Q.Cast();

                    if (ComboUseE && E.IsReady() && isCast)
                    {
                        if (Player.ServerPosition.CountEnemyHerosInRangeFix(Q.Range) > 1) //如果Q范围内就一个人
                        {
                            E.Cast();
                        }
                        else
                        {
                            var bestAAtarget = Orbwalker.GetTarget();
                            if (bestAAtarget != null && bestAAtarget.Type == GameObjectType.AIHeroClient)
                            {
                                var tt = bestAAtarget as AIHeroClient;
                                if (bestAAtarget.Health > GetQDmg(tt, 6 - ComboUseQCount) + Player.GetAutoAttackDamage(tt) * 3)
                                {
                                    E.Cast();
                                }
                            }
                        }
                    }
                }
            }
            if (ComboUseW && W.IsReady() && !Player.Spellbook.IsWindingUp)
            {
                var wwtarget = TargetSelector.GetTargets(W.Range, DamageType.Physical).Where(x => Player.IsDashing() || GetPassiveBuffCount(x) >= ComboUseWCount).FirstOrDefault(x => GetWCollision(Player.IsDashing() ? Player.GetDashInfo().EndPos.ToVector3World() : Player.ServerPosition,x.ServerPosition) == 0);
                if (wwtarget.NewIsValidTarget())
                {
                    CastW(wwtarget);
                }
            }
        }
        private int GetPassiveBuffCount(AIBaseClient t)
        {
            return t.GetBuffCount("kaisapassivemarker");
        }
        private float GetQDmg(AIBaseClient t, int c)
        {
            if (!Q.IsReady())
                return 0;
            int baseDamage = 40 + (Q.Level - 1) * 15;
            float damageAD = (Player.TotalAttackDamage - Player.BaseAttackDamage) * 0.5f;
            float damageAP = Player.TotalMagicalDamage * 0.25f;
            float enddmg = baseDamage + damageAD + damageAP;
            if (c > 1)
            {
                enddmg = enddmg + enddmg * 0.25f * c;
            }
            enddmg = (float)Player.CalculatePhysicalDamage(t, enddmg);
            return enddmg;
        }
        private float GetWDmg(AIBaseClient t)
        {
            if (!W.IsReady())
                return 0;
            int baseDamage = 30 + (W.Level - 1) * 25;
            float damageAD = Player.TotalAttackDamage * 1.3f;
            float damageAP = Player.TotalMagicalDamage * 0.7f;
            float enddmg = baseDamage + damageAD + damageAP;
            if (GetPassiveBuffCount(t) >= 3)
            {
                float new_baseDamage = 0.15f + (0.025f / 100f * Player.TotalMagicalDamage);
                float health = (t.MaxHealth - t.GetRealHeath(DamageType.Magical)) * new_baseDamage;
                enddmg += health;
            }
            enddmg = (float)Player.CalculateMagicDamage(t, enddmg);
            return enddmg;
        }
        private void CastW(AIHeroClient unit)
        {
            if(unit == null)
            {
                return;
            }
            W.From = Player.IsDashing() ? Player.GetDashInfo().EndPos.ToVector3World() : Player.ServerPosition;
            var predW = W.GetPrediction(unit, false, -1, new CollisionObjects[] { CollisionObjects.Minions, CollisionObjects.Heroes, CollisionObjects.YasuoWall });
            if(predW.Hitchance >= HitChance.High)
            {
                W.Cast(predW.CastPosition);
            }
        }
        #endregion
    }
}
