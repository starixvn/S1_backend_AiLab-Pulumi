using System;
using System.Collections.Generic;
using AILabConfig;
using Pulumi;
using Pulumi.Aws.Ec2;
using Pulumi.Aws.LB;
using Pulumi.Aws.LB.Inputs;

return await Deployment.RunAsync(() =>
{
    //Config
    var config = new Config();
    var env = config.Require("ENV");
    var namePrefix = config.Require("NamePrefix");
    var serviceName = Deployment.Instance.StackName;
    var shortName = $"{namePrefix}-{serviceName}";
    var tags = new InputMap<string>()
    {
            { "Name", shortName},
            { "Create-By", "Pulumi"},
            { "Create-Time", DateTimeOffset.UtcNow.ToOffset(new TimeSpan(8,0,0)).ToString("yyyy-MM-dd HH:mm")},
    };
    var vpcConfig = new AILabVpcConfig(env);
    var vpc = Vpc.Get(vpcConfig.VpcId, vpcConfig.VpcId);
    var containerPort = config.GetInt32("ContainerPort") ?? 80;
    var healthCheckPath = config.Get("HealthCheckPath") ?? "/health";

    //Get LoadBalancer
    var awsConfig = new AILabAwsConfig(env);
    var stack = config.Require("LoadBalancerStack");
    var reference = new StackReference($"{awsConfig.PulumiOrganization}/{awsConfig.LoadBalancerProject}/{stack}");
    var publicLoadBalancerArn = reference.RequireOutput("PublicLoadBalancerArn").Apply(v => v.ToString());
    var privateLoadBalancerArn = reference.RequireOutput("PrivateLoadBalancerArn").Apply(v => v.ToString());


    //建立Public服務
    var publicName = $"Public-{shortName}-TG";
    var publicTargetGroup = new TargetGroup(publicName, new()
    {
        Name = publicName,
        Port = containerPort,
        VpcId = vpc.Id,
        Tags = tags,
        TargetType = "ip",
        Protocol = "HTTP",
        HealthCheck = new TargetGroupHealthCheckArgs
        {
            Enabled = true,
            Path = healthCheckPath,
            Interval = 30,
            Timeout = 15,
        }
    });
    var publicListener = new Listener($"Public-{shortName}-Listener", new()
    {
        LoadBalancerArn = publicLoadBalancerArn,
        Port = containerPort,
        Protocol = "HTTP",
        DefaultActions = new[]
        {
            new ListenerDefaultActionArgs
            {
                Type = "forward",
                TargetGroupArn = publicTargetGroup.Arn,
            },
        },
        Tags = tags,
    });

    //建立Private服務
    var privateName = $"Private-{shortName}-TG";
    var privateTargetGroup = new TargetGroup(privateName, new()
    {
        Name = privateName,
        Port = containerPort,
        VpcId = vpc.Id,
        Tags = tags,
        TargetType = "ip",
        Protocol = "HTTP",
        HealthCheck = new TargetGroupHealthCheckArgs
        {
            Enabled = true,
            Path = healthCheckPath,
            Interval = 30,
            Timeout = 15,
        }
    });
    var privateListener = new Listener($"Private-{shortName}-Listener", new()
    {
        LoadBalancerArn = privateLoadBalancerArn,
        Port = containerPort,
        Protocol = "HTTP",
        DefaultActions = new[]
        {
            new ListenerDefaultActionArgs
            {
                Type = "forward",
                TargetGroupArn = privateTargetGroup.Arn,
            },
        },
    });

    //add Nginx 80 port rule
    if (containerPort == 80)
    {
        #region Get TargetGroupStack arn

        // AdminSite Web ALB Rule
        var adminSiteWebTargetGroupStack = config.Require("AdminSiteWebTargetGroupStack");
        var adminSiteWebTargetGroupReference = new StackReference($"{awsConfig.PulumiOrganization}/{awsConfig.TargetGroupProject}/{adminSiteWebTargetGroupStack}");
        var adminSiteWebArn = adminSiteWebTargetGroupReference.RequireOutput("PublicTargetGroupArn").Apply(v => v.ToString());

        // AdminSite API ALB Rule
        var adminSiteApiTargetGroupStack = config.Require("AdminSiteApiTargetGroupStack");
        var adminSiteApiTargetGroupReference = new StackReference($"{awsConfig.PulumiOrganization}/{awsConfig.TargetGroupProject}/{adminSiteApiTargetGroupStack}");
        var adminSiteApiArn = adminSiteApiTargetGroupReference.RequireOutput("PublicTargetGroupArn").Apply(v => v.ToString());

        // MemberSite
        var memberSiteTargetGroupStack = config.Require("MemberSiteTargetGroupStack");
        var memberSiteTargetGroupReference = new StackReference($"{awsConfig.PulumiOrganization}/{awsConfig.TargetGroupProject}/{memberSiteTargetGroupStack}");
        var memberSiteArn = memberSiteTargetGroupReference.RequireOutput("PublicTargetGroupArn").Apply(v => v.ToString());

        // MemberSite App
        var memberSiteAppTargetGroupStack = config.Require("MemberSiteAppTargetGroupStack");
        var memberSiteAppTargetGroupReference = new StackReference($"{awsConfig.PulumiOrganization}/{awsConfig.TargetGroupProject}/{memberSiteAppTargetGroupStack}");
        var memberSiteAppArn = memberSiteAppTargetGroupReference.RequireOutput("PublicTargetGroupArn").Apply(v => v.ToString());

        #endregion

        #region Host + path Rule Priority  要放最前面，編號 1 - 50

        var adminSiteApiHostWithPath1 = new ListenerRule($"80Path1-listenerRule-AdminApi1", new()
        {
            ListenerArn = publicListener.Arn,
            Priority = 1,
            Actions = new[]
            {
                new ListenerRuleActionArgs
                {
                    Type = "forward",
                    TargetGroupArn = adminSiteApiArn,
                },
            },
            Conditions = new[]
            {
                new ListenerRuleConditionArgs
                {
                    HostHeader = new ListenerRuleConditionHostHeaderArgs
                    {
                        Values = new[]
                        {
                            "admin.*",
                        },
                    },
                },
                new ListenerRuleConditionArgs
                {
                    PathPattern = new ListenerRuleConditionPathPatternArgs
                    {
                        Values = new[]
                        {
                            "/api/*",
                            "/apiV2/*",
                            "/lib/*",
                            "/js/*",
                        },
                    },
                }
            },
            Tags = tags
        });
        var adminSiteApiHostWithPath2 = new ListenerRule($"80Path1-listenerRule-AdminApi2", new()
        {
            ListenerArn = publicListener.Arn,
            Priority = 2,
            Actions = new[]
            {
                new ListenerRuleActionArgs
                {
                    Type = "forward",
                    TargetGroupArn = adminSiteApiArn,
                },
            },
            Conditions = new[]
            {
                new ListenerRuleConditionArgs
                {
                    HostHeader = new ListenerRuleConditionHostHeaderArgs
                    {
                        Values = new[]
                        {
                            "admin.*",
                        },
                    },
                },
                new ListenerRuleConditionArgs
                {
                    PathPattern = new ListenerRuleConditionPathPatternArgs
                    {
                        Values = new[]
                        {
                            "/css/*",
                            "/Home/*",
                            "/Admin/*",
                            "/BankFinancial/*",
                        },
                    },
                }
            },
            Tags = tags
        });
        var adminSiteApiHostWithPath3 = new ListenerRule($"80Path1-listenerRule-AdminApi3", new()
        {
            ListenerArn = publicListener.Arn,
            Priority = 3,
            Actions = new[]
            {
                new ListenerRuleActionArgs
                {
                    Type = "forward",
                    TargetGroupArn = adminSiteApiArn,
                },
            },
            Conditions = new[]
            {
                new ListenerRuleConditionArgs
                {
                    HostHeader = new ListenerRuleConditionHostHeaderArgs
                    {
                        Values = new[]
                        {
                            "admin.*",
                        },
                    },
                },
                new ListenerRuleConditionArgs
                {
                    PathPattern = new ListenerRuleConditionPathPatternArgs
                    {
                        Values = new[]
                        {
                            "/CustManage/*",
                            "/Finance/*",
                            "/Marketing/*",
                            "/Message/*",
                        },
                    },
                }
            },
            Tags = tags
        });
        var adminSiteApiHostWithPath4 = new ListenerRule($"80Path1-listenerRule-AdminApi4", new()
        {
            ListenerArn = publicListener.Arn,
            Priority = 4,
            Actions = new[]
            {
                new ListenerRuleActionArgs
                {
                    Type = "forward",
                    TargetGroupArn = adminSiteApiArn,
                },
            },
            Conditions = new[]
            {
                new ListenerRuleConditionArgs
                {
                    HostHeader = new ListenerRuleConditionHostHeaderArgs
                    {
                        Values = new[]
                        {
                            "admin.*",
                        },
                    },
                },
                new ListenerRuleConditionArgs
                {
                    PathPattern = new ListenerRuleConditionPathPatternArgs
                    {
                        Values = new[]
                        {
                            "/Report/*",
                            "/ServerInfo/*",
                            "/common/*",
                            "/resource/*"
                        },
                    },
                }
            },
            Tags = tags
        });
        var adminSiteApiHostWithPath5 = new ListenerRule($"80Path1-listenerRule-AdminApi5", new()
        {
            ListenerArn = publicListener.Arn,
            Priority = 5,
            Actions = new[]
            {
                new ListenerRuleActionArgs
                {
                    Type = "forward",
                    TargetGroupArn = adminSiteApiArn,
                },
            },
            Conditions = new[]
            {
                new ListenerRuleConditionArgs
                {
                    HostHeader = new ListenerRuleConditionHostHeaderArgs
                    {
                        Values = new[]
                        {
                            "admin.*",
                        },
                    },
                },
                new ListenerRuleConditionArgs
                {
                    PathPattern = new ListenerRuleConditionPathPatternArgs
                    {
                        Values = new[]
                        {
                            "/images/*",
                        },
                    },
                }
            },
            Tags = tags
        });

        #endregion

        #region Host Rule (依據 Host 到不同 Target group) Priority  放中間，編號 51 - 100

        var adminSiteWebHostRule = new ListenerRule($"80Host-listenerRule-AdminWeb", new()
        {
            ListenerArn = publicListener.Arn,
            Priority = 51,
            Actions = new[]
            {
                new ListenerRuleActionArgs
                {
                    Type = "forward",
                    TargetGroupArn = adminSiteWebArn,
                },
            },
            Conditions = new[]
            {
                new ListenerRuleConditionArgs
                {
                    HostHeader = new ListenerRuleConditionHostHeaderArgs
                    {
                        Values = new[]
                        {
                            "admin.*",
                        },
                    },
                }
            },
            Tags = tags
        });
        var hostRule = new ListenerRule($"80Host-listenerRule", new()
        {
            ListenerArn = publicListener.Arn,
            Priority = 52,
            Actions = new[]
            {
                new ListenerRuleActionArgs
                {
                    Type = "forward",
                    TargetGroupArn = memberSiteAppArn,
                },
            },
            Conditions = new[]
            {
                new ListenerRuleConditionArgs
                {
                    HostHeader = new ListenerRuleConditionHostHeaderArgs
                    {
                        Values = new[]
                        {
                            "app.*",
                        },
                    },
                }
            },
            Tags = tags
        });

        #endregion

        #region Path Rule (MemberSite) Priority  放最後，編號 101 - 150

        var pathRule1 = new ListenerRule($"80Path1-listenerRule", new()
        {
            ListenerArn = publicListener.Arn,
            Priority = 101,
            Actions = new[]
          {
                new ListenerRuleActionArgs
                {
                    Type = "forward",
                    TargetGroupArn = memberSiteArn,
                },
            },
            Conditions = new[]
          {
                new ListenerRuleConditionArgs
                {
                    PathPattern = new ListenerRuleConditionPathPatternArgs
                    {
                        Values = new[]
                        {
                            "/api/*",
                            "*/Cashier/*",
                            "*/entPage",
                            "*/liveStreamingEntPage",
                        },
                    },
                },
            },
            Tags = tags
        });
        var pathRule2 = new ListenerRule($"80Path2-listenerRule", new()
        {
            ListenerArn = publicListener.Arn,
            Priority = 102,
            Actions = new[]
            {
                new ListenerRuleActionArgs
                {
                    Type = "forward",
                    TargetGroupArn = memberSiteArn,
                },
            },
            Conditions = new[]
            {
                new ListenerRuleConditionArgs
                {
                    PathPattern = new ListenerRuleConditionPathPatternArgs
                    {
                        Values = new[]
                        {
                            "*/Promotion/Bonus",
                            "*/Extra/Signup",
                        },
                    },
                },
            },
            Tags = tags
        });
        var pathRule3 = new ListenerRule($"80Path3-listenerRule", new()
        {
            ListenerArn = publicListener.Arn,
            Priority = 103,
            Actions = new[]
            {
                new ListenerRuleActionArgs
                {
                    Type = "forward",
                    TargetGroupArn = memberSiteArn,
                },
            },
            Conditions = new[]
            {
                new ListenerRuleConditionArgs
                {
                    PathPattern = new ListenerRuleConditionPathPatternArgs
                    {
                        Values = new[]
                        {
                            "*/SiteSkin/*",
                            "*/common/*",
                        },
                    },
                },
            },
            Tags = tags
        });
        var pathRule4 = new ListenerRule($"80Path4-listenerRule", new()
        {
            ListenerArn = publicListener.Arn,
            Priority = 104,
            Actions = new[]
            {
                new ListenerRuleActionArgs
                {
                    Type = "forward",
                    TargetGroupArn = memberSiteArn,
                },
            },
            Conditions = new[]
            {
                new ListenerRuleConditionArgs
                {
                    PathPattern = new ListenerRuleConditionPathPatternArgs
                    {
                        Values = new[]
                        {
                            "*/CMS/*",
                        },
                    },
                },
            },
            Tags = tags
        });

        #endregion
    }

    return new Dictionary<string, object?>
    {
        ["PublicTargetGroup"] = publicTargetGroup.GetResourceName(),
        ["PublicTargetGroupArn"] = publicTargetGroup.Arn,
        ["PrivateTargetGroup"] = privateTargetGroup.GetResourceName(),
        ["PrivateTargetGroupArn"] = privateTargetGroup.Arn,
    };
});

