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

        public static double GetStandardDeviation(List<double> lstSample, out double M)
        {
            double dFirstStandardDeviationOperand = 0;
            double dSecondStandardDeviationOperand = 0;
            double dSum = 0;

            // Calculate avarage and StandardDeviation
            foreach (double dCurr in lstSample)
            {
                dSum += dCurr;
                dFirstStandardDeviationOperand += Math.Pow(dCurr, 2);
            }

            dSecondStandardDeviationOperand = dSum;

            M = dSum / lstSample.Count;
            return (Math.Sqrt(Math.Pow(M, 2) +
                    ((dFirstStandardDeviationOperand -
                    (dSecondStandardDeviationOperand * 2 * M)) /
                    lstSample.Count)));

        }

        public static double KondratenkoKuperinNormalization(double currToNorm, double dMean, double dStandardDeviation)
        {
            return dStandardDeviation == 0 ? 1 : (1 / (1 + Math.Pow(Math.E, (dMean - currToNorm) / dStandardDeviation)));
        }
    }
}
