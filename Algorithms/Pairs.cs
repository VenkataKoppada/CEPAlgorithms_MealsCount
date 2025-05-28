using CEPAlgorithms_MealsCount.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CEPAlgorithms_MealsCount.Algorithms
{
    internal class Pairs : Base_Strategy
    {
        public override string Name => "Pairs";

        public override void CreateGroups(Sponsor sponsor)
        {
            // Sort sites by ISP descending
            var sites = sponsor.Sites.OrderByDescending(s => s.Isp).ToList();

            // First pair up fully covered ISP sites (over 62.5%)
            var (highIsp, lowIsp) = SplitOnThreshold(sites, GlobalConstants.THRESHOLD_ISP);
            CreateMatches(highIsp, lowIsp, GlobalConstants.THRESHOLD_ISP, sponsor);

            // Then pair up partially covered (over 25%)
            (highIsp, lowIsp) = SplitOnThreshold(lowIsp, GlobalConstants.MINIMUM_ISP);
            CreateMatches(highIsp, lowIsp, GlobalConstants.MINIMUM_ISP, sponsor);

            // Remaining sites are not CEP eligible
            if (lowIsp.Count > 0)
            {
                Groups?.Add(new CEPGroup(sponsor, "Not CEP Eligible", lowIsp));
            }

            if (Groups?.Sum(g => g.Sites.Count) != sites.Count)
            {
                throw new InvalidOperationException("Total sites in groups do not match total sites in district.");
            }
        }

        private (List<Site> highIsp, List<Site> lowIsp) SplitOnThreshold(List<Site> sites, double threshold)
        {
            var highIsp = new List<Site>();
            var lowIsp = new List<Site>();

            foreach (var site in sites)
            {
                if (site.Isp > threshold)
                    highIsp.Add(site);
                else
                    lowIsp.Add(site);
            }

            lowIsp = lowIsp.OrderByDescending(s => s.TotalEnrolled).ToList();
            return (highIsp, lowIsp);
        }

        private void CreateMatches(List<Site> highIsp, List<Site> lowIsp, double threshold, Sponsor sponsor)
        {
            foreach (var site in highIsp)
            {
                bool foundMatch = false;
                for (int i = 0; i < lowIsp.Count; i++)
                {
                    var lowSite = lowIsp[i];
                    var group = new CEPGroup(sponsor, $"Group-of-{site.Code}", new List<Site> { site, lowSite });

                    if (group.Isp > threshold)
                    {
                        foundMatch = true;
                        Groups?.Add(group);
                        lowIsp.RemoveAt(i);
                        break;
                    }
                }

                if (!foundMatch)
                {
                    var singletonGroup = new CEPGroup(sponsor, $"Singleton-Group-of-{site.Code}", new List<Site> { site });
                    Groups?.Add(singletonGroup);
                }
            }
        }
    }
}
