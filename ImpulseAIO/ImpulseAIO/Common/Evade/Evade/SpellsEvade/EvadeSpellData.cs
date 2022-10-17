﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
namespace QSharp.Common.Evade.SpellsEvade
{
    public enum SpellValidTargets
    {
        AllyMinions,

        EnemyMinions,

        AllyWards,

        EnemyWards,

        AllyChampions,

        EnemyChampions
    }

    public class EvadeSpellData
    {
        #region Fields

        public bool CanShieldAllies;

        public string CheckSpellName = "";

        public int Delay;

        public bool ExtraDelay;

        public bool FixedRange;

        public bool Invert;

        public bool IsBlink;

        public bool IsDash;

        public bool IsInvulnerability;

        public bool IsMovementSpeedBuff;

        public bool IsShield;

        public bool IsSpellShield;

        public float MaxRange;

        public MoveSpeedAmount MoveSpeedTotalAmount;

        public string Name;

        public bool RequiresPreMove;

        public bool SelfCast;

        public SpellSlot Slot;

        public int Speed;

        public bool UnderTower;

        public SpellValidTargets[] ValidTargets;

        private int dangerLevel;

        #endregion

        #region Delegates

        public delegate float MoveSpeedAmount();

        #endregion

        #region Public Properties

        public int DangerLevel
        {
            get
            {
                if (EvadeInit.EvadeMenu["Evade"]["Spells"][this.Name]["DangerLevel"] != null)
                {
                    return EvadeInit.EvadeMenu["Evade"]["Spells"][this.Name]["DangerLevel"].GetValue<MenuSlider>().Value;
                }
                return this.dangerLevel;
            }
            set
            {
                this.dangerLevel = value;
            }
        }

        public bool Enable => EvadeInit.EvadeMenu["Evade"]["Spells"][this.Name]["Enabled"].GetValue<MenuBool>().Enabled;

        public bool IsReady
            => 
                (this.CheckSpellName == ""
                 || GameObjects.Player.Spellbook.GetSpell(this.Slot).Name == this.CheckSpellName)
                && GameObjects.Player.Spellbook.CanUseSpell(this.Slot) == SpellState.Ready;

        public bool IsTargetted => this.ValidTargets != null;

        #endregion
    }

    internal class DashData : EvadeSpellData
    {
        #region Constructors and Destructors

        public DashData(
            string name,
            SpellSlot slot,
            float range,
            bool fixedRange,
            int delay,
            int speed,
            int dangerLevel)
        {
            this.Name = name;
            this.MaxRange = range;
            this.Slot = slot;
            this.FixedRange = fixedRange;
            this.Delay = delay;
            this.Speed = speed;
            this.DangerLevel = dangerLevel;
            this.IsDash = true;
        }

        #endregion
    }

    internal class BlinkData : EvadeSpellData
    {
        #region Constructors and Destructors

        public BlinkData(string name, SpellSlot slot, float range, int delay, int dangerLevel)
        {
            this.Name = name;
            this.MaxRange = range;
            this.Slot = slot;
            this.Delay = delay;
            this.DangerLevel = dangerLevel;
            this.IsBlink = true;
        }

        #endregion
    }

    internal class InvulnerabilityData : EvadeSpellData
    {
        #region Constructors and Destructors

        public InvulnerabilityData(string name, SpellSlot slot, int delay, int dangerLevel)
        {
            this.Name = name;
            this.Slot = slot;
            this.Delay = delay;
            this.DangerLevel = dangerLevel;
            this.IsInvulnerability = true;
        }

        #endregion
    }

    internal class ShieldData : EvadeSpellData
    {
        #region Constructors and Destructors

        public ShieldData(string name, SpellSlot slot, int delay, int dangerLevel, bool isSpellShield = false)
        {
            this.Name = name;
            this.Slot = slot;
            this.Delay = delay;
            this.DangerLevel = dangerLevel;
            this.IsSpellShield = isSpellShield;
            this.IsShield = !this.IsSpellShield;
        }

        #endregion
    }

    internal class MoveBuffData : EvadeSpellData
    {
        #region Constructors and Destructors

        public MoveBuffData(string name, SpellSlot slot, int delay, int dangerLevel, MoveSpeedAmount amount)
        {
            this.Name = name;
            this.Slot = slot;
            this.Delay = delay;
            this.DangerLevel = dangerLevel;
            this.MoveSpeedTotalAmount = amount;
            this.IsMovementSpeedBuff = true;
        }

        #endregion
    }
}
