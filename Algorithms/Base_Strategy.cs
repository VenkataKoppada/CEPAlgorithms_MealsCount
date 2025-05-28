using CEPAlgorithms_MealsCount.Classes;
using System.Globalization;
using System.Text;

namespace CEPAlgorithms_MealsCount.Algorithms
{
    public abstract class Base_Strategy
    {
        public virtual string Name { get; protected set; } = "Abstract Strategy";
        public Dictionary<string, object> Params { get; set; } = new();
        public List<CEPGroup>? Groups { get; protected set; } = new List<CEPGroup>();

        public int StudentsCovered => Groups?.Sum(g => g.CoveredStudents) ?? 0;

        public int TotalEnrolled => Groups?.Sum(g => g.TotalEnrolled) ?? 0;

        public double ISP => TotalEnrolled == 0 ? 0 : Math.Round((double)StudentsCovered / TotalEnrolled, 4);
        public double FreeRate => ISP * GlobalConstants.CLAIMING_PERC_MULTIPLIER;

        public abstract void CreateGroups(Sponsor sponsor);

        public double Reimbursement => Math.Round(Groups?.Sum(g => g.EstimateReimbursement()) ?? 0.0, 0);

        protected Base_Strategy(Dictionary<string, object>? parameters = null, string? name = null)
        {
            if (parameters != null)
                Params = parameters;

            if (!string.IsNullOrEmpty(name))
                Name = name;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Best Strategy: {Name}");
            sb.AppendLine($"Total Enrolled: {TotalEnrolled}");
            sb.AppendLine($"Students Covered: {StudentsCovered}");
            sb.AppendLine($"ISP: {ISP * 100:0.00}%");
            sb.AppendLine($"Free Rate: {FreeRate * 100:0.00}%");
            sb.AppendLine($"Reimbursement: {Reimbursement.ToString("C", CultureInfo.CurrentCulture)}");
            if (Groups != null && Groups.Count > 0)
            {
                foreach (var group in Groups)
                {
                    sb.AppendLine(group.ToString());
                    foreach (var school in group.Sites)
                    {
                        sb.AppendLine(school.ToString());
                    }
                }
            }
            else
            {
                sb.AppendLine("No groups created.");
            }
            return sb.ToString();
        }
    }

}

