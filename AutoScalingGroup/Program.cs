using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Pulumi;
using Aws = Pulumi.Aws;

return await Deployment.RunAsync(() =>
{
    //設定執行參數
    var config = new Config();
    var namePrefix = config.Require("NamePrefix");
    var env = config.Require("ENV");
    var name = $"{namePrefix}-{env}";

    //設定LaunchTemplate
    var imageId = config.Require("ImageId");
    var instanceType = config.Require("InstanceType");
    var launchTemplate = new Aws.Ec2.LaunchTemplate($"{name}-LT", new()
    {
        NamePrefix = namePrefix,
        ImageId = imageId,
        InstanceType = instanceType,
    });

    //設定AutoScalingGroup
    var availabilityZones = config.RequireObject<JsonElement>("AvailabilityZones").EnumerateArray().Select(i => i.GetString()).ToArray();
    var subnets = config.RequireObject<JsonElement>("Subnets").EnumerateArray().Select(i => i.GetString()).ToArray();
    var maxSize = config.GetInt32("MaxSize") ?? 2;
    var minSize = config.GetInt32("MinSize") ?? 1;
    var autoScalingGroup = new Aws.AutoScaling.Group($"{name}-ASG", new()
    {
        AvailabilityZones = availabilityZones,
        //VpcZoneIdentifiers = subnets,
        DesiredCapacity = 1,
        MaxSize = maxSize,
        MinSize = minSize,
        LaunchTemplate = new Aws.AutoScaling.Inputs.GroupLaunchTemplateArgs
        {
            Id = launchTemplate.Id,
            Version = "$Latest",
        },
    });

    return new Dictionary<string, object?>
    {
        ["LaunchTemplate"] = launchTemplate.GetResourceName(),
        ["AutoScalingGroup"] = autoScalingGroup.GetResourceName(),
    };
});

