using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
namespace QSharp.Common.Evade.SpellsEvade
{
    public static class Config
    {
        public const int DiagonalEvadePointsCount = 7;

        public const int DiagonalEvadePointsStep = 20;

        public const int EvadingFirstTimeOffset = 250;

        public const int EvadingSecondTimeOffset = 80;

        public const int ExtraEvadeDistance = 15;

        public const int GridSize = 10;

        public const int SkillShotsExtraRadius = 9;

        public const int SkillShotsExtraRange = 20;

        public static void CreateMenu(Menu mainMenu)
        {
            var evadeMenu = mainMenu.Add(new Menu("Evade", Program.Chinese ? "技能躲避" : "Spell Evade"));
            {
                evadeMenu.Add(new MenuSeparator("Credit: Evade#", Program.Chinese ? "技能库" : "Spell Data"));
                var evadeSpells = evadeMenu.Add(new Menu("Spells", "Spells"));
                {
                    foreach (var spell in EvadeSpellDatabase.Spells)
                    {
                        var subMenu = evadeSpells.Add(new Menu(spell.Name, spell.Name));
                        {
                            if (spell.UnderTower)
                            {
                                subMenu.Add(new MenuBool(spell.Slot + "Tower", Program.Chinese ? "是否越塔" : "To Enemy Tower", false));
                            }
                            if (spell.ExtraDelay)
                            {
                                subMenu.Add(new MenuSlider(spell.Slot + "Delay", Program.Chinese ? "额外技能延迟" : "Extra Spell Delay", 100, 0, 150));
                            }
                            subMenu.Add(new MenuSlider("DangerLevel", Program.Chinese ? "如果危险等级 >=" : "If DangerLevel >= ", spell.DangerLevel, 1, 5));
                            if (spell.IsTargetted && spell.ValidTargets.Contains(SpellValidTargets.AllyWards))
                            {
                                subMenu.Add(new MenuBool("WardJump", Program.Chinese ? "跳墙" : "Cross Wall"));
                            }
                            subMenu.Add(new MenuBool("Enabled", Program.Chinese ? "开启" : "Enable"));
                        }
                    }
                }

                //DEBUGPORT
                foreach (var hero in (EvadeInit.IsDebugMode ? GameObjects.Heroes : GameObjects.EnemyHeroes)
                    .Where(
                        i =>
                        SpellDatabase.Spells.Any(
                            a =>
                            string.Equals(a.ChampionName, i.CharacterName, StringComparison.InvariantCultureIgnoreCase)))
                    )
                {
                    evadeMenu.Add(new Menu(hero.CharacterName.ToLowerInvariant(),hero.CharacterName));
                }
                foreach (var spell in
                    SpellDatabase.Spells.Where(
                        i =>
                        (EvadeInit.IsDebugMode ? GameObjects.Heroes : GameObjects.EnemyHeroes)
                    .Any(
                            a =>
                            string.Equals(a.CharacterName, i.ChampionName, StringComparison.InvariantCultureIgnoreCase)))
                    )
                {
                    var subMenu =
                        ((Menu)evadeMenu[spell.ChampionName.ToLowerInvariant()]).Add(
                            new Menu(spell.SpellName, $"{spell.SpellName} ({spell.Slot})"));
                    {
                        subMenu.Add(new MenuSlider("DangerLevel", Program.Chinese ? "危险等级" : "DangerLevel", spell.DangerValue, 1, 5));
                        subMenu.Add(new MenuBool("IsDangerous", Program.Chinese ? "是否为危险技能" : "is Danger", spell.IsDangerous));
                        subMenu.Add(new MenuBool("DisableFoW", Program.Chinese ? "无法躲避时使用" : "Use Spell if Can't Evade", false));
                        subMenu.Add(new MenuBool("Draw", Program.Chinese ? "是否绘出" : "Draw"));
                        subMenu.Add(new MenuBool("Enabled", Program.Chinese ? "开启" : "Enable", !spell.DisabledByDefault));
                    }
                }
                var drawMenu = evadeMenu.Add(new Menu("Draw", Program.Chinese ? "绘图" : "Draw"));
                {
                    drawMenu.Add(new MenuBool("Skillshot", Program.Chinese ? "弹道技能" : "Line Spell"));
                    drawMenu.Add(new MenuBool("Status", Program.Chinese ? "躲避状态" : "Evade Flag"));
                }
                evadeMenu.Add(new MenuKeyBind("Enabled", Program.Chinese ? "开启躲避" : "Enable Spell Evade", EnsoulSharp.SDK.MenuUI.Keys.U, KeyBindType.Toggle,true)).AddPermashow();

                evadeMenu.Add(new MenuKeyBind("OnlyDangerous", Program.Chinese ? "仅躲避危险技能" : "Only Evade Danger Spell", EnsoulSharp.SDK.MenuUI.Keys.Space, KeyBindType.Press));
            }
        }
    }
}
