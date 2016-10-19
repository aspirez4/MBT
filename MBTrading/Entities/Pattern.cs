﻿using MBTrading.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MBTrading.Entities
{
    public class Pattern
    {
        public static int OutcomeInterval = 50; // 1
        public static bool TicsDirected = true;
        public static int NumOfTicsSamples = 25;
        public static double SimilarityRate = 0.70;  // 0.7
        public static double SimilarityRateSteps = 0.50;  // 0.2
        public static Dictionary<String, List<Pattern>> AllPatterns;
        /////////////////////////////////////////
        public static double PatternPopularityRatingWeight = 0.6;
        public static double PatternAccuracyRatingWeight = 0.35;
        public static double PatternProfitRatingWeight = 0.05;
        public static double PatternProfitThreshold = 4;
        public static double PatternAccuracyThreshold = 80;
        public static double PatternPopularityThreshold = 20;

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
        public List<ConcurrentDictionary<int, DimensionsData>> FutureDimensions;
        public ConcurrentBag<Pattern> SimPatterns;
        public int? PatternStartHour;
        public int Rating;
        public double Accuracy;
        public double OutcomePrivate;
        public double OutcomeSimPatterns;
        public double FutureMin;
        public double FutureMax;
        public long StartTickIndex;

        public Pattern()
        {
            this.StartTickIndex = 0;
            this.FutureMax = 0;
            this.FutureMin = 0;
            this.PatternStartHour = null;
            this.Rating = 0;
            this.Accuracy = 0;
            this.OutcomePrivate = 0;
            this.OutcomeSimPatterns = 0;
            this.FutureDimensions = new List<ConcurrentDictionary<int, DimensionsData>>();
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
        public static bool Tick(MarketData md, double dLastBid, double dLastAsk, ref List<Pattern> lstSharePatterns, double dPipsUnit, string strSymbol, long lTickIndex)
        {
            int nSmallestDimension = TicsDirected ? 0 : 1;
            double dValue = (dLastBid + dLastAsk) / 2;

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
                        for (int nFuture = Pattern.OutcomeInterval; nFuture > 0; nFuture--)
                            pToFinishilize.FutureDimensions.Add(lstSharePatterns[lstSharePatterns.Count - nFuture].Dimensions);
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
                    currPattern.StartTickIndex = lTickIndex;
                    return true;
                }
            }

            return false;
        }

        public static List<Pattern> BuildPatterns(String strSymbol, long lTickIndex)
        {
            double dTemp;

            Parallel.ForEach(Pattern.AllPatterns[strSymbol], basePattern =>
            {
                basePattern.OutcomeSimPatterns = 0;

                Parallel.ForEach(Pattern.AllPatterns[strSymbol], currPattern =>
                {
                    if (!basePattern.Equals(currPattern) && basePattern.Similar(currPattern, out dTemp))
                    {
                        basePattern.SimPatterns.Add(currPattern);
                        basePattern.OutcomeSimPatterns += currPattern.OutcomePrivate;
                    }
                });
            });


            foreach (Pattern basePattern in Pattern.AllPatterns[strSymbol])
            {
                basePattern.Accuracy = 0;
                if (basePattern.SimPatterns.Count > 0)
                {
                    foreach (Pattern simPattern in basePattern.SimPatterns)
                    {
                        basePattern.Accuracy += basePattern.OutcomeSimPatterns * simPattern.OutcomePrivate > 0 ? 100 : 0;
                    }

                    basePattern.Accuracy /= basePattern.SimPatterns.Count;
                    basePattern.OutcomeSimPatterns /= basePattern.SimPatterns.Count;
                }
            }


            List<Pattern> lstToReturn = relex(strSymbol, 5);  
            List<Pattern> lstNew = new List<Pattern>();
            lstNew.Add(new Pattern());
            lstNew[0].StartTickIndex = lTickIndex;
            Pattern.AllPatterns[strSymbol] = lstNew;

            GC.Collect();

            return lstToReturn;
        }

        public static List<Pattern> relex(string strSymbol, int nWantedSize)
        {
            List<Pattern> lstToRelex = Pattern.AllPatterns[strSymbol];

            double M_Accuracy = 0;
            double M_Profit = 0;
            double M_Popularity = 0;
            double SD_Accuracy = 0;
            double SD_Profit = 0;
            double SD_Popularity = 0;
            List<double> lstAccuracy = new List<double>();
            List<double> lstProfit = new List<double>();
            List<double> lstPopularity = new List<double>();

            foreach (Pattern pCurr in lstToRelex)
            {
                // Only if patten is above thresholds
                if ((pCurr.SimPatterns.Count > PatternPopularityThreshold) &&
                    (pCurr.Accuracy > Pattern.PatternAccuracyThreshold) &&
                    (pCurr.OutcomeSimPatterns * pCurr.OutcomeSimPatterns > Pattern.PatternProfitThreshold))
                {
                    lstAccuracy.Add(pCurr.Accuracy);
                    lstProfit.Add(pCurr.OutcomeSimPatterns * pCurr.OutcomeSimPatterns);
                    lstPopularity.Add(pCurr.SimPatterns.Count);
                }
            }

            SD_Accuracy = MathUtils.GetStandardDeviation(lstAccuracy, out M_Accuracy);
            SD_Profit = MathUtils.GetStandardDeviation(lstProfit, out M_Profit);
            SD_Popularity = MathUtils.GetStandardDeviation(lstPopularity, out M_Popularity);


            Parallel.ForEach(lstToRelex, pCurrForRating =>
            {
                if ((pCurrForRating.SimPatterns.Count > PatternPopularityThreshold) &&
                    (pCurrForRating.Accuracy > Pattern.PatternAccuracyThreshold) &&
                    (pCurrForRating.OutcomeSimPatterns * pCurrForRating.OutcomeSimPatterns > Pattern.PatternProfitThreshold))
                {
                    pCurrForRating.Rating = (int)(
                        (MathUtils.KondratenkoKuperinNormalization(pCurrForRating.Accuracy,M_Accuracy, SD_Accuracy) * Pattern.PatternAccuracyRatingWeight) +
                        (MathUtils.KondratenkoKuperinNormalization(pCurrForRating.OutcomeSimPatterns * pCurrForRating.OutcomeSimPatterns, M_Profit, SD_Profit) * Pattern.PatternProfitRatingWeight) +
                        (MathUtils.KondratenkoKuperinNormalization(pCurrForRating.SimPatterns.Count, M_Popularity, SD_Popularity) * Pattern.PatternPopularityRatingWeight) * 100000000);
                }
            });

            // Sort by patterns ratings
            lstToRelex.Sort((p1, p2) => p2.SimPatterns.Count - p1.SimPatterns.Count);
            lstToRelex.Sort((p1, p2) => p2.Rating - p1.Rating);

            for (int nIndex = 0; nIndex < nWantedSize; nIndex++)
            {
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //StringBuilder strForFile = new StringBuilder("\n");
                //foreach (Pattern ppp in lstToRelex[nIndex].SimPatterns)
                //{
                //    double dlast = 0;
                //    double dlast1 = 0;
                //    double dMin = double.MaxValue;
                //    double dMax = double.MinValue;
                //    if (ppp.Dimensions != null && ppp.FutureDimensions != null)
                //    {
                //        foreach (Params d in ppp.Dimensions[0].Data)
                //        {
                //            strForFile.Append(string.Format("{0:0.000000000},", d.Close));
                //        }
                //        strForFile.Append(",x,");
                //        foreach (ConcurrentDictionary<int, DimensionsData> d in ppp.FutureDimensions)
                //        {
                //            foreach (Params dd in d[0].Data)
                //            {
                //                if (dMin > dd.Close + dlast)
                //                    dMin = dd.Close + dlast;
                //                if (dMax < dd.Close + dlast)
                //                    dMax = dd.Close + dlast;
                //                strForFile.Append(string.Format("{0:0.000000000},", dd.Close + dlast));
                //                dlast1 = dd.Close + dlast;
                //            }
                //            dlast = dlast1;
                //        }
                //        strForFile.Append("\n");
                //        lstToRelex[nIndex].FutureMax = dMax;
                //        lstToRelex[nIndex].FutureMin = dMin;
                //    }
                //}
                //File.AppendAllText(string.Format("C:\\temp\\Or\\x{0}.txt", strSymbol.Remove(3, 1)), strForFile.ToString());
                //File.AppendAllText(string.Format("C:\\temp\\Or\\x{0}.txt", strSymbol.Remove(3, 1)), "\n");
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                foreach (Pattern sim in lstToRelex[nIndex].SimPatterns)
                {
                    sim.claen();
                    lstToRelex.Remove(sim);
                }
            }

            Pattern.claen(lstToRelex, nWantedSize, lstToRelex.Count - 1);
            lstToRelex.RemoveRange(nWantedSize, lstToRelex.Count - nWantedSize);
            
            // Remove rating zero if left
            for (int nRemoveRatingZero = lstToRelex.Count - 1; nRemoveRatingZero >= 0; nRemoveRatingZero--)
            {
                if (lstToRelex[nRemoveRatingZero].Rating == 0)
                    lstToRelex.RemoveAt(nRemoveRatingZero);
            }

            return lstToRelex;
        }
        public static void claen(List<Pattern> lstToClean, int nStartIndex, int nEndIndex)
        {
            Parallel.For(nStartIndex, nEndIndex, nIndex =>
            {
                lstToClean[nIndex].claen();
            });
        }
        public void claen()
        {
            this.FutureDimensions.Clear();
            this.Dimensions = null;
            this.SimPatterns = null;
            this.PatternStartHour = null;
        }
        public bool Similar(Pattern pOther, out double dSimilarity)
        {
            dSimilarity = 0;

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

                    a.Similarity += (1 - dCurr);
                    if (a.Similarity / (nIndex + 1) < Pattern.SimilarityRateSteps)
                        return false;
                }

                a.Similarity /= a.Data.Count;
            }

            double dTotal = 0;
            foreach (DimensionsData dCurrDimension in this.Dimensions.Values)
                dTotal += (dCurrDimension.Similarity * dCurrDimension.Weight);

            dSimilarity = dTotal;

            if (dTotal > Pattern.SimilarityRate)
                return true;
            return false;
        }
    }
}