using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.Rendering;

using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ppp
{
	internal class NewPrediction : IPrediction
	{
		private static Menu predMenu = null;
		private static bool AllowCalcPos => predMenu["AllowCalcPos"].GetValue<MenuBool>().Enabled;
		private static int MaxRange => predMenu["MaxRange"].GetValue<MenuSlider>().Value;
		private static HitChance Click => predMenu["Click"].GetValue<MenuHitChance>().HitChanceIndex;
		private static bool Wall => predMenu["Wall"].GetValue<MenuBool>().Enabled;
		private static int reaction => predMenu["reaction"].GetValue<MenuSlider>().Value;

		private static bool IsCalcPath = false;

		private static string version = "1.0.1.1";
		public NewPrediction()
		{
			if (predMenu == null)
			{
				predMenu = new Menu("IMPrediction", !Program.Chinese ? "GG" + ":Prediction" : "GG" + ":独立预判", true);
				{
					predMenu.Add(new MenuSlider("MaxRange", Program.Chinese ? "预判 最大百分比距离 %" : "Predict Max Range %", 90, 10, 100));
					predMenu.Add(new MenuBool("AllowCalcPos", Program.Chinese ? "允许使用额外走位判断逻辑" : "Enable Extra movement logic", false));
					predMenu.Add(new MenuHitChance("Click", Program.Chinese ? "预判对象重复点击同一位置时 返回的命中率" : "Click the same location more than 3 times", HitChance.Medium));
					predMenu.Add(new MenuBool("Wall", Program.Chinese ? "允许使用墙体预判 - 测试版" : "Use Wall Pred.(test)", false));
					predMenu.Add(new MenuSlider("reaction", Program.Chinese ? "敌人对技能的反应时间" : "Enemy reaction time to spell", 280, 0, 1000));
					predMenu.Add(new MenuSeparator("version", "Version:" + version));
				}
				predMenu.Attach();
			}
		}
		public PredictionOutput GetPrediction(PredictionInput input)
		{
			return GetPrediction(input, true, true);
		}
		public PredictionOutput GetPrediction(PredictionInput input, bool ft, bool checkCollision)
		{
			PredictionOutput result = null;

			bool isSuccessPredImmon = false;
			if (input.Unit.Type == GameObjectType.AIHeroClient && input.Unit.CharacterName == "Yuumi" && GameObjects.Heroes.Any((AIHeroClient x) => x.IsValidTarget(float.MaxValue, false, default(Vector3)) && x.Team == input.Unit.Team && x.HasBuff("YuumiWAlly") && x.Distance(input.Unit) <= 50f))
			{
				return new PredictionOutput { Hitchance = HitChance.None, CastPosition = Vector3.Zero, UnitPosition = Vector3.Zero };
			}
			if (!input.Unit.IsValidTarget(float.MaxValue, false))
			{
				//获取金身预判
				result = GetStagnatePrediction(input) ?? GetRebornPrediction(input);

				if (result == null)
				{
					return new PredictionOutput { Hitchance = HitChance.None, CastPosition = Vector3.Zero, UnitPosition = Vector3.Zero };
				}
				isSuccessPredImmon = true;
			}

			if (ft)
			{
				//Increase the delay due to the latency and server tick:
				input.Delay += Game.Ping / 2000f + 0.06f;

				if (input.Aoe && !isSuccessPredImmon)
				{
					return AoePrediction.GetPrediction(input);
				}
			}

			//Target too far away.
			if (Math.Abs(input.Range - float.MaxValue) > float.Epsilon &&
				input.Unit.DistanceSquared(input.RangeCheckFrom) > Math.Pow(input.Range * 1.5, 2))
			{
				return new PredictionOutput { Input = input, Hitchance = HitChance.OutOfRange };
			}

			//Unit is dashing.
			if (input.Unit.IsDashing())
			{
				result = GetDashingPrediction(input);
			}
			else
			{
				//Unit is immobile.
				var remainingImmobileT = UnitIsImmobileUntil(input.Unit);
				if (remainingImmobileT >= 0d)
				{
					result = GetImmobilePrediction(input, remainingImmobileT);
				}
			}

			//Normal prediction
			if (result == null)
			{
				result = GetStandardPrediction(input);
			}

			//Check if the unit position is in range
			if (Math.Abs(input.Range - float.MaxValue) > float.Epsilon)
			{
				if (result.Hitchance >= HitChance.High &&
					input.RangeCheckFrom.DistanceSquared(input.Unit.ServerPosition) >
					Math.Pow(input.Range + input.RealRadius * 3 / 4, 2))
				{
					result.Hitchance = HitChance.Medium;
				}

				if (input.RangeCheckFrom.DistanceSquared(result.UnitPosition) >
					Math.Pow(input.Range + (input.Type == SpellType.Circle ? input.RealRadius : 0), 2))
				{
					result.Hitchance = HitChance.OutOfRange;
				}



				/* This does not need to be handled for the updated predictions, but left as a reference.*/
				if (input.RangeCheckFrom.DistanceSquared(result.CastPosition) > Math.Pow(input.Range, 2))
				{
					if (result.Hitchance != HitChance.OutOfRange)
					{
						result.CastPosition = input.RangeCheckFrom +
											  input.Range *
											  (result.UnitPosition - input.RangeCheckFrom).ToVector2().Normalized().ToVector3World();
					}
					else
					{
						result.Hitchance = HitChance.OutOfRange;
					}
				}
			}
			//Check for collision
			if (checkCollision && input.Collision)
			{
				var positions = new List<Vector3> { result.UnitPosition, result.CastPosition, input.Unit.Position };
				var originalUnit = input.Unit;
				result.CollisionObjects = Collisions.GetCollision(positions, input);
				result.CollisionObjects.RemoveAll(x => x.NetworkId == originalUnit.NetworkId);
				result.Hitchance = result.CollisionObjects.Count > 0 ? HitChance.Collision : result.Hitchance;
			}
			//Set hit chance
			if (result.Hitchance == HitChance.High || result.Hitchance == HitChance.VeryHigh)
			{
				result = WayPointAnalysis(result, input);
			}
			return result;
		}
		private List<Vector3> GetRotatedFlashPositions(AIBaseClient uns, float ExtraWidth)
		{
			const int currentStep = 60;
			var direction = uns.Direction.ToVector2().Perpendicular();

			var list = new List<Vector3>();
			for (var i = 60; i <= 360; i += currentStep)
			{
				var angleRad = Geometry.DegreeToRadian(i);
				var rotatedPosition = uns.Position.ToVector2() + ((uns.BoundingRadius + ExtraWidth) * direction.Rotated(angleRad));
				list.Add(rotatedPosition.ToVector3());
			}
			return list;
		}
		private PredictionOutput WayPointAnalysis(PredictionOutput result, PredictionInput input)
		{
			if (input.Unit.Type != GameObjectType.AIHeroClient || input.Radius <= 1)
			{
				result.Hitchance = HitChance.VeryHigh;
				return result;
			}
			AIHeroClient PredicUnit = input.Unit as AIHeroClient;
			if (PredicUnit.IsRecalling() || (PredicUnit.IsCastingImporantSpell() && (PredicUnit.HaveImmovableBuff() || !PredicUnit.IsMoving || !PredicUnit.CanMove)))
			{
				//DebugPred("IMPORT CAST SPELL", PredicUnit.CharacterName, input.Spell.Slot);
				result.Hitchance = HitChance.VeryHigh;
				return result;
			}
			// 77.5 / 330 = 0.23
			// 50 / 2000 = 0.5
			//从技能中心计算逃逸速度
			float EscapeSpellRangeTime = (input.RealRadius / 1.5f / GetSpeedIfSlow(input)) + (reaction / 1000);

			bool HasSpedSpell = Math.Abs(input.Speed - float.MaxValue) > float.Epsilon;
			float RealSkillshotHitTime = HasSpedSpell ? ((input.From.Distance(result.CastPosition) / input.Speed) + input.Delay) : input.Delay;

			if (UnitTracker.GetSpecialSpellEndTime(PredicUnit) >= RealSkillshotHitTime ||
				(UnitTracker.GetSpecialSpellEndTime(PredicUnit) > input.Delay && UnitTracker.GetSpecialSpellEndTime(PredicUnit) + EscapeSpellRangeTime >= RealSkillshotHitTime))
			{
				//0.25s  + 0.0.4s
				//DebugPred("SpecialSpellEndtime", PredicUnit.CharacterName, input.Spell.Slot);
				result.CastPosition = PredicUnit.ServerPosition;
				result.UnitPosition = PredicUnit.ServerPosition;
				result.Hitchance = HitChance.VeryHigh;
				return result;
			}

			// PREPARE MATH ///////////////////////////////////////////////////////////////////////////////////

			var orignal = result.Hitchance;

			result.Hitchance = HitChance.Medium;  //设置默认

			var GetUnitWay = IsCalcPath ? UnitTracker.GetPathWayCalc(PredicUnit) : PredicUnit.GetWaypoints();
			var LastWayPoint = GetUnitWay.Last().ToVector3World();
			var UnitToLastWayDistance = PredicUnit.ServerPosition.Distance(LastWayPoint);
			var FromToCasPosDistance = input.From.Distance(result.CastPosition);
			var UnitToCastPosDistance = PredicUnit.Distance(result.CastPosition);
			var FromToLastWayDistance = input.From.Distance(LastWayPoint);
			float moveArea = GetSpeedIfSlow(input) * RealSkillshotHitTime;
			float fixRange = moveArea * (0.4f + input.Delay / 2f);

			double angleMove = 30 + (input.Radius / 17) - (RealSkillshotHitTime * 3);
			var angle = (LastWayPoint - PredicUnit.ServerPosition).AngleBetween(input.From - PredicUnit.ServerPosition);
			float backToFront = moveArea * 1.5f;
			float pathMinLen = 1000f;
			var SUPERAngle = FindAngle(result.CastPosition, PredicUnit.ServerPosition, input.From);
			//----------------必中

			if (UnitToLastWayDistance > 0 && UnitToLastWayDistance < 50)
			{
				result.Hitchance = HitChance.Medium;
				return result;
			}

			if (result.CastPosition.Distance(input.RangeCheckFrom) > input.Range * (MaxRange * 0.01) && input.Type == SpellType.Line && HasSpedSpell)
			{
				result.Hitchance = HitChance.None;
				return result;
			}

			if (UnitTracker.GetLastNewPathTime(PredicUnit) < 0.1d && orignal == HitChance.VeryHigh)
			{
				fixRange = moveArea * 0.3f;
				pathMinLen = 700f + backToFront;
				angleMove += 1.5f;
				result.Hitchance = HitChance.High;
			}

			if (input.Type == SpellType.Circle)
			{
				fixRange -= input.Radius / 2;
			}

			if (UnitTracker.GetLastVisableTime(PredicUnit) < 0.1d)
			{
				//目标刚刚被看见时往往正在走位 此时禁止预判
				result.Hitchance = HitChance.Medium;
				return result;
			}

			if (UnitTracker.PathCalc(PredicUnit) && !IsCalcPath)
			{
				result.Hitchance = HitChance.Medium;
				return result;
			}

			// SPAM CLICK ///////////////////////////////////////////////////////////////////////////////////
			if (IsCalcPath && UnitTracker.PathCalc(PredicUnit) && (RealSkillshotHitTime <= 0.6f || UnitTracker.GetLastNewPathTime(PredicUnit) > 0.3 || result.Hitchance == HitChance.High))
			{
				if (FromToCasPosDistance < input.Range - fixRange)
				{
					//DebugPred("CALC PATH", input.Unit.CharacterName, input.Spell.Slot);
					result.Hitchance = HitChance.VeryHigh;
					return result;
				}
				result.Hitchance = HitChance.Medium;
				return result;
			}

			if (
			   Utils.AngleBetween(input.From,
								  PredicUnit.ServerPosition,
								  result.CastPosition) > 60)
			{
				result.Hitchance = HitChance.Low;
				return result;
			}
			if (UnitTracker.GetLastIssueTime(PredicUnit) <= 0.1d)
			{
				result.Hitchance = HitChance.High;
				return result;
			}
			if (PredicUnit.IsWindingUp)
			{
				var widup = UnitTracker.GetLastAutoAttackWindingUpEndTime(PredicUnit) / 1000d;
				if (widup > 0 && RealSkillshotHitTime > 0)
				{
					if (!HasSpedSpell) //瞬发型技能时
					{
						if ((input.Radius > PredicUnit.BoundingRadius * 0.85f) && (widup > RealSkillshotHitTime * 0.2333f))
						{
							//DebugPred("PRED FAST SPELL - > WINDINGUP", input.Unit.CharacterName, input.Spell.Slot);
							result.CastPosition = PredicUnit.ServerPosition;
							result.Hitchance = HitChance.VeryHigh;
							return result;
						}
					}
					else if ((RealSkillshotHitTime <= (widup + EscapeSpellRangeTime)) || (PredicUnit.Distance(input.From) <= 425 && (input.Delay <= 0.4f || UnitTracker.GetLastStopMoveTime(PredicUnit) > 0.2d))) //到达距离在敌方AA延迟更慢的地方
					{
						//DebugPred("PRED WINDINGUP", input.Unit.CharacterName, input.Spell.Slot);
						result.Hitchance = HitChance.VeryHigh;
						return result;
					}
				}
			}
			else if ((PredicUnit.Path.Count() == 0 || !PredicUnit.IsMoving) && !PredicUnit.Spellbook.IsAutoAttack)
			{
				result.Hitchance = HitChance.Medium;
				if (FromToCasPosDistance > input.Range - fixRange)
				{
					result.Hitchance = HitChance.Medium;
					return result;
				}
				var fastSpell = 0.6d + (RealSkillshotHitTime > 0.5f ? 0.4d : 0d);
				if (UnitTracker.GetLastNewPathTime(PredicUnit) < fastSpell &&
				   (UnitTracker.GetLastStopMoveTime(PredicUnit) < fastSpell + 0.1d || UnitTracker.GetLastAutoAttackTime(PredicUnit) < fastSpell)) //距离上一次移动在0.4秒之前
				{
					//上一次移动在0.4s之前 敌人仅仅停止移动了0.5s时
					result.Hitchance = HitChance.Medium;
				}
				else if (UnitTracker.GetLastStopMoveTime(PredicUnit) > 0.3d)
				{
					//Console.WriteLine("PREC NOT MOVEING | LastNew {0} LastStop {1} AutoAT {2} {3}", new object[] { UnitTracker.GetLastNewPathTime(PredicUnit), UnitTracker.GetLastStopMoveTime(PredicUnit), UnitTracker.GetLastAutoAttackTime(PredicUnit),PredicUnit.CharacterName });
					result.Hitchance = HitChance.VeryHigh;
				}
				return result;
			}

			if (UnitTracker.CheckPathCanBySkillShot(PredicUnit) <= UnitToCastPosDistance && PredicUnit.IsMoving)
			{
				//DebugPred("PATH CHECK" + "--- ANGLE:" + angle.ToString(), PredicUnit.CharacterName, input.Spell.Slot);
				result.Hitchance++;
				angleMove += 1.2f;
			}

			bool successFlag = false;

			if (UnitTracker.CheckPathInOnePos(PredicUnit) && PredicUnit.IsMoving)
			{
				//DebugPred("HIGH - CLICK POS", input.Unit.CharacterName, input.Spell.Slot);
				//medium < high
				if (result.Hitchance < Click)
				{
					result.Hitchance = Click;
				}
				successFlag = true;
				angleMove += 1.3f;
			}
			// FIX RANGE ///////////////////////////////////////////////////////////////////////////////////
			if (FromToLastWayDistance <= PredicUnit.Distance(input.From) && FromToCasPosDistance > input.Range - fixRange)
			{
				result.Hitchance = HitChance.Medium;
				return result;
			}
			else if (UnitToLastWayDistance > 350)
			{
				angleMove += 1.5f;
			}

			if ((angle < 20 || angle > 150) && UnitTracker.GetLastStopMoveTime(PredicUnit) < 0.1d && UnitTracker.GetLastNewPathTime(PredicUnit) < 0.1d)
			{
				//Console.WriteLine("{2}  {3}  --- CALC ANGLE (<20 || >150) --- GetLastNewPathTime :{0}  - GetLastStopMoveTime {1}", new object[] { UnitTracker.GetLastNewPathTime(PredicUnit), UnitTracker.GetLastStopMoveTime(PredicUnit), input.Unit.CharacterName, input.Spell.Slot });
				//DebugPred("CALC ANGLE", input.Unit.CharacterName, input.Spell.Slot);

				result.Hitchance = HitChance.VeryHigh;
				return result;
			}

			if (Wall && FromToCasPosDistance < input.Range - fixRange)
			{
				int Count = 0;
				var wallpoints = GetRotatedFlashPositions(PredicUnit, input.Radius);
				foreach (var wall in wallpoints)
				{
					if (wall.IsWall())
					{
						Count++;
					}
				}
				if (Count >= 4)
				{
					result.Hitchance = HitChance.VeryHigh;
				}
			}

			// LONG CLICK DETECTION ///////////////////////////////////////////////////////////////////////////////////

			if (UnitToLastWayDistance > pathMinLen && successFlag)
			{
				//DebugPred("LONG CLICK DETECTION", input.Unit.CharacterName, input.Spell.Slot);
				result.Hitchance = HitChance.VeryHigh;
				return result;
			}

			// RUN IN LANE DETECTION ///////////////////////////////////////////////////////////////////////////////////

			if (FromToLastWayDistance > FromToCasPosDistance + fixRange && GetAngle(input.From, PredicUnit) < angleMove)
			{
				//DebugPred("RUN IN LANE DETECTION", input.Unit.CharacterName, input.Spell.Slot);
				result.Hitchance = HitChance.VeryHigh;
				return result;
			}

			// ANGLE HIT CHANCE ///////////////////////////////////////////////////////////////////////////////////

			if (input.Type == SpellType.Line && PredicUnit.Path.Count() > 0 && PredicUnit.IsMoving)
			{
				if (GetAngle(input.From, PredicUnit) < angleMove)
				{
					//DebugPred("ANGLE HIT CHANCE", input.Unit.CharacterName, input.Spell.Slot);
					result.Hitchance = HitChance.VeryHigh;
					return result;
				}
			}

			// CIRCLE NEW PATH ///////////////////////////////////////////////////////////////////////////////////

			if (input.Type == SpellType.Circle)
			{
				if (UnitTracker.GetLastNewPathTime(PredicUnit) < 0.1d && FromToCasPosDistance < input.Range - fixRange && UnitToLastWayDistance > fixRange)
				{
					result.Hitchance = HitChance.VeryHigh;
					return result;
				}
				// CALCULATE RUN AWAY
				if (EscapeSpellRangeTime > RealSkillshotHitTime)
				{
					//DebugPred("Calculate HitTime", input.Unit.CharacterName, input.Spell.Slot);
					//Don't use Server Position
					result.Hitchance = HitChance.VeryHigh;
					result.CastPosition = PredicUnit.Position;
					result.UnitPosition = PredicUnit.Position;
					return result;
				}
			}

			return result;
		}
		private void DebugPred(string value, string Name, SpellSlot zsd)
		{

			//return;

			var min = (int)(Game.Time / 60);
			var all = (int)(Game.Time - (min * 60));
			Console.WriteLine("[{0}] : {1}  - {2}  - {3}", new object[] { min + ":" + all, value, Name, zsd });
		}
		private float FindAngle(Vector3 p1, Vector3 center, Vector3 p2)
		{
			var b = Math.Pow(center.X - p1.X, 2) + Math.Pow(center.Y - p1.Y, 2);
			var a = Math.Pow(center.X - p2.X, 2) + Math.Pow(center.Y - p2.Y, 2);
			var c = Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2);
			var angle = Math.Acos((a + b - c) / Math.Sqrt(4 * a * b)) * (180 / Math.PI);
			if (angle > 90)
				angle = 180 - angle;
			return (float)angle;
		}
		private PredictionOutput GetDashingPrediction(PredictionInput input)
		{
			var dashData = input.Unit.GetDashInfo();
			var result = new PredictionOutput()
			{
				CastPosition = dashData.EndPos.ToVector3World(),
				UnitPosition = dashData.EndPos.ToVector3World(),
				Hitchance = HitChance.Medium
			};
			//Normal dashes.
			if (dashData.EndTick >= Variables.GameTimeTickCount && ((dashData.EndTick - Variables.GameTimeTickCount) / 1000f) >= input.Delay / 2f)
			{
				//Mid air:
				var endP = dashData.EndPos;
				if (endP.IsValid() && Utils.IsValidFloat(dashData.Speed))
				{
					var dashPred = GetPositionOnPath(input, new List<Vector2> { input.Unit.ServerPosition.ToVector2(), endP }, dashData.Speed);
					if (dashPred.Hitchance >= HitChance.High)
					{
						dashPred.Hitchance = HitChance.Dash;
						return dashPred;
					}

					//At the end of the dash:
					if (dashData.Path.PathLength() > 200)
					{
						var timeToPoint = input.Delay + (input.From.Distance(endP) / input.Speed);

						if (timeToPoint <=
							(input.Unit.ServerPosition.Distance(endP) / dashData.Speed) + (input.RealRadius / GetSpeedIfSlow(input)))
						{
							return new PredictionOutput
							{
								CastPosition = endP.ToVector3World(),
								UnitPosition = endP.ToVector3World(),
								Hitchance = HitChance.Dash
							};
						}
					}
				}
			}
			return result;
		}
		private PredictionOutput GetRebornPrediction(PredictionInput input)
		{
			if (input.Unit != null && UnitTracker.IsReborn(input.Unit))
			{
				var endtime = UnitTracker.GetRebornEndTime(input.Unit) - Variables.GameTimeTickCount;
				var timeToReachTargetPosition = (input.Delay + (input.Unit.Distance(input.From) / input.Speed)) * 1000 + 120f;
				if (endtime > 0 && timeToReachTargetPosition > 0)
				{
					if (endtime == timeToReachTargetPosition || (endtime - timeToReachTargetPosition < 0 && endtime - (timeToReachTargetPosition - 10) >= 0))
					{
						return new PredictionOutput
						{
							CastPosition = input.Unit.ServerPosition,
							UnitPosition = input.Unit.ServerPosition,
							Hitchance = HitChance.Immobile
						};
					}
				}
			}
			return null;
		}
		private PredictionOutput GetStagnatePrediction(PredictionInput input)
		{
			if (input.Unit != null)
			{
				var BuffInstance = input.Unit.GetBuff("zhonyasringshield");
				if (BuffInstance != null)
				{
					if (Game.Time <= BuffInstance.EndTime && BuffInstance.IsActive)
					{
						var endtime = BuffInstance.EndTime - Game.Time;
						//金身时间1.1秒 击中需要1.0秒  
						var timeToReachTargetPosition = input.Delay + (input.Unit.Distance(input.From) / input.Speed) - 0.12f;
						if (endtime > 0 && timeToReachTargetPosition > 0)
						{
							if (endtime == timeToReachTargetPosition || (endtime - timeToReachTargetPosition < 0 && endtime - (timeToReachTargetPosition - 0.01f) >= 0))
							{
								return new PredictionOutput
								{
									CastPosition = input.Unit.ServerPosition,
									UnitPosition = input.Unit.ServerPosition,
									Hitchance = HitChance.Immobile
								};
							}
						}
					}
				}
			}
			return null;
		}
		private PredictionOutput GetImmobilePrediction(PredictionInput input, double remainingImmobileT)
		{
			var result = new PredictionOutput
			{
				Input = input,
				CastPosition = input.Unit.ServerPosition,
				UnitPosition = input.Unit.ServerPosition,
				Hitchance = HitChance.High
			};

			var timeToReachTargetPosition = input.Delay + (input.Unit.Distance(input.From) / input.Speed);
			var TimeToHit_ToK = Math.Abs(input.Speed - float.MaxValue) > float.Epsilon ? timeToReachTargetPosition : input.Delay;
			if (TimeToHit_ToK <= remainingImmobileT + (input.RealRadius / GetSpeedIfSlow(input)))
			{
				return new PredictionOutput
				{
					CastPosition = input.Unit.ServerPosition,
					UnitPosition = input.Unit.ServerPosition,
					Hitchance = HitChance.Immobile
				};
			}
			return result;
		}
		private PredictionOutput GetStandardPrediction(PredictionInput input)
		{
			//获取真实速度
			var speed = GetSpeedIfSlow(input);
			if (input.Unit.DistanceSquared(input.From) < Math.Pow(250.0, 2.0))
			{
				speed *= 1.5f;
			}
			if (input.Unit.Type == GameObjectType.AIHeroClient && UnitTracker.PathCalc(input.Unit) && AllowCalcPos)
			{
				IsCalcPath = true;
				return GetPositionOnPath(input, UnitTracker.GetPathWayCalc(input.Unit), speed);
			}
			IsCalcPath = false;
			return GetPositionOnPath(input, input.Unit.GetWaypoints(), speed);
		}
		private float GetSpeedIfSlow(PredictionInput input)
		{
			var result =
				input.Unit.Buffs.Where(
					buff =>
						buff.IsActive && Game.Time <= buff.EndTime && buff.Type == BuffType.Slow).MaxOrDefault(x => x.EndTime);
			if (result == null)
			{
				return input.Unit.MoveSpeed;
			}
			var eee = result.EndTime - Game.Time;
			if (eee > 0)
			{
				if (eee < input.Delay)
				{
					return input.Unit.BaseMoveSpeed;
				}
			}
			return input.Unit.MoveSpeed;
		}
		public static double GetAngle(Vector3 from, AIBaseClient target)
		{
			var C = target.ServerPosition.ToVector2();
			var A = target.GetWaypoints().LastOrDefault();

			if (C == A)
			{
				return 60;
			}


			var B = from.ToVector2();

			var AB = Math.Pow((double)A.X - (double)B.X, 2) + Math.Pow((double)A.Y - (double)B.Y, 2);
			var BC = Math.Pow((double)B.X - (double)C.X, 2) + Math.Pow((double)B.Y - (double)C.Y, 2);
			var AC = Math.Pow((double)A.X - (double)C.X, 2) + Math.Pow((double)A.Y - (double)C.Y, 2);

			return Math.Cos((AB + BC - AC) / (2 * Math.Sqrt(AB) * Math.Sqrt(BC))) * 180 / Math.PI;
		}
		private double UnitIsImmobileUntil(AIBaseClient unit)
		{
			if (unit.Type == GameObjectType.AIHeroClient)
			{
				if (Program.HasHookFlag[unit.NetworkId])
					return -1;
			}
			var result =
				unit.Buffs.Where(
					buff =>
						buff.IsActive && Game.Time <= buff.EndTime && (buff.Type == BuffType.Knockup || (buff.Type == BuffType.Stun)
						|| buff.Type == BuffType.Suppression || buff.Type == BuffType.Snare))
					.Aggregate(0d, (current, buff) => Math.Max(current, buff.EndTime));
			return (result - Game.Time);
		}
		private PredictionOutput GetPositionOnPath(PredictionInput input, List<Vector2> path, float speed = -1)
		{
			speed = Math.Abs(speed - (-1)) < float.Epsilon ? input.Unit.MoveSpeed : speed;

			if (path.Count <= 1)
			{
				return new PredictionOutput
				{
					Input = input,
					CastPosition = input.Unit.ServerPosition,
					UnitPosition = input.Unit.ServerPosition,
					Hitchance = HitChance.VeryHigh
				};
			}
			var pLength = path.PathLength();

			//Skillshots with only a delay
			if (pLength >= input.Delay * speed - input.RealRadius && Math.Abs(input.Speed - float.MaxValue) < float.Epsilon) // pLength >= input.Delay * speed - input.RealRadius && 
			{
				var tDistance = input.Delay * speed - input.RealRadius;
				for (var i = 0; i < path.Count - 1; i++)
				{
					var a = path[i];
					var b = path[i + 1];
					var d = a.Distance(b);
					if (d >= tDistance)
					{
						var direction = (b - a).Normalized();

						var cp = a + direction * tDistance;
						var p = a +
								direction *
								((i == path.Count - 2)
									? Math.Min(tDistance + input.RealRadius, d)
									: (tDistance + input.RealRadius));

						return new PredictionOutput
						{

							Input = input,
							CastPosition = cp.ToVector3World(),
							UnitPosition = p.ToVector3World(),
							Hitchance = UnitTracker.GetLastNewPathTime(input.Unit) < 0.1d ? HitChance.VeryHigh
								: HitChance.High
						};
					}

					tDistance -= d;
				}
			}

			//Skillshot with a delay and speed.
			if (pLength >= input.Delay * speed - input.RealRadius &&
				Math.Abs(input.Speed - float.MaxValue) > float.Epsilon)  //pLength >= input.Delay * speed - input.RealRadius &&
			{
				var d = input.Delay * speed - input.RealRadius;
				if (input.Type == SpellType.Line || input.Type == SpellType.Cone)
				{
					if (input.From.DistanceSquared(input.Unit.ServerPosition) < 200 * 200)
					{
						d = input.Delay * speed;
					}
				}
				path = path.CutPath(d);
				var tT = 0f;
				for (var i = 0; i < path.Count - 1; i++)
				{
					var a = path[i];
					var b = path[i + 1];
					var tB = a.Distance(b) / speed;
					var direction = (b - a).Normalized();
					a = a - speed * tT * direction;
					var sol = a.VectorMovementCollision(b, speed, input.From.ToVector2(), input.Speed, tT);
					var t = sol.CollisionTime;
					var pos = sol.CollisionPosition;

					if (pos.IsValid() && t >= tT && t <= tT + tB)
					{
						if (pos.DistanceSquared(b) < 20)
							break;
						var p = pos + input.RealRadius * direction;

						return new PredictionOutput
						{
							Input = input,
							CastPosition = pos.ToVector3World(),
							UnitPosition = p.ToVector3World(),
							Hitchance = UnitTracker.GetLastNewPathTime(input.Unit) < 0.1d ? HitChance.VeryHigh
								: HitChance.High
						};
					}
					tT += tB;
				}
			}
			var position = path.Last();
			return new PredictionOutput
			{
				Input = input,
				CastPosition = position.ToVector3World(),
				UnitPosition = position.ToVector3World(),
				Hitchance = HitChance.Medium
			};
		}
		public static Vector2 GetPosAfter(List<Vector2> path, float speed, float time)
		{
			if (path.Count == 0)
				return new Vector2(0, 0);
			if (path.Count == 1)
				return path[0];

			var distance = time * speed;
			if (distance < 0)
				return path[0].Extend(path[1], distance);
			for (int i = 0; i < path.Count - 1; i++)
			{
				var a = path[i];
				var b = path[i + 1];
				var dist = a.Distance(b);
				if (dist == distance)
					return b;
				else if (dist > distance)
					return a.Extend(b, distance);
				distance = distance - dist;
			}
			return path[path.Count - 1];
		}
		private float Interception(Vector2 startPos, Vector2 endPos, Vector2 source, float speed, float missileSpeed, float delay = 0)
		{
			Vector2 dir = endPos - startPos;
			float magn = dir.Length();
			Vector2 vel = dir * speed / magn;
			dir = startPos - source;
			float a = vel.LengthSquared() - missileSpeed * missileSpeed;
			float b = 2f * Utils.DotProduct(vel, dir);
			float c = dir.LengthSquared();
			float delta = b * b - 4f * a * c;
			if (delta >= 0.0) // at least one solution exists
			{
				delta = (float)Math.Sqrt(delta);
				float t1 = (-b + delta) / (2f * a),
					t2 = (-b - delta) / (2f * a);
				float t = 0f;
				if (t2 >= delay)
					t = (t1 >= delay) ?
						Math.Min(t1, t2) : Math.Max(t1, t2);
				return t; // the final solution
			}
			return 0; // no solutions found
		}
		internal class Utils
		{
			public static float DotProduct(Vector2 p1, Vector2 p2)
			{
				return p1.X * p2.X + p1.Y * p2.Y;
			}
			public static bool IsValidVector3(Vector3 vector)
			{
				if (vector.X.CompareTo(0.0f) == 0 && vector.Y.CompareTo(0.0f) == 0 && vector.Z.CompareTo(0.0f) == 0)
				{
					return false;
				}
				else
				{
					return true;
				}
			}
			public static bool IsValidFloat(float t)
			{
				if (t.CompareTo(0) != 0 && !float.IsNaN(t) && t.CompareTo(float.MaxValue) != 0)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			public static bool Close(float a, float b, float eps)
			{
				if (IsValidFloat(eps))
					eps = eps;
				else
					eps = (float)1e-9;
				return Math.Abs(a - b) <= eps;
			}
			public static double RadianToDegree(double angle)
			{
				return angle * (180.0 / Math.PI);
			}
			public static float Polar(Vector3 v1)
			{
				if (Close(v1.X, 0, 0))
				{
					float area1 = v1.Y;
					if (area1 > 0)
					{
						return 90;
					}
					else if (area1 < 0)
					{
						return 270;
					}
					else
					{
						return 0;
					}
				}
				else
				{
					float area1 = v1.Y;
					var theta = (float)RadianToDegree(Math.Atan((area1) / v1.X));
					if (v1.X < 0)
					{
						theta = theta + 180;
					}
					if (theta < 0)
					{
						theta = theta + 360;
					}
					return theta;
				}
			}
			public static float AngleBetween(Vector3 self, Vector3 v1, Vector3 v2)
			{
				Vector3 p1 = (-self + v1);
				Vector3 p2 = (-self + v2);
				float theta = Polar(p1) - Polar(p2);
				if (theta < 0)
					theta = theta + 360;
				if (theta > 180)
					theta = 360 - theta;
				return theta;
			}
		}
		internal class AoePrediction
		{
			public static PredictionOutput GetPrediction(PredictionInput input)
			{
				switch (input.Type)
				{
					case SpellType.Circle:
						return Circle.GetPrediction(input);
					case SpellType.Cone:
						return Cone.GetPrediction(input);
					case SpellType.Line:
						return Line.GetPrediction(input);
				}
				return new PredictionOutput();
			}

			internal static List<PossibleTarget> GetPossibleTargets(PredictionInput input)
			{
				var result = new List<PossibleTarget>();
				var originalUnit = input.Unit;
				GameObjects.Get<AIHeroClient>().Where(
					i =>
					!i.Compare(originalUnit)
					&& i.IsValidTarget(input.Range + 200 + input.RealRadius, true, input.RangeCheckFrom)).ForEach(
						i =>
						{
							input.Unit = i;

							var prediction = new NewPrediction().GetPrediction(input, false, false);
							if (prediction.Hitchance >= HitChance.High)
							{
								result.Add(
									new PossibleTarget { Position = prediction.UnitPosition.ToVector2(), Unit = i });
							}
						});
				return result;
			}

			public static class Circle
			{
				public static PredictionOutput GetPrediction(PredictionInput input)
				{
					var mainTargetPrediction = new NewPrediction().GetPrediction(input, false, true);
					var posibleTargets = new List<PossibleTarget>
				{
					new PossibleTarget { Position = mainTargetPrediction.UnitPosition.ToVector2(), Unit = input.Unit }
				};

					if (mainTargetPrediction.Hitchance >= HitChance.Medium)
					{
						//Add the posible targets  in range:
						posibleTargets.AddRange(GetPossibleTargets(input));
					}

					while (posibleTargets.Count > 1)
					{

						var mecCircle = Mec.GetMec(posibleTargets.Select(h => h.Position).ToList());

						if (mecCircle.Radius <= input.RealRadius - 10 &&
							Vector2.DistanceSquared(mecCircle.Center, input.RangeCheckFrom.ToVector2()) <
							input.Range * input.Range)
						{
							return new PredictionOutput
							{
								AoeTargetsHit = posibleTargets.Select(h => (AIHeroClient)h.Unit).ToList(),
								CastPosition = mecCircle.Center.ToVector3World(),
								UnitPosition = mainTargetPrediction.UnitPosition,
								Hitchance = mainTargetPrediction.Hitchance,
								Input = input,
								AoeTargetsHitCount = posibleTargets.Count
							};
						}

						float maxdist = -1;
						var maxdistindex = 1;
						for (var i = 1; i < posibleTargets.Count; i++)
						{
							var distance = Vector2.DistanceSquared(posibleTargets[i].Position, posibleTargets[0].Position);
							if (distance > maxdist || maxdist.CompareTo(-1) == 0)
							{
								maxdistindex = i;
								maxdist = distance;
							}
						}
						posibleTargets.RemoveAt(maxdistindex);
					}

					return mainTargetPrediction;
				}
			}

			public static class Cone
			{
				internal static int GetHits(Vector2 end, double range, float angle, List<Vector2> points)
				{
					return (from point in points
							let edge1 = end.Rotated(-angle / 2)
							let edge2 = edge1.Rotated(angle)
							where
								point.DistanceSquared(new Vector2()) < range * range && edge1.CrossProduct(point) > 0 &&
								point.CrossProduct(edge2) > 0
							select point).Count();
				}

				public static PredictionOutput GetPrediction(PredictionInput input)
				{
					var mainTargetPrediction = new NewPrediction().GetPrediction(input, false, true);
					var posibleTargets = new List<PossibleTarget>
				{
					new PossibleTarget { Position = mainTargetPrediction.UnitPosition.ToVector2(), Unit = input.Unit }
				};

					if (mainTargetPrediction.Hitchance >= HitChance.Medium)
					{
						//Add the posible targets  in range:
						posibleTargets.AddRange(GetPossibleTargets(input));
					}

					if (posibleTargets.Count > 1)
					{
						var candidates = new List<Vector2>();

						foreach (var target in posibleTargets)
						{
							target.Position = target.Position - input.From.ToVector2();
						}

						for (var i = 0; i < posibleTargets.Count; i++)
						{
							for (var j = 0; j < posibleTargets.Count; j++)
							{
								if (i != j)
								{
									var p = (posibleTargets[i].Position + posibleTargets[j].Position) * 0.5f;
									if (!candidates.Contains(p))
									{
										candidates.Add(p);
									}
								}
							}
						}

						var bestCandidateHits = -1;
						var bestCandidate = new Vector2();
						var positionsList = posibleTargets.Select(t => t.Position).ToList();

						foreach (var candidate in candidates)
						{
							var hits = GetHits(candidate, input.Range, input.Radius, positionsList);
							if (hits > bestCandidateHits)
							{
								bestCandidate = candidate;
								bestCandidateHits = hits;
							}
						}

						bestCandidate = bestCandidate + input.From.ToVector2();

						if (bestCandidateHits > 1 && input.From.ToVector2().DistanceSquared(bestCandidate) > 50 * 50)
						{
							return new PredictionOutput
							{
								Hitchance = mainTargetPrediction.Hitchance,
								AoeTargetsHitCount = bestCandidateHits,
								UnitPosition = mainTargetPrediction.UnitPosition,
								CastPosition = bestCandidate.ToVector3World(),
								Input = input
							};
						}
					}
					return mainTargetPrediction;
				}
			}

			public static class Line
			{
				internal static IEnumerable<Vector2> GetHits(Vector2 start, Vector2 end, double radius, List<Vector2> points)
				{
					return points.Where(p => p.DistanceSquared(start, end, true) <= radius * radius);
				}

				internal static Vector2[] GetCandidates(Vector2 from, Vector2 to, float radius, float range)
				{
					var middlePoint = (from + to) / 2;
					var intersections = Vector2Extensions.CircleCircleIntersection(
						from, middlePoint, radius, from.Distance(middlePoint));

					if (intersections.Length > 1)
					{
						var c1 = intersections[0];
						var c2 = intersections[1];

						c1 = from + range * (to - c1).Normalized();
						c2 = from + range * (to - c2).Normalized();

						return new[] { c1, c2 };
					}

					return new Vector2[] { };
				}

				public static PredictionOutput GetPrediction(PredictionInput input)
				{
					var mainTargetPrediction = new NewPrediction().GetPrediction(input, false, true);
					var posibleTargets = new List<PossibleTarget>
				{
					new PossibleTarget { Position = mainTargetPrediction.UnitPosition.ToVector2(), Unit = input.Unit }
				};
					if (mainTargetPrediction.Hitchance >= HitChance.Medium)
					{
						//Add the posible targets  in range:
						posibleTargets.AddRange(GetPossibleTargets(input));
					}

					if (posibleTargets.Count > 1)
					{
						var candidates = new List<Vector2>();
						foreach (var target in posibleTargets)
						{
							var targetCandidates = GetCandidates(
								input.From.ToVector2(), target.Position, (input.Radius), input.Range);
							candidates.AddRange(targetCandidates);
						}

						var bestCandidateHits = -1;
						var bestCandidate = new Vector2();
						var bestCandidateHitPoints = new List<Vector2>();
						var positionsList = posibleTargets.Select(t => t.Position).ToList();

						foreach (var candidate in candidates)
						{
							if (
								GetHits(
									input.From.ToVector2(), candidate, (input.Radius + input.Unit.BoundingRadius / 3 - 10),
									new List<Vector2> { posibleTargets[0].Position }).Count() == 1)
							{
								var hits = GetHits(input.From.ToVector2(), candidate, input.Radius, positionsList).ToList();
								var hitsCount = hits.Count;
								if (hitsCount >= bestCandidateHits)
								{
									bestCandidateHits = hitsCount;
									bestCandidate = candidate;
									bestCandidateHitPoints = hits.ToList();
								}
							}
						}

						if (bestCandidateHits > 1)
						{
							float maxDistance = -1;
							Vector2 p1 = new Vector2(), p2 = new Vector2();

							//Center the position
							for (var i = 0; i < bestCandidateHitPoints.Count; i++)
							{
								for (var j = 0; j < bestCandidateHitPoints.Count; j++)
								{
									var startP = input.From.ToVector2();
									var endP = bestCandidate;
									var proj1 = positionsList[i].ProjectOn(startP, endP);
									var proj2 = positionsList[j].ProjectOn(startP, endP);
									var dist = Vector2.DistanceSquared(bestCandidateHitPoints[i], proj1.LinePoint) +
											   Vector2.DistanceSquared(bestCandidateHitPoints[j], proj2.LinePoint);
									if (dist >= maxDistance &&
										(proj1.LinePoint - positionsList[i]).AngleBetween(
											proj2.LinePoint - positionsList[j]) > 90)
									{
										maxDistance = dist;
										p1 = positionsList[i];
										p2 = positionsList[j];
									}
								}
							}

							return new PredictionOutput
							{
								Hitchance = mainTargetPrediction.Hitchance,
								AoeTargetsHitCount = bestCandidateHits,
								UnitPosition = mainTargetPrediction.UnitPosition,
								CastPosition = ((p1 + p2) * 0.5f).ToVector3World(),
								Input = input
							};
						}
					}

					return mainTargetPrediction;
				}
			}

			internal class PossibleTarget
			{
				public Vector2 Position;
				public AIBaseClient Unit;
			}
		}
		internal class PathInfo
		{
			public Vector2 Position { get; set; }
			public float Time { get; set; }
		}
		internal class Spells
		{
			public string name { get; set; }
			public double duration { get; set; }
		}
		internal class UnitTrackerInfo
		{
			public int NetworkId { get; set; }
			public int AaTick { get; set; }
			public int AaWindingUpEndTime { get; set; }
			public int NewPathTick { get; set; }
			public int StopMoveTick { get; set; }
			public int LastInvisableTick { get; set; }
			public bool IsReborn { get; set; }
			public int RebornTime { get; set; }
			public int SpecialSpellFinishTick { get; set; }
			public List<PathInfo> PathBank = new List<PathInfo>();
			public List<PathInfo> CheckClick = new List<PathInfo>();
			public int IssueTick { get; set; }
		}
		internal static class UnitTracker
		{
			public static List<UnitTrackerInfo> UnitTrackerInfoList = new List<UnitTrackerInfo>();
			private static List<AIHeroClient> Champion = new List<AIHeroClient>();
			private static List<Spells> spells = new List<Spells>();
			static UnitTracker()
			{
				#region Aatrox
				spells.Add(new Spells() { name = "aatroxq", duration = 1 }); //剑魔Q1~Q3
				spells.Add(new Spells() { name = "aatroxq2", duration = 1 }); //剑魔Q1~Q3
				spells.Add(new Spells() { name = "aatroxq3", duration = 1 }); //剑魔Q1~Q3
				spells.Add(new Spells() { name = "aatroxw", duration = 0.25 }); //剑魔W
				#endregion
				#region Ahri
				spells.Add(new Spells() { name = "AhriOrbofDeception", duration = 0.25 }); //Q
				spells.Add(new Spells() { name = "AhriSeduce", duration = 0.25 }); //E
				#endregion
				#region Akali
				spells.Add(new Spells() { name = "AkaliQ", duration = 0.25 }); //机器人
				#endregion
				#region Akshan
				spells.Add(new Spells() { name = "AkshanQ", duration = 0.25 }); //机器人
				#endregion
				#region Alistar
				spells.Add(new Spells() { name = "Pulverize", duration = 0.25 }); //Q
				#endregion
				#region Amumu
				spells.Add(new Spells() { name = "BandageToss", duration = 0.25 }); //Q
				spells.Add(new Spells() { name = "Tantrum", duration = 0.25 }); //E
				spells.Add(new Spells() { name = "CurseoftheSadMummy", duration = 0.25 }); //R
				#endregion
				#region Anivia
				spells.Add(new Spells() { name = "FlashFrostSpell", duration = 0.25 }); //机器人
				spells.Add(new Spells() { name = "Crystallize", duration = 0.25 }); //机器人
				#endregion
				#region Annie
				spells.Add(new Spells() { name = "AnnieQ", duration = 0.25 }); //Nunu Q
				spells.Add(new Spells() { name = "AnnieW", duration = 0.25 }); //Nunu Q
				spells.Add(new Spells() { name = "AnnieR", duration = 0.25 }); //Nunu Q
				#endregion
				#region Aphelios

				#endregion //未完成
				#region Ashe
				spells.Add(new Spells() { name = "Volley", duration = 0.25 }); //机器人
				spells.Add(new Spells() { name = "EnchantedCrystalArrow", duration = 0.25 }); //机器人
				#endregion
				#region Aurelion Sol
				spells.Add(new Spells() { name = "AurelionSolR", duration = 0.3 }); //R
				#endregion
				#region Azir
				#endregion //未完成

				#region Bard
				spells.Add(new Spells() { name = "BardQ", duration = 0.25 }); //Q
				#endregion
				#region Blitzcreank
				spells.Add(new Spells() { name = "RocketGrab", duration = 0.25 }); //机器人
				#endregion
				#region Brand
				spells.Add(new Spells() { name = "brandq", duration = 0.25 }); //火男Q
				spells.Add(new Spells() { name = "brandw", duration = 0.25 }); //火男W
				spells.Add(new Spells() { name = "brande", duration = 0.25 }); //火男E
				#endregion

				spells.Add(new Spells() { name = "nunuq", duration = 0.5 }); //Nunu Q
				spells.Add(new Spells() { name = "nunur", duration = 1.5 }); //Nunu R

				spells.Add(new Spells() { name = "threshq", duration = 0.75 }); //Thresh Q
				spells.Add(new Spells() { name = "threshe", duration = 0.4 }); //Thresh E

				spells.Add(new Spells() { name = "fiddlesticksw", duration = 1 }); //Fiddle W
				spells.Add(new Spells() { name = "fiddlesticksr", duration = 1 }); //Fiddle R
				spells.Add(new Spells() { name = "staticfield", duration = 0.5 }); //Blitz R
				spells.Add(new Spells() { name = "katarinar", duration = 1 }); //Kata R
				spells.Add(new Spells() { name = "cassiopeiar", duration = 0.5 }); //Cass R
				spells.Add(new Spells() { name = "caitlynpiltoverpeacemaker", duration = 0.35 }); //Caitlyn Q
				spells.Add(new Spells() { name = "velkozr", duration = 0.5 }); //Velkoz R
				spells.Add(new Spells() { name = "jhinr", duration = 2 }); //Jhin R

				spells.Add(new Spells() { name = "ezrealq", duration = 0.25 }); //伊泽瑞尔Q
				spells.Add(new Spells() { name = "ezrealw", duration = 0.25 }); //伊泽瑞尔W
				spells.Add(new Spells() { name = "ezrealr", duration = 1 }); //伊泽瑞尔大招

				spells.Add(new Spells() { name = "jinxw", duration = 0.4 }); //金克斯W
				spells.Add(new Spells() { name = "jinxr", duration = 1 }); //金克斯R

				spells.Add(new Spells() { name = "gate", duration = 1.8 }); //卡牌传送

				spells.Add(new Spells() { name = "quinnr", duration = 2 }); //Ez R
				spells.Add(new Spells() { name = "galior", duration = 1 }); //Galio R
				spells.Add(new Spells() { name = "luxr", duration = 1 }); //Lux R
				spells.Add(new Spells() { name = "LuxLightBinding", duration = 0.25 }); //Lux Q

				spells.Add(new Spells() { name = "reapthewhirlwind", duration = 1 }); //Janna R
				spells.Add(new Spells() { name = "missfortunebullettime", duration = 1 }); //MF R
				spells.Add(new Spells() { name = "shenr", duration = 3 }); //Shen R

				spells.Add(new Spells() { name = "infiniteduress", duration = 1 }); //WW R

				spells.Add(new Spells() { name = "malzaharr", duration = 2.5 }); //Malza R
				spells.Add(new Spells() { name = "lucianq", duration = 0.25 }); //Lucian Q
				spells.Add(new Spells() { name = "lucianw", duration = 0.25 }); //Lucian Q

				foreach (var hero in GameObjects.Get<AIHeroClient>())
				{
					Champion.Add(hero);
					UnitTrackerInfoList.Add(new UnitTrackerInfo() { NetworkId = hero.NetworkId, AaTick = Variables.GameTimeTickCount, StopMoveTick = Variables.GameTimeTickCount, NewPathTick = Variables.GameTimeTickCount, SpecialSpellFinishTick = Variables.GameTimeTickCount, LastInvisableTick = Variables.GameTimeTickCount, IsReborn = false, RebornTime = 0 });
				}
				AIBaseClient.OnBuffRemove += AIHeroClient_OnBuffRemove;
				AIBaseClient.OnDoCast += AIBaseClient_OnProcessSpellCast;
				AIBaseClient.OnNewPath += AIHeroClient_OnNewPath;
				AIBaseClient.OnIssueOrder += OnIssueOrder;
				Game.OnUpdate += Game_OnGameUpdate;
			}
			private static void OnIssueOrder(AIBaseClient sender, AIBaseClientIssueOrderEventArgs args)
			{
				if (sender == null || sender.Type != GameObjectType.AIHeroClient)
					return;
				if (sender.Type == GameObjectType.AIHeroClient)
				{
					if (sender.Team != GameObjects.Player.Team)
					{
						var info = UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId);
						if (info == null)
						{
							return;
						}
						if (args.Order == GameObjectOrder.AttackUnit)
						{
							if (!sender.InAutoAttackRange(args.Target))
							{
								info.IssueTick = Variables.GameTimeTickCount;
							}
						}
					}
				}

			}
			private static void Game_OnGameUpdate(EventArgs args)
			{
				foreach (var hero in Champion)
				{
					if (hero.IsVisible)
					{
						if (hero.Path.Count() > 0)
							UnitTrackerInfoList.Find(x => x.NetworkId == hero.NetworkId).StopMoveTick = Variables.GameTimeTickCount;
					}
					else
					{
						UnitTrackerInfoList.Find(x => x.NetworkId == hero.NetworkId).LastInvisableTick = Variables.GameTimeTickCount;
					}

					var info = UnitTrackerInfoList.Find(x => x.NetworkId == hero.NetworkId);
					if (info.IsReborn && info.RebornTime < Variables.GameTimeTickCount)
					{
						info.IsReborn = false;
						info.RebornTime = 0;
					}
				}

			}
			private static void AIHeroClient_OnBuffRemove(AIBaseClient sender, AIBaseClientBuffRemoveEventArgs args)
			{
				if (sender == null || sender.Type != GameObjectType.AIHeroClient)
					return;
				var ss = sender as AIHeroClient;
				if (args.Buff.Name == "willrevive")
				{
					var info = UnitTrackerInfoList.Find(x => x.NetworkId == ss.NetworkId && !x.IsReborn);
					if (info == null) { return; }
					if (!ss.InFountain())
					{
						info.IsReborn = true;
						info.RebornTime = Variables.GameTimeTickCount + 4000;
					}
				}
			}
			private static void AIHeroClient_OnNewPath(AIBaseClient sender, AIBaseClientNewPathEventArgs args)
			{
				if (sender == null || sender.Type != GameObjectType.AIHeroClient) //|| sender.IsMe
					return;

				var info = UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId);
				if (info == null)
				{ return; }
				info.NewPathTick = Variables.GameTimeTickCount;

				if (args.Path.Last() != sender.ServerPosition)
				{
					info.PathBank.Add(new PathInfo() { Position = args.Path.Last().ToVector2(), Time = Game.Time });
				}

				if (info.PathBank.Count > 3)
				{
					info.PathBank.Remove(info.PathBank.First());
				}
			}
			private static void AIBaseClient_OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
			{
				if (sender == null || sender.Type != GameObjectType.AIHeroClient)
					return;

				var info = UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId);
				if (info == null)
					return;

				if (Orbwalker.IsAutoAttack(args.SData.Name))
				{
					info.AaTick = Variables.GameTimeTickCount;
					info.AaWindingUpEndTime = info.AaTick + (int)(sender.AttackCastDelay * 1000f);
				}
				else
				{
					var foundSpell = spells.Find(x => args.SData.Name.ToLower() == x.name.ToLower());
					if (foundSpell != null)
					{
						UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId).SpecialSpellFinishTick = Variables.GameTimeTickCount + (int)(foundSpell.duration * 1000f);
					}
				}
			}
			public static bool PathCalc(AIBaseClient unit)
			{
				var TrackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
				if (TrackerUnit == null)
					return false;
				if (TrackerUnit.PathBank.Count < 3)
					return false;
				return TrackerUnit.PathBank[2].Time - TrackerUnit.PathBank[0].Time < 0.40f && TrackerUnit.PathBank[2].Time + 0.1f < Game.Time && TrackerUnit.PathBank[2].Time + 0.2f > Game.Time && (TrackerUnit.PathBank[1].Position.Distance(TrackerUnit.PathBank[2].Position) > unit.Distance(TrackerUnit.PathBank[2].Position));
			}
			public static float CheckPathCanBySkillShot(AIBaseClient unit)
			{
				var TrackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
				if (TrackerUnit == null || TrackerUnit.PathBank.Count < 3)
					return 0f;
				//最后一次位置在第一次
				var ordeyby = TrackerUnit.PathBank.OrderByDescending(x => x.Time).ToList();
				return (ordeyby[0].Position.Distance(unit) + ordeyby[1].Position.Distance(unit)) / 2f;
			}
			public static bool CheckPathInOnePos(AIBaseClient unit)
			{
				var TrackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
				if (TrackerUnit.PathBank.Count < 3)
					return false;
				if (TrackerUnit.PathBank[2].Time - TrackerUnit.PathBank[0].Time < 0.8f &&
					TrackerUnit.PathBank[2].Time + 0.1f < Game.Time && TrackerUnit.PathBank[2].Time + 0.2f > Game.Time)
				{
					if (TrackerUnit.PathBank[0].Position.Distance(TrackerUnit.PathBank[1].Position) <= 100 &&
						TrackerUnit.PathBank[1].Position.Distance(TrackerUnit.PathBank[2].Position) <= 100 &&
						TrackerUnit.PathBank[0].Position.Distance(unit) < TrackerUnit.PathBank[1].Position.Distance(unit) &&
						TrackerUnit.PathBank[1].Position.Distance(unit) < TrackerUnit.PathBank[2].Position.Distance(unit))
					{
						return true;
					}
				}
				return false;
			}
			public static List<Vector2> GetPathWayCalc(AIBaseClient unit)
			{
				var TrackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
				Vector2 sr;
				sr.X = (TrackerUnit.PathBank[0].Position.X + TrackerUnit.PathBank[1].Position.X + TrackerUnit.PathBank[2].Position.X) / 4;
				sr.Y = (TrackerUnit.PathBank[0].Position.Y + TrackerUnit.PathBank[1].Position.Y + TrackerUnit.PathBank[2].Position.Y) / 4;
				List<Vector2> points = new List<Vector2>();
				points.Add(sr);
				return points;
			}
			public static int GetRebornEndTime(AIBaseClient unit)
			{
				var info = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId && IsReborn(unit));
				if (info == null)
				{
					return 0;
				}
				return info.RebornTime;
			}
			public static bool IsReborn(AIBaseClient unit)
			{
				var info = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
				if (info == null)
				{
					return false;
				}
				return info.IsReborn && Variables.GameTimeTickCount < info.RebornTime;
			}
			public static double GetSpecialSpellEndTime(AIBaseClient unit)
			{
				var TrackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
				return (TrackerUnit.SpecialSpellFinishTick - Variables.GameTimeTickCount) / 1000d;
			}
			public static double GetLastAutoAttackTime(AIBaseClient unit)
			{
				var TrackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
				return (Variables.GameTimeTickCount - TrackerUnit.AaTick) / 1000d;
			}
			public static double GetLastIssueTime(AIBaseClient unit)
			{
				var TrackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
				return (Variables.GameTimeTickCount - TrackerUnit.IssueTick) / 1000d;
			}
			public static int GetLastAutoAttackWindingUpEndTime(AIBaseClient unit)
			{
				var TrackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
				return TrackerUnit.AaWindingUpEndTime - Variables.GameTimeTickCount;
			}
			public static double GetLastNewPathTime(AIBaseClient unit)
			{
				var TrackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
				if (TrackerUnit == null)
				{
					return 0d;
				}
				return (Variables.GameTimeTickCount - TrackerUnit.NewPathTick) / 1000d;
			}
			public static double GetLastVisableTime(AIBaseClient unit)
			{
				var TrackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);

				return (Variables.GameTimeTickCount - TrackerUnit.LastInvisableTick) / 1000d;
			}
			public static double GetLastStopMoveTime(AIBaseClient unit)
			{
				var TrackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);

				return (Variables.GameTimeTickCount - TrackerUnit.StopMoveTick) / 1000d;
			}
		}
	}
}
