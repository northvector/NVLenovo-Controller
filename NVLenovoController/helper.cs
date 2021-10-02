using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Bunifu;

namespace NVLenovoController
{
    class helper
    {
        public static void roundAll(Control[] cot, byte size)
        {
            foreach (var control_items in cot)
            {
                foreach (var item in control_items.Controls)
                {
                    Bunifu.Framework.UI.BunifuElipse bround = new Bunifu.Framework.UI.BunifuElipse();
                    bround.TargetControl = ((Control)item);
                    if(item is TextBox || item is ComboBox)
                    {
                        bround.ElipseRadius = 5;
                    }
                    else
                        bround.ElipseRadius = (int)size;
                }
            }
        }
    }
}
