using CEPAlgorithms_MealsCount.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CEPAlgorithms_MealsCount.Classes
{
    public class Sponsor
    {
        public List<Site> Sites { get; private set; } = new List<Site>();
        public List<Base_Strategy> Strategies { get; private set; } = new();
        public Base_Strategy? BestStrategy { get; private set; }
        public string Name { get; }
        public string Code { get; }
        public bool SfaCertified { get; set; }
        public int TotalEnrolled => Sites.Sum(s => s.TotalEnrolled);
        public int StudentsCovered => BestStrategy?.StudentsCovered ?? 0;


        public Sponsor(string name, string code, bool sfaCertified = false)
        {
            Name = name;
            Code = code;
            SfaCertified = sfaCertified;
        }

        public void EvaluateStrategies(string evaluateBy = "reimbursement")
        {
            Base_Strategy? best = null;

            foreach (var strategy in Strategies)
            {
                // invoke create groups to create groups for the strategy
                strategy.CreateGroups(this);

                if (strategy.Groups == null)
                    throw new InvalidOperationException("Strategy groups have not been created.");

                if (evaluateBy == "reimbursement")
                {
                    if (best == null || strategy.Reimbursement > best.Reimbursement)
                        best = strategy;
                }
                else if (evaluateBy == "coverage")
                {
                    if (best == null || strategy.StudentsCovered > best.StudentsCovered)
                        best = strategy;
                }
                else
                {
                    throw new ArgumentException($"Unknown evaluation criteria: {evaluateBy}");
                }
            }

            BestStrategy = best;
        }

        public string PrintBestStrategy()
        {
            return BestStrategy == null ? "No best strategy found." : BestStrategy.ToString();
        }
    }
}
