using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.Rendering;
using SharpDX;
namespace ImpulseAIO.Common.Overwirte
{
	internal class NewOrbwalker : IOrbwalker
    {
        public AttackableUnit ForceTarget { get ; set ; }
        public AttackableUnit LastTarget { get ; set ; }
        public int LastAutoAttackTick { get ; set ; }
        public int LastMovementTick { get ; set ; }
        public bool AttackEnabled { get ; set ; }
        public bool MoveEnabled { get ; set ; }
		private bool ForceChase
		{
			get
			{
				if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && MiscMenu["FindKey"].GetValue<MenuKeyBind>().Active)
				{
					return true;
				}
				return false;
			}
		}
		private int GetFindRange
        {
            get
            {
				return ForceChase ? MiscMenu["Range"].GetValue<MenuSlider>().Value : 0;
			}
        }
		private bool FastLne
        {
            get
            {
				if(Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && Menu["FastLaneClear"].GetValue<MenuKeyBind>().Active)
                {
					return true;
                }
				return false;
            }
        }

		public OrbwalkerMode ActiveMode
		{
			get
			{
				if (!this._initialize)
				{
					return OrbwalkerMode.None;
				}
				if (this._activeMode != OrbwalkerMode.None)
				{
					return this._activeMode;
				}
				if (this.Menu["Combo"].GetValue<MenuKeyBind>().Active || this.Menu["ComboWithMove"].GetValue<MenuKeyBind>().Active)
				{
					return OrbwalkerMode.Combo;
				}
				if (this.Menu["Harass"].GetValue<MenuKeyBind>().Active)
				{
					return OrbwalkerMode.Harass;
				}
				if (this.Menu["LaneClear"].GetValue<MenuKeyBind>().Active)
				{
					return OrbwalkerMode.LaneClear;
				}
				if (this.Menu["LastHit"].GetValue<MenuKeyBind>().Active)
				{
					return OrbwalkerMode.LastHit;
				}
				if (!this.Menu["Flee"].GetValue<MenuKeyBind>().Active)
				{
					return OrbwalkerMode.None;
				}
				return OrbwalkerMode.Flee;
			}
			set
			{
				this._activeMode = value;
			}
		}

		public NewOrbwalker()
        {
			_initialize = true;
			AttackEnabled = true;
			MoveEnabled = true;
			Menu = new Menu("Orbwalker", Program.ScriptName + (Program.Chinese ? ":独立走砍" : ":Orbwalker"), true).Attach();
			Menu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.Orbwalker));
			AttackMenu = Menu.Add(new Menu("Attackable", "Attackable Unit", false));
			AttackMenu.Add(new MenuBool("Barrels", "Barrels", true));
			AttackMenu.Add(new MenuBool("JunglePlant", "Jungle Plant", false));
			AttackMenu.Add(new MenuBool("SpecialMinions", "Pets", true));
			AttackMenu.Add(new MenuBool("Wards", "Wards", true));
			PrioritizeMenu = Menu.Add(new Menu("Prioritize", "Prioritize", false));
			PrioritizeMenu.Add(new MenuBool("FarmOverHarass", "Farm Over Harass", true));
			PrioritizeMenu.Add(new MenuBool("SpecialMinion", "Special Minion", false));
			PrioritizeMenu.Add(new MenuBool("SmallJungle", "Small Jungle", false));
			PrioritizeMenu.Add(new MenuBool("Turret", "Turret", true));
			OrbwalkerMenu = Menu.Add(new Menu("Orbwalker", "Orbwalker", false));
			OrbwalkerMenu.Add(new MenuSlider("ExtraHold", "Extra Hold Position", 50, 0, 250));
			OrbwalkerMenu.Add(new MenuBool("MoveRandom", "Randomize Movement when too close", false));
			OrbwalkerMenu.Add(new MenuSlider("WindupDelay", "Extra Windup Delay", 60, 0, 250));
			OrbwalkerMenu.Add(new MenuBool("LimitAttack", "Don't Kite if Attack Speed > 2.5", false));
			OrbwalkerMenu.Add(new MenuBool("HighOrb", "以更高的频率进行走砍", false));
			OrbwalkerMenu.Add(new MenuBool("CalculateRunaway", Program.Chinese ? "计算极限距离时的逃逸速度": "Calculate Run away time in Orb limit distance", true));
			FarmMenu = Menu.Add(new Menu("Farm", "Farm", false));
			FarmMenu.Add(new MenuSlider("FarmDelay", "Farm Delay", 30, 0, 200));
			FarmMenu.Add(new MenuSlider("FastFarmDelay", Program.Chinese ? "快速清线延迟" : "Fast Farm Delay", 220, 0, 1000));
			FarmMenu.Add(new MenuList("TurretFarm", "Turret Farm Logic", new string[]
			{
				"Enabled",
				"Off"
			}, 0));
			FarmMenu.Add(new MenuSlider("TurretFramMaxLevel", "Disable Turret Farm When Player Level >= x", 13, 1, 18));
			AdvancedMenu = Menu.Add(new Menu("Advanced", "Advanced", false));
			AdvancedMenu.Add(new MenuBool("CalcItemDamage", "Calculate Item Damage", false));
			AdvancedMenu.Add(new MenuBool("YasuoWallCheck", "Check Yasuo WindWall", true));
			AdvancedMenu.Add(new MenuBool("MissileCheck", "Use Missile Checks", true));
			AdvancedMenu.Add(new MenuBool("SupportMode_" + GameObjects.Player.CharacterName, "Support Mode", false));
			DrawMenu = Menu.Add(new Menu("Drawing", "Drawing", false));
			DrawMenu.Add(new MenuBool("DrawAttackRange", "Draw Attack Range", true));
			DrawMenu.Add(new MenuBool("DrawChaseRange", !Program.Chinese ? "Draw Force Chase Range" : "画出强制追击范围", true));
			DrawMenu.Add(new MenuBool("DrawHoldPosition", "Draw Hold Position", false));
			DrawMenu.Add(new MenuBool("DrawKillableMinion", "Draw Killable Minion", false));
			DrawMenu.Add(new MenuBool("ShowFakeClick", "Show FakeClick", false));
			MiscMenu = Menu.Add(new Menu("Misc", Program.Chinese ? "额外追击设置" : "Extra Range Setting"));
			MiscMenu.Add(new MenuSlider("Range", Program.Chinese ? "额外残血追击范围" : "Extra LowHP Target Find range", 200, 0, 500));
			MiscMenu.Add(new MenuKeyBind("FindKey", Program.Chinese ? "启用 强制追击模式(连招模式下)" : "Enable Force chase Mode(Combo Activating)", Keys.LButton, KeyBindType.Press));
			Menu.Add(new MenuKeyBind("Combo", "Combo", Keys.Space, KeyBindType.Press, false));
			Menu.Add(new MenuKeyBind("ComboWithMove", "Combo Without Move", Keys.N, KeyBindType.Press, false));
			Menu.Add(new MenuKeyBind("Harass", "Harass", Keys.C, KeyBindType.Press, false));
			Menu.Add(new MenuKeyBind("LaneClear", "LaneClear", Keys.V, KeyBindType.Press, false));
			Menu.Add(new MenuKeyBind("FastLaneClear", Program.Chinese ? "快速清线模式" : "Fast LaneClear", Keys.LButton, KeyBindType.Press, false));
			Menu.Add(new MenuKeyBind("LastHit", "LastHit", Keys.X, KeyBindType.Press, false));
			Menu.Add(new MenuKeyBind("Flee", "Flee", Keys.Z, KeyBindType.Press, false));
			string characterName = GameObjects.Player.CharacterName;
			if (!(characterName == "Aphelios"))
			{
				if (!(characterName == "Graves"))
				{
					if (!(characterName == "Jhin"))
					{
						if (!(characterName == "Kalista"))
						{
							if (!(characterName == "Rengar"))
							{
								if (characterName == "Sett")
								{
									isSett = true;
								}
							}
							else
							{
								isRengar = true;
							}
						}
						else
						{
							isKalista = true;
						}
					}
					else
					{
						isJhin = true;
					}
				}
				else
				{
					isGraves = true;
				}
			}
			else
			{
				isAphelios = true;
			}
			foreach (AIHeroClient aiheroClient in from x in GameObjects.Heroes
												  where x != null && x.IsValid
												  select x)
			{
				if (aiheroClient.IsEnemy)
				{
					if (aiheroClient.CharacterName == "Jax")
					{
						JaxInGame = true;
					}
					if (aiheroClient.CharacterName == "Gangplank")
					{
						GangplankInGame = true;
					}
				}
				if (!aiheroClient.IsMe && aiheroClient.CharacterName == "TahmKench")
				{
					TahmKenchInGame = true;
				}
			}
			AIBaseClient.OnDoCast += OnDoCast;
			AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
			AIBaseClient.OnPlayAnimation += OnPlayAnimation;
			Spellbook.OnStopCast += OnStopCast;
			GameObject.OnDelete += OnDelete;
			GameEvent.OnGameTick += OnUpdate;
            Render.OnDraw += OnDraw;
		}
		private float GetAttackCastDelay()
		{
			if (isSett && nextAttackIsPassive)
			{
				return GameObjects.Player.AttackCastDelay - GameObjects.Player.AttackCastDelay / 8f;
			}
			return GameObjects.Player.AttackCastDelay;
		}
		private float GetProjectileSpeed()
		{
			string characterName = GameObjects.Player.CharacterName;
			uint num = PrivateImplementationDetails.ComputeStringHash(characterName);
			if (num <= 2116636923U)
			{
				if (num <= 785412301U)
				{
					if (num != 295451893U)
					{
						if (num != 528224650U)
						{
							if (num != 785412301U)
							{
								goto IL_2FA;
							}
							if (!(characterName == "Neeko"))
							{
								goto IL_2FA;
							}
							if (!GameObjects.Player.HasBuff("neekowpassiveready"))
							{
								return this.GetBasicAttackMissileSpeed();
							}
							return float.MaxValue;
						}
						else
						{
							if (!(characterName == "Jinx"))
							{
								goto IL_2FA;
							}
							if (!GameObjects.Player.HasBuff("JinxQ"))
							{
								return this.GetBasicAttackMissileSpeed();
							}
							return 2000f;
						}
					}
					else if (!(characterName == "Zeri"))
					{
						goto IL_2FA;
					}
				}
				else if (num != 1021974584U)
				{
					if (num != 2063770139U)
					{
						if (num != 2116636923U)
						{
							goto IL_2FA;
						}
						if (!(characterName == "Kayle"))
						{
							goto IL_2FA;
						}
						if (GameObjects.Player.AttackRange >= 530f)
						{
							return 2250f;
						}
						return float.MaxValue;
					}
					else
					{
						if (!(characterName == "Jayce"))
						{
							goto IL_2FA;
						}
						if (string.Equals(GameObjects.Player.Spellbook.GetSpell(SpellSlot.Q).Name, "jayceshockblast", StringComparison.CurrentCultureIgnoreCase))
						{
							return 2000f;
						}
						return float.MaxValue;
					}
				}
				else if (!(characterName == "Velkoz"))
				{
					goto IL_2FA;
				}
			}
			else if (num <= 2978615609U)
			{
				if (num != 2361879110U)
				{
					if (num != 2834820125U)
					{
						if (num != 2978615609U)
						{
							goto IL_2FA;
						}
						if (!(characterName == "Azir"))
						{
							goto IL_2FA;
						}
					}
					else
					{
						if (!(characterName == "Ivern"))
						{
							goto IL_2FA;
						}
						if (!GameObjects.Player.HasBuff("ivernwpassive"))
						{
							return this.GetBasicAttackMissileSpeed();
						}
						return 1600f;
					}
				}
				else
				{
					if (!(characterName == "Viktor"))
					{
						goto IL_2FA;
					}
					if (!GameObjects.Player.HasBuff("ViktorPowerTransferReturn"))
					{
						return this.GetBasicAttackMissileSpeed();
					}
					return float.MaxValue;
				}
			}
			else if (num != 3554680443U)
			{
				if (num != 3798837943U)
				{
					if (num != 4223981326U)
					{
						goto IL_2FA;
					}
					if (!(characterName == "Aphelios"))
					{
						goto IL_2FA;
					}
					float num2 = GameObjects.Player.Spellbook.GetSpell(SpellSlot.Q).TooltipVars[2];
					if (num2 == 1f)
					{
						return 2500f;
					}
					if (num2 == 2f)
					{
						return float.MaxValue;
					}
					if (num2 == 3f)
					{
						return 1500f;
					}
					if (num2 == 4f)
					{
						return 4000f;
					}
					return 1500f;
				}
				else if (!(characterName == "Thresh"))
				{
					goto IL_2FA;
				}
			}
			else
			{
				if (!(characterName == "Poppy"))
				{
					goto IL_2FA;
				}
				if (!GameObjects.Player.HasBuff("poppypassivebuff"))
				{
					return this.GetBasicAttackMissileSpeed();
				}
				return 1600f;
			}
			return float.MaxValue;
		IL_2FA:
			return this.GetBasicAttackMissileSpeed();
		}
		private float GetBasicAttackMissileSpeed()
		{
			if (!GameObjects.Player.IsMelee)
			{
				return GameObjects.Player.BasicAttack.MissileSpeed;
			}
			return float.MaxValue;
		}
		private bool CanAttackWithWindWall(AttackableUnit target)
		{
			if (!this._initialize)
			{
				return false;
			}
			if (target == null || !target.IsValidTarget(3.4028235E+38f, true, default(Vector3)))
			{
				return false;
			}
			if (this.JaxInGame)
			{
				AIHeroClient aiheroClient = target as AIHeroClient;
				if (aiheroClient != null && aiheroClient.IsValid && !aiheroClient.IsDead && aiheroClient.CharacterName == "Jax" && aiheroClient.HasBuff("JaxCounterStrike"))
				{
					return false;
				}
			}
			if (!this.AdvancedMenu["YasuoWallCheck"].GetValue<MenuBool>().Enabled)
			{
				return true;
			}
			if (this.WindWallBrokenChampions.Any((string x) => string.Equals(GameObjects.Player.CharacterName, x, StringComparison.CurrentCultureIgnoreCase)) && Collisions.HasYasuoWindWallCollision(GameObjects.Player.ServerPosition, target.Position))
			{
				return false;
			}
			if (this.SpecialWindWallChampions.Any((string x) => string.Equals(GameObjects.Player.CharacterName, x, StringComparison.CurrentCultureIgnoreCase)))
			{
				if (GameObjects.Player.CharacterName == "Elise")
				{
					if (string.Equals(GameObjects.Player.Spellbook.GetSpell(SpellSlot.R).Name, "eliser", StringComparison.CurrentCultureIgnoreCase) && Collisions.HasYasuoWindWallCollision(GameObjects.Player.ServerPosition, target.Position))
					{
						return false;
					}
				}
				else if (GameObjects.Player.CharacterName == "Nidalee")
				{
					if (string.Equals(GameObjects.Player.Spellbook.GetSpell(SpellSlot.Q).Name, "javelintoss", StringComparison.CurrentCultureIgnoreCase) && Collisions.HasYasuoWindWallCollision(GameObjects.Player.ServerPosition, target.Position))
					{
						return false;
					}
				}
				else if (GameObjects.Player.CharacterName == "Jayce")
				{
					if (string.Equals(GameObjects.Player.Spellbook.GetSpell(SpellSlot.Q).Name, "jayceshockblast", StringComparison.CurrentCultureIgnoreCase) && Collisions.HasYasuoWindWallCollision(GameObjects.Player.ServerPosition, target.Position))
					{
						return false;
					}
				}
				else if (GameObjects.Player.CharacterName == "Gnar")
				{
					if (string.Equals(GameObjects.Player.Spellbook.GetSpell(SpellSlot.Q).Name, "gnarq", StringComparison.CurrentCultureIgnoreCase) && Collisions.HasYasuoWindWallCollision(GameObjects.Player.ServerPosition, target.Position))
					{
						return false;
					}
				}
				else if (GameObjects.Player.CharacterName == "Azir" && GameObjects.AzirSoldiers.All((EffectEmitter x) => x != null && x.IsValid && x.Distance(target.Position) > 350f))
				{
					if (Collisions.HasYasuoWindWallCollision(GameObjects.Player.ServerPosition, target.Position))
					{
						return false;
					}
				}
				else if (GameObjects.Player.CharacterName == "Neeko" && Collisions.HasYasuoWindWallCollision(GameObjects.Player.ServerPosition, target.Position))
				{
					return false;
				}
			}
			return true;
		}
		private List<AIMinionClient> GetMinions(float range = 0f)
		{
			if (!this._initialize)
			{
				return new List<AIMinionClient>();
			}
			List<AIMinionClient> list = new List<AIMinionClient>();
			list.AddRange(from m in GameObjects.EnemyMinions
						  where m.InCurrentAutoAttackRange(range, true) && !this.ignoreMinions.Any((string b) => string.Equals(m.CharacterName, b, StringComparison.CurrentCultureIgnoreCase)) && !m.GetMinionType().HasFlag(MinionTypes.JunglePlant) && m.IsMinion()
						  select m);
			list.AddRange(from j in GameObjects.Jungle
						  where j.InCurrentAutoAttackRange(range, true) && j.IsJungle() && !j.GetMinionType().HasFlag(MinionTypes.JunglePlant)
						  select j);
			return list;
		}
		private AttackableUnit GetSpecialMinion(OrbwalkerMode mode)
		{
			if (!this._initialize)
			{
				return null;
			}
			if (this.AttackMenu == null)
			{
				return null;
			}
			List<AIMinionClient> list = new List<AIMinionClient>();
			if (this.AttackMenu["SpecialMinions"].GetValue<MenuBool>().Enabled)
			{
				list.AddRange((from s in GameObjects.EnemyMinions
							   where s.InCurrentAutoAttackRange(0f, true) && s.IsPet(true)
							   select s).ToList<AIMinionClient>());
			}
			if (this.AttackMenu["Wards"].GetValue<MenuBool>().Enabled && mode != OrbwalkerMode.Combo)
			{
				list.AddRange((from w in GameObjects.EnemyWards
							   where w.InCurrentAutoAttackRange(0f, true)
							   select w).ToList<AIMinionClient>());
			}
			if (this.AttackMenu["JunglePlant"].GetValue<MenuBool>().Enabled && mode != OrbwalkerMode.Combo)
			{
				list.AddRange((from p in GameObjects.JunglePlant
							   where p.InCurrentAutoAttackRange(0f, true)
							   select p).ToList<AIMinionClient>());
			}
			return list.FirstOrDefault<AIMinionClient>();
		}
		private bool ShouldWait(IEnumerable<AIMinionClient> minions)
		{
			if (!this._initialize)
			{
				return false;
			}
			foreach (AIMinionClient aiminionClient in minions)
			{
				int value = this.FarmMenu["FarmDelay"].GetValue<MenuSlider>().Value;
				float num = !FastLne ? GameObjects.Player.AttackDelay * 1000f * 2f : (GameObjects.Player.AttackDelay * 1000f) + FarmMenu["FastFarmDelay"].GetValue<MenuSlider>().Value;
				float prediction = HealthPrediction.GetPrediction(aiminionClient, (int)num, value, HealthPredictionType.Simulated);
				if (prediction < GameObjects.Player.GetAutoAttackDamage(aiminionClient, true, this.CalcItemDamage))
				{
					return true;
				}
			}
			return false;
		}
		private bool CanTurretFarm(List<AIMinionClient> minions)
		{
			if (!this._initialize)
			{
				return false;
			}
			Menu farmMenu = this.FarmMenu;
			if (farmMenu != null && farmMenu["TurretFarm"].GetValue<MenuList>().Index == 1)
			{
				return false;
			}
			if (this.IsSupportMode())
			{
				return false;
			}
			if (GameObjects.Player.Level >= this.FarmMenu["TurretFramMaxLevel"].GetValue<MenuSlider>().Value)
			{
				return false;
			}
			if (minions.Count == 0)
			{
				return false;
			}
			if (minions.Any((AIMinionClient x) => x.HasBuff("exaltedwithbaronnashorminion") && x.IsMinion()))
			{
				return false;
			}
			return !minions.Any((AIMinionClient x) => x.CharacterName.Contains("MinionSuper") && x.IsMinion());
		}
		private bool IsSupportMode()
		{
			if (!this._initialize)
			{
				return false;
			}
			Menu advancedMenu = this.AdvancedMenu;
			if (((advancedMenu != null) ? advancedMenu["SupportMode_" + GameObjects.Player.CharacterName] : null) == null)
			{
				return false;
			}
			if (!this.AdvancedMenu["SupportMode_" + GameObjects.Player.CharacterName].GetValue<MenuBool>().Enabled)
			{
				return false;
			}
			float realAutoAttackRange = GameObjects.Player.GetRealAutoAttackRange();
			float range = Math.Max(1200f, realAutoAttackRange * 2f);
			if (GameObjects.Player.CountAllyHeroesInRange(range, GameObjects.Player) > 0)
			{
				if (GameObjects.Player.HasBuff("talentreaperstacksone") || GameObjects.Player.HasBuff("talentreaperstackstwo") || GameObjects.Player.HasBuff("talentreaperstacksthree") || GameObjects.Player.HasBuff("talentreaperstacksfour"))
				{
					return false;
				}
			}
			else if (GameObjects.Player.CountAllyHeroesInRange(2000f, GameObjects.Player) == 0)
			{
				return false;
			}
			return true;
		}
		private void OnOrbwalkerProcessSpellCastDelayed(AIBaseClientProcessSpellCastEventArgs args)
		{
			if (this.IsAutoAttackReset(args.SData.Name))
			{
				this.ResetAutoAttackTimer();
			}
			if (this.IsAutoAttack(args.SData.Name))
			{
				Orbwalker.FireAfterAttack(args.Target as AttackableUnit, "NewOrbwalker");
				this.MissileLaunched = true;
			}
		}
		private bool CanOrbObj(AIBaseClient g)
        {
			if (!OrbwalkerMenu["CalculateRunaway"].GetValue<MenuBool>().Enabled)
			{
				return true;
			}
			if(g == null)
            {
				return true;
            }
			if (!(g.Type == GameObjectType.AIHeroClient ||
				g.Type == GameObjectType.AIMinionClient)) {
				return true;
			}
            if (g.IsMoving)
            {
				//如果目标正在移动时
				var FromDist = g.ServerPosition.DistanceToPlayer();
				var NormalRange = GameObjects.Player.AttackRange + GameObjects.Player.BoundingRadius;
				//检查普通距离
				if (FromDist <= NormalRange)
                {
					return true;
                }
				//检查极限距离
				if (FromDist > NormalRange &&
					FromDist <= NormalRange + g.BoundingRadius)
                {
					var pred = Prediction.GetPrediction(g, GetAttackCastDelay());
					if(pred == null)
                    {
						return false;
                    }
					//仍然处于范围之内
					if(pred.UnitPosition.DistanceToPlayer() <= NormalRange + g.BoundingRadius)
                    {
						return true;
                    }
					return false;
				}
				if (ForceChase)
				{
					if(FromDist > NormalRange + g.BoundingRadius + GetFindRange)
                    {
						return false;
                    }
				}
			}
			return true;
        }
		// Token: 0x0600029A RID: 666 RVA: 0x000123DC File Offset: 0x000105DC
		private void OnDoCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
		{
			if (!this._initialize)
			{
				return;
			}
			if (!sender.IsMe)
			{
				return;
			}
			string name = args.SData.Name;
			if (this.IsAutoAttackReset(name) && args.CastTime == 0f)
			{
				this.ResetAutoAttackTimer();
			}
			if (!this.IsAutoAttack(name))
			{
				return;
			}
			AttackableUnit attackableUnit = args.Target as AttackableUnit;
			if (attackableUnit != null && attackableUnit.IsValid)
			{
				this.LastAutoAttackTick = Variables.GameTimeTickCount - Game.Ping / 2;
				this.MissileLaunched = false;
				this.LastMovementTick = 0;
				this.AutoAttackCounter++;
				if (!attackableUnit.Compare(this.LastTarget))
				{
					this.LastTarget = attackableUnit;
				}
				Orbwalker.FireOnAttack(attackableUnit, "NewOrbwalker");
			}
		}

		// Token: 0x0600029B RID: 667 RVA: 0x0001249C File Offset: 0x0001069C
		private void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
		{
			if (!this._initialize)
			{
				return;
			}
			if (!sender.IsMe)
			{
				return;
			}
			if (this.isSett && args.Slot == SpellSlot.Q)
			{
				this.nextAttackIsPassive = false;
			}
			if (Game.Ping <= 30)
			{
				DelayAction.Add(30 - Game.Ping, delegate
				{
					this.OnOrbwalkerProcessSpellCastDelayed(args);
				});
				return;
			}
			this.OnOrbwalkerProcessSpellCastDelayed(args);
		}

		// Token: 0x0600029C RID: 668 RVA: 0x0001251C File Offset: 0x0001071C
		private void OnPlayAnimation(AIBaseClient sender, AIBaseClientPlayAnimationEventArgs args)
		{
			if (!this._initialize)
			{
				return;
			}
			if (!sender.IsMe)
			{
				return;
			}
			if (this.isRengar && args.Animation == "Spell5")
			{
				int num = 0;
				if (this.LastTarget != null && this.LastTarget.IsValid && this.LastTarget.Position.IsValid())
				{
					num += (int)Math.Min(GameObjects.Player.Distance(this.LastTarget) / 1.5f, 0.6f);
				}
				this.LastAutoAttackTick = Variables.GameTimeTickCount - Game.Ping / 2 + num;
			}
			if (this.isSett)
			{
				if (args.Animation.Contains("Attack"))
				{
					if (args.Animation.Contains("Passive"))
					{
						this.Info = new SettAttackInfo(true, Variables.GameTimeTickCount);
						this.nextAttackIsPassive = false;
						return;
					}
					this.Info = new SettAttackInfo(false, Variables.GameTimeTickCount);
					this.nextAttackIsPassive = true;
					return;
				}
				else
				{
					if (args.Animation == "Spell1_A")
					{
						this.Info = new SettAttackInfo(false, Variables.GameTimeTickCount);
						this.nextAttackIsPassive = true;
						return;
					}
					if (args.Animation == "Spell1_B")
					{
						this.Info = new SettAttackInfo(true, Variables.GameTimeTickCount);
						this.nextAttackIsPassive = false;
					}
				}
			}
		}

		// Token: 0x0600029D RID: 669 RVA: 0x00012674 File Offset: 0x00010874
		private void OnStopCast(Spellbook sender, SpellbookStopCastEventArgs args)
		{
			if (!this._initialize)
			{
				return;
			}
			if (((sender != null) ? sender.Owner : null) != null && sender.Owner.IsMe && args.DestroyMissile && args.KeepAnimationPlaying)
			{
				this.ResetAutoAttackTimer();
			}
		}

		// Token: 0x0600029E RID: 670 RVA: 0x000126C4 File Offset: 0x000108C4
		private void OnDelete(GameObject sender, EventArgs args)
		{
			if (!this._initialize)
			{
				return;
			}
			if (sender == null || !sender.IsValid)
			{
				return;
			}
			if (this.ForceTarget != null && sender.Compare(this.ForceTarget))
			{
				this.ForceTarget = null;
			}
			if (this.LaneClearMinion != null && sender.Compare(this.LaneClearMinion))
			{
				this.LaneClearMinion = null;
			}
			if (this.LastTarget != null && sender.Compare(this.LastTarget))
			{
				this.LastTarget = null;
			}
			if (this.isAphelios && sender.Type == GameObjectType.MissileClient)
			{
				MissileClient missileClient = sender as MissileClient;
				if (((missileClient != null) ? missileClient.SData : null) != null && missileClient.SpellCaster != null && missileClient.SpellCaster.IsMe && missileClient.Name == "ApheliosCrescendumAttackMisIn")
				{
					this.ResetAutoAttackTimer();
				}
			}
		}

		// Token: 0x0600029F RID: 671 RVA: 0x000127B0 File Offset: 0x000109B0
		private void OnUpdate(EventArgs args)
		{
			if (!this._initialize)
			{
				return;
			}
			if (this.isSett && this.nextAttackIsPassive && this.Info.AttackTime > 0 && Variables.GameTimeTickCount - this.Info.AttackTime > 2000)
			{
				this.nextAttackIsPassive = false;
			}
			this.CalcItemDamage = this.AdvancedMenu["CalcItemDamage"].GetValue<MenuBool>().Enabled;
			if (GameObjects.Player == null || !GameObjects.Player.IsValid || GameObjects.Player.IsDead)
			{
				return;
			}
			if (MenuGUI.IsChatOpen || MenuGUI.IsShopOpen)
			{
				return;
			}
			if (this.ActiveMode == OrbwalkerMode.None)
			{
				return;
			}
			AttackableUnit target = this.GetTarget();
			this.Orbwalk(target, this._orbwalkerPosition);
		}

		// Token: 0x060002A0 RID: 672 RVA: 0x00012878 File Offset: 0x00010A78
		private static int colorindex = 0;
		private void OnDraw(EventArgs args)
		{
			if (!this._initialize)
			{
				return;
			}
			if (GameObjects.Player == null || GameObjects.Player.IsDead)
			{
				return;
			}
			if (MenuGUI.IsChatOpen || MenuGUI.IsShopOpen)
			{
				return;
			}
			if (GameObjects.Player.Position.IsValid())
			{
				if (this.DrawMenu["DrawAttackRange"].GetValue<MenuBool>().Enabled && GameObjects.Player.Position.IsOnScreen(GameObjects.Player.GetRealAutoAttackRange()))
				{
					//Drawing.DrawCircle(GameObjects.Player.Position,200,1,50,true,System.Drawing.Color.Red);
					CircleRender.Draw(GameObjects.Player.Position, GameObjects.Player.GetRealAutoAttackRange(), Color.PaleVioletRed, 3, false);
				}
				
				if (this.Menu["DrawHoldPosition"].GetValue<MenuBool>().Enabled && GameObjects.Player.Position.IsOnScreen(0f))
				{
					CircleRender.Draw(GameObjects.Player.Position, GameObjects.Player.BoundingRadius + (float)this.OrbwalkerMenu["ExtraHold"].GetValue<MenuSlider>().Value, Color.Purple, 3, false);
				}
                if (FastLne)
                {
					Drawing.DrawText(Drawing.WorldToScreen(Game.CursorPos), System.Drawing.Color.White, Program.Chinese ? "     快速清线" : "Fast Farm Mode");
                }
				if (ForceChase)
				{
					Drawing.DrawText(Drawing.WorldToScreen(Game.CursorPos), System.Drawing.Color.White, Program.Chinese ? "     强制追击" : "Force Chase Mode");

                    if (DrawMenu["DrawChaseRange"].GetValue<MenuBool>().Enabled)
                    {
						colorindex++;
						if (colorindex >= 450)
							colorindex = 0;
						var colorm = ImpulseAIO.Common.Base.PlusRender.GetFullColorList(450);
						 CircleRender.Draw(GameObjects.Player.Position, GameObjects.Player.GetRealAutoAttackRange() + GetFindRange, colorm[colorindex], 2, false);
					}
				}
			}
			if (this.DrawMenu["DrawKillableMinion"].GetValue<MenuBool>().Enabled)
			{
				IEnumerable<AIMinionClient> enumerable = from x in GameObjects.EnemyMinions
														 where x.IsValidTarget(GameObjects.Player.GetRealAutoAttackRange() * 2f, true, default(Vector3)) && x.IsMinion() && x.Position.IsOnScreen(0f) && (double)x.Health < GameObjects.Player.GetAutoAttackDamage(x, true, this.CalcItemDamage)
														 select x;
				foreach (AIMinionClient aiminionClient in enumerable)
				{
					CircleRender.Draw(aiminionClient.Position, aiminionClient.BoundingRadius * 2f, new Color(0, 255, 0, 255), 3, false);
				}
			}
		}

		public bool Attack(AttackableUnit target)
        {
			if (!this._initialize)
			{
				return false;
			}
			var t = target as AIBaseClient;
			if(t != null)
            {
				if(!CanOrbObj(t))
                {
					return false;
                }
            }
			if (target == null || !target.InCurrentAutoAttackRange(0f, true))
			{
				return false;
			}
			if (!this.CanAttackWithWindWall(target))
			{
				return false;
			}
			BeforeAttackEventArgs beforeAttackEventArgs = Orbwalker.FireBeforeAttack(target, "NewOrbwalker");
			if (beforeAttackEventArgs.Process)
			{
				if (this.isKalista)
				{
					this.MissileLaunched = false;
				}
				if (GameObjects.Player.IssueOrder(GameObjectOrder.AttackUnit, target))
				{
					this.LastLocalAttackTick = Variables.GameTimeTickCount;
					this.LastTarget = target;
				}
				return true;
			}
			return false;
		}

        public bool CanAttack()
        {
			return this.CanAttack(0f);
		}

        public bool CanAttack(float extraWindup)
        {
			if (!this._initialize)
			{
				return false;
			}
			if (this.AllPauseTick > 0 && this.AllPauseTick - Variables.GameTimeTickCount > 0)
			{
				return false;
			}
			if (this.AttackPauseTick > 0 && this.AttackPauseTick - Variables.GameTimeTickCount > 0)
			{
				return false;
			}
			if (this.TahmKenchInGame && GameObjects.Player.HasBuff("tahmkenchwhasdevouredtarget"))
			{
				return false;
			}
			if (GameObjects.Player.HasBuffOfType(BuffType.Fear))
			{
				return false;
			}
			if (GameObjects.Player.HasBuffOfType(BuffType.Polymorph) || GameObjects.Player.HasBuff("Polymorph"))
			{
				return false;
			}
			if (!this.isKalista && GameObjects.Player.HasBuff("blindingdart"))
			{
				return false;
			}
			if (this.isRengar && (GameObjects.Player.HasBuff("RengarQ") || GameObjects.Player.HasBuff("RengarQEmp")))
			{
				return true;
			}
			if (this.isAphelios && GameObjects.Player.HasBuff("apheliospreload"))
			{
				return false;
			}
			if (this.isJhin && GameObjects.Player.HasBuff("JhinPassiveReload"))
			{
				return false;
			}
			float num = GameObjects.Player.AttackDelay * 1000f;
			if (this.isGraves)
			{
				if (!GameObjects.Player.HasBuff("gravesbasicattackammo1"))
				{
					return false;
				}
				num = GameObjects.Player.AttackDelay * 1000f * 1.0740297f - 716.2381f;
			}
			else if (this.isSett && this.nextAttackIsPassive)
			{
				num = GameObjects.Player.AttackDelay * 1000f / 8f;
			}
			return (float)(Variables.GameTimeTickCount + Game.Ping / 2 + 25) >= (float)this.LastAutoAttackTick + num + extraWindup;
		}

        public bool CanMove()
        {
			return CanMove(0f, false);
		}

        public bool CanMove(float extraWindup, bool disableMissileCheck)
        {
			if (!this._initialize)
			{
				return false;
			}
			if (this.AllPauseTick > 0 && this.AllPauseTick - Variables.GameTimeTickCount > 0)
			{
				return false;
			}
			if (this.MovePauseTick > 0 && this.MovePauseTick - Variables.GameTimeTickCount > 0)
			{
				return false;
			}
			if (this.TahmKenchInGame && GameObjects.Player.HasBuff("tahmkenchwhasdevouredtarget"))
			{
				return false;
			}
			if (this.isKalista)
			{
				return true;
			}
			if (this.MissileLaunched && !disableMissileCheck && this.AdvancedMenu["MissileCheck"].GetValue<MenuBool>().Enabled)
			{
				return true;
			}
			int num = 0;
			if (this.isRengar && (GameObjects.Player.HasBuff("RengarQ") || GameObjects.Player.HasBuff("RengarQEmp")))
			{
				num = 200;
			}
			return (float)(Variables.GameTimeTickCount + Game.Ping / 2) >= (float)this.LastAutoAttackTick + this.GetAttackCastDelay() * 1000f + extraWindup + (float)num;
		}

        public AttackableUnit GetTarget()
        {
			if (!this._initialize)
			{
				return null;
			}
			OrbwalkerMode activeMode = this.ActiveMode;
			if (activeMode == OrbwalkerMode.None || activeMode == OrbwalkerMode.Flee)
			{
				return null;
			}
			AttackableUnit attackableUnit = null;
			List<AIMinionClient> minions = GetMinions(200f);

			if ((activeMode == OrbwalkerMode.Harass || (activeMode == OrbwalkerMode.LaneClear && !GameObjects.Player.IsUnderEnemyTurret(0f))) && !this.PrioritizeMenu["FarmOverHarass"].GetValue<MenuBool>().Enabled)
			{
				AIHeroClient target = TargetSelector.GetTarget(from x in Base.Cache.EnemyHeroes.Where(x => x.IsValidTarget())
															   where x.InCurrentAutoAttackRange(GetFindRange, true) && CanOrbObj(x) && this.CanAttackWithWindWall(x)
															   select x, DamageType.Physical, true, null);
				if (target != null) // 
				{
					return target;
				}
			}
			
			if (this.AttackMenu["Barrels"].GetValue<MenuBool>().Enabled && this.GangplankInGame)
			{
				List<AIMinionClient> list = (from j in GameObjects.Jungle
											 where j.InCurrentAutoAttackRange(0f, true) && string.Equals(j.CharacterName, "gangplankbarrel", StringComparison.CurrentCultureIgnoreCase)
											 select j).ToList();
				foreach (AIMinionClient aiminionClient in list)
				{
					if (aiminionClient.InCurrentAutoAttackRange(0f, true) && aiminionClient.Health > 0f)
					{
						AIHeroClient aiheroClient = aiminionClient.Owner as AIHeroClient;
						if (aiheroClient != null && aiheroClient.IsValid && aiheroClient.Level > 0)
						{
							if (aiminionClient.Health <= 1f)
							{
								return aiminionClient;
							}
							if (aiminionClient.HasBuff("gangplankebarrelactive") && aiminionClient.Health <= 2f)
							{
								BuffInstance buff = aiminionClient.GetBuff("gangplankebarrelactive");
								float num = GameObjects.Player.ServerPosition.Distance(aiminionClient.ServerPosition) - GameObjects.Player.BoundingRadius;
								float num2 = Math.Max(1f, 1000f * Math.Max(0f, num / this.GetProjectileSpeed()));
								float num3 = this.GetAttackCastDelay() * 1000f + (float)Game.Ping / 2f + num2;
								double num4 = (aiheroClient.Level >= 13) ? 0.5 : ((double)((aiheroClient.Level >= 7) ? 1 : 2));
								double num5 = (double)buff.StartTime + num4 * 2.0;
								if ((double)buff.StartTime + num4 > (double)Game.Time)
								{
									num5 = (double)buff.StartTime + num4;
								}
								if (num5 < (double)(Game.Time + num3 / 1000f))
								{
									return aiminionClient;
								}
							}
						}
					}
				}
			}

			if (activeMode != OrbwalkerMode.Combo && !this.IsSupportMode())
			{
				List<AIMinionClient> list2 = (from m in minions
											  where m.InCurrentAutoAttackRange(0f, true) && !m.IsJungle()
											  orderby m.CharacterName.Contains("Siege") descending, m.CharacterName.Contains("Super")
											  select m).ThenBy(x => Math.Ceiling((double)(x.Health / GameObjects.Player.TotalAttackDamage))).ThenByDescending((AIMinionClient m) => m.MaxHealth).ToList<AIMinionClient>();
				foreach (AIMinionClient aiminionClient2 in list2)
				{
					if (aiminionClient2.MaxHealth <= 10f)
					{
						if (aiminionClient2.Health <= 1f)
						{
							return aiminionClient2;
						}
					}
					else
					{
						float projectileSpeed = this.GetProjectileSpeed();
						float num6 = this.GetAttackCastDelay() * 1000f - 100f + (float)Game.Ping / 2f + 1000f * Math.Max(0f, GameObjects.Player.Distance(aiminionClient2) - GameObjects.Player.BoundingRadius) / projectileSpeed;
						float prediction = HealthPrediction.GetPrediction(aiminionClient2, (int)num6, this.FarmMenu["FarmDelay"].GetValue<MenuSlider>().Value, HealthPredictionType.Default);
						if (prediction <= 0f)
						{
							Orbwalker.FireNonKillableMinion(aiminionClient2, "NewOrbwalker");
						}
						double autoAttackDamage = GameObjects.Player.GetAutoAttackDamage(aiminionClient2, true, this.CalcItemDamage);
						if ((double)prediction <= autoAttackDamage)
						{
							return aiminionClient2;
						}
					}
				}
			}

			if (this.ForceTarget != null && this.ForceTarget.IsValidTarget(3.4028235E+38f, true, default(Vector3)) && this.ForceTarget.InCurrentAutoAttackRange(0f, true))
			{
				return this.ForceTarget;
			}
			
			if (activeMode != OrbwalkerMode.Combo && (!minions.Any<AIMinionClient>() || this.PrioritizeMenu["Turret"].GetValue<MenuBool>().Enabled))
			{
				using (IEnumerator<AITurretClient> enumerator3 = (from t in GameObjects.EnemyTurrets
																  where t.IsValidTarget(float.MaxValue, true, default(Vector3)) && t.InCurrentAutoAttackRange(0f, true)
																  select t).GetEnumerator())
				{
					if (enumerator3.MoveNext())
					{
						return enumerator3.Current;
					}
				}
				using (IEnumerator<BarracksDampenerClient> enumerator4 = (from i in GameObjects.EnemyInhibitors
																		  where i.IsValidTarget(float.MaxValue, true, default(Vector3)) && i.InCurrentAutoAttackRange(0f, true)
																		  select i).GetEnumerator())
				{
					if (enumerator4.MoveNext())
					{
						return enumerator4.Current;
					}
				}
				if (GameObjects.EnemyNexus != null && GameObjects.EnemyNexus.IsValidTarget(3.4028235E+38f, true, default(Vector3)) && GameObjects.EnemyNexus.InCurrentAutoAttackRange(0f, true))
				{
					return GameObjects.EnemyNexus;
				}
			}
			
			if (activeMode != OrbwalkerMode.LastHit && (activeMode != OrbwalkerMode.LaneClear || !this.ShouldWait(minions)))
			{
				AIHeroClient target2 = TargetSelector.GetTarget(from x in Base.Cache.EnemyHeroes.Where(x => x.IsValidTarget())
																where x.InCurrentAutoAttackRange(GetFindRange, true) && CanOrbObj(x) && this.CanAttackWithWindWall(x)
																select x, DamageType.Physical, true, null);
				if (target2 != null)
				{
					return target2;
				}
			}

			if (this.PrioritizeMenu["SpecialMinion"].GetValue<MenuBool>().Enabled && activeMode != OrbwalkerMode.Combo && !this.ShouldWait(minions))
			{
				AttackableUnit specialMinion = this.GetSpecialMinion(activeMode);
				if (specialMinion != null && specialMinion.InCurrentAutoAttackRange(0f, true))
				{
					return specialMinion;
				}
			}

			if (activeMode == OrbwalkerMode.Harass || activeMode == OrbwalkerMode.LaneClear || activeMode == OrbwalkerMode.LastHit)
			{
				IEnumerable<AIMinionClient> source = from j in minions
													 where j.Team == GameObjectTeam.Neutral && j.InCurrentAutoAttackRange(0f, true)
													 select j;
				AttackableUnit attackableUnit2;
				if (!this.PrioritizeMenu["SmallJungle"].GetValue<MenuBool>().Enabled)
				{
					attackableUnit2 = (from m in source
									   orderby m.MaxHealth descending
									   select m).FirstOrDefault<AIMinionClient>();
				}
				else
				{
					attackableUnit2 = (from m in source
									   orderby m.MaxHealth
									   select m).FirstOrDefault<AIMinionClient>();
				}
				attackableUnit = attackableUnit2;
				if (attackableUnit != null && attackableUnit.InCurrentAutoAttackRange(0f, true))
				{
					return attackableUnit;
				}
			}

			if (activeMode != OrbwalkerMode.Combo && this.FarmMenu["TurretFarm"].GetValue<MenuList>().Index == 0)
			{
				AITurretClient closestTower = (from t in GameObjects.AllyTurrets
											   where t.IsValidTarget(1500f, false, default(Vector3))
											   select t into x
											   orderby x.DistanceToPlayer()
											   select x).FirstOrDefault<AITurretClient>();
				if (closestTower != null && closestTower.IsValidTarget(1500f, false, default(Vector3)) && this.CanTurretFarm(minions))
				{
					List<AIMinionClient> source2 = (from x in minions
													where (double)x.DistanceSquared(closestTower) < Math.Pow(900.0, 2.0)
													orderby x.Distance(closestTower)
													select x).ToList<AIMinionClient>();
					if (source2.Any<AIMinionClient>())
					{
						AIMinionClient aiminionClient3 = source2.FirstOrDefault(new Func<AIMinionClient, bool>(HealthPrediction.HasTurretAggro));
						if (aiminionClient3 != null)
						{
							float projectileSpeed2 = this.GetProjectileSpeed();
							double autoAttackDamage2 = closestTower.GetAutoAttackDamage(aiminionClient3, true, false);
							float num7 = closestTower.AttackCastDelay * 1000f + 1000f * Math.Max(0f, aiminionClient3.Distance(closestTower) - closestTower.BoundingRadius) / (closestTower.BasicAttack.MissileSpeed + 70f);
							float num8 = this.GetAttackCastDelay() * 1000f - 100f + (float)Game.Ping / 2f + 1000f * Math.Max(0f, GameObjects.Player.Distance(aiminionClient3) - GameObjects.Player.BoundingRadius) / projectileSpeed2;
							float prediction2 = HealthPrediction.GetPrediction(aiminionClient3, (int)(num7 + num8), 70, HealthPredictionType.Simulated);
							if ((double)prediction2 > autoAttackDamage2)
							{
								foreach (AIMinionClient aiminionClient4 in from x in source2
																		   where x.IsValidTarget(float.MaxValue, true, default(Vector3)) && !HealthPrediction.HasTurretAggro(x)
																		   select x)
								{
									double autoAttackDamage3 = GameObjects.Player.GetAutoAttackDamage(aiminionClient4, true, this.CalcItemDamage);
									double autoAttackDamage4 = closestTower.GetAutoAttackDamage(aiminionClient4, true, false);
									if (!HealthPrediction.HasMinionAggro(aiminionClient4))
									{
										float num9 = this.GetAttackCastDelay() * 1000f - 100f + (float)Game.Ping / 2f + 1000f * Math.Max(0f, GameObjects.Player.Distance(aiminionClient4) - GameObjects.Player.BoundingRadius) / projectileSpeed2;
										float prediction3 = HealthPrediction.GetPrediction(aiminionClient4, (int)(num9 + num7), 70, HealthPredictionType.Simulated);
										if ((double)prediction3 < autoAttackDamage4 * 2.0 || (double)prediction3 > autoAttackDamage4 * 2.0 + autoAttackDamage3)
										{
											if ((double)prediction3 > autoAttackDamage4 + autoAttackDamage3 && (double)prediction3 <= autoAttackDamage4 + autoAttackDamage3 * 2.0)
											{
												return aiminionClient4;
											}
											if ((double)prediction3 > autoAttackDamage4 * 2.0 + autoAttackDamage3 * 2.0)
											{
												return aiminionClient4;
											}
										}
									}
								}
							}
							return null;
						}
						AIMinionClient aiminionClient5 = source2.FirstOrDefault<AIMinionClient>();
						if (aiminionClient5 != null)
						{
							double autoAttackDamage5 = closestTower.GetAutoAttackDamage(aiminionClient5, true, false);
							double num10 = (double)HealthPrediction.GetPrediction(aiminionClient5, 1500, 70, HealthPredictionType.Simulated) - autoAttackDamage5 * 1.100000023841858;
							if (num10 > GameObjects.Player.GetAutoAttackDamage(aiminionClient5, true, this.CalcItemDamage) && num10 < autoAttackDamage5 * 1.100000023841858)
							{
								return aiminionClient5;
							}
							if (num10 > autoAttackDamage5 * 2.0 + GameObjects.Player.GetAutoAttackDamage(aiminionClient5, true, this.CalcItemDamage) * 2.0)
							{
								return aiminionClient5;
							}
						}
						return null;
					}
				}
			}

			if (activeMode == OrbwalkerMode.LaneClear && !this.ShouldWait(minions))
			{
				if (this.LaneClearMinion != null && this.LaneClearMinion.IsValid && this.LaneClearMinion.IsValidTarget(3.4028235E+38f, true, default(Vector3)) && this.LaneClearMinion.InCurrentAutoAttackRange(0f, true))
				{
					if (this.LaneClearMinion.MaxHealth <= 10f)
					{
						return this.LaneClearMinion;
					}
					float prediction4 = HealthPrediction.GetPrediction(this.LaneClearMinion, (int)(GameObjects.Player.AttackDelay * 2000f), this.FarmMenu["FarmDelay"].GetValue<MenuSlider>().Value, HealthPredictionType.Simulated);
					if ((double)prediction4 >= 2.0 * GameObjects.Player.GetAutoAttackDamage(this.LaneClearMinion, true, this.CalcItemDamage) || Math.Abs(prediction4 - this.LaneClearMinion.Health) < 1E-45f)
					{
						return this.LaneClearMinion;
					}
				}
				attackableUnit = (from m in minions
								  where m.InCurrentAutoAttackRange(0f, true) && !m.IsJungle()
								  select m into minion
								  let predHealth = HealthPrediction.GetPrediction(minion, (int)(GameObjects.Player.AttackDelay * 2000f), this.FarmMenu["FarmDelay"].GetValue<MenuSlider>().Value, HealthPredictionType.Simulated)
								  where (double)predHealth >= 2.0 * GameObjects.Player.GetAutoAttackDamage(minion, true, this.CalcItemDamage) || Math.Abs(predHealth - minion.Health) < float.Epsilon
								  select minion into m
								  orderby m.Health descending
								  select m).FirstOrDefault<AIMinionClient>();
				if (attackableUnit != null && attackableUnit.InCurrentAutoAttackRange(0f, true))
				{
					this.LaneClearMinion = (AIMinionClient)attackableUnit;
					return attackableUnit;
				}
			}

			if (!this.ShouldWait(minions) && activeMode != OrbwalkerMode.Combo)
			{
				AttackableUnit specialMinion2 = this.GetSpecialMinion(activeMode);
				if (specialMinion2 != null && specialMinion2.InCurrentAutoAttackRange(0f, true))
				{
					return specialMinion2;
				}
			}

			

			return attackableUnit;
		}

        public bool IsAutoAttack(string name)
        {
			return (name.IndexOf("attack", StringComparison.CurrentCultureIgnoreCase) >= 0 && !this.NoAttacks.Any((string x) => string.Equals(name, x, StringComparison.CurrentCultureIgnoreCase))) || this.Attacks.Any((string x) => string.Equals(name, x, StringComparison.CurrentCultureIgnoreCase));
			throw new NotImplementedException();
        }

        public bool IsAutoAttackReset(string name)
        {
			return this.AttackResets.Any((string x) => string.Equals(name, x, StringComparison.CurrentCultureIgnoreCase));
		}

        public void Move(Vector3 position)
        {
			if (!this._initialize)
			{
				return;
			}
			Vector3 vector = position;
			if (!vector.IsValid())
			{
				return;
			}
			float num = Math.Max(30f, (float)this.OrbwalkerMenu["ExtraHold"].GetValue<MenuSlider>().Value);
			if ((double)vector.DistanceSquared(GameObjects.Player.ServerPosition) < Math.Pow((double)num, 2.0))
			{
				if (GameObjects.Player.Path.Length != 0)
				{
					this.LastMovementTick = Variables.GameTimeTickCount - 70;
				}
				return;
			}
			if (this.OrbwalkerMenu["MoveRandom"].GetValue<MenuBool>().Enabled && (double)GameObjects.Player.Position.DistanceSquared(vector) < Math.Pow(150.0, 2.0))
			{
				vector = GameObjects.Player.ServerPosition.Extend(position, (this.random.NextFloat(0.6f, 1f) + 0.2f) * 400f);
			}
			bool highmode = OrbwalkerMenu["HighOrb"].GetValue<MenuBool>().Enabled;
            if (!highmode)
            {
				float num2 = 0f;
				List<Vector2> waypoints = GameObjects.Player.GetWaypoints();
				if (waypoints.Count > 1 && waypoints.PathLength() > 100f)
				{
					Vector3[] path = GameObjects.Player.GetPath(vector);
					if (path.Length > 1)
					{
						Vector2 vector2 = waypoints[1] - waypoints[0];
						Vector3 toVector = path[1] - path[0];
						num2 = vector2.AngleBetween(toVector);
						float num3 = path.LastOrDefault<Vector3>().DistanceSquared(waypoints.LastOrDefault<Vector2>());
						if ((num2 < 10f && (double)num3 < Math.Pow(500.0, 2.0)) || (double)num3 < Math.Pow(50.0, 2.0))
						{
							return;
						}
					}
				}
				if (Variables.GameTimeTickCount - this.LastMovementTick < 70 + Math.Min(60, Game.Ping) && num2 < 60f)
				{
					return;
				}
				if (num2 >= 60f && Variables.GameTimeTickCount - this.LastMovementTick < 60)
				{
					return;
				}
            }
            else
            {
				if (Variables.GameTimeTickCount - this.LastMovementTick < 50 + Math.Min(60, Game.Ping))
				{
					return;
				}
			}
			BeforeMoveEventArgs beforeMoveEventArgs = Orbwalker.FirePreMove(vector, "NewOrbwalker");
			if (beforeMoveEventArgs.Process)
			{
				if (this.DrawMenu["ShowFakeClick"].GetValue<MenuBool>().Enabled && (float)(Variables.GameTimeTickCount - this.LastFakeClickTick) > 250f - (float)Game.Ping * 10f)
				{
					Hud.ShowClick(ClickType.Move, beforeMoveEventArgs.MovePosition);
					this.LastFakeClickTick = Variables.GameTimeTickCount;
				}
				if (GameObjects.Player.IssueOrder(GameObjectOrder.MoveTo, beforeMoveEventArgs.MovePosition))
				{
					this.LastMovementTick = Variables.GameTimeTickCount;
				}
			}
		}

        public void Orbwalk(AttackableUnit target, Vector3 position)
        {
			if (!this._initialize)
			{
				return;
			}
			if (Variables.GameTimeTickCount - this.LastLocalAttackTick < 70 + Math.Min(60, Game.Ping))
			{
				return;
			}
			if (this.AttackEnabled && this.CanAttack() && this.Attack(target))
			{
				return;
			}
			if (this.MoveEnabled && this.CanMove((float)this.OrbwalkerMenu["WindupDelay"].GetValue<MenuSlider>().Value, false))
			{
				if (this.Menu["ComboWithMove"].GetValue<MenuKeyBind>().Active)
				{
					return;
				}
				if (this.OrbwalkerMenu["LimitAttack"].GetValue<MenuBool>().Enabled && GameObjects.Player.AttackDelay < 0.3846154f && this.AutoAttackCounter % 3 != 0 && !this.CanMove(500f, true))
				{
					return;
				}
				Vector3 position2 = position.IsValid() ? position : Game.CursorPos;
				this.Move(position2);
			}
		}

        public void ResetAutoAttackTimer()
        {
			this.AllPauseTick = 0;
			this.AttackPauseTick = 0;
			this.LastAutoAttackTick = 0;
			this.MovePauseTick = 0;
		}

        public void SetAttackPauseTime(int time)
        {
			this.AttackPauseTick = Variables.GameTimeTickCount + time;
		}

        public void SetAttackServerPauseTime()
        {
			this.AttackPauseTick = Variables.GameTimeTickCount + Game.Ping + 1;
		}

        public void SetMovePauseTime(int time)
        {
			this.MovePauseTick = Variables.GameTimeTickCount + time;
		}

        public void SetMoveServerPauseTime()
        {
			this.MovePauseTick = Variables.GameTimeTickCount + Game.Ping + 1;
		}

        public void SetOrbwalkerPosition(Vector3 position)
        {
			this._orbwalkerPosition = position;
		}

        public void SetPauseTime(int time)
        {
			this.AllPauseTick = Variables.GameTimeTickCount + time;
		}

        public void SetServerPauseTime()
        {
			this.AllPauseTick = Variables.GameTimeTickCount + Game.Ping + 1;
		}

        public void Dispose()
        {
			MenuManager.Instance.Remove(this.Menu);
			AIBaseClient.OnDoCast -= this.OnDoCast;
			AIBaseClient.OnProcessSpellCast -= this.OnProcessSpellCast;
			AIBaseClient.OnPlayAnimation -= this.OnPlayAnimation;
			Spellbook.OnStopCast -= this.OnStopCast;
			GameObject.OnDelete -= this.OnDelete;
			GameEvent.OnGameTick -= this.OnUpdate;
            Render.OnDraw -= this.OnDraw;
		}
		// Token: 0x04000112 RID: 274
		private readonly string[] AttackResets = new string[]
		{
			"asheq",
			"camilleq2",
			"camilleq",
			"dariusnoxiantacticsonh",
			"elisespiderw",
			"fiorae",
			"gravesmove",
			"garenq",
			"gangplankqwrapper",
			"illaoiw",
			"jaycehypercharge",
			"jaxempowertwo",
			"kaylee",
			"luciane",
			"leonashieldofdaybreakattack",
			"leonashieldofdaybreak",
			"mordekaisermaceofspades",
			"monkeykingdoubleattack",
			"meditate",
			"masochism",
			"netherblade",
			"nautiluspiercinggaze",
			"nasusq",
			"powerfist",
			"rengarqemp",
			"rengarq",
			"renektonpreexecute",
			"reksaiq",
			"settq",
			"sivirw",
			"shyvanadoubleattack",
			"sejuaninorthernwinds",
			"trundletrollsmash",
			"talonnoxiandiplomacy",
			"takedown",
			"vorpalspikes",
			"volibearq",
			"vie",
			"vaynetumble",
			"xinzhaoq",
			"xinzhaocombotarget",
			"yorickspectral",
			"apheliosinfernumq",
			"gravesautoattackrecoilcastedummy"
		};

		// Token: 0x04000113 RID: 275
		private readonly string[] Attacks = new string[]
		{
			"caitlynpassivemissile",
			"itemtitanichydracleave",
			"itemtiamatcleave",
			"kennenmegaproc",
			"masteryidoublestrike",
			"quinnwenhanced",
			"renektonsuperexecute",
			"renektonexecute",
			"trundleq",
			"viktorqbuff",
			"xinzhaoqthrust1",
			"xinzhaoqthrust2",
			"xinzhaoqthrust3"
		};

		// Token: 0x04000114 RID: 276
		private readonly string[] NoAttacks = new string[]
		{
			"asheqattacknoonhit",
			"annietibbersbasicattack",
			"annietibbersbasicattack2",
			"bluecardattack",
			"dravenattackp_r",
			"dravenattackp_rc",
			"dravenattackp_rq",
			"dravenattackp_l",
			"dravenattackp_lc",
			"dravenattackp_lq",
			"elisespiderlingbasicattack",
			"gravesbasicattackspread",
			"gravesautoattackrecoil",
			"goldcardattack",
			"heimertyellowbasicattack",
			"heimertyellowbasicattack2",
			"heimertbluebasicattack",
			"heimerdingerwattack2",
			"heimerdingerwattack2ult",
			"ivernminionbasicattack2",
			"ivernminionbasicattack",
			"kindredwolfbasicattack",
			"monkeykingdoubleattack",
			"malzaharvoidlingbasicattack",
			"malzaharvoidlingbasicattack2",
			"malzaharvoidlingbasicattack3",
			"redcardattack",
			"shyvanadoubleattackdragon",
			"shyvanadoubleattack",
			"talonqdashattack",
			"talonqattack",
			"volleyattackwithsound",
			"volleyattack",
			"yorickghoulmeleebasicattack",
			"yorickghoulmeleebasicattack2",
			"yorickghoulmeleebasicattack3",
			"yorickbigghoulbasicattack",
			"zyraeplantattack",
			"zoebasicattackspecial1",
			"zoebasicattackspecial2",
			"zoebasicattackspecial3",
			"zoebasicattackspecial4",
			"apheliosseverumattackmis",
			"aphelioscrescendumattackmisin",
			"aphelioscrescendumattackmisout",
			"gravesautoattackrecoilcastedummy",
			"gravesautoattackrecoil",
			"gravesbasicattackspread"
		};

		// Token: 0x04000115 RID: 277
		private readonly string[] WindWallBrokenChampions = new string[]
		{
			"annie",
			"twistedfate",
			"leblanc",
			"urgot",
			"vladimir",
			"fiddlesticks",
			"ryze",
			"sivir",
			"soraka",
			"teemo",
			"tristana",
			"missfortune",
			"ashe",
			"morgana",
			"zilean",
			"twitch",
			"karthus",
			"anivia",
			"sona",
			"janna",
			"corki",
			"karma",
			"veigar",
			"swain",
			"caitlyn",
			"orianna",
			"brand",
			"vayne",
			"cassiopeia",
			"heimerdinger",
			"ezreal",
			"kennen",
			"kogmaw",
			"lux",
			"xerath",
			"ahri",
			"graves",
			"varus",
			"viktor",
			"lulu",
			"ziggs",
			"draven",
			"quinn",
			"syndra",
			"aurelionsol",
			"zoe",
			"zyra",
			"kaisa",
			"taliyah",
			"jhin",
			"kindred",
			"jinx",
			"lucian",
			"yuumi",
			"thresh",
			"kalista",
			"xayah",
			"aphelios",
			"bard",
			"ivern",
			"nami",
			"velkoz",
			"lissandra",
			"malzahar"
		};

		// Token: 0x04000116 RID: 278
		private readonly string[] SpecialWindWallChampions = new string[]
		{
			"kayle",
			"elise",
			"nidalee",
			"jayce",
			"gnar",
			"azir",
			"neeko"
		};

		// Token: 0x04000117 RID: 279
		private readonly string[] ignoreMinions = new string[]
		{
			"jarvanivstandard"
		};
		// Token: 0x04000118 RID: 280
		private int LastLocalAttackTick;

        // Token: 0x04000119 RID: 281
        private int AutoAttackCounter;

        // Token: 0x0400011A RID: 282
        private int AttackPauseTick;

        // Token: 0x0400011B RID: 283
        private int MovePauseTick;

        // Token: 0x0400011C RID: 284
        private int AllPauseTick;

        // Token: 0x0400011D RID: 285
        private int LastFakeClickTick;

        // Token: 0x0400011E RID: 286
        private bool _initialize;

        // Token: 0x0400011F RID: 287
        private bool isAphelios;

        // Token: 0x04000120 RID: 288
        private bool isGraves;

        // Token: 0x04000121 RID: 289
        private bool isJhin;

        // Token: 0x04000122 RID: 290
        private bool isKalista;

        // Token: 0x04000123 RID: 291
        private bool isRengar;

        // Token: 0x04000124 RID: 292
        private bool isSett;

        // Token: 0x04000125 RID: 293
        private bool MissileLaunched;

        // Token: 0x04000126 RID: 294
        private bool nextAttackIsPassive;

        // Token: 0x04000127 RID: 295
        private bool JaxInGame;

        // Token: 0x04000128 RID: 296
        private bool GangplankInGame;

        // Token: 0x04000129 RID: 297
        private bool TahmKenchInGame;

        // Token: 0x0400012A RID: 298
        private bool CalcItemDamage;

        // Token: 0x0400012B RID: 299
        private Menu Menu;

        // Token: 0x0400012C RID: 300
        private Menu AttackMenu;

        // Token: 0x0400012D RID: 301
        private Menu PrioritizeMenu;

        // Token: 0x0400012E RID: 302
        private Menu OrbwalkerMenu;

        // Token: 0x0400012F RID: 303
        private Menu FarmMenu;

        // Token: 0x04000130 RID: 304
        private Menu AdvancedMenu;

        // Token: 0x04000131 RID: 305
        private Menu DrawMenu;

		private Menu MiscMenu;

		// Token: 0x04000132 RID: 306
		private Vector3 _orbwalkerPosition = Vector3.Zero;

        // Token: 0x04000133 RID: 307
        private AIBaseClient LaneClearMinion;

        // Token: 0x04000134 RID: 308
        private OrbwalkerMode _activeMode = OrbwalkerMode.None;

        // Token: 0x04000135 RID: 309
        private SettAttackInfo Info = new SettAttackInfo(true, 0);

        // Token: 0x04000136 RID: 310
        private readonly Random random = new Random(Variables.GameTimeTickCount);

        // Token: 0x0200045B RID: 1115
        internal class SettAttackInfo
        {
            // Token: 0x060014B8 RID: 5304 RVA: 0x000478C6 File Offset: 0x00045AC6
            public SettAttackInfo(bool isLeft, int time)
            {
                IsLeftPunch = isLeft;
                AttackTime = time;
            }

            // Token: 0x04000B55 RID: 2901
            public int AttackTime;

            // Token: 0x04000B56 RID: 2902
            public bool IsLeftPunch;
        }
		internal sealed class PrivateImplementationDetails
		{
			internal static uint ComputeStringHash(string s)
			{
				uint num = 0;
				if (s != null)
				{
					num = 2166136261U;
					for (int i = 0; i < s.Length; i++)
					{
						num = ((uint)s[i] ^ num) * 16777619U;
					}
				}
				return num;
			}
		}
	}
}
