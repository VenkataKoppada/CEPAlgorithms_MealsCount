using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CEPAlgorithms_MealsCount.Classes
{
    public class Site
    {
        public string Code { get; }
        public int TotalEligible { get; private set; }
        public int TotalEnrolled { get; private set; }
        public int BfastServed { get; private set; }
        public int LunchServed { get; private set; }
        public double Isp { get; private set; }
        public double FreeBreakfastRate { get; private set; }
        public double PaidBreakfastRate { get; private set; }
        public double FreeLunchRate { get; private set; }
        public double PaidLunchRate { get; private set; }
        public double ClaimingPercentage { get; private set; }
        public double ReimbursementAmount { get; private set; } = 0.0;

        public Site(string astrSiteCode, int aintTotalEnrolled, int aintTotalEligible, int aintBfastServed = 0, int aintLunchServed = 0,
                            double adblFreeBreakfastRate = GlobalConstants.FREE_BREAKFAST_RATE, double adlbPaidBreakfastRate = GlobalConstants.PAID_BREAKFAST_RATE,
                            double adblFreeLunchRate = GlobalConstants.FREE_LUNCH_RATE, double adblPaidLunchRate = GlobalConstants.PAID_LUNCH_RATE)
        {
            Code = astrSiteCode;
            TotalEnrolled = aintTotalEnrolled;
            TotalEligible = aintTotalEligible;

            if (TotalEligible > TotalEnrolled)
                TotalEligible = TotalEnrolled;

            BfastServed = aintBfastServed == 0 ? (int)Math.Round(TotalEnrolled * GlobalConstants.BreakfastEstParticipation) : aintBfastServed;
            LunchServed = aintLunchServed == 0 ? (int)Math.Round(TotalEnrolled * GlobalConstants.LunchEstParticipation) : aintLunchServed;

            Isp = TotalEnrolled == 0 ? 0 : Math.Round((double)TotalEligible / TotalEnrolled, 4);

            FreeBreakfastRate = adblFreeBreakfastRate;
            PaidBreakfastRate = adlbPaidBreakfastRate;
            FreeLunchRate = adblFreeLunchRate;
            PaidLunchRate = adblPaidLunchRate;
        }

        public double CalcReimbursement(double adeClaimingPerc, bool ablnSFACertified)
        {
            ReimbursementAmount = 0;
            ClaimingPercentage = adeClaimingPerc;
            if (ClaimingPercentage > 0)
            {
                double result = BfastServed * FreeBreakfastRate * ClaimingPercentage
                                + BfastServed * PaidBreakfastRate * (1 - ClaimingPercentage)
                                + LunchServed * FreeLunchRate * ClaimingPercentage
                                + LunchServed * PaidLunchRate * (1 - ClaimingPercentage);

                if (ablnSFACertified)
                {
                    result += LunchServed * 0.07;
                }

                ReimbursementAmount = Math.Round(result, 2) * GlobalConstants.NUMBER_OF_SERVING_DAYS;
            }
            return ReimbursementAmount;
        }

        public override string ToString()
        {
            return $"  Site: {Code}, Enrolled: {TotalEnrolled}, Eligible: {TotalEligible}, ISP: {Isp * 100:0.00}%, Claiming Perc: {ClaimingPercentage * 100:0.00}%, Reimbursement: {ReimbursementAmount.ToString("C", CultureInfo.CurrentCulture)}";
        }
    }
}
