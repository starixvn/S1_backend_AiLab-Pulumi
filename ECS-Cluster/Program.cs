using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Pulumi;
using Pulumi.Aws.Ecs.Inputs;
using Aws = Pulumi.Aws;

return await Deployment.RunAsync(() =>
{
    //Config
    var config = new Config();
    var env = config.Require("ENV");
    var namePrefix = config.Require("NamePrefix");
    var shortName = $"{env}-{namePrefix}";

    //EC2 LaunchTemplate
    var imageId = config.Require("ImageId");
    var instanceType = config.Require("InstanceType");
    var launchTemplate = new Aws.Ec2.LaunchTemplate($"{shortName}-LT", new()
    {
        NamePrefix = namePrefix,
        ImageId = imageId,
        InstanceType = instanceType,
    });

    //EC2 AutoScalingGroup
    var availabilityZones = config.RequireObject<JsonElement>("AvailabilityZones").EnumerateArray().Select(i => i.GetString()).ToArray();
    //var subnets = config.RequireObject<JsonElement>("Subnets").EnumerateArray().Select(i => i.GetString()).ToArray();
    var maxSize = config.GetInt32("MaxSize") ?? 3;
    var minSize = config.GetInt32("MinSize") ?? 1;
    var desired = config.GetInt32("Desired") ?? 1;
    var autoScalingGroup = new Aws.AutoScaling.Group($"{shortName}-ASG", new()
    {
        AvailabilityZones = availabilityZones,
        //VpcZoneIdentifiers = subnets,
        DesiredCapacity = desired,
        MaxSize = maxSize,
        MinSize = minSize,
        LaunchTemplate = new Aws.AutoScaling.Inputs.GroupLaunchTemplateArgs
        {
            Id = launchTemplate.Id,
            Version = "$Latest",
        },
        NamePrefix = shortName,
        Tags = new[]
        {
            new Aws.AutoScaling.Inputs.GroupTagArgs
            {
                Key = "AmazonECSManaged",
                Value = "true",
                PropagateAtLaunch = true,
            },
        },
    });

    //ECS Capacity Provider
    var capacityProvider = new Aws.Ecs.CapacityProvider($"{shortName}-Provider", new()
    {
        AutoScalingGroupProvider = new CapacityProviderAutoScalingGroupProviderArgs
        {
            AutoScalingGroupArn = autoScalingGroup.Arn,
            ManagedScaling = new CapacityProviderAutoScalingGroupProviderManagedScalingArgs
            {
                MaximumScalingStepSize = maxSize,
                MinimumScalingStepSize = minSize,
                Status = "ENABLED",
                TargetCapacity = desired,
            },
        },
    });

    //ECS Cluster
    var cluster = new Aws.Ecs.Cluster($"{shortName}-Cluster");

    return new Dictionary<string, object?>
    {
        ["LaunchTemplate"] = launchTemplate.GetResourceName(),
        ["AutoScalingGroup"] = autoScalingGroup.GetResourceName(),
        ["CapacityProvider"] = capacityProvider.GetResourceName(),
        ["Cluster"] = cluster.GetResourceName(),
    };
});

