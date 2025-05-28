using CEPAlgorithms_MealsCount.Algorithms;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CEPAlgorithms_MealsCount.Classes
{
    public class CEPGroup
    {
        public Sponsor District { get; }
        public string Name { get; }
        public List<Site> Sites { get; }
        public CEPGroup(Sponsor district, string groupName, List<Site> sites)
        {
            District = district;
            Name = groupName;
            Sites = sites;
        }

        public HashSet<string> SiteCodes => new HashSet<string>(Sites.Select(s => s.Code));
        public int TotalEligible => Sites.Sum(s => s.TotalEligible);
        public int TotalEnrolled => Sites.Sum(s => s.TotalEnrolled);
        public double Isp => TotalEnrolled == 0 ? 0 : Math.Round((double)TotalEligible / TotalEnrolled, 4);
        public double FreeRate => GlobalConstants.IspToFreeRate(Isp);
        public int CoveredStudents => (int)Math.Round(FreeRate * TotalEnrolled);

        public bool CepEligible => FreeRate > 0;

        public int DailyLunchServed => Sites.Sum(s => s.LunchServed);
        public int DailyBreakfastServed => Sites.Sum(s => s.BfastServed);
        public double GroupReimbursement => Sites.Sum(s => s.ReimbursementAmount);

        public double EstimateReimbursement()
        {
            foreach (Site site in Sites)
            {
                site.CalcReimbursement(FreeRate, District.SfaCertified);
            }
            return Sites.Sum(s => s.ReimbursementAmount);
        }

        public override string ToString()
        {
            if (TotalEnrolled == 0)
                return $"{District} / {Name} -- no students enrolled --";

            return $"Sponsor: {Name} ISP={Isp * 100:0}% ENROLLED={TotalEnrolled} FREE_RATE={FreeRate * 100:0.00} REIMBURSEMENT={EstimateReimbursement().ToString("C", CultureInfo.CurrentCulture)}";
        }

    }
}
