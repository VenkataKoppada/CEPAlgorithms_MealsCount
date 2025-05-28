using CEPAlgorithms_MealsCount.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CEPAlgorithms_MealsCount.Algorithms
{
    internal class OneToOne : Base_Strategy
    {
        public override string Name => "OneToOne";
        public override void CreateGroups(Sponsor sponsor)
        {
            if (sponsor.Sites.Count != 0)
            {
                Groups = sponsor.Sites
                    .Select(site => new CEPGroup(sponsor, site.Code, new List<Site> { site }))
                    .ToList();
            }
        }
    }
}
