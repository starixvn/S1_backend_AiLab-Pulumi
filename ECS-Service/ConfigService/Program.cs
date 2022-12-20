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
    var containerPort = config.GetInt32("containerPort") ?? 80;

    //建立TargetGroup服務
    var targetGroup = new Aws.LB.TargetGroup($"{shortName}-TG", new()
    {
        Port = containerPort,
        VpcId = vpc.Id,
        Tags = tags,
        TargetType = "ip",
        Protocol = "HTTP",
    });

    var loadBalancer = config.Require("LoadBalancer");
    var listener = new Aws.LB.Listener("DEV-AI1-Listener", new()
    {
        LoadBalancerArn = loadBalancer,
        Port = containerPort,
        Protocol = "HTTP",
        DefaultActions = new[]
        {
            new ListenerDefaultActionArgs
            {
                Type = "forward",
                TargetGroupArn = targetGroup.Arn,
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

    //建立ECS服務
    var service = new FargateService(
        name: shortName,
        args: new FargateServiceArgs()
        {
            Cluster = cluster.Arn,
            DesiredCount = desiredCount,
            TaskDefinitionArgs = new FargateServiceTaskDefinitionArgs
            {
                Container = new TaskDefinitionContainerDefinitionArgs
                {
                    Name = serviceName,
                    Image = imageUri,
                    Cpu = cpu,
                    Memory = memory,
                    Essential = true,
                    PortMappings = new[]
                    {
                        new TaskDefinitionPortMappingArgs
                        {
                            TargetGroup = targetGroup,
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
        });

    return new Dictionary<string, object?>
    {
        ["ServiceName"] = service.GetResourceName(),
        ["TargetGroup"] = targetGroup.GetResourceName(),
    };
});

