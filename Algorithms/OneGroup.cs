using CEPAlgorithms_MealsCount.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CEPAlgorithms_MealsCount.Algorithms
{
    internal class OneGroup : Base_Strategy
    {
        public override string Name => "One Group";
        public override void CreateGroups(Sponsor sponsor)
        {
            if (sponsor.Sites.Count != 0)
            {
                // Create a single group with all schools
                var allSites = sponsor.Sites.ToList();
                var group = new CEPGroup(sponsor, "G1", allSites);
                Groups = new List<CEPGroup> { group };
            }
        }
    }
}
