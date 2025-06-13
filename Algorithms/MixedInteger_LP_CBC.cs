using CEPAlgorithms_MealsCount.Classes;
using Google.OrTools.LinearSolver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CEPAlgorithms_MealsCount.Algorithms
{
    public class MixedInteger_LP_CBC : Base_Strategy
    {
        public override string Name => "MixedInteger LP CBC";

        public MixedInteger_LP_CBC() : base(null, "MixedInteger LP CBC")
        {
        }

        public MixedInteger_LP_CBC(Dictionary<string, object>? parameters = null)
            : base(parameters, "MixedInteger LP CBC")
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

            var optimizer = new GreedyLPOptimizer();
            var solution = optimizer.Solve(eligibleSites);

            Groups = ProcessOptimizationResults(sponsor, eligibleSites, solution);
        }

        private List<Site> FilterEligibleSites(IEnumerable<Site> sites)
        {
            return sites
                .Where(site => site.TotalEnrolled > 0)
                .GroupBy(site => site.Code)
                .Select(group => group.First())
                .ToList();
        }

        private List<CEPGroup> ProcessOptimizationResults(Sponsor sponsor, List<Site> sites, OptimizationSolution solution)
        {
            if (solution?.Groups == null || !solution.Groups.Any())
            {
                return new List<CEPGroup>
                {
                    new CEPGroup(sponsor, "Singleton-Group", sites)
                };
            }

            var groups = new List<CEPGroup>();
            var processedSiteCodes = new HashSet<string>();

            foreach (var (groupId, groupSites) in solution.Groups)
            {
                groups.Add(new CEPGroup(sponsor, $"Group-{groupId}", groupSites));
                foreach (var site in groupSites)
                {
                    processedSiteCodes.Add(site.Code);
                }
            }

            var unassignedSites = sites.Where(s => !processedSiteCodes.Contains(s.Code)).ToList();
            if (unassignedSites.Any())
            {
                groups.Add(new CEPGroup(sponsor, "Not Selected", unassignedSites));
            }

            return groups;
        }
    }

    internal class GreedyLPOptimizer
    {
        private const double TARGET_ISP = 0.625;
        private const double MINIMUM_ISP = GlobalConstants.MINIMUM_ISP;

        public OptimizationSolution Solve(List<Site> sites)
        {
            int n = sites.Count;

            // Generate bin sizes similar to Python implementation
            var binSizes = GenerateBinSizes(n);

            OptimizationSolution bestSolution = new OptimizationSolution
            {
                TotalReimbursement = -1e20,
                Groups = new Dictionary<int, List<Site>>()
            };

            foreach (var binSize in binSizes)
            {
                var solution = RunConfiguration(sites, binSize);
                if (solution != null && solution.TotalReimbursement > bestSolution.TotalReimbursement)
                {
                    bestSolution = solution;
                }
            }

            return bestSolution.TotalReimbursement > -1e20 ? bestSolution : null;
        }

        private List<int> GenerateBinSizes(int n)
        {
            var binSizes = new HashSet<int>();

            for (int x = 0; x < 100; x++)
            {
                int binSize = (int)(2 + x * (n - 2) / 99.0);
                binSizes.Add(binSize);
            }

            var result = binSizes.OrderBy(x => x).ToList();

            if (n > 1000)
            {
                result = result.Where(x => x > 20).ToList();
            }

            return result;
        }

        private OptimizationSolution RunConfiguration(List<Site> sites, int binSize)
        {
            var results = new List<(int GroupId, List<Site> Sites)>();
            var remainingSites = sites.ToList();
            int groupId = 1;

            while (remainingSites.Count > 1)
            {
                var groupSites = SolveSingleBin(remainingSites, binSize);

                if (groupSites == null || !groupSites.Any())
                    break;

                results.Add((groupId, groupSites));
                groupId++;

                // Remove selected sites from remaining sites
                var selectedCodes = groupSites.Select(s => s.Code).ToHashSet();
                remainingSites = remainingSites.Where(s => !selectedCodes.Contains(s.Code)).ToList();
            }

            if (!results.Any())
                return null;

            return CalculateReimbursement(results);
        }

        private List<Site> SolveSingleBin(List<Site> sites, int binSize)
        {
            var solver = Solver.CreateSolver("CBC_MIXED_INTEGER_PROGRAMMING");
            if (solver == null) return null;

            // Create binary variables for each site
            var variables = new Dictionary<int, Variable>();
            for (int i = 0; i < sites.Count; i++)
            {
                variables[i] = solver.MakeIntVar(0, 1, $"x_{i}");
            }

            // ISP constraint lower bound: Group ISP >= MINIMUM_ISP
            var lowerBoundConstraint = solver.MakeConstraint(0, double.PositiveInfinity, "isp_lower_bound");
            for (int i = 0; i < sites.Count; i++)
            {
                lowerBoundConstraint.SetCoefficient(variables[i],
                    sites[i].TotalEligible - MINIMUM_ISP * sites[i].TotalEnrolled);
            }

            // ISP constraint upper bound: Group ISP <= TARGET_ISP
            var upperBoundConstraint = solver.MakeConstraint(double.NegativeInfinity, 0, "isp_upper_bound");
            for (int i = 0; i < sites.Count; i++)
            {
                upperBoundConstraint.SetCoefficient(variables[i],
                    sites[i].TotalEligible - TARGET_ISP * sites[i].TotalEnrolled);
            }

            // Bin capacity constraint
            var capacityConstraint = solver.MakeConstraint(0, binSize, "bin_capacity");
            for (int i = 0; i < sites.Count; i++)
            {
                capacityConstraint.SetCoefficient(variables[i], 1);
            }

            // Objective: Maximize sum of ISPs
            var objective = solver.Objective();
            for (int i = 0; i < sites.Count; i++)
            {
                objective.SetCoefficient(variables[i], sites[i].Isp);
            }
            objective.SetMaximization();

            var status = solver.Solve();

            if (status != Solver.ResultStatus.OPTIMAL || objective.Value() == 0)
                return null;

            // Extract solution
            var selectedSites = new List<Site>();
            for (int i = 0; i < sites.Count; i++)
            {
                if (variables[i].SolutionValue() > 0)
                {
                    selectedSites.Add(sites[i]);
                }
            }

            return selectedSites;
        }

        private OptimizationSolution CalculateReimbursement(List<(int GroupId, List<Site> Sites)> groups)
        {
            double totalReimbursement = 0;
            var groupDict = new Dictionary<int, List<Site>>();

            foreach (var (groupId, sites) in groups)
            {
                groupDict[groupId] = sites;

                // Calculate group ISP
                var totalEligible = sites.Sum(s => s.TotalEligible);
                var totalEnrolled = sites.Sum(s => s.TotalEnrolled);
                var groupIsp = totalEnrolled > 0 ? (double)totalEligible / totalEnrolled : 0;

                if (groupIsp >= MINIMUM_ISP)
                {
                    var claimingRate = Math.Min(1.0, groupIsp * GlobalConstants.CLAIMING_PERC_MULTIPLIER);

                    // Calculate reimbursement
                    var yearlyBreakfast = sites.Sum(s => s.BfastServed) * GlobalConstants.NUMBER_OF_SERVING_DAYS;
                    var yearlyLunch = sites.Sum(s => s.LunchServed) * GlobalConstants.NUMBER_OF_SERVING_DAYS;

                    var breakfastReimbursement = yearlyBreakfast *
                        (GlobalConstants.FREE_BREAKFAST_RATE * claimingRate +
                         GlobalConstants.PAID_BREAKFAST_RATE * (1 - claimingRate));

                    var lunchReimbursement = yearlyLunch *
                        (GlobalConstants.FREE_LUNCH_RATE * claimingRate +
                         GlobalConstants.PAID_LUNCH_RATE * (1 - claimingRate));

                    totalReimbursement += breakfastReimbursement + lunchReimbursement;
                }
            }

            return new OptimizationSolution
            {
                Groups = groupDict,
                TotalReimbursement = totalReimbursement
            };
        }
    }

    internal class OptimizationSolution
    {
        public Dictionary<int, List<Site>> Groups { get; set; } = new();
        public double TotalReimbursement { get; set; }
    }
}
