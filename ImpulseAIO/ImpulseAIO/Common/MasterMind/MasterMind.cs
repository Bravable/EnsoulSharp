using System.IO;
using System.Linq;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using ImpulseAIO.Common.MasterMind.Components;
using SharpDX;

namespace ImpulseAIO.Common.MasterMind
{
    internal class MasterMind : Base
    {
        public static readonly string ConfigFolderPath = Path.Combine(EnsoulSharp.SDK.Core.Config.LogDirectory, "ImpulseMind");
        public static readonly TextureLoader TextureLoader = new TextureLoader();
        public static bool IsSpectatorMode { get; private set; }

        public static Menu Menu { get; private set; }

        private static readonly IComponent[] Components =
        {
            new MapHack(),
        };

        public static void EnableMind()
        {

            // Create the config folder
            Directory.CreateDirectory(ConfigFolderPath);

            // Initialize menu
            
            Menu = new Menu("ImpulseMind", Program.Chinese ? Program.ScriptName + ":地图追踪" : Program.ScriptName + ":MapHack",true).Attach();
            Menu.SetLogo(EnsoulSharp.SDK.Rendering.SpriteRender.CreateLogo(Resource1.MapHack));
            Menu.Attach();
            // Initialize properties
            IsSpectatorMode = false;

            // Initialize components
            foreach (var component in Components.Where(component => component.ShouldLoad(IsSpectatorMode)))
            {
                component.InitializeComponent();
            }

            return;
        }
    }
}
