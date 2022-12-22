using System;
using System.Collections.Generic;

using AILabConfig;
using Pulumi;
using Pulumi.Aws.Ecs.Inputs;
using Aws = Pulumi.Aws;
using Awsx = Pulumi.Awsx;



return await Deployment.RunAsync(() =>
{
    //Config
    var config = new Config();
    var env = config.Require("ENV");
    var namePrefix = config.Require("NamePrefix");
    var shortName = $"{namePrefix}-{env}";
    var tags = new InputMap<string>()
    {
         { "Name", shortName},
         { "Create-By", "Pulumi"},
         { "Create-Time", DateTimeOffset.UtcNow.ToOffset(new TimeSpan(8,0,0)).ToString("yyyy-MM-dd HH:mm")},
    };

    //Create ECS Cluster
    var cluster = new Aws.Ecs.Cluster($"{shortName}-Cluster", new Aws.Ecs.ClusterArgs()
    {
        Tags = tags
    });

    return new Dictionary<string, object?>
    {
        ["Cluster"] = cluster.GetResourceName(),
    };
});

