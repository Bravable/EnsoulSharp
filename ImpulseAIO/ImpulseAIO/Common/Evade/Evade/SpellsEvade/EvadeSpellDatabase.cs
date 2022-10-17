using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnsoulSharp;
using EnsoulSharp.SDK;
namespace QSharp.Common.Evade.SpellsEvade
{
    internal class EvadeSpellDatabase
    {
        #region Static Fields

        public static List<EvadeSpellData> Spells = new List<EvadeSpellData>();
        public static List<EvadeSpellData> Items = new List<EvadeSpellData>();

        #endregion

        #region Constructors and Destructors

        static EvadeSpellDatabase()
        {
            EvadeSpellData spell;

            #region Champion SpellShields

            #region Sivir

            if (GameObjects.Player.CharacterName == "Sivir")
            {
                spell = new ShieldData("Sivir E", SpellSlot.E, 100, 1, true);
                Spells.Add(spell);
            }

            #endregion

            #region Gwen

            if (GameObjects.Player.CharacterName == "Gwen")
            {
                spell = new ShieldData("Gwen W", SpellSlot.W, 100, 4) { ExtraDelay = false };
                Spells.Add(spell);
            }

            #endregion

            #region Nocturne

            if (GameObjects.Player.CharacterName == "Nocturne")
            {
                spell = new ShieldData("Nocturne W", SpellSlot.W, 100, 1, true);
                Spells.Add(spell);
            }

            #endregion

            #endregion

            #region Champion MoveSpeed buffs

            #region Blitzcrank

            if (GameObjects.Player.CharacterName == "Blitzcrank")
            {
                spell = new MoveBuffData(
                    "Blitzcrank W",
                    SpellSlot.W,
                    100,
                    3,
                    () =>
                    GameObjects.Player.MoveSpeed
                    * (1 + 0.12f + 0.04f * GameObjects.Player.Spellbook.GetSpell(SpellSlot.W).Level));
                Spells.Add(spell);
            }

            #endregion

            #region Draven

            if (GameObjects.Player.CharacterName == "Draven")
            {
                spell = new MoveBuffData(
                    "Draven W",
                    SpellSlot.W,
                    100,
                    3,
                    () =>
                    GameObjects.Player.MoveSpeed
                    * (1 + 0.35f + 0.05f * GameObjects.Player.Spellbook.GetSpell(SpellSlot.W).Level));
                Spells.Add(spell);
            }

            #endregion

            #region Garen

            if (GameObjects.Player.CharacterName == "Garen")
            {
                spell = new MoveBuffData("Garen Q", SpellSlot.Q, 100, 3, () => GameObjects.Player.MoveSpeed * (1.35f));
                Spells.Add(spell);
            }

            #endregion

            #region Katarina

            if (GameObjects.Player.CharacterName == "Katarina")
            {
                spell = new MoveBuffData(
                    "Katarina W",
                    SpellSlot.W,
                    100,
                    3,
                    () =>
                    GameObjects.Get<AIHeroClient>().Any(h => h.IsValidTarget(375))
                        ? GameObjects.Player.MoveSpeed
                          * (1 + 0.10f + 0.05f * GameObjects.Player.Spellbook.GetSpell(SpellSlot.W).Level)
                        : 0);
                Spells.Add(spell);
            }

            #endregion

            #region Karma 

            if (GameObjects.Player.CharacterName == "Karma")
            {
                spell = new MoveBuffData(
                    "Karma E",
                    SpellSlot.E,
                    100,
                    3,
                    () =>
                    GameObjects.Player.MoveSpeed
                    * (1 + 0.35f + 0.05f * GameObjects.Player.Spellbook.GetSpell(SpellSlot.E).Level));
                Spells.Add(spell);
            }

            #endregion

            #region Kennen

            if (GameObjects.Player.CharacterName == "Kennen")
            {
                spell = new MoveBuffData("Kennen E", SpellSlot.E, 100, 3, () => 200 + GameObjects.Player.MoveSpeed);
                Spells.Add(spell);
            }

            #endregion

            #region Khazix

            if (GameObjects.Player.CharacterName == "Khazix")
            {
                spell = new MoveBuffData("Khazix R", SpellSlot.R, 100, 5, () => GameObjects.Player.MoveSpeed * 1.4f);
                Spells.Add(spell);
            }

            #endregion

            #region Lulu

            if (GameObjects.Player.CharacterName == "Lulu")
            {
                spell = new MoveBuffData(
                    "Lulu W",
                    SpellSlot.W,
                    100,
                    5,
                    () => GameObjects.Player.MoveSpeed * (1.3f + GameObjects.Player.FlatMagicDamageMod / 100 * 0.1f));
                Spells.Add(spell);
            }

            #endregion

            #region Nunu

            if (GameObjects.Player.CharacterName == "Nunu")
            {
                spell = new MoveBuffData(
                    "Nunu W",
                    SpellSlot.W,
                    100,
                    3,
                    () =>
                    GameObjects.Player.MoveSpeed
                    * (1 + 0.1f + 0.01f * GameObjects.Player.Spellbook.GetSpell(SpellSlot.W).Level));
                Spells.Add(spell);
            }

            #endregion

            #region Ryze

            if (GameObjects.Player.CharacterName == "Ryze")
            {
                spell = new MoveBuffData("Ryze R", SpellSlot.R, 100, 5, () => 80 + GameObjects.Player.MoveSpeed);
                Spells.Add(spell);
            }

            #endregion

            #region Shyvana

            if (GameObjects.Player.CharacterName == "Sivir")
            {
                spell = new MoveBuffData("Sivir R", SpellSlot.R, 100, 5, () => GameObjects.Player.MoveSpeed * (1.6f));
                Spells.Add(spell);
            }

            #endregion

            #region Shyvana

            if (GameObjects.Player.CharacterName == "Shyvana")
            {
                spell = new MoveBuffData(
                    "Shyvana W",
                    SpellSlot.W,
                    100,
                    4,
                    () =>
                    GameObjects.Player.MoveSpeed
                    * (1 + 0.25f + 0.05f * GameObjects.Player.Spellbook.GetSpell(SpellSlot.W).Level))
                { CheckSpellName = "ShyvanaImmolationAura" };
                Spells.Add(spell);
            }

            #endregion

            #region Sona

            if (GameObjects.Player.CharacterName == "Sona")
            {
                spell = new MoveBuffData(
                    "Sona E",
                    SpellSlot.E,
                    100,
                    3,
                    () =>
                    GameObjects.Player.MoveSpeed
                    * (1 + 0.12f + 0.01f * GameObjects.Player.Spellbook.GetSpell(SpellSlot.E).Level
                       + GameObjects.Player.FlatMagicDamageMod / 100 * 0.075f
                       + 0.02f * GameObjects.Player.Spellbook.GetSpell(SpellSlot.R).Level));
                Spells.Add(spell);
            }

            #endregion

            #region Teemo

            if (GameObjects.Player.CharacterName == "Teemo")
            {
                spell = new MoveBuffData(
                    "Teemo W",
                    SpellSlot.W,
                    100,
                    3,
                    () =>
                    GameObjects.Player.MoveSpeed
                    * (1 + 0.06f + 0.04f * GameObjects.Player.Spellbook.GetSpell(SpellSlot.W).Level));
                Spells.Add(spell);
            }

            #endregion

            #region Udyr

            if (GameObjects.Player.CharacterName == "Udyr")
            {
                spell = new MoveBuffData(
                    "Udyr E",
                    SpellSlot.E,
                    100,
                    3,
                    () =>
                    GameObjects.Player.MoveSpeed
                    * (1 + 0.1f + 0.05f * GameObjects.Player.Spellbook.GetSpell(SpellSlot.E).Level));
                Spells.Add(spell);
            }

            #endregion

            #region Zilean

            if (GameObjects.Player.CharacterName == "Zilean")
            {
                spell = new MoveBuffData("Zilean E", SpellSlot.E, 100, 3, () => GameObjects.Player.MoveSpeed * 1.55f);
                Spells.Add(spell);
            }

            #endregion

            #endregion

            #region Champion Dashes

            #region Aatrox

            if (GameObjects.Player.CharacterName == "Aatrox")
            {
                spell = new DashData("Aatrox E", SpellSlot.E, 300f, false, 0, 1500, 3) { Invert = true };
                Spells.Add(spell);
            }

            #endregion

            #region Akali

            if (GameObjects.Player.CharacterName == "Akali")
            {
                spell = new DashData("Akali R", SpellSlot.R, 800, false, 100, 2461, 5)
                {
                    ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions }
                };
                Spells.Add(spell);
            }

            #endregion

            #region Alistar

            if (GameObjects.Player.CharacterName == "Alistar")
            {
                spell = new DashData("Alistar W", SpellSlot.W, 650, false, 100, 1900, 3)
                {
                    ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions }
                };
                Spells.Add(spell);
            }

            #endregion

            #region Caitlyn

            if (GameObjects.Player.CharacterName == "Caitlyn")
            {
                spell = new DashData("Caitlyn E", SpellSlot.E, 490, true, 250, 1000, 3) { Invert = true };
                Spells.Add(spell);
            }

            #endregion

            #region Corki

            if (GameObjects.Player.CharacterName == "Corki")
            {
                spell = new DashData("Corki W", SpellSlot.W, 790, false, 250, 1044, 3);
                Spells.Add(spell);
            }

            #endregion

            #region Fizz

            if (GameObjects.Player.CharacterName == "Fizz")
            {
                spell = new DashData("Fizz Q", SpellSlot.Q, 550, true, 100, 1400, 4)
                {
                    ValidTargets = new[] { SpellValidTargets.EnemyMinions, SpellValidTargets.EnemyChampions }
                };
                Spells.Add(spell);
            }

            #endregion

            #region Gragas

            if (GameObjects.Player.CharacterName == "Gragas")
            {
                spell = new DashData("Gragas E", SpellSlot.E, 600, true, 250, 911, 3);
                Spells.Add(spell);
            }

            #endregion

            #region Gnar

            if (GameObjects.Player.CharacterName == "Gnar")
            {
                spell = new DashData("Gnar E", SpellSlot.E, 50, false, 0, 900, 3) { CheckSpellName = "GnarE" };
                Spells.Add(spell);
            }

            #endregion

            #region Graves

            if (GameObjects.Player.CharacterName == "Graves")
            {
                spell = new DashData("Graves E", SpellSlot.E, 425, true, 100, 1223, 3);
                Spells.Add(spell);
            }

            #endregion

            #region Irelia

            if (GameObjects.Player.CharacterName == "Irelia")
            {
                spell = new DashData("Irelia Q", SpellSlot.Q, 600, false, 0, 1400 + (int)GameObjects.Player.MoveSpeed, 3)
                {
                    ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions },
                    UnderTower = true
                };
                Spells.Add(spell);
            }

            #endregion

            #region Kindred

            if (GameObjects.Player.CharacterName == "Kindred")
            {
                spell = new DashData("Kindred Q", SpellSlot.Q, 340f, false, 0, 1400, 3);
                Spells.Add(spell);
            }

            #endregion

            #region Jax

            if (GameObjects.Player.CharacterName == "Jax")
            {
                spell = new DashData("Jax Q", SpellSlot.Q, 700, false, 100, 1400, 3)
                {
                    ValidTargets =
                                    new[]
                                        {
                                            SpellValidTargets.EnemyWards, SpellValidTargets.AllyWards,
                                            SpellValidTargets.AllyMinions, SpellValidTargets.AllyChampions,
                                            SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions
                                        }
                };
                Spells.Add(spell);
            }

            #endregion

            #region Leblanc

            if (GameObjects.Player.CharacterName == "Leblanc")
            {
                spell = new DashData("LeBlanc W1", SpellSlot.W, 600, false, 100, 1621, 3)
                { CheckSpellName = "LeblancSlide" };
                Spells.Add(spell);
            }

            if (GameObjects.Player.CharacterName == "Leblanc")
            {
                spell = new DashData("LeBlanc RW", SpellSlot.R, 600, false, 100, 1621, 3)
                { CheckSpellName = "LeblancSlideM" };
                Spells.Add(spell);
            }

            #endregion

            #region LeeSin

            if (GameObjects.Player.CharacterName == "LeeSin")
            {
                spell = new DashData("LeeSin W", SpellSlot.W, 700, false, 250, 2000, 3)
                {
                    ValidTargets =
                                    new[]
                                        {
                                            SpellValidTargets.AllyChampions, SpellValidTargets.AllyMinions,
                                            SpellValidTargets.AllyWards
                                        },
                    CheckSpellName = "BlindMonkWOne"
                };
                Spells.Add(spell);
            }

            #endregion

            #region Lucian

            if (GameObjects.Player.CharacterName == "Lucian")
            {
                spell = new DashData("Lucian E", SpellSlot.E, 425, false, 100, 1350, 2);
                Spells.Add(spell);
            }

            #endregion

            #region Nidalee

            if (GameObjects.Player.CharacterName == "Nidalee")
            {
                spell = new DashData("Nidalee W", SpellSlot.W, 375, true, 250, 943, 3) { CheckSpellName = "Pounce" };
                Spells.Add(spell);
            }

            #endregion

            #region Pantheon

            if (GameObjects.Player.CharacterName == "Pantheon")
            {
                spell = new DashData("Pantheon W", SpellSlot.W, 600, false, 100, 1000, 3)
                {
                    ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions }
                };
                Spells.Add(spell);
            }

            #endregion

            #region Riven

            if (GameObjects.Player.CharacterName == "Riven")
            {
                spell = new DashData("Riven Q", SpellSlot.Q, 222, true, 250, 560, 3) { RequiresPreMove = true };
                Spells.Add(spell);
                spell = new DashData("Riven E", SpellSlot.E, 250, false, 250, 1200, 3);
                Spells.Add(spell);
            }

            #endregion

            #region Tristana

            if (GameObjects.Player.CharacterName == "Tristana")
            {
                spell = new DashData("Tristana W", SpellSlot.W, 900, true, 300, 800, 5);
                Spells.Add(spell);
            }

            #endregion

            #region Tryndamare

            if (GameObjects.Player.CharacterName == "Tryndamere")
            {
                spell = new DashData("Tryndamere E", SpellSlot.E, 650, true, 250, 900, 3);
                Spells.Add(spell);
            }

            #endregion

            #region Vayne

            if (GameObjects.Player.CharacterName == "Vayne")
            {
                spell = new DashData("Vayne Q", SpellSlot.Q, 300, true, 0, 830, 2);
                Spells.Add(spell);
            }

            #endregion

            #region Wukong

            if (GameObjects.Player.CharacterName == "MonkeyKing")
            {
                spell = new DashData("Wukong E", SpellSlot.E, 650, false, 100, 1400, 3)
                {
                    ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions }
                };
                Spells.Add(spell);
            }

            #endregion

            #region Yasuo

            if (GameObjects.Player.CharacterName == "Yasuo")
            {
                spell = new DashData("Yasuo E", SpellSlot.E, 475, true, Game.Ping, (int)(750 + (GameObjects.Player.MoveSpeed * 0.6f)), 2)
                {
                    ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions },
                    UnderTower = true
                };
                Spells.Add(spell);
            }

            #endregion

            #region Gwen

            if (GameObjects.Player.CharacterName == "Gwen")
            {
                spell = new DashData("Gwen E", SpellSlot.E, 400f, true, 50, 700, 2);
                Spells.Add(spell);
            }

            #endregion

            #region Samira

            if (GameObjects.Player.CharacterName == "Samira")
            {
                spell = new DashData("Samira E", SpellSlot.E, 600f, true, 50, 500, 4)
                {
                    ValidTargets = new[] { SpellValidTargets.EnemyChampions },
                    UnderTower = true
                };
                Spells.Add(spell);
            }

            #endregion

            #region Kaisa

            if (GameObjects.Player.CharacterName == "Kaisa")
            {
                spell = new DashData("Kaisa R", SpellSlot.R, 1500f, true, 0, 4000, 5) { UnderTower = false };
                Spells.Add(spell);
            }

            #endregion

            #region Fiora

            if (GameObjects.Player.CharacterName == "Fiora")
            {
                spell = new DashData("Fiora Q", SpellSlot.Q, 400, true, 250, 500, 2);
                Spells.Add(spell);
            }

            #endregion

            #region Fiora

            if (GameObjects.Player.CharacterName == "Zeri")
            {
                spell = new DashData("Zeri E", SpellSlot.E, 300, false, 0, 600 + (int)GameObjects.Player.MoveSpeed,3);
                Spells.Add(spell);
            }

            #endregion

            #endregion

            #region Champion Blinks

            #region Ezreal

            if (GameObjects.Player.CharacterName == "Ezreal")
            {
                spell = new BlinkData("Ezreal E", SpellSlot.E, 475, 250, 3);
                Spells.Add(spell);
            }

            #endregion

            #region Kassadin

            if (GameObjects.Player.CharacterName == "Kassadin")
            {
                spell = new BlinkData("Kassadin R", SpellSlot.R, 700, 200, 5);
                Spells.Add(spell);
            }

            #endregion

            #region Katarina

            if (GameObjects.Player.CharacterName == "Katarina")
            {
                spell = new BlinkData("Katarina E", SpellSlot.E, 700, 200, 3)
                {
                    ValidTargets =
                                    new[]
                                        {
                                            SpellValidTargets.AllyChampions, SpellValidTargets.AllyMinions,
                                            SpellValidTargets.AllyWards, SpellValidTargets.EnemyChampions,
                                            SpellValidTargets.EnemyMinions, SpellValidTargets.EnemyWards
                                        }
                };
                Spells.Add(spell);
            }

            #endregion

            #region Shaco

            if (GameObjects.Player.CharacterName == "Shaco")
            {
                spell = new BlinkData("Shaco Q", SpellSlot.Q, 400, 350, 3);
                Spells.Add(spell);
            }

            #endregion

            #region Talon

            if (GameObjects.Player.CharacterName == "Talon")
            {
                spell = new BlinkData("Talon E", SpellSlot.E, 700, 100, 3)
                {
                    ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions }
                };
                Spells.Add(spell);
            }

            #endregion

            #region Zed

            if (GameObjects.Player.CharacterName == "Zed")
            {
                spell = new BlinkData("Zed R2", SpellSlot.R, 20000, 100, 4)
                { ExtraDelay = true, CheckSpellName = "zedr2", SelfCast = true };
                Spells.Add(spell);
            }

            #endregion

            #endregion

            #region Champion Invulnerabilities

            #region Elise

            if (GameObjects.Player.CharacterName == "Elise")
            {
                spell = new InvulnerabilityData("Elise E", SpellSlot.E, 250, 3)
                { CheckSpellName = "EliseSpiderEInitial", SelfCast = true };
                Spells.Add(spell);
            }

            #endregion

            #region Vladimir

            if (GameObjects.Player.CharacterName == "Vladimir")
            {
                spell = new InvulnerabilityData("Vladimir W", SpellSlot.W, 250, 3) { SelfCast = true };
                Spells.Add(spell);
            }

            #endregion

            #region Fizz

            if (GameObjects.Player.CharacterName == "Fizz")
            {
                spell = new InvulnerabilityData("Fizz E", SpellSlot.E, 250, 3);
                Spells.Add(spell);
            }

            #endregion

            #region MasterYi

            if (GameObjects.Player.CharacterName == "MasterYi")
            {
                spell = new InvulnerabilityData("MasterYi Q", SpellSlot.Q, 250, 3)
                {
                    MaxRange = 600,
                    ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions }
                };
                Spells.Add(spell);
            }

            #endregion

            #region Yasuo

            if (GameObjects.Player.CharacterName == "Yasuo")
            {
                spell = new InvulnerabilityData("Yasuo W", SpellSlot.W, 100, 3);
                Spells.Add(spell);
            }

            #endregion

            #region Irelia

            if (GameObjects.Player.CharacterName == "Irelia")
            {
                spell = new InvulnerabilityData("Irelia W", SpellSlot.W, 20, 3) { SelfCast = true };
                Spells.Add(spell);
            }

            #endregion

            #region Samira

            if (GameObjects.Player.CharacterName == "Samira")
            {
                spell = new InvulnerabilityData("Samira W", SpellSlot.W, 100, 5);
                Spells.Add(spell);
            }

            #endregion



            #region Fiora

            if (GameObjects.Player.CharacterName == "Fiora")
            {
                spell = new InvulnerabilityData("Fiora W", SpellSlot.W, 500, 3)
                { MaxRange = 800, Speed = 3200, ValidTargets = new[] { SpellValidTargets.EnemyChampions } };
                Spells.Add(spell);
            }

            #endregion

            #endregion

            #region Champion Shields

            #region Karma

            if (GameObjects.Player.CharacterName == "Karma")
            {
                spell = new ShieldData("Karma E", SpellSlot.E, 100, 2) { CanShieldAllies = true, MaxRange = 800 };
                Spells.Add(spell);
            }

            #endregion

            #region Janna

            if (GameObjects.Player.CharacterName == "Janna")
            {
                spell = new ShieldData("Janna E", SpellSlot.E, 100, 1) { CanShieldAllies = true, MaxRange = 800 };
                Spells.Add(spell);
            }

            #endregion

            #region Morgana

            if (GameObjects.Player.CharacterName == "Morgana")
            {
                spell = new ShieldData("Morgana E", SpellSlot.E, 100, 3) { CanShieldAllies = true, MaxRange = 750 };
                Spells.Add(spell);
            }

            #endregion

            #endregion

            #region Item Dashes

            #endregion

        }

        #endregion
    }
}
