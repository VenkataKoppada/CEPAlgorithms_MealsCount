using CEPAlgorithms_MealsCount.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CEPAlgorithms_MealsCount.Algorithms
{
    internal class Exhaustive : Base_Strategy
    {
        // create and evaluate the exhaustive strategy
        /*
         * number of input records: 10 = 0.7 seconds
         * number of input records: 11 = 3.0 seconds
         * Hence limit the number of Sites to 11 for exhaustive strategy
         */
        private int maxCount = 11;
        public override string Name => "Exhaustive";

        public override void CreateGroups(Sponsor sponsor)
        {
            var sites = sponsor.Sites;

            if (sites.Count() != 0 && sites.Count() <= maxCount)
            {
                var possibleGroups = new Dictionary<string, CEPGroup>();
                int groupIndex = 0;

                foreach (var group in PowerSet<Site>((IList<Site>)sites))
                {
                    var key = GroupKey(group);
                    possibleGroups[key] = new CEPGroup(sponsor, "G" + groupIndex++, group.ToList());
                }

                var evaluateBy = Params.ContainsKey("evaluate_by") ? Params["evaluate_by"].ToString() : "reimbursement";
                List<CEPGroup> bestGrouping = new List<CEPGroup>();
                (double, double) bestOption = (0, 0);

                foreach (var partition in Partitions<Site>((IList<Site>)sites))
                {
                    double estReimbursement = partition.Sum(group => possibleGroups[GroupKey(group)].EstimateReimbursement());

                    if (evaluateBy == "reimbursement")
                    {
                        if (estReimbursement > bestOption.Item1)
                        {
                            bestGrouping = partition.Select(group => possibleGroups[GroupKey(group)]).ToList();
                            bestOption = (estReimbursement, 0);
                        }
                    }
                    else if (evaluateBy == "coverage")
                    {
                        int coveredStudents = partition.Sum(group => possibleGroups[GroupKey(group)].CoveredStudents);
                        if (coveredStudents > bestOption.Item1)
                        {
                            bestGrouping = partition.Select(group => possibleGroups[GroupKey(group)]).ToList();
                            bestOption = (coveredStudents, estReimbursement);
                        }
                        else if (coveredStudents == bestOption.Item1 && estReimbursement > bestOption.Item2)
                        {
                            bestGrouping = partition.Select(group => possibleGroups[GroupKey(group)]).ToList();
                            bestOption = (coveredStudents, estReimbursement);
                        }
                    }
                }

                // assign the best grouping to the Groups property
                Groups = bestGrouping;
            }
        }

        private IEnumerable<IEnumerable<T>> PowerSet<T>(IList<T> list)
        {
            int count = list.Count;
            return Enumerable.Range(1, (1 << count) - 1)
                .Select(i => list.Where((_, j) => (i & (1 << j)) != 0));
        }

        private IEnumerable<List<List<T>>> Partitions<T>(IList<T> list)
        {
            if (list.Count == 1)
            {
                yield return new List<List<T>> { new List<T> { list[0] } };
                yield break;
            }

            T first = list[0];
            foreach (var smaller in Partitions(list.Skip(1).ToList()))
            {
                for (int i = 0; i < smaller.Count; i++)
                {
                    var newSubset = new List<T>(smaller[i]) { first };
                    var newPartition = new List<List<T>>(smaller);
                    newPartition[i] = newSubset;
                    yield return newPartition;
                }

                var newGroup = new List<List<T>> { new List<T> { first } };
                newGroup.AddRange(smaller);
                yield return newGroup;
            }
        }

        private string GroupKey<T>(IEnumerable<T> group)
        {
            return string.Join(",", group.Select(g => g.GetHashCode()).OrderBy(x => x));
        }
    }
}
