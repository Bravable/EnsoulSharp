using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Rendering;

using Newtonsoft.Json;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using ObjectManager = EnsoulSharp.ObjectManager;
using Rectangle = System.Drawing.Rectangle;
using Sprite = EnsoulSharp.SDK.Rendering.SpriteRender;
using Version = System.Version;

namespace ImpulseAIO.Common.MasterMind.Components
{
    public class TextureLoader : IDisposable
    {
        internal readonly Dictionary<string, Tuple<Bitmap, Texture>> Textures = new Dictionary<string, Tuple<Bitmap, Texture>>();

        public TextureLoader()
        {
            // Listen to reset events to reload the textures
            Drawing.OnPostReset += OnReset;

            // Listen to appdomain unloads or exits to make sure we dispose the textures
            AppDomain.CurrentDomain.DomainUnload += OnAppDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += OnAppDomainUnload;
        }

        /// <summary>
        /// Returns the texture which is indexed by the given key
        /// </summary>
        /// <param name="key">The index key</param>
        /// <returns></returns>
        public Texture this[string key]
        {
            get { return Textures[key].Item2; }
        }

        public Texture Load(Bitmap bitmap, out string uniqueKey)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException("bitmap");
            }

            string unique;
            do
            {
                unique = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            } while (Textures.ContainsKey(unique));

            uniqueKey = unique;
            return Load(unique, bitmap);
        }

        /// <summary>
        /// Loads and converts the given bitmap to a texture
        /// </summary>
        /// <param name="key">The index key</param>
        /// <param name="bitmap">The bitmap to convert and load</param>
        /// <returns>The loaded texture</returns>
        public Texture Load(string key, Bitmap bitmap)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }
            if (Textures.ContainsKey(key))
            {
                throw new ArgumentException(string.Format("The given key '{0}' is already present!", key));
            }
            if (bitmap == null)
            {
                throw new ArgumentNullException("bitmap");
            }

            Textures[key] = new Tuple<Bitmap, Texture>(bitmap, BitmapToTexture(bitmap));

            return Textures[key].Item2;
        }

        /// <summary>
        /// Unloads the texture which is associated with the index key from memory
        /// </summary>
        /// <param name="key">The index key</param>
        /// <returns></returns>
        public bool Unload(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }

            if (Textures.ContainsKey(key))
            {
                Textures[key].Item2.Dispose();
                Textures.Remove(key);
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            foreach (var entry in Textures.Values)
            {
                entry.Item1.Dispose();
                entry.Item2.Dispose();
            }
            Textures.Clear();

            AppDomain.CurrentDomain.DomainUnload -= OnAppDomainUnload;
            AppDomain.CurrentDomain.ProcessExit -= OnAppDomainUnload;
        }

        public static Texture BitmapToTexture(Bitmap bitmap)
        {
            
            return Texture.FromMemory(
                Drawing.Direct3DDevice9,
                (byte[])new ImageConverter().ConvertTo(bitmap, typeof(byte[])),
                bitmap.Width,
                bitmap.Height,
                0,
                Usage.None,
                Format.A1,
                Pool.Managed,
                Filter.Default,
                Filter.Default,
                0);
        }

        internal void OnReset(EventArgs args)
        {
            foreach (var entry in Textures.ToList())
            {
                entry.Value.Item2.Dispose();
                Textures[entry.Key] = new Tuple<Bitmap, Texture>(entry.Value.Item1, BitmapToTexture(entry.Value.Item1));
            }
        }

        internal void OnAppDomainUnload(object sender, EventArgs eventArgs)
        {
            Dispose();
        }
    }
    public sealed class MapHack : IComponent
    {
        internal class UnitTrackerInfo
        {
            public Vector3 face;
        }

        public static readonly string ConfigFile = Path.Combine(MasterMind.ConfigFolderPath, "MapHack.json");
        public static readonly string ChampionImagesFolderPath = Path.Combine(MasterMind.ConfigFolderPath, "ChampionImages");

        private static readonly Version ForceUpdateIconsVersion = new Version(0, 0);

        private const string VersionUrl = "https://ddragon.leagueoflegends.com/api/versions.json";
        private const string ChampSquareUrl = "http://ddragon.leagueoflegends.com/cdn/{0}/img/champion/{1}.png";
        private const string ChampSquarePrefix = "MasterMindMapHack";
        private const string ChampSquareSuffix = ".png";
        private const string ChampSquareMinimapSuffix = "_minimap" + ChampSquareSuffix;

        private const int MinimapIconSize = 25;
        private static readonly Vector2 MinimapIconOffset = new Vector2((int) -Math.Round(MinimapIconSize / 2f));

        private WebClient WebClient { get; set; }
        private string LiveVersionString { get; set; }
        private Version LiveVersion { get; set; }

        private Dictionary<string, Func<Texture>> LoadedChampionTextures { get; set; }
        private Dictionary<string, Sprite.SpriteCache> ChampionSprites { get; set; }
        private TextRender.FontCache TimerText { get; set; }

        public Menu Menu { get; private set; }
        public MenuBool DrawGlobal { get; private set; }
        public MenuBool DrawRecallCircle { get; private set; }
        public MenuBool DrawMovementCircle { get; private set; }
        public MenuBool DrawInvisibleTime { get; private set; }
        public MenuSlider DelayInvisibleTime { get; private set; }
        public MenuSlider RangeCircleDisableRange { get; private set; }

        private Vector3 EnemySpawnPoint { get; set; }

        private HashSet<int> DeadHeroes { get; set; } 
        private Dictionary<int, int> LastSeen { get; set; }
        private Dictionary<int, Vector3> LastSeenPosition { get; set; }
        private Dictionary<int, float> LastSeenRange { get; set; }
        private Dictionary<int, Tuple<int, int>> RecallingHeroes { get; set; }

        private Dictionary<int, Vector3> LastFace { get; set; }

        private int LastUpdate { get; set; }

        public bool ShouldLoad(bool isSpectatorMode = false)
        {
            // Only load when not in spectator mode
            return !isSpectatorMode;
        }



        public void InitializeComponent()
        {
            // Initialize properties
            LoadedChampionTextures = new Dictionary<string, Func<Texture>>();
            ChampionSprites = new Dictionary<string, Sprite.SpriteCache>();
            DeadHeroes = new HashSet<int>();
            LastSeen = new Dictionary<int, int>();
            LastSeenPosition = new Dictionary<int, Vector3>();
            LastFace = new Dictionary<int, Vector3>();
            LastSeenRange = new Dictionary<int, float>();
            EnemySpawnPoint = ObjectManager.Get<Obj_SpawnPoint>().First(o => o.IsEnemy).Position;
            RecallingHeroes = new Dictionary<int, Tuple<int, int>>();
            TimerText = TextRender.CreateFont(16);
            LastUpdate = Variables.GameTimeTickCount;

            #region Menu Creation


            
            Menu = MasterMind.Menu.Add(new Menu("MapHack", Program.Chinese ? "小地图追踪" : "MapHack",true));

            DrawGlobal = Menu.Add(new MenuBool("global", "Enable"));
            DrawRecallCircle = Menu.Add(new MenuBool("recall", Program.Chinese ? "启动 回城追踪" : "Enable Recall Tracker"));
            DrawMovementCircle = Menu.Add(new MenuBool("movement", Program.Chinese ? "启动 移动追踪" : "Enable movement Tracker"));
            DrawInvisibleTime = Menu.Add(new MenuBool("time", Program.Chinese ? "显示敌人已消失秒数" : "Draw enemy un visible time"));

            DelayInvisibleTime = Menu.Add(new MenuSlider("timeDelay", Program.Chinese ? "仅当敌人消失了 x 秒后才显示" : "draw time only enemy un visiable x time", 10, 0, 30));
            RangeCircleDisableRange = Menu.Add(new MenuSlider("disableRange", Program.Chinese ? "当 移动追踪半径 >= X时取消显示" : "Disable enemy draw if movement tracker range >= X", 800, 200, 2000));

            #endregion

            ToMiniMap();

            // Load local champion images
            LoadChampionImages();

            
            // Create sprite objects from the images
            CreateSprites();

            // Listen to required events
            GameEvent.OnGameTick += OnTick;
            Drawing.OnEndScene += OnDraw;
            Teleport.OnTeleport += OnTeleport;
            GameObject.OnCreate += OnCreate;
            AIBaseClient.OnNewPath += OnNewPath;
            // Initialize version download
            WebClient = new WebClient();
            WebClient.DownloadStringCompleted += DownloadVersionCompleted;

            try
            {
                // Download the version from Rito
                WebClient.DownloadStringAsync(new Uri(VersionUrl, UriKind.Absolute));
            }
            catch (Exception)
            {
                ContinueInitialization();
            }
        }
        private void OnNewPath(AIBaseClient sender, AIBaseClientNewPathEventArgs args)
        {
            if (sender == null || sender.Type != GameObjectType.AIHeroClient || !sender.IsEnemy) //|| sender.IsMe
                return;

            if (args.Path.Length != 1)
            {
                LastFace[sender.NetworkId] = args.Path.Last();

                if (LastSeen.ContainsKey(sender.NetworkId))
                {
                    //如果目标不可视时
                    LastSeenPosition[sender.NetworkId] = args.Path.FirstOrDefault();
                }
            }
        }
        private void DrawAngle(Vector2 startPt,Vector2 endPt)
        {
            //箭头的宽
            float width = 10;
            //箭头夹角
            double angle = 60.0 / 180 * Math.PI;

            //求BC长度
            double widthBE = width / 2 / (Math.Tan(angle / 2));

            //直线向量
            Vector2 lineVector = new Vector2(endPt.X - startPt.X, endPt.Y - startPt.Y);
            //单位向量
            lineVector.Normalize();

            //求BE向量
            Vector2 beVector = (float)widthBE * -lineVector;

            //求E点坐标
            Vector2 ePt = new Vector2();
            //ePt - endPt = bcVector
            ePt.X = endPt.X + beVector.X;
            ePt.Y = endPt.Y + beVector.Y;

            //因为CD向量和AB向量垂直,所以CD方向向量为
            Vector2 cdVector = new Vector2(-lineVector.Y, lineVector.X);
            //求单位向量
            cdVector.Normalize();

            //求CE向量
            Vector2 ceVector = width / 2 * cdVector;
            //求C点坐标,ePt - cPt = ceVector;
            Vector2 cPt = new Vector2();
            cPt.X = ePt.X - ceVector.X;
            cPt.Y = ePt.Y - ceVector.Y;

            //求DE向量
            Vector2 deVector = width / 2 * -cdVector;
            //求D点,ePt-dPt = deVector;
            Vector2 dPt = new Vector2();
            dPt.X = ePt.X - deVector.X;
            dPt.Y = ePt.Y - deVector.Y;

            //绘制线
            LineRender.Draw(Utilities.LinePen, startPt, endPt, SharpDX.Color.Red, 2);
            LineRender.Draw(Utilities.LinePen, endPt, cPt, SharpDX.Color.Red, 2);
            LineRender.Draw(Utilities.LinePen, endPt, dPt, SharpDX.Color.Red, 2);
        }

        private void OnTick(EventArgs args)
        {
            // Time elapsed since last update
            var elapsed = Variables.GameTimeTickCount - LastUpdate;
            LastUpdate = Variables.GameTimeTickCount;

            foreach (var enemy in GameObjects.Get<AIHeroClient>().Where(x => x.IsEnemy))
            {
                // Check if hero is dead
                if (enemy.IsDead && !DeadHeroes.Contains(enemy.NetworkId))
                {
                    DeadHeroes.Add(enemy.NetworkId);
                }

                // Check if hero was dead but respawned
                if (!enemy.IsDead && DeadHeroes.Contains(enemy.NetworkId))
                {
                    DeadHeroes.Remove(enemy.NetworkId);

                    LastSeen[enemy.NetworkId] = Variables.GameTimeTickCount;
                    LastSeenPosition[enemy.NetworkId] = EnemySpawnPoint;
                    LastSeenRange[enemy.NetworkId] = 0;
                    LastFace.Remove(enemy.NetworkId);
                }

                // Update last seen range
                if (elapsed > 0 && LastSeenRange.ContainsKey(enemy.NetworkId) && !RecallingHeroes.ContainsKey(enemy.NetworkId))
                {
                    LastSeenRange[enemy.NetworkId] = LastSeenRange[enemy.NetworkId] + (enemy.MoveSpeed > 1 ? enemy.MoveSpeed : 540) * elapsed / 1000f;
                }

                if (enemy.InRange(EnemySpawnPoint, 250))
                {
                    LastSeenPosition[enemy.NetworkId] = EnemySpawnPoint;
                    LastFace.Remove(enemy.NetworkId);
                }

                if (enemy.IsHPBarRendered || enemy.IsVisible)
                {
                    // Remove from last seen
                    LastSeen.Remove(enemy.NetworkId);
                    LastSeenPosition.Remove(enemy.NetworkId);
                    //LastFace.Remove(enemy.NetworkId);
                }
                else
                {
                    if (!LastSeen.ContainsKey(enemy.NetworkId))
                    {
                        // Add to last seen
                        LastSeen.Add(enemy.NetworkId, Variables.GameTimeTickCount);
                        LastSeenPosition[enemy.NetworkId] = enemy.Position;
                        LastSeenRange[enemy.NetworkId] = 0;
                    }
                }
            }
        }

        private void OnDraw(EventArgs args)
        {
            if (!DrawGlobal.Enabled)
            {
                // Complete drawing turned off
                return;
            }

            foreach (var enemy in GameObjects.Get<AIHeroClient>().Where(o => o.IsEnemy && !o.IsDead || o.InRange(EnemySpawnPoint, 250)))
            {
                // Get the minimap position
                
                var pos = Drawing.WorldToMinimap(enemy.Position);

                if (LastSeen.ContainsKey(enemy.NetworkId))
                {
                    // Update the position
                    pos = Drawing.WorldToMinimap(LastSeenPosition[enemy.NetworkId]);

                    // Get the time being invisible in seconds
                    var invisibleTime = (Variables.GameTimeTickCount - LastSeen[enemy.NetworkId]) / 1000f;

                    // Predicted movement circle
                    if (DrawMovementCircle.Enabled)
                    {
                        // Get the radius the champ could have walked
                        var radius = LastSeenRange.ContainsKey(enemy.NetworkId) ? LastSeenRange[enemy.NetworkId] : (enemy.MoveSpeed > 1 ? enemy.MoveSpeed : 540) * invisibleTime;

                        // Don't roast toasters
                        if (radius < RangeCircleDisableRange.Value * 10)
                        {
                            Utilities.DrawCricleMinimap(pos, radius * Utilities.MinimapMultiplicator, Color.Red, 1, 500);
                        }
                    }

                    // Draw the minimap icon
                    if (LastFace.ContainsKey(enemy.NetworkId))
                    {
                        var facePos = LastFace[enemy.NetworkId];
                        var ExtendFace = enemy.Position.Extend(facePos, 1100f);
                        Vector2 LineStart = pos;
                        Vector2 LineEnd = Drawing.WorldToMinimap(ExtendFace);
                        DrawAngle(LineStart, LineEnd);
                        //LineRender.Draw(Utilities.LinePen, LineStart, LineEnd, SharpDX.Color.Red,2);
                        
                    }
                    ChampionSprites[enemy.CharacterName].Draw(pos + MinimapIconOffset);

                    // Draw the time being invisible
                    if (DrawInvisibleTime.Enabled && invisibleTime >= DelayInvisibleTime.Value)
                    {
                        var text = Math.Floor(invisibleTime).ToString(CultureInfo.InvariantCulture);
                        var bounding = Drawing.GetTextExtent(text);

                        TimerText.Draw(text,pos - (new Vector2(bounding.Width, bounding.Height) / 2) + 1,SharpDX.Color.Red);
                    }
                }

                // Draw recall circle
                if (DrawRecallCircle.Enabled && RecallingHeroes.ContainsKey(enemy.NetworkId))
                {
                    var startTime = RecallingHeroes[enemy.NetworkId].Item1;
                    var duration = RecallingHeroes[enemy.NetworkId].Item2;

                    Utilities.DrawArc(pos, (MinimapIconSize + 4) / 2f, Color.Aqua, 3.1415f, Utilities.PI2 * ((Variables.GameTimeTickCount - startTime) / (float) duration), 2f, 100);
                }
            }
        }

        private void OnTeleport(AIBaseClient sender, Teleport.TeleportEventArgs args)
        {
            // Only check for enemy Heroes and recall teleports
            if (sender.Type == GameObjectType.AIHeroClient && sender.IsEnemy && args.Type == Teleport.TeleportType.Recall)
            {
                switch (args.Status)
                {
                    case Teleport.TeleportStatus.Start:
                        RecallingHeroes[sender.NetworkId] = new Tuple<int, int>(Variables.GameTimeTickCount, args.Duration);
                        break;

                    case Teleport.TeleportStatus.Abort:
                        RecallingHeroes.Remove(sender.NetworkId);
                        break;

                    case Teleport.TeleportStatus.Finish:
                        LastSeen[sender.NetworkId] = Variables.GameTimeTickCount;
                        LastSeenPosition[sender.NetworkId] = EnemySpawnPoint;
                        LastSeenRange[sender.NetworkId] = 0;
                        LastFace.Remove(sender.NetworkId);
                        RecallingHeroes.Remove(sender.NetworkId);
                        break;
                }
            }
        }

        private void OnCreate(GameObject sender, EventArgs args)
        {
            // Check only enemy MissileClient
            if (sender.Type == GameObjectType.MissileClient)
            {
                // Validate missile
                var missile = (MissileClient) sender;
                if (missile.SpellCaster != null && missile.SpellCaster.Type == GameObjectType.AIHeroClient && missile.SpellCaster.IsEnemy && !missile.StartPosition.IsZero)
                {
                    // Set last seen position
                    LastSeen[sender.NetworkId] = Variables.GameTimeTickCount;
                    LastSeenPosition[sender.NetworkId] = missile.StartPosition;
                    LastSeenRange[sender.NetworkId] = 0;
                    LastFace[sender.NetworkId] = missile.Position;
                }
            }
        }

        private void LoadChampionImages()
        {
            // Load the unknown champ icon
            string unknownTextureKey;
            MasterMind.TextureLoader.Load(TransformToMinimapIcon(Resource1.UnknownChamp), out unknownTextureKey);

            // Load the current champions
            if (Directory.Exists(ChampionImagesFolderPath))
            {
                
                foreach (var champion in GameObjects.Get<AIHeroClient>().Where(x => x.IsEnemy).Where(champion => !LoadedChampionTextures.ContainsKey(champion.CharacterName)))
                {
                    var championName = champion.CharacterName;

                    // Check if file for champ exists
                    var filePath = Path.Combine(ChampionImagesFolderPath, championName + ChampSquareMinimapSuffix);
                    if (!File.Exists(filePath))
                    {
                        // Use unknown champ image
                        LoadedChampionTextures.Add(champion.CharacterName, () => MasterMind.TextureLoader[unknownTextureKey]);
                        continue;
                    }

                    // Load local image
                    Bitmap champIcon;
                    try
                    {
                        using (var bmpTemp = new Bitmap(filePath))
                        {
                            champIcon = new Bitmap(bmpTemp);
                        }
                    }
                    catch (Exception e)
                    {
                        Game.Print(e.ToString());
                        File.Delete(filePath);

                        // Use unknown champ image
                        LoadedChampionTextures.Add(champion.CharacterName, () => MasterMind.TextureLoader[unknownTextureKey]);
                        continue;
                    }

                    MasterMind.TextureLoader.Load(ChampSquarePrefix + championName, champIcon);
                    LoadedChampionTextures.Add(champion.CharacterName, () => MasterMind.TextureLoader[ChampSquarePrefix + championName]);
                }
            }
            else
            {
                // No champion images exist, use unknown image
                foreach (var champion in GameObjects.Get<AIHeroClient>().Where(x => x.IsEnemy).Where(champion => !LoadedChampionTextures.ContainsKey(champion.CharacterName)))
                {
                    LoadedChampionTextures.Add(champion.CharacterName, () => MasterMind.TextureLoader[unknownTextureKey]);
                }
            }
        }

        private void CreateSprites()
        {
            // Create a sprite object for each champion loaded
            foreach (var textureEntry in LoadedChampionTextures)
            {
                var key = textureEntry.Key;
                
                ChampionSprites[key] = SpriteRender.CreateSprite(LoadedChampionTextures[key]());
            }
        }

        private async void ContinueInitialization()
        {
            await Task.Run(() =>
            {
                // Dispose the WebClient
                WebClient.Dispose();
                WebClient = null;

                // Create config file if not existing
                if (!File.Exists(ConfigFile))
                {
                    File.Create(ConfigFile).Close();
                }

                // Open the json file
                var config = JsonConvert.DeserializeObject<MapHackConfig>(File.ReadAllText(ConfigFile)) ?? new MapHackConfig();

                #region Checking Version

                // Helpers
                var downloadImages = false;
                var updateMinimapIcons = false;

                // Check for the version
                if (config.Version == null && LiveVersion == null)
                {
                    Game.Print("[Impulse] Can't continue initialization of MapHack due to failed version download and no local files.");
                }
                else if (LiveVersion == null)
                {
                    Game.Print("[Impulse] Version check failed, using local cached images.");
                }
                else if (config.Version != null)
                {
                    // Compare versions
                    if (config.Version < LiveVersion)
                    {
                        // Update the version
                        config.Version = LiveVersion;

                        Game.Print("[Impulse] Redownloading champion images due to League update.");
                        downloadImages = true;
                    }
                }
                else
                {
                    // Update the version
                    config.Version = LiveVersion;

                    Game.Print("[Impulse] Downloading champion images for the first time.");
                    downloadImages = true;
                }

                // Check if a forced update of the minimap icon is needed
                if (config.ForceUpdateIconsVersion != null && config.ForceUpdateIconsVersion < ForceUpdateIconsVersion)
                {
                    Game.Print("[Impulse] Updating minimap icons.");
                    updateMinimapIcons = true;
                }

                #endregion

                // Update config values
                config.ForceUpdateIconsVersion = ForceUpdateIconsVersion;

                // Save the json file
                File.WriteAllText(ConfigFile, JsonConvert.SerializeObject(config));

                // Download images and update icons if needed
                if (downloadImages || updateMinimapIcons)
                {
                    if (downloadImages)
                    {
                        // Wait till all images have been downloaded
                        DownloadChampionImages().Wait();
                        Game.Print("[Impulse] Download of champion images completed.");
                    }
                    // Update minimap icons
                    UpdateMinimapIcons();
                }
            });
        }

        private async Task DownloadChampionImages()
        {
            // Create the champion images folder
            Directory.CreateDirectory(ChampionImagesFolderPath);
            
            await Task.Run(() =>
            {
                // Redownload all images
                using (WebClient = new WebClient())
                {
                    foreach (var champion in GameObjects.Get<AIHeroClient>().Where(x => x.IsEnemy))
                    {
                        try
                        {
                            //http://ddragon.leagueoflegends.com/cdn/12.3.1/img/champion/Zeri.png
                            var filePath = Path.Combine(ChampionImagesFolderPath, champion.CharacterName + ChampSquareSuffix);
                            if (!File.Exists(filePath))
                            {
                                // Download the image of the champion
                                WebClient.DownloadFile(new Uri(string.Format(ChampSquareUrl, LiveVersionString, champion.CharacterName), UriKind.Absolute), filePath);
                            }
                        }
                        catch (Exception)
                        {
                            Game.Print("[Impulse] Failed to download champion image of {0}!", new object[] { champion.CharacterName });
                        }
                    }
                }
            });
        }
        private void ToMiniMap()
        {
            var filePath = Path.Combine(ChampionImagesFolderPath, "Renata" + ChampSquareSuffix);

            // Load the image as bitmap
            using (var bitmap = (Bitmap)Image.FromFile(filePath))
            {
                // Transform the image into a minimap icon
                var minimapIcon = TransformToMinimapIcon(bitmap);

                // Save the icon to file
                minimapIcon.Save(Path.Combine(ChampionImagesFolderPath, "Renata" + ChampSquareMinimapSuffix), ImageFormat.Png);

                // Replace the current icon
                ReplaceChampionImage("Renata", minimapIcon);
            }
        }
        private async void UpdateMinimapIcons()
        {
            // Create the champion images folder
            Directory.CreateDirectory(ChampionImagesFolderPath);

            await Task.Run(() =>
            {
                foreach (var champion in GameObjects.Get<AIHeroClient>().Where(x => x.IsEnemy))
                {
                    try
                    {
                        var filePath = Path.Combine(ChampionImagesFolderPath, champion.CharacterName + ChampSquareSuffix);

                        // Load the image as bitmap
                        using (var bitmap = (Bitmap) Image.FromFile(filePath))
                        {
                            // Transform the image into a minimap icon
                            var minimapIcon = TransformToMinimapIcon(bitmap);

                            // Save the icon to file
                            minimapIcon.Save(Path.Combine(ChampionImagesFolderPath, champion.CharacterName + ChampSquareMinimapSuffix), ImageFormat.Png);

                            // Replace the current icon
                            ReplaceChampionImage(champion.CharacterName, minimapIcon);
                        }
                    }
                    catch (Exception)
                    {
                        Game.Print("[Impulse] Failed to update minimap icon for {0}!",new object[] { champion.CharacterName });
                    }
                }
            });
        }

        private void ReplaceChampionImage(string champion, Bitmap minimapIcon)
        {
            // Replace images in sync
            DelayAction.Add(100,() =>
            {
                if (LoadedChampionTextures.ContainsKey(champion))
                {
                    // Unload the current texture
                    MasterMind.TextureLoader.Unload(ChampSquarePrefix + champion);

                    // Load the new texture
                    MasterMind.TextureLoader.Load(ChampSquarePrefix + champion, minimapIcon);

                    // Replace the texture
                    LoadedChampionTextures[champion] = () => MasterMind.TextureLoader[ChampSquarePrefix + champion];
                }
            });
        }

        private void DownloadVersionCompleted(object sender, DownloadStringCompletedEventArgs args)
        {
            if (args.Cancelled || args.Error != null)
            {
                Game.Print("[Impulse] Error while downloading the versions, cancelled.");
                if (args.Error != null)
                {
                    Game.Print(args.Error.ToString());
                    ContinueInitialization();
                    return;
                }
            }

            try
            {
                // Get the current version as string
                LiveVersionString = JsonConvert.DeserializeObject<List<string>>(args.Result)[0];

                // Parse the current version
                LiveVersion = new Version(LiveVersionString);
            }
            catch (Exception e)
            {
                Game.Print("[Impulse] Error while parsing the downloaded version string.");
                Game.Print(e.ToString());
            }

            ContinueInitialization();
        }

        // Credits to "Christian Brutal Sniper" (https://www.EnsoulSharp.net/user/16502-/)
        // Adjusted to fit my needs
        public static Bitmap TransformToMinimapIcon(Bitmap source, int iconSize = MinimapIconSize)
        {
            var tempBtm = new Bitmap(source.Width + 4, source.Height + 4);
            var finalBitmap = new Bitmap(iconSize, iconSize);

            using (var g = Graphics.FromImage(source))
            {
                using (Brush brsh = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                {
                    g.FillRectangle(brsh, new Rectangle(0, 0, source.Width, source.Height));
                }
            }
            using (var g = Graphics.FromImage(tempBtm))
            {
                using (Brush brsh = new SolidBrush(Color.Red))
                {
                    g.FillEllipse(brsh, 2, 2, source.Width - 4, source.Height - 4);
                }
                using (Brush brsh = new TextureBrush(source))
                {
                    g.FillEllipse(brsh, 6, 6, source.Width - 12, source.Height - 12);
                }
            }
            using (var g = Graphics.FromImage(finalBitmap))
            {
                g.InterpolationMode = InterpolationMode.High;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawImage(tempBtm, new Rectangle(0, 0, iconSize, iconSize));
            }
            tempBtm.Dispose();

            return finalBitmap;
        }
    }

    [DataContract]
    public class MapHackConfig
    {
        [DataMember]
        public string VersionString { get; set; }
        [DataMember]
        public string ForceUpdateIconsString { get; set; }

        private Version _version;
        public Version Version
        {
            get { return VersionString == null ? null : _version ?? (_version = new Version(VersionString)); }
            set
            {
                VersionString = value.ToString();
                _version = value;
            }
        }

        private Version _forceUpdateIconsVersion;
        public Version ForceUpdateIconsVersion
        {
            get { return ForceUpdateIconsString == null ? null : _forceUpdateIconsVersion ?? (_forceUpdateIconsVersion = new Version(ForceUpdateIconsString)); }
            set
            {
                ForceUpdateIconsString = value.ToString();
                _forceUpdateIconsVersion = value;
            }
        }
    }
}
