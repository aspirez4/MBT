using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MBTrading.Entities
{
    public class Pattern
    {
        public static int OutcomeInterval = 1;
        public static bool TicsDirected = true;
        public static int NumOfTicsSamples = 150;
        public static double SimilarityRate = 0.8;  // 0.7
        public static double SimilarityRateSteps = 0;  // 0.2

        public static Dictionary<String, List<Pattern>> AllPatterns;
        static Pattern()
        {
            AllPatterns = new Dictionary<string, List<Pattern>>();
            foreach (String strCurrSymbol in Program.SymbolsNamesList.Keys)
            {
                AllPatterns.Add(strCurrSymbol, new List<Pattern>());
                AllPatterns[strCurrSymbol].Add(new Pattern());
            }
        }

        public class Params
        {
            public int? ParamsStartMinute;
            public double High;
            public double Low;
            public double Open;
            public double Close;
            public double LastPrice;
        }
        public class DimensionsData
        {
            public List<Params> Data;
            public int Count = 0;
            public double Similarity = 0;
            public double Weight = 0;
        }
        public ConcurrentDictionary<int, DimensionsData> Dimensions;
        public ConcurrentDictionary<int, DimensionsData> FutureDimensions;
        public ConcurrentBag<Pattern> SimPatterns;
        public int? PatternStartHour;
        public double Accuracy;
        public double OutcomePrivate;
        public double OutcomeSimPatterns;

        public Pattern()
        {
            this.PatternStartHour = null;
            this.Accuracy = 0;
            this.OutcomePrivate = 0;
            this.OutcomeSimPatterns = 0;
            this.Dimensions = new ConcurrentDictionary<int, DimensionsData>();
            this.SimPatterns = new ConcurrentBag<Pattern>();
            List<Params> lstTics = new List<Params>();
            List<Params> lst60 = new List<Params>(); for (int i = 0; i < 60; i++) lst60.Add(new Params());
            List<Params> lst12 = new List<Params>(); for (int i = 0; i < 12; i++) lst12.Add(new Params());
            List<Params> lst6 = new List<Params>(); for (int i = 0; i < 6; i++) lst6.Add(new Params());
            List<Params> lst4 = new List<Params>(); for (int i = 0; i < 4; i++) lst4.Add(new Params());
            List<Params> lst2 = new List<Params>(); for (int i = 0; i < 2; i++) lst2.Add(new Params());
            DimensionsData T = new DimensionsData(); T.Data = lstTics; T.Weight = TicsDirected ? 1 : 0;//0.2;
            DimensionsData a = new DimensionsData(); a.Data = lst60; a.Weight = TicsDirected ? 0 : 1;//0.2;
            DimensionsData b = new DimensionsData(); b.Data = lst12; b.Weight = 0;//0.3;
            DimensionsData c = new DimensionsData(); c.Data = lst6;  c.Weight = 0;//0.3;
            DimensionsData d = new DimensionsData(); d.Data = lst4;  d.Weight = 0;//0.2;
            DimensionsData e = new DimensionsData(); e.Data = lst2;  e.Weight = 0;//0.1;
            this.Dimensions.TryAdd(0, T);
            this.Dimensions.TryAdd(1, a);
            this.Dimensions.TryAdd(5, b);
            this.Dimensions.TryAdd(10, c);
            this.Dimensions.TryAdd(15, d);
            this.Dimensions.TryAdd(30, e);
        }
        public static double PercentChange(double dFirst, double dSecond)
        {
            try
            {
                double dRet = ((dSecond - dFirst) / Math.Abs(dFirst));

                if (dRet == 0.0)
                    return (0.0000000000001);
                else
                    return (dRet);
            }
            catch
            {
                return (0.0000000000001);
            }
        }

        public static void transform(Pattern currPattern, bool bTicsDirected)
        {
            foreach (int nDimensionKey in currPattern.Dimensions.Keys)
            {
                DimensionsData currDimension = currPattern.Dimensions[nDimensionKey];
                if (!bTicsDirected && nDimensionKey == 0)
                    continue;
                if (currDimension.Weight == 0)
                    continue;

                double dRef = currDimension.Data[0].Open;
                for (int nIndex = 0; nIndex < currDimension.Data.Count; nIndex++)
                {
                    currDimension.Data[nIndex].LastPrice = currDimension.Data[nIndex].Close;
                    currDimension.Data[nIndex].Open = Pattern.PercentChange(dRef, currDimension.Data[nIndex].Open);
                    currDimension.Data[nIndex].High = Pattern.PercentChange(dRef, currDimension.Data[nIndex].High);
                    currDimension.Data[nIndex].Low = Pattern.PercentChange(dRef, currDimension.Data[nIndex].Low);
                    currDimension.Data[nIndex].Close = Pattern.PercentChange(dRef, currDimension.Data[nIndex].Close);
                }
            }
        }

        public static void Tick(MarketData md, ref List<Pattern> lstSharePatterns, double dPipsUnit)
        {
            int nSmallestDimension = TicsDirected ? 0 : 1;
            double dValue = md.Value;
            if (md.DataType == MarketDataType.Volume)
            {
                dValue = md.Price;
            }

            if (dValue != -1)
            {
                Pattern currPattern = lstSharePatterns[lstSharePatterns.Count - 1];

                foreach (int currKey in currPattern.Dimensions.Keys)
                {
                    DimensionsData currDimension = currPattern.Dimensions[currKey];

                    if (currPattern.PatternStartHour == null)
                        currPattern.PatternStartHour = md.Time.HourPrimery;

                    if (currDimension.Weight == 0)
                        continue;

                  



                    
                    if (currKey != 0)
                    {
                        if ((md.Time.MinutePrimery % currKey == 0) || (md.Time.MinutePrimery / currKey != currDimension.Count - 1) || (currDimension.Count == 0))
                        {
                            int nDimmensionCount = currDimension.Count;
                            for (int nCount = 0; nCount < ((md.Time.MinutePrimery / currKey) - nDimmensionCount) + 1; nCount++)
                            {
                                Params pNewParam = currDimension.Data.ElementAt((md.Time.MinutePrimery / currKey) - nCount);
                                pNewParam.ParamsStartMinute = md.Time.MinutePrimery;
                                pNewParam.Open = dValue;
                                pNewParam.High = dValue;
                                pNewParam.Low = dValue;
                                pNewParam.Close = dValue;
                                currDimension.Count++;
                            }
                        }

                        Params pLast = currDimension.Data.ElementAt(md.Time.MinutePrimery / currKey);
                        pLast.Close = dValue;
                        if (pLast.High < dValue)
                            pLast.High = dValue;
                        if (pLast.Low > dValue)
                            pLast.Low = dValue;
                    }
                    else if (TicsDirected)
                    {
                        Params pNewParam = new Params();
                        pNewParam.ParamsStartMinute = md.Time.MinutePrimery;
                        pNewParam.Open = dValue;
                        pNewParam.High = dValue;
                        pNewParam.Low = dValue;
                        pNewParam.Close = dValue;
                        currDimension.Count++;
                        currDimension.Data.Add(pNewParam);
                    }








                    if ((!TicsDirected && currPattern.PatternStartHour != md.Time.HourPrimery) ||
                      (TicsDirected && currPattern.Dimensions[nSmallestDimension].Data.Count == NumOfTicsSamples))
                    {
                        // Complete uncompleted data
                        foreach (DimensionsData lastDimension in currPattern.Dimensions.Values)
                        {
                            if (lastDimension.Weight == 0)
                                continue;

                            for (int nFillCounter = lastDimension.Count; nFillCounter < lastDimension.Data.Count; nFillCounter++)
                            {
                                double dLastClose = lastDimension.Data[nFillCounter - 1].Close;
                                lastDimension.Data[nFillCounter].Open = dLastClose;
                                lastDimension.Data[nFillCounter].High = dLastClose;
                                lastDimension.Data[nFillCounter].Low = dLastClose;
                                lastDimension.Data[nFillCounter].Close = dLastClose;
                                lastDimension.Count++;
                            }
                        }

                        // If there is more patterns than Outcome interval - Calculate outcomes
                        if (lstSharePatterns.Count > Pattern.OutcomeInterval)
                        {
                            Pattern pToFinishilize = lstSharePatterns[lstSharePatterns.Count - (Pattern.OutcomeInterval + 1)];
                            pToFinishilize.FutureDimensions = lstSharePatterns[lstSharePatterns.Count - 1].Dimensions;
                            List<Params> futureOneMinuteData = lstSharePatterns[lstSharePatterns.Count - 1].Dimensions[nSmallestDimension].Data;
                            pToFinishilize.OutcomePrivate = (((futureOneMinuteData[futureOneMinuteData.Count - 5].Close +
                                                               futureOneMinuteData[futureOneMinuteData.Count - 4].Close +
                                                               futureOneMinuteData[futureOneMinuteData.Count - 3].Close +
                                                               futureOneMinuteData[futureOneMinuteData.Count - 2].Close +
                                                               futureOneMinuteData[futureOneMinuteData.Count - 1].Close) / 5) -
                                                               pToFinishilize.Dimensions[nSmallestDimension].Data.Last().LastPrice) / dPipsUnit;
                        }

                        // Transform all data to PercentChange data
                        Pattern.transform(currPattern, TicsDirected);

                        // Add next pattern and update references
                        currPattern = new Pattern();
                        currDimension = currPattern.Dimensions[currKey];
                        lstSharePatterns.Add(currPattern);
                    }
                }
            }
        }

        public static List<Pattern> BuildPatterns(String strSymbol)
        {
            Parallel.ForEach(Pattern.AllPatterns[strSymbol], basePattern =>
            {
                basePattern.OutcomeSimPatterns = 0;

                Parallel.ForEach(Pattern.AllPatterns[strSymbol], currPattern =>
                {
                    if (basePattern.Similar(currPattern))
                    {
                        basePattern.SimPatterns.Add(currPattern);
                        basePattern.OutcomeSimPatterns += currPattern.OutcomePrivate;
                    }
                });
            });


            foreach (Pattern basePattern in Pattern.AllPatterns[strSymbol])
            {
                basePattern.OutcomeSimPatterns /= basePattern.SimPatterns.Count;
                basePattern.Accuracy = 0;

                foreach (Pattern simPattern in basePattern.SimPatterns)
                {
                    basePattern.Accuracy += basePattern.OutcomeSimPatterns * simPattern.OutcomePrivate > 0 ? 100 : 0;
                }

                basePattern.Accuracy /= basePattern.SimPatterns.Count;
            }


            List<Pattern> lstToReturn = relex(strSymbol, 5);  
            List<Pattern> lstNew = new List<Pattern>();
            lstNew.Add(new Pattern());
            Pattern.AllPatterns[strSymbol] = lstNew;

            GC.Collect();

            return lstToReturn;
        }

        public static List<Pattern> relex(string strSymbol, int nWantedSize)
        {
            List<Pattern> lstToRelex = Pattern.AllPatterns[strSymbol];

            for (int nIndex = 0; nIndex < nWantedSize; nIndex++)
            {
                lstToRelex.Sort((p1, p2) => p2.SimPatterns.Count - p1.SimPatterns.Count);
           
                foreach (Pattern sim in lstToRelex[nIndex].SimPatterns)
                {
                    if (!sim.Equals(lstToRelex[nIndex]))
                    {
                        sim.claen();
                        lstToRelex.Remove(sim);
                    }
                }
            }

            Pattern.claen(lstToRelex, nWantedSize, lstToRelex.Count - 1);
            lstToRelex.RemoveRange(nWantedSize, lstToRelex.Count - nWantedSize);
            
            return lstToRelex;
        }
        public static void claen(List<Pattern> lstToClean, int nStartIndex, int nEndIndex)
        {
            Parallel.For(nStartIndex, nEndIndex, nIndex =>
            //for (int nIndex = nStartIndex; nIndex <= nEndIndex; nIndex++)
            {
                lstToClean[nIndex].claen();
            });
        }
        public void claen()
        {
            //if (this.Dimensions != null)
            //{
            //    foreach (DimensionsData d in this.Dimensions.Values)
            //        d.Data = null;
            //}
            //if (this.FutureDimensions != null)
            //{
            //    foreach (DimensionsData d in this.FutureDimensions.Values)
            //        d.Data = null;
            //}

            this.Dimensions = null;
            this.FutureDimensions = null;
            this.SimPatterns = null;
            this.PatternStartHour = null;
        }
        public bool Similar(Pattern pOther)
        {
            foreach (int nCurrDimension in this.Dimensions.Keys)
            {
                DimensionsData a = this.Dimensions[nCurrDimension];
                DimensionsData b = pOther.Dimensions[nCurrDimension];
                a.Similarity = 0;

                if (a.Weight == 0)
                    continue;
                if (a.Data.Count != b.Data.Count)
                    return false;

                for (int nIndex = 0; nIndex < a.Data.Count; nIndex++)
                {
                    double dCurr = (//0.0 * (Math.Abs(Pattern.PercentChange(a.Data[nIndex].Open, b.Data[nIndex].Open))) +
                                    //0.0 * (Math.Abs(Pattern.PercentChange(a.Data[nIndex].High, b.Data[nIndex].High))) +
                                    //0.0 * (Math.Abs(Pattern.PercentChange(a.Data[nIndex].Low, b.Data[nIndex].Low))) +
                                    1.0 * (Math.Abs(Pattern.PercentChange(a.Data[nIndex].Close, b.Data[nIndex].Close))));

                    if ((1 - dCurr) < Pattern.SimilarityRateSteps)
                        return false;

                    a.Similarity += (1 - dCurr);
                }


                a.Similarity /= a.Data.Count;
            }

            double dTotal = 0;
            foreach (DimensionsData dCurrDimension in this.Dimensions.Values)
                dTotal += (dCurrDimension.Similarity * dCurrDimension.Weight);

            if (dTotal > Pattern.SimilarityRate)
                return true;
            return false;
        }
    }
}
