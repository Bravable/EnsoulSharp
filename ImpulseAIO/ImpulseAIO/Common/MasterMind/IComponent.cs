using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpulseAIO.Common.MasterMind
{
    public interface IComponent
    {
        bool ShouldLoad(bool isSpectatorMode = false);

        void InitializeComponent();
    }
}
