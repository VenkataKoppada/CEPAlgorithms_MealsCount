using CEPAlgorithms_MealsCount.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CEPAlgorithms_MealsCount.Algorithms
{
    internal class Spread : Base_Strategy
    {
        public override string Name => "Spread";
        public override void CreateGroups(Sponsor sponsor)
        {
            var sites = sponsor.Sites.OrderByDescending(s => s.Isp).ToList();

            var highISP = new List<Site>();
            var lowISP = new List<Site>();

            foreach (var site in sites)
            {
                if (site.Isp > GlobalConstants.THRESHOLD_ISP)
                    highISP.Add(site);
                else
                    lowISP.Add(site);
            }

            foreach (var site in highISP)
            {
                var currentGroupSites = new List<Site> { site };
                var group = new CEPGroup(sponsor, $"Group-of-{site.Code}", currentGroupSites);

                while (site.Isp > GlobalConstants.THRESHOLD_ISP && lowISP.Count > 0)
                {
                    var nextSchool = lowISP[0];
                    var newGroupSchools = new List<Site>(currentGroupSites) { nextSchool };
                    var biggerGroup = new CEPGroup(sponsor, $"Group-of-{site.Code}", currentGroupSites);

                    if (biggerGroup.Isp < GlobalConstants.THRESHOLD_ISP)
                    {
                        // Put the school back
                        break;
                    }

                    currentGroupSites = newGroupSchools;
                    group = biggerGroup;
                    lowISP.RemoveAt(0);
                }

                Groups?.Add(group);
            }

            if (lowISP.Count > 0)
            {
                Groups?.Add(new CEPGroup(sponsor, "Remainder", lowISP));
            }
        }
    }
}
