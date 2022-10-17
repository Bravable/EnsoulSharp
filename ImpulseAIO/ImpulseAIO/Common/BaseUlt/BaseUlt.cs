using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;

using SharpDX;
namespace ImpulseAIO.Common.BaseUlt
{
    class Bases
    {
        public class BaseChamps
        {
            public bool IsRecall = false;
            public bool Teleporting = false;

            public int NetworkID { get; set; }
            public Vector3 PosBase { get; set; }
            public Teleport.TeleportStatus TStatus { get; set; }
            public Teleport.TeleportType TType { get; set; }
            public int Duration { get; set; }
            public int Start { get; set; }

            public BaseChamps(AIHeroClient target, Vector3 pos, Teleport.TeleportStatus status,
                Teleport.TeleportType type, int duration, int start)
            {
                NetworkID = target.NetworkId;
                PosBase = pos;
                TStatus = status;
                TType = type;
                Duration = duration;
                Start = start;
            }
        }
        public static List<BaseChamps> GetChamps = new List<BaseChamps>();
        public static List<AIBaseClient> BaseULTChamps = new List<AIBaseClient>();
        public static int Delay = 0;
        public static int Speed = 0;
    }
    public class BaseUlt 
    {
        private static Menu BaseUltMenu;
        private static AIHeroClient Player => GameObjects.Player;
        private static Spell BaseUltSpell = null;
        private static bool Enable_BaseUlt => BaseUltMenu["enable"].GetValue<MenuBool>().Enabled;
        private static int range => BaseUltMenu["range"].GetValue<MenuSlider>().Value;
        private static bool NoActiveKey => BaseUltMenu["NoActiveKey"].GetValue<MenuKeyBind>().Active;
        public static void Initialize(Menu BaseMenu,Spell spell)
        {
            BaseUltMenu = BaseMenu.Add(new Menu("BaseUlt", Program.Chinese ? "基地大招" : "BaseUlt", true));
            {
                BaseUltMenu.Add(new MenuBool("enable", Program.Chinese ? "开启基地大招" : "Enable BaseUlt"));
                BaseUltMenu.Add(new MenuSlider("range", Program.Chinese ? "如果自身x码有敌人则不使用大招" : "Don't BaseUlt If Player x Range Has Enemy",1200,100,2000));
                BaseUltMenu.Add(new MenuKeyBind("NoActiveKey", Program.Chinese ? "禁止基地大招" : "Disable BaseUlt",Keys.Space,KeyBindType.Press));
            }
            BaseUltSpell = spell;
            if (GameObjects.EnemyHeroes == null)
            {
                return;
            }
            var BasePos = GameObjects.EnemySpawnPoints.FirstOrDefault();
            foreach (var target in GameObjects.EnemyHeroes)
            {
                if (BasePos == null)
                {
                    return;
                }
                var nem = new Bases.BaseChamps(target, BasePos.Position, Teleport.TeleportStatus.Unknown,
                    Teleport.TeleportType.Unknown, 0, 0);
                Bases.GetChamps.Add(nem);
            }
            Game.OnUpdate += OnGameUpdate;
            Teleport.OnTeleport += OnTeleport;
        }
        private static void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead || NoActiveKey || !BaseUltSpell.IsReady() || !Enable_BaseUlt)
            {
                return;
            }
            int CountEnemy = Player.CountEnemyHerosInRangeFix(range);
            if(CountEnemy != 0)
            {
                return;
            }
            foreach (var target in Base.Cache.EnemyHeroes.Where(x => x.IsEnemy && !x.HaveSpellShield() && !x.IsDead && !x.IsZombie() && !x.IsInvulnerable))
            {
                var info = Bases.GetChamps.FirstOrDefault(x => x.NetworkID == target.NetworkId);
                if (info == null)
                {
                    continue; //切换下一个英雄
                }
                if (info.TType == Teleport.TeleportType.Recall && info.TStatus == Teleport.TeleportStatus.Start)
                {
                    if (Player.CharacterName.Equals("Senna"))
                    {
                        if (GetSennaRDmg(target) > target.GetRealHeath(DamageType.Physical) && target.GetRealHeath(DamageType.Physical) > 0)
                        {
                            Bases.BaseULTChamps.Add(target);

                            var x1 = info.PosBase.DistanceToPlayer() / BaseUltSpell.Speed * 1000 + (BaseUltSpell.Delay * 1000);
                            var x2 = info.Duration - (Variables.GameTimeTickCount - info.Start);

                            if (x1 >= x2 && x1 - x2 < 100)
                            {
                                if (BaseUltSpell.Cast(info.PosBase))
                                {
                                    return;
                                }
                            }
                        }
                    }
                    if (Player.CharacterName.Equals("Ezreal"))
                    {
                        if (Player.GetSpellDamage(target, SpellSlot.R) > target.GetRealHeath(DamageType.Magical) && target.GetRealHeath(DamageType.Magical) > 0)
                        {
                            Bases.BaseULTChamps.Add(target);
                            var x1 = info.PosBase.DistanceToPlayer() / BaseUltSpell.Speed * 1000 + (BaseUltSpell.Delay * 1000);
                            var x2 = info.Duration - (Variables.GameTimeTickCount - info.Start);

                            if (x1 >= x2 && x1 - x2 < 100)
                            {
                                if (BaseUltSpell.Cast(info.PosBase))
                                {
                                    return;
                                }
                            }
                        }
                    }
                    if (Player.CharacterName.Equals("Jinx"))
                    {
                        var delayshort = 2000 / 1700 * 1000;
                        var maxspeed = 2200;
                        var delay = 600;

                        if (GetJinxRDmg(target, 4, Player.Distance(info.PosBase)) > target.GetRealHeath(DamageType.Physical) && target.GetRealHeath(DamageType.Physical) > 0)
                        {
                            Bases.BaseULTChamps.Add(target);
                            if (Player.Distance(info.PosBase) > 2000)
                            {
                                delay = 600 + delayshort;
                                var x1 = (info.PosBase.DistanceToPlayer() - 2000) / maxspeed * 1000 + delay;
                                var x2 = info.Duration - (Variables.GameTimeTickCount - info.Start);
                                if (x1 >= x2 && x1 - x2 < 100)
                                {
                                    if (BaseUltSpell.Cast(info.PosBase))
                                    {
                                        return;
                                    }
                                }

                            }
                            else
                            {
                                var x1 = info.PosBase.DistanceToPlayer() / 1700f * 1000 + delay;
                                var x2 = info.Duration - (Variables.GameTimeTickCount - info.Start);

                                if (x1 >= x2 && x1 - x2 < 100)
                                {
                                    if (BaseUltSpell.Cast(info.PosBase))
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Bases.BaseULTChamps.Remove(target);
                    continue;
                }
            }
            Bases.BaseULTChamps.Clear();
        }
        private static void OnTeleport(AIBaseClient sender, Teleport.TeleportEventArgs args)
        {
            var Champ = Bases.GetChamps.FirstOrDefault(x => x.NetworkID == sender.NetworkId);
            if (Champ != null)
            {
                Champ.TStatus = args.Status;
                Champ.TType = args.Type;
                Champ.Duration = args.Duration;
                if (args.Status == Teleport.TeleportStatus.Start)
                {
                    Champ.Start = Variables.GameTimeTickCount;
                }
                else
                {
                    Champ.Start = 0;
                }
            }
            else
            {
                if (sender is AIHeroClient)
                {
                    var BasePos = GameObjects.EnemySpawnPoints.FirstOrDefault();
                    var nem = new Bases.BaseChamps((AIHeroClient)sender, BasePos.Position, args.Status, args.Type,
                        args.Duration, args.Start);
                    Bases.GetChamps.Add(nem);
                }
            }
        }
        private static float GetJinxRDmg(AIBaseClient unit, float FlyTime,float distance)
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
                if(distance >= 1500)
                {
                    return (float)EnsoulSharp.SDK.Damage.CalculatePhysicalDamage(Player, unit, MaxDamage + ExtraDmg);
                }
                float DmgPercent = 0.1f + (int)(distance / 100) * 0.06f;
                var endDamage = MaxDamage * DmgPercent + ExtraDmg;
                return (float)EnsoulSharp.SDK.Damage.CalculatePhysicalDamage(Player, unit, endDamage);
            }

        }
        private static float GetSennaRDmg(AIHeroClient unit)
        {
            int BaseDamage = 250 + (BaseUltSpell.Level - 1) * 125;
            float ExtraDmg = (Player.TotalMagicalDamage * 0.7f) + (Player.TotalAttackDamage - Player.BaseAttackDamage);
            return (float)Player.CalculatePhysicalDamage(unit, BaseDamage + ExtraDmg);
        }
    }
}
