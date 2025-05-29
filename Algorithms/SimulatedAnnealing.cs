using CEPAlgorithms_MealsCount.Classes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CEPAlgorithms_MealsCount.Algorithms
{
    internal class SimulatedAnnealing : Base_Strategy
    {
        private Random? _random;
        private bool _debug;

        public override string Name => "SimulatedAnnealing";

        private double T_MIN => GlobalConstants.MINIMUM_ISP * GlobalConstants.CLAIMING_PERC_MULTIPLIER;

        // Use lazy initialization property to avoid null reference
        private Random Random => _random ??= new Random(Params?.ContainsKey("seed") == true ? Convert.ToInt32(Params["seed"]) : 42);

        // Public parameterless constructor required for AddStrategy<T>
        public SimulatedAnnealing() : base(null, "SimulatedAnnealing")
        {
        }

        public SimulatedAnnealing(Dictionary<string, object>? parameters = null)
            : base(parameters, "SimulatedAnnealing")
        {
        }

        public override void CreateGroups(Sponsor sponsor)
        {
            var sites = sponsor.Sites.ToList();
            _debug = Params?.ContainsKey("step_debug") == true && Convert.ToBoolean(Params["step_debug"]);

            if (Params?.ContainsKey("original") == true && Convert.ToBoolean(Params["original"]))
            {
                DoNYCMODA(sponsor);
            }
            else if (sites.Count > 10) // less than 10 we do exhaustive
            {
                bool clearGroups = Params?.ContainsKey("clear_groups") == true && Convert.ToBoolean(Params["clear_groups"]);
                bool consolidateGroups = Params?.ContainsKey("regroup") == true && Convert.ToBoolean(Params["regroup"]);

                // CHANGED: Set defaults to match expected output format
                int freshStarts = Params?.ContainsKey("fresh_starts") == true ? Convert.ToInt32(Params["fresh_starts"]) : 50; // Changed from 10 to 50
                int iterations = Params?.ContainsKey("iterations") == true ? Convert.ToInt32(Params["iterations"]) : 1000; // Changed from 150 to 1000

                int? ngroups = null;
                if (Params?.ContainsKey("ngroups") == true && Params["ngroups"] != null &&
                    Params["ngroups"].ToString() != "None" && Params["ngroups"].ToString() != "null")
                {
                    ngroups = Convert.ToInt32(Params["ngroups"]);
                }
                string evaluateBy = Params?.ContainsKey("evaluate_by") == true ? Params["evaluate_by"].ToString() : "reimbursement";

                Groups = Simplified(sponsor, clearGroups, consolidateGroups, freshStarts, iterations, ngroups, evaluateBy);

                // prune 0 school groups since we don't need to report them
                Groups = Groups?.Where(g => g.Sites.Count > 0).ToList();
            }
            else
            {
                Groups = new List<CEPGroup> { new CEPGroup(sponsor, "OneGroup", sites) };
            }
        }

        private List<CEPGroup> Simplified(Sponsor sponsor, bool clearGroups = false, bool consolidateGroups = false,
            int freshStarts = 1, int iterations = 1000, int? ngroups = null, string evaluateBy = "reimbursement")
        {
            var sites = sponsor.Sites.ToList();
            if (sites.Count <= 3) return new List<CEPGroup>(); // safeguard some assumptions

            // CHANGED: Increase default tfactor for better exploration
            double tFactor = Params?.ContainsKey("tfactor") == true ? Convert.ToDouble(Params["tfactor"]) : 1000000; // Increased from 100000
            bool useAnnealing = Params?.ContainsKey("annealing") == true && Convert.ToInt32(Params["annealing"]) == 1;
            double deltaT = Params?.ContainsKey("delta_t") == true ? Convert.ToDouble(Params["delta_t"]) : 0.01; // Python uses 0.01

            double overall = 0;
            List<CEPGroup> bestGrouping = null;

            for (int start = 0; start < freshStarts; start++)
            {
                var groups = RandomStart(sponsor, ngroups);

                if (_debug)
                {
                    foreach (var g in groups)
                    {
                        Console.WriteLine($"{g.Name}: {string.Join(",", g.Sites.Select(s => s.Code))}");
                    }
                }

                // Python uses np.arange(1,0,-deltaT) which goes from 1.0 down to deltaT
                for (double T = 1.0; T > 0; T -= deltaT)
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        if (_debug)
                        {
                            Console.WriteLine($"{i}\t${groups.Sum(g => g.EstimateReimbursement()):F0}");
                            Console.WriteLine($"\t{string.Join(" ", groups.Select(g => new string('*', g.Sites.Count)))}");
                        }

                        bool? changed = Step(groups, T, tFactor, useAnnealing, evaluateBy);
                        if (changed == null)
                        {
                            break; // If we have hit a point where we can no longer shuffle, stop our iterations short
                        }

                        if (changed == true && clearGroups)
                        {
                            groups = groups.Where(g => g.Sites.Count > 0).ToList();
                        }

                        if (_debug)
                        {
                            Console.WriteLine($"\t{string.Join(" ", groups.Select(g => new string('*', g.Sites.Count)))} {(changed == true ? "Keep" : "Discard")}");
                            Console.WriteLine($"\t => ${groups.Sum(g => g.EstimateReimbursement()):F0}");
                        }
                    }
                }

                double latest = groups.Sum(g => g.EstimateReimbursement());
                if (latest >= overall)
                {
                    overall = latest;
                    bestGrouping = groups;
                }
            }

            return bestGrouping ?? new List<CEPGroup>();
        }

        private List<CEPGroup> RandomStart(Sponsor sponsor, int? ngroups = null)
        {
            var sites = sponsor.Sites.ToList();
            int ng;

            if (ngroups.HasValue)
            {
                ng = Random.Next(2, ngroups.Value + 1); // FIXED: Use Random property instead of _random
            }
            else
            {
                ng = Random.Next(2, sites.Count); // FIXED: Use Random property instead of _random
            }

            var groups = new List<CEPGroup>();
            for (int i = 0; i < ng; i++)
            {
                groups.Add(new CEPGroup(sponsor, $"Group {i}", new List<Site>()));
            }

            foreach (var site in sites)
            {
                groups[Random.Next(0, ng)].Sites.Add(site); // FIXED: Use Random property instead of _random
            }

            // prune empty groups. assignment is random
            return groups.Where(g => g.Sites.Count > 0).ToList();
        }

        private bool? Step(List<CEPGroup> groups, double T, double tFactor, bool useAnnealing, string evaluateBy)
        {
            // Get 2 random groups - Python uses sample(groups, 2)
            if (groups.Count <= 2)
            {
                return null;
            }

            var availableGroups = groups.Where(g => g.Sites.Count > 0).ToList();
            if (availableGroups.Count < 2)
            {
                return null;
            }

            // Python: g1,g2 = sample(groups,2) - select 2 random groups
            var selectedGroups = availableGroups.OrderBy(x => Random.Next()).Take(2).ToList(); // FIXED: Use Random property
            var g1 = selectedGroups[0];
            var g2 = selectedGroups[1];

            // Python: while len(g1.schools) == 0: g1,g2 = sample(groups,2)
            if (g1.Sites.Count == 0)
            {
                return null;
            }

            // track our starting values - Python code
            double startR = Math.Round(g1.EstimateReimbursement() + g2.EstimateReimbursement());
            int startC = g1.CoveredStudents + g2.CoveredStudents;
            int startS = (IsCEPEligible(g1) ? 1 : 0) + (IsCEPEligible(g2) ? 1 : 0);
            int startF = (Math.Abs(g1.FreeRate - 1.0) < 0.001 ? 1 : 0) + (Math.Abs(g2.FreeRate - 1.0) < 0.001 ? 1 : 0);

            // Python: s = g1.schools.pop(randint(0,len(g1.schools)-1))
            var siteIndex = Random.Next(0, g1.Sites.Count); // FIXED: Use Random property
            var site = g1.Sites[siteIndex];
            g1.Sites.RemoveAt(siteIndex);
            g2.Sites.Add(site);

            bool passing = false;
            double stepR = Math.Round(g1.EstimateReimbursement() + g2.EstimateReimbursement());

            // Python temperature calculation
            double stepTemp = startR > 0 ? (stepR - startR) / startR * tFactor : -0.01 * tFactor;

            switch (evaluateBy)
            {
                case "reimbursement":
                    passing = stepR > startR;
                    break;

                case "coverage":
                    int stepC = g1.CoveredStudents + g2.CoveredStudents;
                    if (stepC == startC)
                        passing = stepR > startR;
                    else
                        passing = stepC > startC;
                    break;

                case "schools":
                    int stepS = (IsCEPEligible(g1) ? 1 : 0) + (IsCEPEligible(g2) ? 1 : 0);
                    if (startS < stepS)
                        passing = true;
                    else if (startS == stepS && stepR > startR)
                        passing = true;
                    break;

                case "schools_free":
                    int stepF = (Math.Abs(g1.FreeRate - 1.0) < 0.001 ? 1 : 0) + (Math.Abs(g2.FreeRate - 1.0) < 0.001 ? 1 : 0);
                    if (startF < stepF)
                        passing = true;
                    else if (startF == stepF && stepR > startR)
                        passing = true;
                    break;
            }

            // FIXED: Corrected Python logic - if not passing or (use_annealing and random() < np.exp( step_temp/T)):
            if (!passing || (useAnnealing && Random.NextDouble() < Math.Exp(stepTemp / T))) // FIXED: Use Random property
            {
                // Undo the move - Python: s = g2.schools.pop(); g1.schools.append(s)
                g2.Sites.Remove(site);
                g1.Sites.Add(site);
                return false;
            }

            return true;
        }

        private void DoNYCMODA(Sponsor sponsor)
        {
            // This would implement the original NYCMODA algorithm using DataFrames
            // For now, we'll use the simplified version as the original requires
            // extensive DataFrame operations that would need additional libraries
            Groups = Simplified(sponsor);
        }

        // Helper methods to calculate missing properties
        private bool IsCEPEligible(CEPGroup group)
        {
            return GetISP(group) >= GlobalConstants.MINIMUM_ISP;
        }

        private double GetISP(CEPGroup group)
        {
            return group.TotalEnrolled == 0 ? 0 : (double)group.TotalEligible / group.TotalEnrolled;
        }
    }
}
