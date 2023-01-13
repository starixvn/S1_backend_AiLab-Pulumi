using System;
using System.Collections.Generic;
using Pulumi;
using Pulumi.Aws.Ecs;

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
    var name = $"{shortName}-Cluster";
    var cluster = new Cluster(name, new ClusterArgs()
    {
        Name = name,
        Tags = tags
    });

    return new Dictionary<string, object?>
    {
        ["Cluster"] = cluster.GetResourceName(),
        ["ClusterArn"] = cluster.Arn,
    };
});

