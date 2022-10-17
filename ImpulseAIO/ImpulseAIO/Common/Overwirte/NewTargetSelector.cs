using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using EnsoulSharp.SDK.Utility;
using SharpDX;
namespace ImpulseAIO.Common.Overwirte
{
	internal class NewTargetSelector : ITargetSelector
    {
		private static Menu Menu;
		private static Menu PriorityMenu;
		private static Menu DrawingsMenu;
		public AIHeroClient SelectedTarget { get; set; }
        public NewTargetSelector()
        {
			Menu = new Menu("TargetSelector", Program.ScriptName + (!Program.Chinese ?":Target Selector" : ":目标选择器"), true).Attach();
			Menu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.TS));
			PriorityMenu = Menu.Add<Menu>(new Menu("Priority", "Priority", false));
			foreach (AIHeroClient aiheroClient in from x in GameObjects.EnemyHeroes
												  where x != null && x.IsValid
												  select x)
			{
				if (PriorityMenu["TS_" + aiheroClient.CharacterName] == null)
				{
					int defaultPriority = this.GetDefaultPriority(aiheroClient);
					MenuSlider component = new MenuSlider("TS_" + aiheroClient.CharacterName, aiheroClient.CharacterName, defaultPriority, 1, 5);
					PriorityMenu.Add<MenuSlider>(component);
				}
			}
			DrawingsMenu = Menu.Add<Menu>(new Menu("Drawings", "Drawings", false));
			DrawingsMenu.Add<MenuColor>(new MenuColor("SelectColor", "^ Draw Color", new ColorBGRA(byte.MaxValue, 0, 0, byte.MaxValue)));
			DrawingsMenu.Add<MenuBool>(new MenuBool("DrawSelect", "Draw Selected Target", true));
			DrawingsMenu.Add<MenuBool>(new MenuBool("LightSelect", "HighLight Selected Target", true));
			Menu.Add<MenuBool>(new MenuBool("ForceSelectTarget", "Force on Select Target", true));
			Menu.Add<MenuBool>(new MenuBool("OnlySelectTarget", "Only Attack Select Target", false));
			Menu.Add<MenuList>(new MenuList("TSMode_" + GameObjects.Player.CharacterName, Program.Chinese ? "目标选择模式" : "TS Mode", new string[]
			{
				"Smart AD/AP",
				"Lowest Health",
				"Most Priority"
			}, 0));
            Render.OnDraw += this.OnDraw;
			Game.OnWndProc += this.OnWndProc;
            Render.OnRenderMouseOvers += OnLight;
		}
		private void OnLight(EventArgs args)
        {
			if (DrawingsMenu == null)
			{
				return;
			}
			if (!DrawingsMenu["LightSelect"].GetValue<MenuBool>().Enabled)
			{
				return;
			}
			if (this.SelectedTarget == null || !this.SelectedTarget.IsValidTarget(3.4028235E+38f, true, default(Vector3)))
			{
				return;
			}
			this.SelectedTarget.Glow(System.Drawing.Color.Purple, 5, 1);
		}
		private void OnWndProc(GameWndEventArgs args)
		{
			uint msg = args.Msg;
			if ((ulong)msg == 513UL)
			{
				Vector3 clickPosition = Game.CursorPos;
				IOrderedEnumerable<AIHeroClient> source = from x in Base.Cache.EnemyHeroes
														  where x.IsValidTarget(5000f, true, default(Vector3))
														  orderby x.Distance(clickPosition)
														  select x;
				AIHeroClient aiheroClient = source.FirstOrDefault((AIHeroClient x) => x.Type == GameObjectType.AIHeroClient);
				if (aiheroClient != null && Game.CursorPos.Distance(aiheroClient.ServerPosition) <= 300f)
				{
					this.SelectedTarget = aiheroClient;
					return;
				}
				this.SelectedTarget = null;
			}
		}
		private void OnDraw(EventArgs args)
		{
			if (DrawingsMenu == null)
			{
				return;
			}
			if (!DrawingsMenu["DrawSelect"].GetValue<MenuBool>().Enabled)
			{
				return;
			}
			if (this.SelectedTarget == null || !this.SelectedTarget.IsValidTarget(3.4028235E+38f, true, default(Vector3)))
			{
				return;
			}
			var color = DrawingsMenu["SelectColor"].GetValue<MenuColor>().Color;
			CircleRender.Draw(this.SelectedTarget.Position, this.SelectedTarget.BoundingRadius, color, 10, false);
		}
		public void Dispose()
        {
            throw new NotImplementedException();
        }
        public int GetDefaultPriority(AIHeroClient target)
        {
			if (MaxPriority.Any((string x) => string.Equals(target.CharacterName, x, StringComparison.CurrentCultureIgnoreCase)))
			{
				return 5;
			}
			if (HighPriority.Any((string x) => string.Equals(target.CharacterName, x, StringComparison.CurrentCultureIgnoreCase)))
			{
				return 4;
			}
			if (MediumPriority.Any((string x) => string.Equals(target.CharacterName, x, StringComparison.CurrentCultureIgnoreCase)))
			{
				return 3;
			}
			if (!LowPriority.Any((string x) => string.Equals(target.CharacterName, x, StringComparison.CurrentCultureIgnoreCase)))
			{
				return 1;
			}
			return 2;
		}
        public int GetPriority(AIHeroClient target)
        {
			if (target == null)
			{
				return 0;
			}
			if (PriorityMenu["TS_" + target.CharacterName] == null)
			{
				return this.GetDefaultPriority(target);
			}
			return PriorityMenu["TS_" + target.CharacterName].GetValue<MenuSlider>().Value;
        }
        public AIHeroClient GetTarget(IEnumerable<AIHeroClient> possibleTargets, DamageType damageType, bool ignoreShields = true, Vector3? checkFrom = null)
        {
			List<AIHeroClient> list = (from x in possibleTargets
									   where this.IsValidTarget(x, float.MaxValue, checkFrom)
									   select x).ToList<AIHeroClient>();

			if (this.SelectedTarget != null && this.SelectedTarget.IsValid && !this.SelectedTarget.IsDead)
			{
				if (Menu["ForceSelectTarget"].GetValue<MenuBool>().Enabled && possibleTargets.Any((AIHeroClient x) => x.Compare(this.SelectedTarget)) && this.IsValidTarget(this.SelectedTarget, 3.4028235E+38f, checkFrom))
				{
					return this.SelectedTarget;
				}
				if (Menu["OnlySelectTarget"].GetValue<MenuBool>().Enabled && this.IsValidTarget(this.SelectedTarget, 3.4028235E+38f, checkFrom))
				{
					return this.SelectedTarget;
				}
			}
			switch (Menu["TSMode_" + GameObjects.Player.CharacterName].GetValue<MenuList>().Index)
			{
				case 0:
					return list.MinOrDefault(x => Base.AaIndicator(x,damageType));
				case 1:
					return list.MinOrDefault(x => x.GetRealHeath(damageType));
				case 2:
					return list.MinOrDefault(x => GetPriority(x));
				default:
					return null;
			}
		}
        public AIHeroClient GetTarget(float range, DamageType damageType, bool ignoreShields = true, Vector3? checkFrom = null, IEnumerable<AIHeroClient> ignoreChampions = null)
        {
			if (ignoreChampions == null)
			{
				ignoreChampions = new List<AIHeroClient>();
			}
			if (this.SelectedTarget != null && this.SelectedTarget.IsValid)
			{
				if (Menu["ForceSelectTarget"].GetValue<MenuBool>().Enabled && this.IsValidTarget(this.SelectedTarget, range, checkFrom))
				{
					return this.SelectedTarget;
				}
				if (Menu["OnlySelectTarget"].GetValue<MenuBool>().Enabled && this.IsValidTarget(this.SelectedTarget, 3.4028235E+38f, checkFrom))
				{
					return this.SelectedTarget;
				}
			}
			List<AIHeroClient> source = Base.Cache.EnemyHeroes.ToList<AIHeroClient>().FindAll((AIHeroClient x) => this.IsValidTarget(x, range, checkFrom) && ignoreChampions.All((AIHeroClient ignored) => ignored != null && ignored.IsValid && !ignored.Compare(x)));
			
			switch (Menu["TSMode_" + GameObjects.Player.CharacterName].GetValue<MenuList>().Index)
			{
				case 0:
					return source.MinOrDefault(x => Base.AaIndicator(x, damageType));
				case 1:
					return source.MinOrDefault(x => x.GetRealHeath(damageType));
				case 2:
					return source.MinOrDefault(x => GetPriority(x));
				default:
					return null;
			}
		}
        public List<AIHeroClient> GetTargets(float range, DamageType damageType, bool ignoreShields = true, Vector3? checkFrom = null, IEnumerable<AIHeroClient> ignoreChampions = null)
        {
			if (ignoreChampions == null)
			{
				ignoreChampions = new List<AIHeroClient>();
			}
			if (Menu["OnlySelectTarget"].GetValue<MenuBool>().Enabled && this.SelectedTarget != null && this.SelectedTarget.IsValidTarget(3.4028235E+38f, true, default(Vector3)))
			{
				return new List<AIHeroClient>
				{
					this.SelectedTarget
				};
			}
			List<AIHeroClient> list = this.GetOrderedTargetsByMode(range, damageType, ignoreShields, checkFrom, ignoreChampions).ToList<AIHeroClient>();

			if (Menu["ForceSelectTarget"].GetValue<MenuBool>().Enabled && this.SelectedTarget != null && this.SelectedTarget.IsValidTarget(range, true, default(Vector3)))
			{
				bool flag = list.Any((AIHeroClient x) => x.Compare(this.SelectedTarget));
				if (flag)
				{
					list.RemoveAll((AIHeroClient x) => x.Compare(this.SelectedTarget));
					list.Insert(0, this.SelectedTarget);
				}
			}
			return list;
        }
		private IEnumerable<AIHeroClient> GetOrderedTargetsByMode(float range, DamageType damageType, bool ignoreShield, Vector3? from = null, IEnumerable<AIHeroClient> ignoreChampions = null)
		{
			if (ignoreChampions == null)
			{
				ignoreChampions = new List<AIHeroClient>();
			}
			List<AIHeroClient> source = Base.Cache.EnemyHeroes.ToList<AIHeroClient>().FindAll((AIHeroClient x) => this.IsValidTarget(x, range, from) && ignoreChampions.All((AIHeroClient ignored) => ignored.NetworkId != x.NetworkId));
			IEnumerable<AIHeroClient> result = null;
			switch (Menu["TSMode_" + GameObjects.Player.CharacterName].GetValue<MenuList>().Index)
			{
				case 0:
					result = from hero in source
							 orderby Base.AaIndicator(hero, damageType)
							 select hero;
					break;
				case 1:
					result = from hero in source
							 orderby hero.GetRealHeath(damageType)
							 select hero;
					break;
				case 2:
					result = from hero in source
							 orderby GetPriority(hero) descending
							 select hero;
					break;
			}
			return result;
		}
		private bool IsValidTarget(AIHeroClient hero, float range, Vector3? from = null)
		{
			Vector3 position = (from != null && from.Value.IsValid()) ? from.Value : GameObjects.Player.ServerPosition;
			if (hero != null && hero.IsValidTarget(3.4028235E+38f, true, default(Vector3)))
			{
				if (hero.IsInvulnerableVisual || hero.IsInvulnerableVisual)
				{
					return false;
				}
                if (hero.IsZombie())
                {
					return false;
                }
                if (!hero.IsTargetable)
                {
					return false;
                }
                if (hero.HasBuff("UndyingRage") && hero.Health <= 71)
                {
					return false;
                }
				if (range > 0f)
				{
					if ((double)hero.DistanceSquared(position) < Math.Pow((double)(range + hero.BoundingRadius), 2.0))
					{
						return true;
					}
				}
				else if (range < 0f && this.InCurrentAutoAttackRange(hero, 0f))
				{
					return true;
				}
			}
			return false;
		}
		private bool InCurrentAutoAttackRange(AIHeroClient target, float extraRange = 0f)
		{
			if (!this.IsValidTarget(target, 3.4028235E+38f, null))
			{
				return false;
			}
			if (GameObjects.Player.CharacterName == "Azir")
			{
				IEnumerable<GameObject> azirSoldiers = GameObjects.AzirSoldiers;
				if (azirSoldiers.Any<GameObject>() && azirSoldiers.Any((GameObject x) => x != null && x.IsValid && !x.IsDead && (double)x.DistanceSquared(GameObjects.Player.ServerPosition) <= Math.Pow(770.0, 2.0) && (double)target.DistanceSquared(x) <= Math.Pow(350.0, 2.0)))
				{
					return true;
				}
			}
			float num = target.GetCurrentAutoAttackRange() + extraRange;
			return (double)Vector2.DistanceSquared(target.ServerPosition.ToVector2(), GameObjects.Player.ServerPosition.ToVector2()) <= Math.Pow((double)num, 2.0);
		}

		// Token: 0x04000162 RID: 354
		private static readonly string[] MaxPriority = new string[]
		{
			"Ahri",
			"Aphelios",
			"Anivia",
			"Annie",
			"Ashe",
			"Azir",
			"Brand",
			"Caitlyn",
			"Cassiopeia",
			"Corki",
			"Draven",
			"Ezreal",
			"Graves",
			"Jinx",
			"Kalista",
			"Kaisa",
			"Karma",
			"Karthus",
			"Katarina",
			"Kennen",
			"KogMaw",
			"Kindred",
			"Leblanc",
			"Lucian",
			"Lux",
			"Malzahar",
			"MasterYi",
			"MissFortune",
			"Neeko",
			"Orianna",
			"Quinn",
			"Sivir",
			"Sylas",
			"Syndra",
			"Talon",
			"Teemo",
			"Tristana",
			"TwistedFate",
			"Twitch",
			"Varus",
			"Vayne",
			"Veigar",
			"Velkoz",
			"Viktor",
			"Xerath",
			"Zed",
			"Ziggs",
			"Jhin",
			"Soraka",
			"AurelionSol",
			"Taliayh",
			"Qayana",
			"Zoe",
			"Xayah",
			"Taliyah",
			"Samira"
		};

		// Token: 0x04000163 RID: 355
		private static readonly string[] HighPriority = new string[]
		{
			"Akali",
			"Diana",
			"Ekko",
			"FiddleSticks",
			"Fiora",
			"Fizz",
			"Heimerdinger",
			"Jayce",
			"Kassadin",
			"Kayle",
			"KhaZix",
			"Lissandra",
			"Mordekaiser",
			"Nidalee",
			"Riven",
			"Senna",
			"Shaco",
			"Vladimir",
			"Yasuo",
			"Zilean",
			"Camille",
			"Kayn",
			"Yone"
		};

		// Token: 0x04000164 RID: 356
		private static readonly string[] MediumPriority = new string[]
		{
			"Aatrox",
			"Darius",
			"Elise",
			"Evelynn",
			"Galio",
			"Gangplank",
			"Gragas",
			"Irelia",
			"Jax",
			"LeeSin",
			"Maokai",
			"Morgana",
			"Nocturne",
			"Pantheon",
			"Poppy",
			"Pyke",
			"Rengar",
			"Rumble",
			"Ryze",
			"Sett",
			"Swain",
			"Trundle",
			"Tryndamere",
			"Udyr",
			"Urgot",
			"Vi",
			"XinZhao",
			"RekSai",
			"Illaoi",
			"Kled",
			"Lillia"
		};

		// Token: 0x04000165 RID: 357
		private static readonly string[] LowPriority = new string[]
		{
			"Alistar",
			"Amumu",
			"Bard",
			"Blitzcrank",
			"Braum",
			"Chogath",
			"DrMundo",
			"Garen",
			"Gnar",
			"Hecarim",
			"Janna",
			"JarvanIV",
			"Leona",
			"Lulu",
			"Malphite",
			"Nami",
			"Nasus",
			"Nautilus",
			"Nunu",
			"Olaf",
			"Rammus",
			"Renekton",
			"Sejuani",
			"Shen",
			"Shyvana",
			"Singed",
			"Sion",
			"Skarner",
			"Sona",
			"Taric",
			"TahmKench",
			"Thresh",
			"Volibear",
			"Warwick",
			"MonkeyKing",
			"Yorick",
			"Yuumi",
			"Zac",
			"Zyra",
			"Ornn",
			"Rakan",
			"Ivern",
			"Rell"
		};
	}
}
