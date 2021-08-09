using System;
using EnsoulSharp.SDK;

namespace Overwrite
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad()
        {
            // ts overwrite
            {
                TargetSelector.AddTargetSelector("NewTargetSelector", new NewTargetSelector());
                Console.WriteLine("Add NewTargetSelector");

                TargetSelector.SetTargetSelector("NewTargetSelector");
                Console.WriteLine("Set Global TS to NewTargetSelector");

                TargetSelector.GetTargetSelector("SDK").Dispose();
                Console.WriteLine("Remove SDK TargetSelector");
            }

            // orb overwrite
            {
                Orbwalker.AddOrbwalker("NewOrbwalker", new NewOrbwalker());
                Console.WriteLine("Add NewOrbwalker");

                Orbwalker.SetOrbwalker("NewOrbwalker");
                Console.WriteLine("Set Global Orb to NewOrbwalker");

                Orbwalker.GetOrbwalker("SDK").Dispose();
                Console.WriteLine("Remove SDK Orbwalker");
            }

            // pred overwrite
            {
                Prediction.AddPrediction("NewPrediction", new NewPrediction());
                Console.WriteLine("Add NewPrediction");

                Prediction.SetPrediction("NewPrediction");
                Console.WriteLine("Set Global Pred to NewOrbwalker");

                // we not support remove SDK prediction, you dont need to remove it
            }
        }
    }
}
