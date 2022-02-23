using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpaceEngineersVR
{
    static class Globals
    {
        public static readonly Version Version = new Version(0, 1, 0);
        public static readonly Icon Icon = new Icon(Util.GetAssetFolder() + "icon.ico");
        public static readonly string Name = "Space Engineers VR";
    }
}
