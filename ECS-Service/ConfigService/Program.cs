using System;
using System.Collections.Generic;
using AILabConfig;
using Pulumi;
using Pulumi.Aws.LB.Inputs;
using Pulumi.Aws.Ec2;
using Pulumi.Awsx.Awsx.Inputs;
using Pulumi.Awsx.Ecs.Inputs;
using Aws = Pulumi.Aws;
using Pulumi.Awsx.Ecs;
using Pulumi.Aws.Ecs.Inputs;

return await Deployment.RunAsync(() =>
{
    //Config
    var config = new Config();
    var env = config.Require("ENV");
    var namePrefix = config.Require("NamePrefix");
    var serviceName = config.Require("ServiceName");
    var shortName = $"{env}-{namePrefix}-{serviceName}";
    var tags = new InputMap<string>()
    {
         { "Name", shortName},
         { "Create-By", "Pulumi"},
         { "Create-Time", DateTimeOffset.UtcNow.ToOffset(new TimeSpan(8,0,0)).ToString("yyyy-MM-dd HH:mm")},
    };

    var vpcConfig = new AILabVpcConfig(env);
    var vpc = Vpc.Get(vpcConfig.VpcId, vpcConfig.VpcId);
    var containerPort = config.GetInt32("ContainerPort") ?? 80;

    //建立Public服務
    var publicTargetGroup = new Aws.LB.TargetGroup($"Public-{shortName}", new()
    {
        Port = containerPort,
        VpcId = vpc.Id,
        Tags = tags,
        TargetType = "ip",
        Protocol = "HTTP",
        HealthCheck = new TargetGroupHealthCheckArgs
        {
            Enabled = true,
            Path = "/health",
            Interval = 30,
            Timeout = 15,
        }
    });
    var publicListener = new Aws.LB.Listener($"Public-{shortName}-Listener", new()
    {
        LoadBalancerArn = config.Require("PublicLoadBalancerArn"),
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
    });

    //建立Private服務
    var privateTargetGroup = new Aws.LB.TargetGroup($"Private-{shortName}", new()
    {
        Port = containerPort,
        VpcId = vpc.Id,
        Tags = tags,
        TargetType = "ip",
        Protocol = "HTTP",
        HealthCheck = new TargetGroupHealthCheckArgs
        {
            Enabled = true,
            Path = "/health",
            Interval = 30,
            Timeout = 15,
        }
    });
    var privateListener = new Aws.LB.Listener($"Private-{shortName}-Listener", new()
    {
        LoadBalancerArn = config.Require("PrivateLoadBalancerArn"),
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

    var clusterName = config.Require("Cluster");
    var cluster = Aws.Ecs.Cluster.Get(clusterName, clusterName);

    var awsConfig = new AILabAwsConfig(env);
    var cpu = config.GetInt32("cpu") ?? 512;
    var memory = config.GetInt32("memory") ?? 512;
    var desiredCount = config.GetInt32("desiredCount") ?? 1;

    var imagePath = config.Require("ImagePath");
    var version = config.Require("Version");
    var imageUri = $"{awsConfig.DockerRegistry}/{imagePath}:{version}";

    var securityGroupConfig = new AILabSecurityGroupConfig(env);
    //建立ECS服務
    var service = new FargateService(shortName,
        new FargateServiceArgs
        {
            HealthCheckGracePeriodSeconds = 60,
            LoadBalancers = new[]
            {
                 new ServiceLoadBalancerArgs
                 {
                    ContainerName = serviceName,
                    ContainerPort = containerPort,
                    TargetGroupArn = publicTargetGroup.Arn
                 },
                 new ServiceLoadBalancerArgs
                 {
                    ContainerName = serviceName,
                    ContainerPort = containerPort,
                    TargetGroupArn = privateTargetGroup.Arn
                 },
            },
            Cluster = cluster.Arn,
            DesiredCount = desiredCount,
            TaskDefinitionArgs = new FargateServiceTaskDefinitionArgs
            {
                Containers = new Dictionary<string, TaskDefinitionContainerDefinitionArgs>()
                {
                    [serviceName] = new TaskDefinitionContainerDefinitionArgs()
                    {
                        HealthCheck = new TaskDefinitionHealthCheckArgs
                        {
                            Command = new string[]
                            {
                                "CMD-SHELL",                                
                                $"curl -f http://localhost:{containerPort}/health || exit 0",
                            },
                            StartPeriod = 60,
                        },
                        Name = serviceName,
                        Image = imageUri,
                        Cpu = cpu,
                        Memory = memory,
                        Essential = true,
                        PortMappings = new[]
                        {
                            new TaskDefinitionPortMappingArgs
                            {
                                ContainerPort = containerPort,
                            },
                        },
                        Environment = new[]
                        {
                            new TaskDefinitionKeyValuePairArgs
                            {
                                Name = "ASPNETCORE_ENVIRONMENT",
                                Value = awsConfig.ASPNETCORE_ENVIRONMENT
                            }
                        },
                        RepositoryCredentials = new TaskDefinitionRepositoryCredentialsArgs
                        {
                            CredentialsParameter = awsConfig.AWSSecretsManagerName,
                        },
                        EntryPoint = Array.Empty<string>(),
                        Command = Array.Empty<string>(),
                    }
                },
                LogGroup = new DefaultLogGroupArgs
                {
                    Skip = true
                },
                TaskRole = new DefaultRoleWithPolicyArgs
                {
                    RoleArn = awsConfig.TaskRoleArn
                },
                ExecutionRole = new DefaultRoleWithPolicyArgs
                {
                    RoleArn = awsConfig.TaskRoleArn
                },
                Tags = tags,
            },
            EnableEcsManagedTags = true,
            NetworkConfiguration = new ServiceNetworkConfigurationArgs
            {
                Subnets = vpcConfig.PrivateSubnets,
                SecurityGroups = new InputList<string>
                {
                    securityGroupConfig.PublicEcsIds,
                    securityGroupConfig.InternalEcsIds,
                }
            },
        });

    return new Dictionary<string, object?>
    {
        ["ServiceName"] = service.GetResourceName(),
        ["PublicTargetGroup"] = publicTargetGroup.GetResourceName(),
        ["PrivateTargetGroup"] = privateTargetGroup.GetResourceName(),
    };
});

