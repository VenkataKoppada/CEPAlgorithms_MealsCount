using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CEPAlgorithms_MealsCount.Classes
{
    public static class GlobalConstants
    {
        // Equivalent of isp_to_free_rate
        public static double IspToFreeRate(double isp)
        {
            double freeRate = isp * CLAIMING_PERC_MULTIPLIER;
            if (freeRate > MAX_CLAIMING_PERCENTAGE) freeRate = MAX_CLAIMING_PERCENTAGE;
            else if (isp < MINIMUM_ISP) freeRate = 0;
            return freeRate;
        }

        // Constants for participation rates
        public static readonly double BreakfastEstParticipation = 0.5; // 50% of total
        public static readonly double LunchEstParticipation = 0.5; // 50% of total
        public const double CLAIMING_PERC_MULTIPLIER = 1.6;
        public const double MAX_CLAIMING_PERCENTAGE = 1.0;
        public const double MINIMUM_ISP = 0.25;
        public const double THRESHOLD_ISP = 0.625;
        public const int NUMBER_OF_SERVING_DAYS = 180;

        public const double FREE_BREAKFAST_RATE = 2.28;
        public const double PAID_BREAKFAST_RATE = 0.38;
        public const double FREE_LUNCH_RATE = 4.27;
        public const double PAID_LUNCH_RATE = 0.42;

    }
}
