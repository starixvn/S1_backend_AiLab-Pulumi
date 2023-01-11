using AILabConfig;
using Pulumi;
using Pulumi.Aws.LB.Inputs;
using Pulumi.Aws.Ec2;
using Pulumi.Awsx.Awsx.Inputs;
using Pulumi.Awsx.Ecs.Inputs;
using Aws = Pulumi.Aws;
using Pulumi.Awsx.Ecs;
using Pulumi.Aws.Ecs.Inputs;
using Pulumi.Aws.AppAutoScaling.Inputs;

namespace AILab.Core
{
    public static class AILabFargateService
    {
        public static Func<IDictionary<string, object?>> CreateFargateService()
        {
            return () =>
            {
                //Config
                var config = new Config();
                var env = Deployment.Instance.StackName;
                var namePrefix = config.Require("NamePrefix");
                var serviceName = config.Require("ServiceName");
                var shortName = $"{namePrefix}-{serviceName}-{env}";
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
                var serviceType= config.Get("ServiceType") ?? "API";

                //建立Public服務
                var publicTargetGroup = new Aws.LB.TargetGroup($"P-{shortName}-TG", new()
                {
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
                var privateTargetGroup = new Aws.LB.TargetGroup($"I-{shortName}-TG", new()
                {
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

                //add 80 port rule
                if (containerPort == 80)
                {
                    AILabListenerRule.Add80PortListenerRule(publicListener.Arn,publicTargetGroup.Arn);
                }

                var clusterName = config.Require("Cluster");
                var cluster = Aws.Ecs.Cluster.Get(clusterName, clusterName);

                var awsConfig = new AILabAwsConfig(env);
                var cpu = config.GetInt32("Cpu") ?? 512;
                var memory = config.GetInt32("Memory") ?? 512;
                var desiredCount = config.GetInt32("DesiredCount") ?? 1;
                var maxCapacity = config.GetInt32("MaxCapacity") ?? 1;

                var imagePath = config.Require("ImagePath");              
                var version = config.Require("Version");
                var imageUri = $"{awsConfig.DockerRegistry}/{imagePath}:{version}";

                var containerPath = config.Require("ContainerPath");
                var volumePath = config.Require("VolumePath");
                

                var securityGroupConfig = new AILabSecurityGroupConfig(env);
                //建立ECS服務
                var service = new FargateService($"{shortName}", new FargateServiceArgs
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
                                        $"curl -f http://localhost:{containerPort}{healthCheckPath} || exit 0",
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
                                    },
                                    new TaskDefinitionKeyValuePairArgs
                                    {
                                        Name = "ServiceType",
                                        Value = serviceType
                                    }
                                },
                                RepositoryCredentials = new TaskDefinitionRepositoryCredentialsArgs
                                {
                                    CredentialsParameter = awsConfig.AWSSecretsManagerName,
                                },
                                EntryPoint = Array.Empty<string>(),
                                Command = Array.Empty<string>(),
                                MountPoints = new[]
                                {
                                    new TaskDefinitionMountPointArgs
                                    {
                                        ContainerPath = containerPath,
                                        SourceVolume = "efs"
                                    }
                                }
                            }
                        },
                        Volumes = new[]
                        {
                          new TaskDefinitionVolumeArgs()
                          {
                               Name = "efs",
                               EfsVolumeConfiguration = new TaskDefinitionVolumeEfsVolumeConfigurationArgs()
                               {
                                   FileSystemId = awsConfig.EFSFileSystemId,
                                   RootDirectory = volumePath
                               }
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
                    ForceNewDeployment = true,
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
                
                //設定Auto Scaling
                if (config.GetBoolean("AutoScaling") ?? false)
                {
                    var fargateServiceName = service.Service.Apply(i => i.Name);
                    var ecsTarget = new Aws.AppAutoScaling.Target($"{shortName}-ecsTarget", new()
                    {
                        MaxCapacity = maxCapacity,
                        MinCapacity = desiredCount,
                        ResourceId = Output.Format($"service/{clusterName}/{fargateServiceName}"),
                        ScalableDimension = "ecs:service:DesiredCount",
                        ServiceNamespace = "ecs",
                    });

                    var ECSCpuPolicy = new Aws.AppAutoScaling.Policy($"{shortName}-ECSCPUPolicy", new()
                    {
                        ResourceId = ecsTarget.ResourceId,
                        ScalableDimension = ecsTarget.ScalableDimension,
                        ServiceNamespace = ecsTarget.ServiceNamespace,
                        PolicyType = "TargetTrackingScaling",
                        TargetTrackingScalingPolicyConfiguration = new PolicyTargetTrackingScalingPolicyConfigurationArgs
                        {
                            ScaleInCooldown = 300,
                            ScaleOutCooldown = 300,
                            PredefinedMetricSpecification = new PolicyTargetTrackingScalingPolicyConfigurationPredefinedMetricSpecificationArgs
                            {
                                PredefinedMetricType = "ECSServiceAverageCPUUtilization",
                            },
                            TargetValue = 75,
                        },
                    });
                    var ECSMemoryPolicy = new Aws.AppAutoScaling.Policy($"{shortName}-ECSMemoryPolicy", new()
                    {
                        PolicyType = "TargetTrackingScaling",
                        ResourceId = ecsTarget.ResourceId,
                        ScalableDimension = ecsTarget.ScalableDimension,
                        ServiceNamespace = ecsTarget.ServiceNamespace,
                        TargetTrackingScalingPolicyConfiguration = new PolicyTargetTrackingScalingPolicyConfigurationArgs
                        {
                            ScaleInCooldown = 300,
                            ScaleOutCooldown = 300,
                            PredefinedMetricSpecification = new PolicyTargetTrackingScalingPolicyConfigurationPredefinedMetricSpecificationArgs
                            {
                                PredefinedMetricType = "ECSServiceAverageMemoryUtilization",
                            },
                            TargetValue = 75,
                        }
                    });
                }

                return new Dictionary<string, object?>
                {
                    ["ServiceName"] = service.GetResourceName(),
                    ["PublicTargetGroup"] = publicTargetGroup.GetResourceName(),
                    ["PrivateTargetGroup"] = privateTargetGroup.GetResourceName(),
                };
            };
        }
    }
}
