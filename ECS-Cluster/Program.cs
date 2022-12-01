using System;
using System.Collections.Generic;
using AILabConfig;
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
    var tags = new InputMap<string>()
    {
         { "AmazonECSManaged", "true"},
         { "Name", shortName},
         { "Create-By", "Pulumi"},
         { "Create-Time", DateTimeOffset.UtcNow.ToOffset(new TimeSpan(8,0,0)).ToString("yyyy-MM-dd HH:mm")},
    };

    //EC2 LaunchTemplate
    var awsConfig = new AILabAwsConfig(env);
    var imageId = awsConfig.ImageId;
    var instanceType = awsConfig.InstanceType;
    var keyName = awsConfig.KeyName;

    var securityGroupConfig = new AILabSecurityGroupConfig(env);
    var launchTemplate = new Aws.Ec2.LaunchTemplate($"{shortName}-LT", new()
    {
        KeyName = keyName,
        NamePrefix = shortName,
        ImageId = imageId,
        InstanceType = instanceType,
        VpcSecurityGroupIds = securityGroupConfig.SecurityGroupIds,
    });

    //EC2 AutoScalingGroup
    var vpcConfig = new AILabVpcConfig(env);
    var maxSize = config.GetInt32("MaxSize") ?? 3;
    var minSize = config.GetInt32("MinSize") ?? 1;
    var desired = config.GetInt32("Desired") ?? 1;
    var autoScalingGroup = new Aws.AutoScaling.Group($"{shortName}-ASG", new()
    {
        VpcZoneIdentifiers = vpcConfig.Subnets,
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
            new Aws.AutoScaling.Inputs.GroupTagArgs
            {
                Key = "Name",
                Value = shortName,
                PropagateAtLaunch = true,
            },
            new Aws.AutoScaling.Inputs.GroupTagArgs
            {
                Key = "Create-By",
                Value = "Pulumi",
                PropagateAtLaunch = true,
            },
            new Aws.AutoScaling.Inputs.GroupTagArgs
            {
                Key = "Create-Time",
                Value = DateTimeOffset.UtcNow.ToOffset(new TimeSpan(8,0,0)).ToString("yyyy-MM-dd HH:mm"),
                PropagateAtLaunch = true,
            },
        },
    });

    //ECS Cluster
    var cluster = new Aws.Ecs.Cluster($"{shortName}-Cluster", new Aws.Ecs.ClusterArgs()
    {
        Tags = tags
    });

    var capacityProvider = new Aws.Ecs.CapacityProvider($"{shortName}-CP", new()
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
        Tags = tags
    });

    var ClusterCapacityProviders = new Aws.Ecs.ClusterCapacityProviders($"{shortName}-CCP", new()
    {
        ClusterName = cluster.Name,
        CapacityProviders = new[]
        {
            capacityProvider.Name,
        },
        DefaultCapacityProviderStrategies = new[]
        {
            new ClusterCapacityProvidersDefaultCapacityProviderStrategyArgs
            {
                Base = 1,
                Weight = 100,
                CapacityProvider = capacityProvider.Name,        
            },
        },     
    });

    return new Dictionary<string, object?>
    {
        ["LaunchTemplate"] = launchTemplate.GetResourceName(),
        ["AutoScalingGroup"] = autoScalingGroup.GetResourceName(),
        ["Cluster"] = cluster.GetResourceName(),
        ["CapacityProvider"] = capacityProvider.GetResourceName(),
        ["ClusterCapacityProviders"] = ClusterCapacityProviders.GetResourceName(),
    };
});

