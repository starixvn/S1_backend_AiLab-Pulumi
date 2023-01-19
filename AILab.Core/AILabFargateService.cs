using AILabConfig;
using Pulumi;
using Pulumi.Awsx.Awsx.Inputs;
using Pulumi.Awsx.Ecs.Inputs;
using Aws = Pulumi.Aws;
using Pulumi.Awsx.Ecs;
using Pulumi.Aws.Ecs.Inputs;
using Pulumi.Aws.AppAutoScaling.Inputs;
using System.Text.Json;

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
                var env = config.Require("ENV");
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
                var containerPort = config.GetInt32("ContainerPort") ?? 80;
                var healthCheckPath = config.Get("HealthCheckPath") ?? "/health";
                var serviceType = config.Get("ServiceType") ?? "API";

                var awsConfig = new AILabAwsConfig(env);
                var cpu = config.GetInt32("Cpu") ?? 512;
                var memory = config.GetInt32("Memory") ?? 512;
                var desiredCount = config.GetInt32("DesiredCount") ?? 1;
                var maxCapacity = config.GetInt32("MaxCapacity") ?? desiredCount;

                var imagePath = config.Require("ImagePath");
                var version = config.Require("Version");
                var imageUri = $"{awsConfig.DockerRegistry}/{imagePath}:{version}";

                //設定Volumes
                var containerPaths = new InputList<TaskDefinitionMountPointArgs>();
                var volumePaths = new InputList<TaskDefinitionVolumeArgs>();
                var volumes = config.GetObject<JsonElement>("Volumes");
                if (!volumes.Equals(default(JsonElement)))
                {
                    var i = 1;
                    foreach (var path in volumes.EnumerateArray())
                    {                 
                        var name = $"volume-{i}";
                        var containerPath = path.GetProperty("ContainerPath").ToString();
                        var taskDefinitionMountPointArgs = new TaskDefinitionMountPointArgs
                        {
                            ContainerPath = containerPath,
                            SourceVolume = name
                        };
                        containerPaths.Add(taskDefinitionMountPointArgs);                       
                        var volumePath = path.GetProperty("VolumePath").ToString();
                        var taskDefinitionVolumeArgs = new TaskDefinitionVolumeArgs()
                        {
                            Name = name,
                            EfsVolumeConfiguration = new TaskDefinitionVolumeEfsVolumeConfigurationArgs()
                            {
                                FileSystemId = awsConfig.EFSFileSystemId,
                                RootDirectory = volumePath
                            }
                        };
                        volumePaths.Add(taskDefinitionVolumeArgs);
                        i++;
                    }
                }

                //Get Cluster
                var clusterStack = config.Require("ClusterStack");
                var clusterReference = new StackReference($"{awsConfig.PulumiOrganization}/{awsConfig.ECSClusterProject}/{clusterStack}");
                var clusterName = clusterReference.RequireOutput("Cluster").Apply(v => v.ToString());
                var clusterArn = clusterReference.RequireOutput("ClusterArn").Apply(v => v.ToString());

                var loadBalancers = new InputList<ServiceLoadBalancerArgs>();
                var isAPI = serviceType.Equals("API");
                //設定Target Group
                if (isAPI)
                {
                    var targetGroupStack = config.Require("TargetGroupStack");
                    var reference = new StackReference($"{awsConfig.PulumiOrganization}/{awsConfig.TargetGroupProject}/{targetGroupStack}");
                    var publicTargetGroupArn = reference.RequireOutput("PublicTargetGroupArn").Apply(v => v.ToString());
                    var privateTargetGroupArn = reference.RequireOutput("PrivateTargetGroupArn").Apply(v => v.ToString());
                    loadBalancers.AddRange(new[]
                    {
                        new ServiceLoadBalancerArgs
                        {
                            ContainerName = serviceName,
                            ContainerPort = containerPort,
                            TargetGroupArn = publicTargetGroupArn
                        },
                        new ServiceLoadBalancerArgs
                        {
                            ContainerName = serviceName,
                            ContainerPort = containerPort,
                            TargetGroupArn = privateTargetGroupArn
                        },
                    });
                }
               
                var securityGroupConfig = new AILabSecurityGroupConfig(env);
                //建立ECS服務
                var service = new FargateService(shortName, new FargateServiceArgs
                {
                    HealthCheckGracePeriodSeconds = isAPI ? 60 : null,
                    LoadBalancers = loadBalancers,
                    Cluster = clusterArn,
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
                                MountPoints = containerPaths,
                            }
                        },
                        Volumes = volumePaths,
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
                    Tags = tags
                });

                //設定Auto Scaling
                if (maxCapacity > desiredCount)
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
                };
            };
        }
    }
}
