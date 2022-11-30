using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Pulumi;
using Pulumi.Aws.Ec2;
using Pulumi.Aws.Ecs.Inputs;
using Pulumi.Awsx.Awsx.Inputs;
using Pulumi.Awsx.Ecs.Inputs;
using Aws = Pulumi.Aws;
using Awsx = Pulumi.Awsx;

return await Deployment.RunAsync(() =>
{
    //todo:取得外部傳入的參數
    var siteId = 1;
    var brand = $"AI{siteId}";
    var env = "DEV";
    var version = $"3772c1b";

    //設定執行參數
    var config = new Config();
    var name = config.Require("Name");
    var registry = config.Require("Registry");
    var vpcId = config.Require("VpcId");
    var iamRole = config.Require("IamRole");

    var vpc = Vpc.Get(vpcId, vpcId);
    var subnets = config.RequireObject<JsonElement>("Subnets").EnumerateArray().Select(i => i.GetString()).ToArray();
    var securityGroups = config.RequireObject<JsonElement>("SecurityGroups").EnumerateArray().Select(i => i.GetString()).ToArray();

    var containerPort = config.GetInt32("containerPort") ?? 80;
    var cpu = config.GetInt32("cpu") ?? 4;
    var memory = config.GetInt32("memory") ?? 2;
    var desiredCount = config.GetInt32("desiredCount") ?? 1;
    var serviceName = $"{brand}-{env}-{name}-{version}";

    //var imageUri = $"{registry}:{version}";
    //var image = Pulumi.Docker.RegistryImage.Get(imageUri, imageUri);
    //Log.Info($"imageUri:{image.Urn}");
    var repo = new Awsx.Ecr.Repository("test");
    var image = new Awsx.Ecr.Image(serviceName, new Awsx.Ecr.ImageArgs
    {
        RepositoryUrl = repo.Url,
        Path = "./app",
    });

    //建立TargetGroup服務
    var targetGroup = new Aws.LB.TargetGroup($"{brand}-{env}-{name}-TG", new()
    {
        Port = containerPort,
        Protocol = "HTTP",
        VpcId = vpc.Id,
        TargetType = "instance"
    });

    //建立ECS服務
    var service = new Awsx.Ecs.EC2Service(serviceName, new()
    {
        Cluster = $"{brand}-{env}",
        //IamRole = iamRole,
        DesiredCount = desiredCount,
        //LoadBalancers = new[]
        //{
        //    new ServiceLoadBalancerArgs
        //    {
        //        TargetGroupArn = targetGroup.Arn,
        //        ContainerName = serviceName,
        //        ContainerPort = containerPort,
        //    },
        //},
        NetworkConfiguration = new ServiceNetworkConfigurationArgs
        {
            Subnets = subnets,
            SecurityGroups = securityGroups
        },
        TaskDefinitionArgs = new EC2ServiceTaskDefinitionArgs
        {
            Container = new TaskDefinitionContainerDefinitionArgs
            {
                Name = serviceName,
                Image = image.ImageUri,
                Cpu = cpu,
                Memory = memory,
                Essential = true,
                PortMappings = new[]
                {
                    new TaskDefinitionPortMappingArgs
                    {
                        ContainerPort = containerPort,
                        TargetGroup =  targetGroup
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
            NetworkMode = "bridge",
        },
    });





    //var autoScalingGroup = new Aws.AutoScaling.Group("testGroup", new()
    //{
    //    Tags = new[]
    //{
    //        new Aws.AutoScaling.Inputs.GroupTagArgs
    //        {
    //            Key = "AmazonECSManaged",
    //            Value = "true",
    //            PropagateAtLaunch = true,
    //        },
    //    },
    //});

    //var capacityProvider = new Aws.Ecs.CapacityProvider("testCapacityProvider", new()
    //{
    //    AutoScalingGroupProvider = new CapacityProviderAutoScalingGroupProviderArgs
    //    {
    //        AutoScalingGroupArn = autoScalingGroup.Arn,
    //        ManagedTerminationProtection = "ENABLED",
    //        ManagedScaling = new CapacityProviderAutoScalingGroupProviderManagedScalingArgs
    //        {
    //            MaximumScalingStepSize = 3,
    //            MinimumScalingStepSize = 1,
    //            Status = "ENABLED",
    //            TargetCapacity = 10,
    //        },
    //    },
    //});







    return new Dictionary<string, object?>
    {
        ["Image"] = image.GetResourceName(),
        ["ServiceName"] = service.GetResourceName(),
        ["TargetGroup"] = targetGroup.GetResourceName(),
    };
});

