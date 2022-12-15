using System;
using System.Collections.Generic;
using AILabConfig;
using Pulumi;
using Pulumi.Aws.LB.Inputs;
using Pulumi.Aws.Ec2;
using Pulumi.Awsx.Awsx.Inputs;
using Pulumi.Awsx.Ecs.Inputs;
using Aws = Pulumi.Aws;
using Awsx = Pulumi.Awsx;

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
        Protocol= "HTTP",
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
    var cpu = config.GetInt32("cpu") ?? 4;
    var memory = config.GetInt32("memory") ?? 2;
    var desiredCount = config.GetInt32("desiredCount") ?? 1;

    var imagePath = config.Require("ImagePath");
    var version = config.Require("Version");
    var imageUri = $"{awsConfig.DockerRegistry}/{imagePath}:{version}";
    //建立ECS服務
    var service = new Awsx.Ecs.FargateService(serviceName, new()
    {
        Cluster = cluster.Arn,
        DesiredCount = desiredCount,
        TaskDefinitionArgs = new FargateServiceTaskDefinitionArgs
        {
            Container = new TaskDefinitionContainerDefinitionArgs
            {
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
            },
            LogGroup = new DefaultLogGroupArgs
            {
                Skip = true
            },
            TaskRole = new DefaultRoleWithPolicyArgs
            {
                Skip = true,
            },
            ExecutionRole = new DefaultRoleWithPolicyArgs
            {
                Skip = true,
            },
        },
    }) ;

    return new Dictionary<string, object?>
    {
        ["ServiceName"] = service.GetResourceName(),
        ["TargetGroup"] = targetGroup.GetResourceName(),
    };
});

