namespace AILabConfig
{
    public class AILabSecurityGroupConfig
    {
        public AILabSecurityGroupConfig(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; }

        public string[] OfficeIds
        {
            get
            {
                return new string[]
                {
                    "sg-07de3ec96d717730d", //Taipei-Office-Linux
                };
            }
        }

        public string[] ApiServiceIds
        {
            get
            {
                return EnvironmentName.Equals("PRD") ? PRD_ApiServiceIds : DEV_ApiServiceIds;
            }
        }

        private static readonly string[] DEV_ApiServiceIds = 
        { 
            "sg-0c98649099677665b", //LinuxService
        };

        private static readonly string[] PRD_ApiServiceIds = 
        { 
            "sg-0c98649099677665b", //LinuxService
        };

        /// <summary>
        /// 開放AWS內部網段All TCP Port
        /// </summary>
        public string[] InternalEcsIds
        {
            get
            {
                return EnvironmentName.Equals("PRD") ? PRD_InternalEcsIds : DEV_InternalEcsIds;
            }
        }

        private static readonly string[] DEV_InternalEcsIds =
        {
            "sg-0d90cb7044517daad"  //Internal-DEV-ECS
        };

        private static readonly string[] PRD_InternalEcsIds =
        {
            "sg-0d90cb7044517daad"  //Internal-DEV-ECS
        };


        /// <summary>
        /// 開放AWS外部網段All TCP Port
        /// </summary>
        public string[] PublicEcsIds
        {
            get
            {
                return new string[]
              {
                    "sg-00dd4cbf31d5356fa", //Public-DEV-ECS
              };
            }
        }

        /// <summary>
        /// CloudFront SG
        /// </summary>
        public string[] CloudFrontIds
        {
            get
            {
                return new string[]
                {
                    "sg-034243fc6be6b0111",//CLOUDFRONT80-Q6G
                    "sg-05b9a3f218549f98a",//CLOUDFRONT80-IOQ
                    "sg-0fb825317deb8fe65",//CLOUDFRONT80-QUY
                };
            }
        }
        public string[] CloudflareIds
        {
            get
            {
                return new string[]
                {
                    "sg-07de57958c88b7c9f", //Cloudflare
                };
            }
        }
    }
}
