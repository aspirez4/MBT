using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace MBTrading
{
    public class ChangingPriceForOrder
    {
        public double StrategyStopLossPrice     { get; set; }
        public double ReversalStopLossPrice     { get; set; }
        public double BollingerStopLossPrice    { get; set; }

        public ChangingPriceForOrder()
        {
            this.StrategyStopLossPrice  = 0;
            this.ReversalStopLossPrice  = 0;
            this.BollingerStopLossPrice = 0;
        }
        public double GetStopLossPriceByName(string strActivatingProp)
        {
            return ((double)typeof(ChangingPriceForOrder).GetProperty(strActivatingProp).GetValue(this, null));
        }
    }
}
