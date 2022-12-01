namespace AILabConfig
{
    public class AILabSecurityGroupConfig
    {
        public AILabSecurityGroupConfig(string environmentName)
        {
            EnvironmentName = environmentName;
        }
        public string EnvironmentName { get; }
        public string[] SecurityGroupIds
        {
            get
            {
                return EnvironmentName.Equals("PRD") ? PRD_SecurityGroupIds : DEV_SecurityGroupIds;
            }
        }

        private static readonly string[] DEV_SecurityGroupIds = 
        { 
            "sg-0c98649099677665b", 
            "sg-070f508e34d6a96d3" 
        };
        private static readonly string[] PRD_SecurityGroupIds = 
        { 
            "sg-0c98649099677665b", 
            "sg-070f508e34d6a96d3" 
        };
    }
}
