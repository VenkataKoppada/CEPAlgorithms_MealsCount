using CEPAlgorithms_MealsCount.Classes;
using Google.OrTools.LinearSolver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CEPAlgorithms_MealsCount.Algorithms
{
    public class MixedInteger_LP : Base_Strategy
    {
        public override string Name => "Mixed Integer LP";

        public MixedInteger_LP() : base(null, "Mixed Integer LP")
        {
        }

        public MixedInteger_LP(Dictionary<string, object>? parameters = null)
            : base(parameters, "Mixed Integer LP")
        {
        }

        public override void CreateGroups(Sponsor sponsor)
        {
            var eligibleSites = FilterEligibleSites(sponsor.Sites);
            if (!eligibleSites.Any())
            {
                Groups = new List<CEPGroup>();
                return;
            }

            var optimizer = InitializeOptimizer();
            var optimalSolution = optimizer.FindOptimalGrouping(eligibleSites);

            Groups = ProcessOptimizationResults(sponsor, eligibleSites, optimalSolution);
        }

        private List<Site> FilterEligibleSites(IEnumerable<Site> sites)
        {
            return sites
                .Where(site => site.TotalEnrolled > 0)
                .GroupBy(site => site.Code)
                .Select(group => group.First())
                .ToList();
        }

        private SchoolGroupOptimizer InitializeOptimizer()
        {
            return new SchoolGroupOptimizer(
                new OptimizationParameters
                {
                    TargetThreshold = GlobalConstants.THRESHOLD_ISP,
                    MinimumThreshold = GlobalConstants.MINIMUM_ISP,
                    BreakfastRates = new MealRates
                    {
                        FreeRate = GlobalConstants.FREE_BREAKFAST_RATE,
                        PaidRate = GlobalConstants.PAID_BREAKFAST_RATE
                    },
                    LunchRates = new MealRates
                    {
                        FreeRate = GlobalConstants.FREE_LUNCH_RATE,
                        PaidRate = GlobalConstants.PAID_LUNCH_RATE
                    }
                });
        }

        private List<CEPGroup> ProcessOptimizationResults(Sponsor sponsor, List<Site> sites, GroupingSolution solution)
        {
            if (solution?.IsValid != true)
            {
                return new List<CEPGroup>
                {
                    new CEPGroup(sponsor, "Group 0", sites)
                };
            }

            return BuildCEPGroupsFromSolution(sponsor, sites, solution);
        }

        private List<CEPGroup> BuildCEPGroupsFromSolution(Sponsor sponsor, List<Site> sites, GroupingSolution solution)
        {
            var groups = new List<CEPGroup>();
            var processedSiteCodes = new HashSet<string>();

            int groupIndex = 0;
            foreach (var cluster in solution.SiteClusters)
            {
                var clusterSites = cluster.Value.ToList();
                groups.Add(new CEPGroup(sponsor, $"Group {groupIndex}", clusterSites));

                foreach (var site in clusterSites)
                {
                    processedSiteCodes.Add(site.Code);
                }
                groupIndex++;
            }

            var unassignedSites = sites.Where(s => !processedSiteCodes.Contains(s.Code)).ToList();
            if (unassignedSites.Any())
            {
                groups.Add(new CEPGroup(sponsor, $"Group {groupIndex}", unassignedSites));
            }

            return groups;
        }
    }

    internal class SchoolGroupOptimizer
    {
        private readonly OptimizationParameters _parameters;
        private readonly ReimbursementCalculator _calculator;

        public SchoolGroupOptimizer(OptimizationParameters parameters)
        {
            _parameters = parameters;
            _calculator = new ReimbursementCalculator(parameters);
        }

        public GroupingSolution FindOptimalGrouping(List<Site> sites)
        {
            // Use a completely different approach: Dynamic Programming with Set Partitioning
            return SolveSetPartitioningProblem(sites);
        }

        private GroupingSolution SolveSetPartitioningProblem(List<Site> sites)
        {
            var solver = Solver.CreateSolver("SCIP");
            if (solver == null) return null;

            // Generate all feasible subsets that meet ISP constraints
            var feasibleSubsets = GenerateFeasibleSubsets(sites);

            if (!feasibleSubsets.Any())
                return null;

            // Create binary variables for each feasible subset
            var subsetVariables = new Dictionary<int, Variable>();
            for (int i = 0; i < feasibleSubsets.Count; i++)
            {
                subsetVariables[i] = solver.MakeIntVar(0, 1, $"subset_{i}");
            }

            // Constraint: Each site must be covered by exactly one subset
            for (int siteIdx = 0; siteIdx < sites.Count; siteIdx++)
            {
                var constraint = solver.MakeConstraint(1, 1, $"cover_site_{siteIdx}");

                for (int subsetIdx = 0; subsetIdx < feasibleSubsets.Count; subsetIdx++)
                {
                    if (feasibleSubsets[subsetIdx].Contains(sites[siteIdx]))
                    {
                        constraint.SetCoefficient(subsetVariables[subsetIdx], 1);
                    }
                }
            }

            // Objective: Maximize total reimbursement
            var objective = solver.Objective();
            for (int i = 0; i < feasibleSubsets.Count; i++)
            {
                var reimbursement = _calculator.CalculateSubsetReimbursement(feasibleSubsets[i]);
                objective.SetCoefficient(subsetVariables[i], reimbursement);
            }
            objective.SetMaximization();

            var status = solver.Solve();

            if (status != Solver.ResultStatus.OPTIMAL)
                return null;

            return ExtractSolutionFromSetPartitioning(feasibleSubsets, subsetVariables);
        }

        private List<List<Site>> GenerateFeasibleSubsets(List<Site> sites)
        {
            var feasibleSubsets = new List<List<Site>>();

            // Use a different approach: Generate subsets using power set with ISP filtering
            int maxSubsetSize = Math.Min(sites.Count, 15); // Limit for computational efficiency

            for (int size = 1; size <= maxSubsetSize; size++)
            {
                var combinations = GetCombinations(sites, size);

                foreach (var combination in combinations)
                {
                    if (IsSubsetFeasible(combination))
                    {
                        feasibleSubsets.Add(combination.ToList());
                    }
                }
            }

            return feasibleSubsets;
        }

        private IEnumerable<IEnumerable<Site>> GetCombinations(List<Site> sites, int size)
        {
            return GetCombinationsRecursive(sites, size, 0);
        }

        private IEnumerable<IEnumerable<Site>> GetCombinationsRecursive(List<Site> sites, int size, int startIndex)
        {
            if (size == 0)
            {
                yield return Enumerable.Empty<Site>();
                yield break;
            }

            for (int i = startIndex; i <= sites.Count - size; i++)
            {
                foreach (var combination in GetCombinationsRecursive(sites, size - 1, i + 1))
                {
                    yield return new[] { sites[i] }.Concat(combination);
                }
            }
        }

        private bool IsSubsetFeasible(IEnumerable<Site> subset)
        {
            var siteList = subset.ToList();
            if (!siteList.Any()) return false;

            var totalEligible = siteList.Sum(s => s.TotalEligible);
            var totalEnrolled = siteList.Sum(s => s.TotalEnrolled);

            if (totalEnrolled == 0) return false;

            var isp = (double)totalEligible / totalEnrolled;

            return isp >= _parameters.MinimumThreshold && isp <= _parameters.TargetThreshold;
        }

        private GroupingSolution ExtractSolutionFromSetPartitioning(
            List<List<Site>> feasibleSubsets,
            Dictionary<int, Variable> subsetVariables)
        {
            var selectedGroups = new Dictionary<int, List<Site>>();
            int groupId = 1;

            for (int i = 0; i < feasibleSubsets.Count; i++)
            {
                if (subsetVariables[i].SolutionValue() > 0.5)
                {
                    selectedGroups[groupId] = feasibleSubsets[i];
                    groupId++;
                }
            }

            if (!selectedGroups.Any())
                return null;

            return new GroupingSolution
            {
                SiteClusters = selectedGroups,
                TotalValue = _calculator.ComputeTotalReimbursement(selectedGroups),
                IsValid = true
            };
        }
    }

    internal class ReimbursementCalculator
    {
        private readonly OptimizationParameters _parameters;

        public ReimbursementCalculator(OptimizationParameters parameters)
        {
            _parameters = parameters;
        }

        public double CalculateSubsetReimbursement(List<Site> sites)
        {
            var groupMetrics = CalculateGroupMetrics(sites);

            if (groupMetrics.ISP < _parameters.MinimumThreshold)
                return 0;

            var claimingRate = Math.Min(1.0, groupMetrics.ISP * GlobalConstants.CLAIMING_PERC_MULTIPLIER);
            return CalculateAnnualReimbursement(groupMetrics, claimingRate);
        }

        public double ComputeTotalReimbursement(Dictionary<int, List<Site>> groups)
        {
            double totalReimbursement = 0;

            foreach (var group in groups.Values)
            {
                totalReimbursement += CalculateSubsetReimbursement(group);
            }

            return totalReimbursement;
        }

        private GroupMetrics CalculateGroupMetrics(List<Site> sites)
        {
            var totalEligible = sites.Sum(s => s.TotalEligible);
            var totalEnrolled = sites.Sum(s => s.TotalEnrolled);

            return new GroupMetrics
            {
                TotalEligible = totalEligible,
                TotalEnrolled = totalEnrolled,
                ISP = totalEnrolled > 0 ? (double)totalEligible / totalEnrolled : 0,
                DailyBreakfast = sites.Sum(s => s.BfastServed),
                DailyLunch = sites.Sum(s => s.LunchServed)
            };
        }

        private double CalculateAnnualReimbursement(GroupMetrics metrics, double claimingRate)
        {
            var annualBreakfast = metrics.DailyBreakfast * GlobalConstants.NUMBER_OF_SERVING_DAYS;
            var annualLunch = metrics.DailyLunch * GlobalConstants.NUMBER_OF_SERVING_DAYS;

            var breakfastReimbursement = annualBreakfast *
                (_parameters.BreakfastRates.FreeRate * claimingRate +
                 _parameters.BreakfastRates.PaidRate * (1 - claimingRate));

            var lunchReimbursement = annualLunch *
                (_parameters.LunchRates.FreeRate * claimingRate +
                 _parameters.LunchRates.PaidRate * (1 - claimingRate));

            return breakfastReimbursement + lunchReimbursement;
        }
    }

    // Supporting classes remain the same
    internal class OptimizationParameters
    {
        public double TargetThreshold { get; set; }
        public double MinimumThreshold { get; set; }
        public MealRates BreakfastRates { get; set; }
        public MealRates LunchRates { get; set; }
    }

    internal class MealRates
    {
        public double FreeRate { get; set; }
        public double PaidRate { get; set; }
    }

    internal class GroupMetrics
    {
        public int TotalEligible { get; set; }
        public int TotalEnrolled { get; set; }
        public double ISP { get; set; }
        public int DailyBreakfast { get; set; }
        public int DailyLunch { get; set; }
    }

    internal class GroupingSolution
    {
        public Dictionary<int, List<Site>> SiteClusters { get; set; } = new();
        public double TotalValue { get; set; }
        public bool IsValid { get; set; }
    }
}
