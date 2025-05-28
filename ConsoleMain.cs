using CEPAlgorithms_MealsCount.Algorithms;
using CEPAlgorithms_MealsCount.Classes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.ConstrainedExecution;

namespace CEPAlgorithms_MealsCount
{
    class ConsoleMain
    {
        static void Main(string[] args)
        {
            // input data for the sponsor
            Sponsor sponsor = new Sponsor("Valley District", "498");
            sponsor.SfaCertified = true;

            sponsor.Sites.Add(new Site("S1", 100, 10));
            sponsor.Sites.Add(new Site("S2", 100, 15));
            sponsor.Sites.Add(new Site("S3", 100, 20));
            sponsor.Sites.Add(new Site("S4", 100, 25));
            sponsor.Sites.Add(new Site("S5", 100, 30));
            sponsor.Sites.Add(new Site("S6", 100, 35));
            sponsor.Sites.Add(new Site("S7", 100, 40));
            sponsor.Sites.Add(new Site("S8", 100, 45));
            sponsor.Sites.Add(new Site("S9", 100, 50));
            sponsor.Sites.Add(new Site("S10", 100, 55));
            sponsor.Sites.Add(new Site("S11", 100, 11));
            sponsor.Sites.Add(new Site("S12", 100, 16)); // $385,438 using NYCMODA - yet to develop
            /*            sponsor.Sites.Add(new Site("S13", 100, 21));
                        sponsor.Sites.Add(new Site("S14", 100, 26));
                        sponsor.Sites.Add(new Site("S15", 100, 31));
                        sponsor.Sites.Add(new Site("S16", 100, 36));
                        sponsor.Sites.Add(new Site("S17", 100, 41));
                        sponsor.Sites.Add(new Site("S18", 100, 46));
                        sponsor.Sites.Add(new Site("S19", 100, 51));
                        sponsor.Sites.Add(new Site("S20", 100, 56));*/

            // start the timer
            TimeOnly startTime = TimeOnly.FromDateTime(DateTime.Now);

            // Adding all strategies to the sponsor
            AddStrategy<Exhaustive>(sponsor);
            AddStrategy<OneGroup>(sponsor);
            AddStrategy<OneToOne>(sponsor);
            AddStrategy<Pairs>(sponsor);
            AddStrategy<Spread>(sponsor);
            AddStrategy<Binning>(sponsor);
            AddStrategy<SimulatedAnnealing>(sponsor);
            AddStrategy<MixedInteger_LP>(sponsor);

            // evaluate all the strategies and print the best strategy
            sponsor.EvaluateStrategies();
            Console.WriteLine(sponsor.PrintBestStrategy());

            // calculate and print execution time
            TimeOnly endTime = TimeOnly.FromDateTime(DateTime.Now);
            TimeSpan timeSpan = endTime - startTime;
            Console.WriteLine($"Execution Time: {timeSpan.TotalSeconds} seconds");
        }

        // Helper method to add and register a strategy by type
        static void AddStrategy<T>(Sponsor sponsor) where T : Base_Strategy, new()
        {
            var strategy = new T();
            sponsor.Strategies.Add(strategy);
        }
    }
}