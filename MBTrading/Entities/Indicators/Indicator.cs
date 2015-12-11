using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBTrading.Entities.Indicators
{
    public interface Indicator
    {
        void RegisterIndicator(CandlesList clParentCandlesList);
        void NewIndicatorValue();
        void UpdateIndicatorValue();
        void BeforeNewCandleActions(Candle cNewCandle);
        void CompleteInitializationActions();
    }
}
