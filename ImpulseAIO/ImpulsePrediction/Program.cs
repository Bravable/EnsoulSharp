using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnsoulSharp;
using EnsoulSharp.SDK;

namespace ppp
{
    class Program
    {
        public static string[] HookList = new string[] { "Blitzcrank", "Thresh", "Pyke", "Nautilus" };
        public static Dictionary<int, bool> HasHookFlag = new Dictionary<int, bool>();
        public static bool HasHookChamp = false;
        private static int CheckTime = 0;
        public static bool Chinese = true;
        public static void Main(string[] args)
        {
            GameEvent.OnGameLoad += GameLoad;
        }
        private static void GameLoad()
        {
            InitCCTracker();
            Prediction.AddPrediction("GG", new NewPrediction());
            Prediction.SetPrediction("GG");
        }
        public static void InitCCTracker()
        {
            HasHookChamp = GameObjects.Get<AIHeroClient>().Where(x => x.IsAlly && HookList.Any(y => y.Equals(x.CharacterName))).Any();
            foreach (var enemy in GameObjects.Get<AIHeroClient>().Where(x => x.IsEnemy))
            {
                HasHookFlag.Add(enemy.NetworkId, false);
            }
            AIBaseClient.OnBuffAdd += OnBuffAdd_ByCC;
            AIBaseClient.OnBuffRemove += OnBuffRemove_ByCC;
        }
        static int www;
        private static void OnBuffAdd_ByCC(AIBaseClient sender, AIBaseClientBuffAddEventArgs args)
        {
            if (sender.IsEnemy && HasHookChamp)
            {
                if (args.Buff.Name == "Stun")
                {
                    www = Variables.GameTimeTickCount;
                }
                if (args.Buff.Name == "rocketgrab2")
                {
                    HasHookFlag[sender.NetworkId] = true;
                }
                if (args.Buff.Name == "ThreshQ")
                {
                    HasHookFlag[sender.NetworkId] = true;
                }
            }
        }
        private static void OnBuffRemove_ByCC(AIBaseClient sender, AIBaseClientBuffRemoveEventArgs args)
        {
            if (sender.IsEnemy && HasHookChamp)
            {
                if (args.Buff.Name == "rocketgrab2")
                {
                    HasHookFlag[sender.NetworkId] = false;
                }
                if (args.Buff.Name == "ThreshQ")
                {
                    HasHookFlag[sender.NetworkId] = false;
                }
            }
        }
    }
}
