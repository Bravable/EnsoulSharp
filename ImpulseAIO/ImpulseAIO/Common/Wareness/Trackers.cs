using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;
using EnsoulSharp.SDK.Rendering.Caches;
using Color = System.Drawing.Color;
using ImpulseAIO.Common;
using SharpDX;
namespace ImpulseAIO.Common.Wareness
{
    internal class CloneTracker
    {
        public CloneTracker()
        {
            Render.OnDraw += Drawing_OnDraw;
        }

        ~CloneTracker()
        {
            Render.OnDraw -= Drawing_OnDraw;
        }

        public bool IsActive()
        {
            return Menus.Tracker.GetActive() && Menus.CloneTracker.GetActive();
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (!IsActive())
                return;

            foreach (AIHeroClient hero in GameObjects.Get<AIHeroClient>())
            {
                if (hero.IsEnemy && !hero.IsDead && hero.IsVisible)
                {
                    if (hero.CharacterName.Contains("Shaco") ||
                        hero.CharacterName.Contains("Leblanc") ||
                        hero.CharacterName.Contains("MonkeyKing") ||
                        hero.CharacterName.Contains("Neeko") ||
                        hero.CharacterName.Contains("Yorick"))
                    {
                        ImpulseAIO.Common.Base.PlusRender.DrawCircle(hero.ServerPosition, hero.BoundingRadius, System.Drawing.Color.Green.ToSharpDxColor());
                    }

                }
            }
        }
    }

    internal class HiddenObject
    {
        public enum ObjectType
        {
            Vision,
            Sight,
            Trap,
            Unknown
        }

        private const int WardRange = 900;
        private const int TrapRange = 300;
        public List<ObjectData> HidObjects = new List<ObjectData>();
        public List<Object> Objects = new List<Object>();
        private FontCache SVTText { get; set; }
        public HiddenObject()
        {
            //普通 黄色守卫
            /*Objects.Add(new Object(ObjectType.Sight, "SightWard", "YellowTrinket", "TrinketTotemLvl1", 60.0f,Color.Yellow));
            Objects.Add(new Object(ObjectType.Sight, "SightWard", "BlueTrinket", "TrinketOrbLvl3", float.MaxValue,Color.Blue));
            Objects.Add(new Object(ObjectType.Sight, "JammerDevice", "JammerDeviceBase", "JammerDevice", float.MaxValue, Color.Red));

            /*Objects.Add(new Object(ObjectType.Vision, "Vision Ward", "VisionWard", "VisionWard", float.MaxValue, 8,
                6424612, Color.BlueViolet));
            Objects.Add(new Object(ObjectType.Sight, "Warding Totem (Trinket)", "YellowTrinket", "TrinketTotemLvl1", 60.0f,
                56, 263796881, Color.Green));
            Objects.Add(new Object(ObjectType.Sight, "Warding Totem (Trinket)", "YellowTrinketUpgrade", "TrinketTotemLvl2", 120.0f,
                56, 263796882, Color.Green));
            Objects.Add(new Object(ObjectType.Sight, "Greater Stealth Totem (Trinket)", "SightWard", "TrinketTotemLvl3",
                180.0f, 56, 263796882, Color.Green));
            Objects.Add(new Object(ObjectType.Sight, "Greater Vision Totem (Trinket)", "VisionWard", "TrinketTotemLvl3B",
                9999.9f, 137, 194218338, Color.BlueViolet));
            Objects.Add(new Object(ObjectType.Sight, "Wriggle's Lantern", "SightWard", "wrigglelantern", 180.0f, 73,
                177752558, Color.Green));
            Objects.Add(new Object(ObjectType.Sight, "Quill Coat", "SightWard", "", 180.0f, 73, 135609454, Color.Green));
            Objects.Add(new Object(ObjectType.Sight, "Ghost Ward", "SightWard", "ItemGhostWard", 180.0f, 229, 101180708,
                Color.Green));

            Objects.Add(new Object(ObjectType.Trap, "Yordle Snap Trap", "Cupcake Trap", "CaitlynYordleTrap", 240.0f, 62,
                176176816, Color.Red));
            Objects.Add(new Object(ObjectType.Trap, "Jack In The Box", "Jack In The Box", "JackInTheBox", 60.0f, 2,
                44637032, Color.Red));
            Objects.Add(new Object(ObjectType.Trap, "Bushwhack", "Noxious Trap", "Bushwhack", 240.0f, 9, 167611995,
                Color.Red));
            Objects.Add(new Object(ObjectType.Trap, "Noxious Trap", "Noxious Trap", "BantamTrap", 600.0f, 48, 176304336,
                Color.Red));*/
            SVTText = TextRender.CreateFont(16);
            //AIBaseClient.OnDoCast += AIBaseClient_OnProcessSpellCast;
            GameObject.OnDelete += Obj_AI_Base_OnDelete;
            Render.OnPresent += Drawing_OnDraw;
            GameObject.OnCreate += GameObject_OnCreate;
            foreach (var obj in ObjectManager.Get<GameObject>())
            {
                GameObject_OnCreate(obj, new EventArgs());
            }
        }
        ~HiddenObject()
        {
            GameObject.OnCreate -= GameObject_OnCreate;
            GameObject.OnDelete -= Obj_AI_Base_OnDelete;
            Render.OnDraw -= Drawing_OnDraw;
        }

        public bool IsActive()
        {
            return Menus.Detector.GetActive() && Menus.VisionDetector.GetActive();
        }

        private void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (!IsActive())
                return;

            if (sender == null || !sender.IsValid)
                return;

            AIBaseClient Trink = sender as AIBaseClient;

            if (Trink != null && Trink.IsValid && GameObjects.Player.Team != sender.Team)
            {
                if (Trink.Name == "SightWard")
                {
                    if (Trink.MaxHealth == 3)
                    {
                        var buff = Trink.GetBuff("relicyellowward");
                        HidObjects.Add(new ObjectData(Color.Yellow, ObjectType.Sight, Trink.Position, buff.EndTime, Trink.Name, null, Trink.NetworkId, Trink));
                    }
                    if (Trink.MaxHealth == 1)
                    {
                        HidObjects.Add(new ObjectData(Color.Blue, ObjectType.Sight, Trink.Position, float.MaxValue, Trink.Name, null, Trink.NetworkId, Trink));
                    }
                }
                if (Trink.Name == "JammerDevice")
                {
                    HidObjects.Add(new ObjectData(Color.Red, ObjectType.Sight, Trink.Position, float.MaxValue, Trink.Name, null, Trink.NetworkId, Trink));
                }
            }
        }
        private void Drawing_OnDraw(EventArgs args)
        {
            if (!IsActive())
                return;
            try
            {
                for (int i = 0; i < HidObjects.Count; i++)
                {
                    ObjectData obj = HidObjects[i];
                    if (Game.Time > obj.EndTime || !obj.ObjPointer.IsValid)
                    {
                        HidObjects.RemoveAt(i);
                        break;
                    }
                    Vector2 objMPos = Drawing.WorldToMinimap(obj.EndPosition);
                    Vector2 objPos = Drawing.WorldToScreen(obj.EndPosition);
                    ImpulseAIO.Common.Base.PlusRender.DrawCircle(obj.EndPosition, 50, obj.ObjColor.ToSharpDxColor());
                    float endTime = obj.EndTime - Game.Time;
                    float m = 0;
                    float s = 0;
                    String endTimee = null;
                    if (!float.IsInfinity(endTime) && !float.IsNaN(endTime) && endTime.CompareTo(float.MaxValue) != 0)
                    {
                        m = (float)Math.Floor(endTime / 60);
                        s = (float)Math.Ceiling(endTime % 60);
                        endTimee = (s < 10 ? m + ":0" + s : m + ":" + s);
                        Drawing.DrawText(objPos[0], objPos[1], obj.ObjColor, endTimee);
                        Drawing.DrawText(objPos[0], objPos[1], obj.ObjColor, endTimee);
                    }
                    switch (obj.ObjType)
                    {
                        case ObjectType.Sight:
                            if (Menus.VisionDetector.GetMenuItem("SAwarenessVisionDetectorDrawRange").GetValue<MenuBool>().Enabled)
                            {
                                var points = GetRotatedFlashPositions(obj.ObjPointer, WardRange, 30, -180, 180);
                                for (int j = 0; j < points.Count - 1; j++)
                                {
                                    Vector2 visionPos1 = Drawing.WorldToScreen(points[j]);
                                    Vector2 visionPos2 = Drawing.WorldToScreen(points[j + 1]);
                                    Drawing.DrawLine(visionPos1.X, visionPos1.Y, visionPos2.X, visionPos2.Y, 1.0f,
                                        Color.White);
                                }
                            }
                            Drawing.DrawText(objMPos.X, objMPos.Y, obj.ObjColor, endTimee + "S");
                            break;
                        case ObjectType.Trap:
                            if (Menus.VisionDetector.GetMenuItem("SAwarenessVisionDetectorDrawRange").GetValue<MenuBool>().Enabled)
                            {
                                ImpulseAIO.Common.Base.PlusRender.DrawCircle(obj.EndPosition, TrapRange, obj.ObjColor.ToSharpDxColor());
                            }
                            Drawing.DrawText(objMPos[0], objMPos[1], obj.ObjColor, endTimee + "T");
                            break;

                        case ObjectType.Vision:
                            if (Menus.VisionDetector.GetMenuItem("SAwarenessVisionDetectorDrawRange").GetValue<MenuBool>().Enabled)
                            {
                                ImpulseAIO.Common.Base.PlusRender.DrawCircle(obj.EndPosition, WardRange, obj.ObjColor.ToSharpDxColor());
                            }
                            Drawing.DrawText(objMPos[0], objMPos[1], obj.ObjColor, endTimee + "V");
                            break;
                        case ObjectType.Unknown:
                            Drawing.DrawLine(Drawing.WorldToScreen(obj.StartPosition), Drawing.WorldToScreen(obj.EndPosition), 1, obj.ObjColor);
                            break;
                    }
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("HiddenObjectDraw: " + ex);
            }
        }
        private List<Vector3> GetRotatedFlashPositions(AIBaseClient uns, float ExtraWidth, int step, int start, int end)
        {
            int currentStep = step;
            var direction = uns.Direction.ToVector2().Perpendicular();
            var list = new List<Vector3>();
            bool flag = false;
            for (var i = start; i <= end; i += currentStep)
            {
                flag = false;
                var angleRad = Geometry.DegreeToRadian(i);
                var rotatedPosition = uns.Position.ToVector2() + ((uns.BoundingRadius + ExtraWidth) * direction.Rotated(angleRad));

                float segment = ExtraWidth / 10;
                for (int b = 1; b <= 10; b++)
                {
                    var extpos = uns.Position.Extend(rotatedPosition.ToVector3(), b * segment);
                    if (extpos.IsWall())
                    {
                        flag = true;
                        list.Add(extpos);
                        break;
                    }

                }
                if (!flag)
                {
                    list.Add(rotatedPosition.ToVector3());
                }
                
            }
            return list;
        }
        private void AIBaseClient_OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (!IsActive())
                return;
            try
            {
                if (!sender.IsValid)
                    return;

                if(args.SData.Name == "TrinketTotemLvl1") //黄色饰品
                {

                }
                if (args.SData.Name == "TrinketOrbLvl3") //蓝色饰品
                {

                }
                if (args.SData.Name == "JammerDevice") //真眼
                {

                }
                Game.Print(args.SData.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine("HiddenObjectSpell: " + ex);
            }
        }
        private bool ObjectExist(Vector3 pos)
        {
            return HidObjects.Any(obj => pos.Distance(obj.EndPosition) < 30);
        }
        private void Obj_AI_Base_OnDelete(GameObject sender, EventArgs args)
        {
            if (!IsActive())
                return;

            if (sender == null || !sender.IsValid)
                return;

            for (int i = 0; i < HidObjects.Count; i++)
            {
                ObjectData obj = HidObjects[i];
                if (obj.NetworkId == sender.NetworkId)
                {
                    HidObjects.RemoveAt(i);
                }
            }
        }
        public class ObjectData
        {
            public String Creator;
            public float EndTime;
            public int NetworkId;
            public System.Drawing.Color ObjColor;
            public ObjectType ObjType;
            public List<Vector2> Points;
            public Vector3 EndPosition;
            public Vector3 StartPosition;
            public AIBaseClient ObjPointer;
            public ObjectData(System.Drawing.Color Color,ObjectType Type, Vector3 endPosition, float endTime, String creator, List<Vector2> points,
                int networkId, AIBaseClient Obj,Vector3 startPosition = new Vector3())
            {
                ObjColor = Color;
                ObjType = Type;
                EndPosition = endPosition;
                EndTime = endTime;
                Creator = creator;
                Points = points;
                NetworkId = networkId;
                StartPosition = startPosition;
                ObjPointer = Obj;
            }
        }
    }
}
