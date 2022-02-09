using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayBattleRoyal.Managers
{
    public class ColorManager
    {
        public static System.Windows.Media.Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            System.Drawing.Color color = System.Drawing.Color.White;
            if (hi == 0)
                color = System.Drawing.Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                color = System.Drawing.Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                color = System.Drawing.Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                color = System.Drawing.Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                color = System.Drawing.Color.FromArgb(255, t, p, v);
            else
                color = System.Drawing.Color.FromArgb(255, v, p, q);

            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}
