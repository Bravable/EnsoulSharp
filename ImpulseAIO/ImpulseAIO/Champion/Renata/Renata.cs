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
namespace ImpulseAIO.Champion.Renata
{
    class TrapsInfo
    {
        public int Width { get; set; }
        public AIBaseClient Pointer { get; set; }
        public int VaildTime { get; set; }
    }
    public enum CastState
    {
        NotReady,
        First,
        Second
    }
    internal class Renata : Base 
    {
        private static Spell Q, W,E, R;
        private static int Delay = 0;
        private AIBaseClient Qedtarget => Cache.EnemyHeroes.Find(e => e.HasBuff("RenataQ") && e.GetBuff("RenataQ").Caster.IsMe);
        private static List<TrapsInfo> TrapList = new List<TrapsInfo>();

        private static bool ComboUseQ => ChampionMenu["Combo"]["CQ"].GetValue<MenuBool>().Enabled;
        private static bool ComboUseE => ChampionMenu["Combo"]["CE"].GetValue<MenuBool>().Enabled;

        private static int WMode => ChampionMenu["Bailout"]["WMode"].GetValue<MenuList>().Index;
        private static int UseWHELP => ChampionMenu["Bailout"]["UseWHELP"].GetValue<MenuList>().Index;
        private static int UseWHELP2 => ChampionMenu["Bailout"]["UseWHELP2"].GetValue<MenuSlider>().Value;
        private static int UseWHELP21 => ChampionMenu["Bailout"]["UseWHELP21"].GetValue<MenuSlider>().Value;

        private static int RMode => ChampionMenu["Hostile"]["RMode"].GetValue<MenuList>().Index;
        private static int RMinHits => ChampionMenu["Hostile"]["RMinHits"].GetValue<MenuSlider>().Value;
        private static int RCheckRange => ChampionMenu["Hostile"]["RCheckRange"].GetValue<MenuSlider>().Value;
        private static int Count => ChampionMenu["Hostile"]["Count"].GetValue<MenuSlider>().Value;

        private static bool DQ => ChampionMenu["Drawing"]["DQ"].GetValue<MenuBool>().Enabled;
        private static bool DW => ChampionMenu["Drawing"]["DW"].GetValue<MenuBool>().Enabled;
        private static bool DE => ChampionMenu["Drawing"]["DE"].GetValue<MenuBool>().Enabled;
        private static bool DR => ChampionMenu["Drawing"]["DR"].GetValue<MenuBool>().Enabled;
        public Renata()
        {
            Q = new Spell(SpellSlot.Q, 900f);
            Q.SetSkillshot(0.25f, 70f, 1450f, true, SpellType.Line);
            W = new Spell(SpellSlot.W, 800f);
            W.SetTargetted(0f, float.MaxValue);
            E = new Spell(SpellSlot.E, 800f);
            E.SetSkillshot(0.25f,110f,1450f,false,SpellType.Line);
            R = new Spell(SpellSlot.R,2000f);
            R.SetSkillshot(0.75f,250f,650f,false,SpellType.Line);
            InitFlowersArray();
            OnMenuLoad();
            AllyChampSaver.Initialize();
            Game.OnUpdate += GameOnUpdate;
            Render.OnDraw += (s) => {
                if(DQ && Q.IsReady())
                {
                    PlusRender.DrawCircle(Player.Position, Q.Range, Color.Red);
                }
                if (DW && W.IsReady())
                {
                    PlusRender.DrawCircle(Player.Position, W.Range, Color.Green);
                }
                if (DE && E.IsReady())
                {
                    PlusRender.DrawCircle(Player.Position, E.Range, Color.CadetBlue);
                }
                if (DR && R.IsReady())
                {
                    PlusRender.DrawCircle(Player.Position, R.Range, Color.OrangeRed);
                }
            };
        }
        private bool IsHookChamp(AIHeroClient unit)
        {
            return ChampionMenu["Hook"]["zq." + unit.CharacterName].GetValue<MenuBool>().Enabled;
        }
        private void OnMenuLoad()
        {
            ChampionMenu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Renata));
            var Combo = ChampionMenu.Add(new Menu("Combo", "Combo"));
            {
                Combo.Add(new MenuBool("CQ", "Use Q"));
                Combo.Add(new MenuBool("CE", "Use E"));
            }
            var HookList = ChampionMenu.Add(new Menu("Hook", Program.Chinese ? "高优先级抓取列表": "High Priority Hook List"));
            {
                foreach (var obj in Cache.EnemyHeroes)
                {
                    HookList.Add(new MenuBool("zq." + obj.CharacterName, obj.CharacterName,TargetSelector.GetPriority(obj) >= 3));
                }
                HookList.Add(new MenuSeparator("SD", "如果开启了此选项 那么该英雄会被拉入到友军集火中"));
                HookList.Add(new MenuSeparator("SD1", "举个关闭例子 如果对方是石头人/皇子 你会希望他进入友军堆中么"));
            }
            var Bailout = ChampionMenu.Add(new Menu("Bailout", "Bailout"));
            {
                Bailout.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Renata_Glasc_Bailout));
                Bailout.Add(new MenuList("WMode", "Use W Mode",new string[] { "Only Combo","Always","Disable"},1));
                Bailout.Add(new MenuList("UseWHELP", Program.Chinese ? "使用 W 救治友军" : "Use W Help ally", new string[] { "Auto","W Health","Disable"}));
                Bailout.Add(new MenuSlider("UseWHELP2", Program.Chinese ? "->当上述条件为血量判断时 友军血量<=X%" : "Enable <w help>  when ally health <= X%", 20,0,100));
                Bailout.Add(new MenuSlider("UseWHELP21", Program.Chinese ? "->当上述条件为血量判断时 友军周围敌人数 >= X" : "ally enemy count >= X", 1,1,5));
                var WBlackList = Bailout.Add(new Menu("Black", Program.Chinese ? "W 优先级设置 (0为不使用)" : "W Priority Set (0 is disable)"));
                {
                    foreach(var obj in Cache.AlliesHeroes)
                    {
                        var yxj = TargetSelector.GetDefaultPriority(obj);
                        WBlackList.Add(new MenuSlider("notw." + obj.CharacterName, obj.CharacterName, yxj,0,5));
                    }
                }
            }
            var Hostile = ChampionMenu.Add(new Menu("Hostile", "Hostile Takeover"));
            {
                Hostile.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Renata_Glasc_Hostile_Takeover));
                Hostile.Add(new MenuList("RMode", "Use R Mode", new string[] { "Only Combo", "Always", "Disable" }));
                Hostile.Add(new MenuSlider("RMinHits", Program.Chinese ? "使用 R 最少命中敌人数" :"Use R Min hits X enemy",2,1,5));
                Hostile.Add(new MenuSlider("RCheckRange", Program.Chinese ? "当敌人距离 <= X时使用" : "Use R When Enemy dist player <= X", 900, 400, 2000));
                Hostile.Add(new MenuSlider("Count", Program.Chinese ? "当 X码内有敌人时不使用" : "Don't R if has Enemy in x Range", 400, 0, 1000));
            }
            var Drawing = ChampionMenu.Add(new Menu("Drawing", "Draw"));
            {
                Drawing.Add(new MenuBool("DQ", "Q Range"));
                Drawing.Add(new MenuBool("DW", "W Range",false));
                Drawing.Add(new MenuBool("DE", "E Range"));
                Drawing.Add(new MenuBool("DR", "R Range"));
            }
        }
        private void RLogic()
        {
            if (!R.IsReady() || Player.CountEnemyHerosInRangeFix(Count) != 0)
                return;

            if((RMode == 0 && Orbwalker.ActiveMode == OrbwalkerMode.Combo) || RMode == 1)
            {
                foreach(var obj in Cache.EnemyHeroes.Where(x => x.IsValidTarget(R.Range)))
                {
                    if(obj.DistanceToPlayer() <= RCheckRange)
                    {
                        var pred = R.GetPrediction(obj, true, -1, new CollisionObjects[] { CollisionObjects.YasuoWall });
                        if(pred.Hitchance >= HitChance.VeryHigh && pred.AoeTargetsHitCount >= RMinHits)
                        {
                            R.Cast(pred.CastPosition);
                        }
                    }
                }
            }
        }
        private static int GetWPriority(AIHeroClient unit)
        {
            return ChampionMenu["Bailout"]["Black"]["notw." + unit.CharacterName].GetValue<MenuSlider>().Value;
        }
        private void GameOnUpdate(EventArgs args)
        {
            Orbwalker.AttackEnabled = !(GetQState() == CastState.Second || (Variables.GameTimeTickCount - Q.LastCastAttemptTime <= 400));
            WLogic();
            RLogic();
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
        private void WLogic()
        {
            if (!W.IsReady()) return;
            if((WMode == 0 && Orbwalker.ActiveMode == OrbwalkerMode.Combo) || WMode == 1)
            {
                if(UseWHELP != 2)
                {
                    if(UseWHELP == 1)
                    {
                        //降幂排序
                        foreach(var obj in Cache.AlliesHeroes.Where(x => x.IsValidTarget(W.Range, false)).
                            OrderByDescending(x => GetWPriority(x)))
                        {
                            if(GetWPriority(obj) != 0)
                            {
                                if(obj.HealthPercent <= UseWHELP2 && obj.CountEnemyHerosInRangeFix(500) >= UseWHELP21)
                                {
                                    W.CastOnUnit(obj);
                                }
                            }
                        }
                    }
                }
            }
        }
        private void Combo()
        {
            if (ComboUseE && E.IsReady())
            {
                var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                if (target.IsValidTarget())
                {
                    var pred = E.GetPrediction(target, true, -1, new CollisionObjects[] { CollisionObjects.YasuoWall });
                    if(pred.Hitchance >= HitChance.High)
                    {
                        var endpos = Player.Position.Extend(pred.CastPosition, E.Range);
                        E.Cast(endpos);
                    }
                }
            }
            if (ComboUseQ)
            {
                if (GetQState() == CastState.First && Variables.GameTimeTickCount > Delay)
                {
                    var Ret = IMPGetTarGet(Q, false, HitChance.High);
                    if(Ret.SuccessFlag && Ret.Obj.IsValid)
                    {
                        Q.Cast(Ret.CastPosition);
                        Delay = Variables.GameTimeTickCount + 500;
                    }
                }
                if (GetQState() == CastState.Second)
                {
                    if (Qedtarget.IsValidTarget() && Qedtarget.Type == GameObjectType.AIHeroClient)
                    {
                        var HERO = Qedtarget as AIHeroClient;
                        if (HERO.IsValidTarget())
                        {
                            if(GetBuffLaveTime(HERO, "RenataQ") < 0.1 || HERO.IsCastingImporantSpell())
                            {
                                Q.Cast(GetBestBackPosition(HERO));
                            }
                        }
                    }
                }
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
            if (ToBase.Name.Equals("Noxious Trap") && ToBase.MaxHealth == 6 && ToBase.IsAlly)  //烬 陷阱
            {
                TrapList.Add(new TrapsInfo() { Pointer = ToBase, VaildTime = Variables.GameTimeTickCount + 180000,Width = 160 });
                return;
            }
            if (ToBase.Name.Equals("Cupcake Trap") && ToBase.MaxHealth == 100 && ToBase.IsAlly) //女警 夹子
            {
                TrapList.Add(new TrapsInfo() { Pointer = ToBase, VaildTime = Variables.GameTimeTickCount + 180000,Width = 15 });
                return;
            }
            if (ToBase.Name.Equals("Noxious Trap") && ToBase.MaxHealth == 1 && ToBase.IsAlly)  //金克斯 夹子
            {
                TrapList.Add(new TrapsInfo() { Pointer = ToBase, VaildTime = Variables.GameTimeTickCount + 3000, Width = 115 });
                return;
            }
        }
        private void Game_OnObjectDelete(GameObject obj, EventArgs s)
        {
            var ToBase = obj as AIBaseClient;
            if (ToBase == null)
            {
                return;
            }
            var FindNetwork = TrapList.Find(x => x.Pointer.NetworkId == ToBase.NetworkId);
            if (FindNetwork != null)
            {
                TrapList.RemoveAll(x => x.Pointer.NetworkId == FindNetwork.Pointer.NetworkId);
            }
        }
        private bool IsSafePos(Vector2 obj)
        {
            return obj.CountAllysHerosInRangeFix(400) <= 2;
        }
        private Vector3 GetBestBackPosition(AIBaseClient unit)
        {
            //没有阻碍的陷阱
            var FirstTrap = TrapList.FirstOrDefault(x => x.Pointer.Distance(unit) <= 250f && (GetFirstWallPoint(unit.ServerPosition.ToVector2(),x.Pointer.Position.ToVector2()) == Vector2.Zero));
            if(FirstTrap != null && FirstTrap.Pointer != null)
            {
                return FirstTrap.Pointer.Position;
            }
            //碰撞到英雄
            var enemys = Cache.EnemyHeroes.Where(x => x.NetworkId != unit.NetworkId && GetFirstWallPoint(unit.ServerPosition.ToVector2(), x.ServerPosition.ToVector2()) == Vector2.Zero && x.IsValidTarget(250f + x.BoundingRadius, true, unit.ServerPosition)).ToList();
            var CastingImport = enemys.FirstOrDefault(x => x.IsCastingImporantSpell() || x.IsDashing());
            if (CastingImport != null)
            {
                return CastingImport.ServerPosition;
            }
            var enemyToHit = enemys.FirstOrDefault();
            if (enemyToHit != null)
            {
                return enemyToHit.ServerPosition;
            }
            var Circle = new Geometry.Circle(unit.ServerPosition, 250f,30);
            foreach(var obj in Circle.Points.Select(x => unit.ServerPosition.Extend(x,250f).ToVector2()))
            {
                //如果有墙体阻碍 就跳过本次点位
                if(GetFirstWallPoint(unit.ServerPosition.ToVector2(),obj) != Vector2.Zero)
                {
                    continue;
                }
                //如果坐标在友军防御塔内时 拉到防御塔内
                if (obj.IsUnderAllyTurret() && (!unit.IsUnderAllyTurret() || unit.DistanceToPlayer() > obj.DistanceToPlayer()))
                {
                    //坐标周围友军<=2
                    if (Player.IsUnderAllyTurret())
                    {
                        return Player.Position;
                    }
                    if (IsSafePos(obj))
                    {
                        return obj.ToVector3World();
                    }
                }
                //如果目标在防御塔内 但是圆坐标不在防御塔内时 拉回来时更近了
                if (unit.IsUnderEnemyTurret() && !obj.IsUnderEnemyTurret() && unit.DistanceToPlayer() > obj.DistanceToPlayer())
                {
                    if (IsSafePos(obj))
                    {
                        return obj.ToVector3World();
                    }
                }
                //拉到友军列表内
                var toHero = unit as AIHeroClient;
                if(toHero.IsValidTarget() && IsHookChamp(toHero))
                {
                    if(unit.CountAllysHerosInRangeFix(500) < obj.CountAllysHerosInRangeFix(500))
                    {
                        return obj.ToVector3World();
                    }
                }
            }
            //血量较低 而且对方血量比我高
            if (Player.HealthPercent <= 35 && unit.HealthPercent > Player.HealthPercent && Player.CountAllysHerosInRangeFix(500) <= 2 && unit.Position.Extend(Player.Position,250f).DistanceToPlayer() <= 400)
            {
                return Player.Position.Extend(unit.ServerPosition,Player.Distance(unit)) + 400;
            }
            return Player.Position;
        }
        private CastState GetQState()
        {
            if (!Q.IsReady())
            {
                return CastState.NotReady;
            }
            if (Q.Instance.Name == "RenataQ")
            {
                return CastState.First;
            }
            if (Q.Instance.Name == "RenataQRecast")
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
        internal class AllyChampSaver
        {
            public static AIHeroClient SoulBound { get; private set; }

            private static readonly Dictionary<float, float> IncDamage = new Dictionary<float, float>();
            private static readonly Dictionary<float, float> InstDamage = new Dictionary<float, float>();
            public static float IncomingDamage
            {
                get { return IncDamage.Sum(e => e.Value) + InstDamage.Sum(e => e.Value); }
            }

            public static void Initialize()
            {
                // Listen to related events
                GameEvent.OnGameTick += OnTick;
                AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            }

            private static void OnTick(EventArgs args)
            {
                // SoulBound is not found yet!
                foreach(var obj in Cache.AlliesHeroes.Where(x => x.IsValid && x.IsValidTarget(W.Range,false)).OrderByDescending(x => GetWPriority(x)))
                {
                    if (GetWPriority(obj) == 0)
                        continue;
                    SoulBound = obj;
                }
                
                if (((WMode == 0 && Orbwalker.ActiveMode == OrbwalkerMode.Combo) || WMode == 1) && W.IsReady())
                {
                    // Ult casting
                    if (SoulBound.HealthPercent < 10 && SoulBound.CountEnemyHeroesInRange(500) > 0 ||
                        IncomingDamage > SoulBound.Health)
                    {
                        W.CastOnUnit(SoulBound);
                    }
                        
                }
                // Check spell arrival
                foreach (var entry in IncDamage.Where(entry => entry.Key < Game.Time).ToArray())
                {
                    IncDamage.Remove(entry.Key);
                }

                // Instant damage removal
                foreach (var entry in InstDamage.Where(entry => entry.Key < Game.Time).ToArray())
                {
                    InstDamage.Remove(entry.Key);
                }
            }
            private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
            {
                
                if (sender.IsEnemy)
                {
                    // Calculations to save your souldbound
                    if (SoulBound != null && ((WMode == 0 && Orbwalker.ActiveMode == OrbwalkerMode.Combo) || WMode == 1))
                    {
                        // Auto attacks
                        if (Orbwalker.IsAutoAttack(args.SData.Name) && args.Target != null && args.Target.NetworkId == SoulBound.NetworkId)
                        {
                            // Calculate arrival time and damage
                            IncDamage[SoulBound.ServerPosition.Distance(sender.ServerPosition) / args.SData.MissileSpeed + Game.Time] = (float)sender.GetAutoAttackDamage(SoulBound);
                        }
                        // Sender is a hero
                        else
                        {

                            var attacker = sender as AIHeroClient;
                            if (attacker != null)
                            {
                                var slot = attacker.GetSpellSlotFromName(args.SData.Name);

                                if (slot != SpellSlot.Unknown)
                                {
                                    if (slot == attacker.GetSpellSlotFromName("SummonerDot") && args.Target != null && args.Target.NetworkId == SoulBound.NetworkId)
                                    {
                                        // Ingite damage (dangerous)
                                        InstDamage[Game.Time + 2] = (float)attacker.GetSummonerSpellDamage(SoulBound, SummonerSpell.Ignite);
                                    }
                                    else
                                    {
                                        switch (slot)
                                        {
                                            case SpellSlot.Q:
                                            case SpellSlot.W:
                                            case SpellSlot.E:
                                            case SpellSlot.R:
                                                if ((args.Target != null && args.Target.NetworkId == SoulBound.NetworkId) || args.End.Distance(SoulBound.ServerPosition) < Math.Pow(args.SData.LineWidth, 2))
                                                {
                                                    // Instant damage to target
                                                    InstDamage[Game.Time + 2] = (float)attacker.GetSpellDamage(SoulBound, slot);
                                                }
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
