using CEPAlgorithms_MealsCount.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CEPAlgorithms_MealsCount.Algorithms
{
    internal class Binning : Base_Strategy
    {
        public override string Name => "Binning";
        public override void CreateGroups(Sponsor sponsor)
        {
            if (sponsor.Sites == null || sponsor.Sites.Count == 0) return;

            List<Site> highISP = sponsor.Sites.Where(s => s.Isp > GlobalConstants.THRESHOLD_ISP).ToList();
            List<Site> theRest = sponsor.Sites.Where(s => s.Isp <= GlobalConstants.THRESHOLD_ISP)
                                                      .OrderBy(s => s.Isp)
                                                      .ToList();

            Func<List<Site>, double> newISP = sites =>
            {
                double totalEligible = sites.Sum(s => s.TotalEligible);
                double totalEnrolled = sites.Sum(s => s.TotalEnrolled);
                return totalEnrolled > 0 ? totalEligible / totalEnrolled : 0;
            };

            void FillUp(List<Site> target, double threshold)
            {
                while (theRest.Count > 0)
                {
                    var site = theRest.Last();
                    theRest.RemoveAt(theRest.Count - 1);
                    target.Add(site);
                    if (newISP(target) < threshold) break;
                }
            }

            // First group: high ISP schools, top-down fill
            double thresholdLevel = GlobalConstants.THRESHOLD_ISP;
            FillUp(highISP, thresholdLevel);
            Groups.Add(new CEPGroup(sponsor, "High-ISP", highISP));

            double ispWidth = Params.ContainsKey("isp_width") ? Convert.ToDouble(Params["isp_width"]) : 0.02;

            while (theRest.Count > 0 && thresholdLevel >= GlobalConstants.MINIMUM_ISP)
            {
                thresholdLevel = theRest.Last().Isp - ispWidth;
                var binGroup = new List<Site>();
                FillUp(binGroup, thresholdLevel);

                string groupName = $"ISP-{thresholdLevel:0.00}_to_{thresholdLevel + ispWidth:0.00}";
                Groups.Add(new CEPGroup(sponsor, groupName, binGroup));
            }

            if (theRest.Count > 0)
            {
                Groups.Add(new CEPGroup(sponsor, "The-Rest-Low-ISP", theRest));
            }
        }
    }
}
