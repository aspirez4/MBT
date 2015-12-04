using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBTrading.Utils
{
    public static class MathUtils
    {
        public static double PercentChange(double dFirst, double dSecond)
        {
            try
            {
                double dRet = ((dSecond - dFirst) / Math.Abs(dFirst));

                if (dRet == 0.0)
                    return (0.000001);
                else if (dFirst == 0)
                    return (dSecond);
                else
                    return (dRet);
            }
            catch
            {
                return (0.000001);
            }
        }
    }
}
