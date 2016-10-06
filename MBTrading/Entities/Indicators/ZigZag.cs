using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MBTrading.Entities.Indicators
{
    public delegate void ZigZagHandler(int nIndex, double dLow);

    public class ZigZag : Indicator
    {
        public event ZigZagHandler ZigZagLowEvent = delegate {};

        public bool         NNActivation = false;
        public CandlesList  ParentCandleList = null;
        public List<Candle> zzSourceList;
        public List<double> ZigZagMap;
        public List<double> HighMap;
        public List<double> LowMap;
        
        int     i               = 0;
        int     counterZ        = 0;
        int     nWhatLookFor    = 0;
        int     nShift          = 0;
        int     back            = 0;
        int     nLastHighIndex  = 0;
        int     nLastLowIndex   = 0;
        double  dValue          = 0;
        double  dRes            = 0;
        double  dCurrLow        = 0;
        double  dCurrHigh       = 0;
        double  dLastHigh       = 0;
        double  dLastLow        = 0;

        public  int Length              = 110;  // Minimum 101
        private int nZigZagCalculationStartIndex;
        private int nUserExtDepth       = 5;    // Origainaly 12
        private int nUserExtDeviation   = 5;
        private int nUserExtBackstep    = 3;
        private int nLevel              = 3;    // recounting depth
        private double dDeviation;              // deviation in points


        public ZigZag(int nZigZagDepth, bool bNNActivation)
        {
            this.NNActivation = bNNActivation;
            this.nUserExtDepth = nZigZagDepth;
            this.zzSourceList = new List<Candle>();
            this.ZigZagMap = new List<double>();
            this.HighMap = new List<double>();
            this.LowMap = new List<double>();        
        }

        public void RegisterIndicator(CandlesList clParentCandlesList)
        {
            // Register inicator
            this.ParentCandleList = clParentCandlesList;
            clParentCandlesList.IndicatorsList.Add(this);

            // Initialize indicator list
            int nStopIndex = Math.Max(0, clParentCandlesList.CountDec - Consts.esMA_PARAMETERS_LENGTH);
            for (int nCounter = 0; nCounter < Length; nCounter++)
            {
                this.zzSourceList.Add(clParentCandlesList.Candles[clParentCandlesList.CountDec]);
                this.ZigZagMap.Add(0);
                this.HighMap.Add(0);
                this.LowMap.Add(0);
            }

            this.nZigZagCalculationStartIndex = this.nUserExtDepth;
            this.dDeviation = this.nUserExtDeviation * (this.ParentCandleList.ParentShare.PipsUnit / 10);
        }

        public void UpdateIndicatorValue()
        {
            //UpdateIndicatorValue();
        }

        public void NewIndicatorValue()
        {
            i               = 0;
            counterZ        = 0;
            nWhatLookFor    = 0;
            nShift          = 0;
            back            = 0;
            nLastHighIndex  = 0;
            nLastLowIndex   = 0;
            dValue          = 0;
            dRes            = 0;
            dCurrLow        = 0;
            dCurrHigh       = 0;
            dLastHigh       = 0;
            dLastLow        = 0;

            #region Find the first index to 100 candles befor or to the thired zigzag from the last - set all to 0
 
            // ZigZag was already counted before
            if (this.nZigZagCalculationStartIndex != nUserExtDepth)
            {
		        // searching third extremum from the last uncompleted bar
                for (i = this.zzSourceList.Count - 1; (i > nUserExtBackstep) && (counterZ < nLevel); i--)
                {
			        if(ZigZagMap[i] != 0) 
			        {
				        counterZ++;
			        }
                }
		
		        i++;
                this.nZigZagCalculationStartIndex = i;
	
		        // what type of exremum we are going to find
		        if ((LowMap[i] != 0) && (ZigZagMap[i] == LowMap[i]))
		        {
                    // searching for next high
			        dCurrLow = LowMap[i];
                    nLastLowIndex = i;
			        nWhatLookFor = 1;
		        }
		        else
		        {
                    // searching for next low
			        dCurrHigh = HighMap[i];
                    nLastHighIndex = i;
			        nWhatLookFor = -1;
		        }
		
		        // chipping
                for (i = this.nZigZagCalculationStartIndex + 1; i < this.zzSourceList.Count; i++)
                {
                    ZigZagMap[i] = 0;
                    LowMap[i] = 0;
                    HighMap[i] = 0;
                }
            }

            #endregion

            #region Find the low and high

            // Searching High and Low
            for (nShift = this.nZigZagCalculationStartIndex; nShift < this.zzSourceList.Count; nShift++)       
            {
                // Low
                dValue = Lowest(this.nUserExtDepth, nShift); 

		        if(dValue == dLastLow) 			
		        {
			        dValue = 0;
                    this.LowMap[nShift] = 0;								 
		        }
		        else
                {
			        dLastLow = dValue;
                    if ((this.zzSourceList[nShift].R_Low - dValue) > dDeviation)                               
			        {	
				        dValue = 0;						 
			        }
			        else
                    {
				        for (back = 1; back <= nUserExtBackstep; back++)
				        {
					        dRes = this.LowMap[nShift - back];
					        if ((dRes != 0) && (dRes > dValue))
					        {
                                this.LowMap[nShift - back] = 0;
					        }
				        }
			        }

                    if (this.zzSourceList[nShift].R_Low == dValue)
                    {
                        this.LowMap[nShift] = dValue;
                    }
                    else
                    {
                        this.LowMap[nShift] = 0;
                    }
                }

                // High
                dValue = Highest(this.nUserExtDepth, nShift);

                if (dValue == dLastHigh)
                {
                    dValue = 0;
                    HighMap[nShift] = 0;
                }
                else
                {
                    dLastHigh = dValue;
                    if ((dValue - this.zzSourceList[nShift].R_High) > dDeviation)
                    {
                        dValue = 0;
                    }
                    else
                    {
                        for (back = 1; back <= nUserExtBackstep; back++)
                        {
                            dRes = HighMap[nShift - back];
                            if ((dRes != 0) && (dRes < dValue))
                            {
                                HighMap[nShift - back] = 0;
                            }
                        }
                    }

                    if (this.zzSourceList[nShift].R_High == dValue)
                    {
                        HighMap[nShift] = dValue;
                    }
                    else
                    {
                        HighMap[nShift] = 0;
                    }
                }
            }

            #endregion

            #region Set the ZigZag map

            // Last preparation
            if (nWhatLookFor == 0) // uncertain quantity
            {
                dLastLow = 0;
                dLastHigh = 0;
            }
            else
            {
                dLastLow = dCurrLow;
                dLastHigh = dCurrHigh;
            }

            // Final rejection
            for (nShift = this.nZigZagCalculationStartIndex; nShift < this.zzSourceList.Count; nShift++)
            {
                dRes = 0;
                switch (nWhatLookFor)
                {
                    case -1: // Search for lawn
                    {
                        if ((HighMap[nShift] != 0) && (HighMap[nShift] > dLastHigh) && (LowMap[nShift] == 0))
                        {
                            ZigZagMap[nLastHighIndex] = 0;
                            nLastHighIndex = nShift;
                            dLastHigh = HighMap[nShift];
                            ZigZagMap[nShift] = dLastHigh;
                            //ZigZagLowEvent(nShift, dLastHigh);
                            dLast = dLastHigh;
                            nLast = 1;
                            shift = nShift;
                        }

                        if ((LowMap[nShift] != 0) && (LowMap[nShift] < dLastHigh) && (HighMap[nShift] == 0))
                        {
                            dLastLow = LowMap[nShift];
                            nLastLowIndex = nShift;
                            ZigZagMap[nShift] = dLastLow;
                            nWhatLookFor = 1;
                            this.ParentCandleList.ParentShare.ZigZagLowEvent(nShift, dLastLow);
                            //ZigZagLowEvent(nShift, dLastLow);
                            dLast = dLastLow;
                            nLast = 0;
                            shift = nShift;
                        }

                        
                        break;
                    }
                    case 0: // Search for peak or lawn
                    {
                        if ((dLastLow == 0) && (dLastHigh == 0))
                        {
                            if (HighMap[nShift] != 0)
                            {
                                dLastHigh = this.zzSourceList[nShift].R_High;
                                nLastHighIndex = nShift;
                                nWhatLookFor = -1;
                                ZigZagMap[nShift] = dLastHigh;
                                //ZigZagLowEvent(nShift, dLastHigh);
                                dLast = dLastHigh;
                                nLast = 1;
                                shift = nShift;
                            }

                            if (LowMap[nShift] != 0)
                            {
                                dLastLow = this.zzSourceList[nShift].R_Low;
                                nLastLowIndex = nShift;
                                nWhatLookFor = 1;
                                ZigZagMap[nShift] = dLastLow;
                                this.ParentCandleList.ParentShare.ZigZagLowEvent(nShift, dLastLow);
                                ZigZagLowEvent(nShift, dLastLow);
                                dLast = dLastLow;
                                nLast = 0;
                                shift = nShift;
                            }
                        }
                        break;
                    }
                    case 1: // Search for peak
                    {
                        if ((LowMap[nShift] != 0) && (LowMap[nShift] < dLastLow) && (HighMap[nShift] == 0))
                        {
                            ZigZagMap[nLastLowIndex] = 0;
                            nLastLowIndex = nShift;
                            dLastLow = LowMap[nShift];
                            ZigZagMap[nShift] = dLastLow;
                            this.ParentCandleList.ParentShare.ZigZagLowEvent(nShift, dLastLow);
                            //ZigZagLowEvent(nShift, dLastLow);
                            dLast = dLastLow;
                            nLast = 0;
                            shift = nShift;
                        }

                        if ((HighMap[nShift] != 0) && (HighMap[nShift] > dLastLow) && (LowMap[nShift] == 0))
                        {
                            dLastHigh = HighMap[nShift];
                            nLastHighIndex = nShift;
                            ZigZagMap[nShift] = dLastHigh;
                            nWhatLookFor = -1;
                            //ZigZagLowEvent(nShift, dLastHigh);
                            dLast = dLastHigh;
                            nLast = 1;
                            shift = nShift;
                        }
                        break;
                    }
                }
            }


            double prev = 0;
            int bNext = -100;
            foreach (double curr in ZigZagMap)
            {
                if (curr != 0)
                {
                    if (prev != 0)
                    {
                        if ((bNext != -100) && (bNext != Convert.ToInt32(curr < prev)))
                        {
                            int n = 9;
                        }

                        bNext = Convert.ToInt32(!(curr < prev));
                    }

                    prev = curr;
                }
            }
            #endregion

 ///////////////////////////////////////           print(this.zzSourceList.Count - this.shift, this.dLast, this.nLast);
            this.nZigZagCalculationStartIndex = 0;
        }

        private double dLast = 0;
        private int nLast = 0;
        private double dPrev = 0;
        private int nPrev = 0;
        private int shift = 0;

        private void print(int nFromEnd, double val, int nDir)
        {
            if (val != dPrev)
            {
                File.AppendAllText(string.Format("C:\\Temp\\Or\\o{1}.txt", Consts.FilesPath, this.ParentCandleList.ParentShare.Symbol.Remove(3, 1)),
                    string.Format("{0};{1};{2};{3};{4}\n", 999, this.ParentCandleList.ParentShare.Symbol, nFromEnd, nDir, this.ParentCandleList.ParentShare.OffLineCandleIndex));
                this.nPrev = nDir;
                this.dPrev = val;
            }
        }
        public void BeforeNewCandleActions(Candle cNewCandle)
        {
            File.AppendAllText(string.Format("C:\\Users\\Or\\Projects\\MBTrading - Graph\\WindowsFormsApplication1\\bin\\x64\\Debug\\b\\o{1}.txt", Consts.FilesPath, this.ParentCandleList.ParentShare.Symbol.Remove(3, 1)),
                string.Format("{0};{1};{2};{3}\n", this.nUserExtDepth ,this.ParentCandleList.ParentShare.Symbol, this.ZigZagMap[0], this.ParentCandleList.ParentShare.OffLineCandleIndex - this.Length + 1));


            this.zzSourceList.RemoveAt(0);
            this.ZigZagMap.RemoveAt(0);
            this.HighMap.RemoveAt(0);
            this.LowMap.RemoveAt(0);
            this.zzSourceList.Add(cNewCandle);
            this.ZigZagMap.Add(0);
            this.HighMap.Add(0);
            this.LowMap.Add(0);
        }
        public void CompleteInitializationActions()
        {
            
        }
        // Searching index of the highest bar From startIndex --> Backwards
        private double Highest(int nDepth, int nStartIndex)
        {
	        // Depth correction if need
	        if (nStartIndex - nDepth < 0) 
	        {
		        nDepth = nStartIndex;
	        }
	
	        double dMaxVal = this.zzSourceList[nStartIndex].R_High;
	
	        // Start searching
	        for (int nIndex = nStartIndex; nIndex > nStartIndex - nDepth; nIndex--)
	        {
                if (this.zzSourceList[nIndex].R_High > dMaxVal)
		        {
                    dMaxVal = this.zzSourceList[nIndex].R_High;
		        }
	        }
	
	        // Return index of the highest bar
            return (dMaxVal);
        }
        // Searching index of the lowest bar From startIndex --> Backwards
        private double Lowest(int nDepth, int nStartIndex)
        {
	        // Depth correction if need
	        if (nStartIndex - nDepth < 0) 
	        {
		        nDepth = nStartIndex;
	        }

            double nMinVal = this.zzSourceList[nStartIndex].R_Low;
	
	        // Start searching
	        for (int nIndex = nStartIndex; nIndex > nStartIndex - nDepth; nIndex--)
	        {
                if (this.zzSourceList[nIndex].R_Low < nMinVal)
		        {
                    nMinVal = this.zzSourceList[nIndex].R_Low;
		        }
	        }
	
	        // Return index of the lowest bar
            return (nMinVal);
        }
    }
}