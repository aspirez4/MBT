using System;
using System.Threading;

namespace MBTrading
{
    public static class Time
    {
        public static DateTime EST;

        static Time()
        {
            Time.UpdateTimeMembers();
            new Thread(() => { while (Program.IsProgramAlive) { Thread.Sleep(1000); Time.UpdateTimeMembers(); } }).Start();
        }
        public static void UpdateTimeMembers()
        {
            Time.EST = DateTime.Now;
        }

        public static TimeSpan ESTToWait(int nHour, int nMinute)
        {
            if (nHour > 23)
                nHour = 0;

            // EST = UTC-5
            DateTime dtESTNow = DateTime.Now;
            DateTime dtESTToWaitFor = new DateTime(dtESTNow.Year,
                                                   dtESTNow.Month,
                                                   dtESTNow.Day,
                                                   nHour, nMinute, 0);

            TimeSpan tTimeToWait = dtESTToWaitFor.Subtract(dtESTNow);

            // Set Rollover time
            if (tTimeToWait < new TimeSpan(0))
            {
                DateTime dtTimeToAdd = dtESTNow.AddDays(1);
                dtESTToWaitFor = new DateTime(dtTimeToAdd.Year,
                                              dtTimeToAdd.Month,
                                              dtTimeToAdd.Day,
                                              nHour, nMinute, 0);

                tTimeToWait = dtESTToWaitFor.Subtract(dtESTNow);
            }

            return (tTimeToWait);
        }
        public static bool IsWeekendNow()
        {
            if (Time.EST.DayOfWeek == DayOfWeek.Saturday)
            {
                return (true);
            }
            if (Time.EST.DayOfWeek == DayOfWeek.Friday)
	        {
 	            if (Time.EST.Hour >= 17)
		            return (true);
 	        }
 	        if (Time.EST.DayOfWeek == DayOfWeek.Sunday)
 	        {
                if (Time.EST.Hour < 17)
                    return (true);
                else if ((Time.EST.Hour == 17) && (Time.EST.Minute <= 5))
                    return (true);
	        }

	        return (false);
        }
    }
}
