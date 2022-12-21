using System;
using System.Collections.Generic;
using AILabConfig;
using Pulumi;
using Aws = Pulumi.Aws;
using Awsx = Pulumi.Awsx;

return await Deployment.RunAsync(() =>
{
    //Config
    var config = new Config();
    var env = config.Require("ENV");
    var namePrefix = config.Require("NamePrefix");
    var shortName = $"{env}-{namePrefix}";
    var tags = new InputMap<string>()
    {
         { "Name", shortName},
         { "Create-By", "Pulumi"},
         { "Create-Time", DateTimeOffset.UtcNow.ToOffset(new TimeSpan(8,0,0)).ToString("yyyy-MM-dd HH:mm")},
    };

    var vpcConfig = new AILabVpcConfig(env);
    var securityGroupConfig = new AILabSecurityGroupConfig(env);

    //Public
    var publicSubnetMappings = new InputList<Aws.LB.Inputs.LoadBalancerSubnetMappingArgs>();
    foreach (var subnet in vpcConfig.PublicSubnets)
    {
        var mapping = new Aws.LB.Inputs.LoadBalancerSubnetMappingArgs
        {
            SubnetId = subnet,
        };
        publicSubnetMappings.Add(mapping);
    }  
    var publicLoadBalancer = new Aws.LB.LoadBalancer($"Public-{shortName}-ALB", new()
    {
        SubnetMappings = publicSubnetMappings,
        Tags = tags,
        SecurityGroups = new InputList<string>
        {
            securityGroupConfig.CloudFrontIds,
            securityGroupConfig.CloudflareIds,
            securityGroupConfig.OfficeIds,
        },
    }) ;
    
    //Private
    var privateSubnetMappings = new InputList<Aws.LB.Inputs.LoadBalancerSubnetMappingArgs>();
    foreach (var subnet in vpcConfig.PrivateSubnets)
    {
        var mapping = new Aws.LB.Inputs.LoadBalancerSubnetMappingArgs
        {
            SubnetId = subnet,
        };
        privateSubnetMappings.Add(mapping);
    }   
    var privateLoadBalancer = new Aws.LB.LoadBalancer($"Private-{shortName}-ALB", new()
    {
        SubnetMappings = privateSubnetMappings,
        Tags = tags,
        SecurityGroups = securityGroupConfig.InternalEcsIds,
        Internal = true,
    });

    return new Dictionary<string, object?>
    {
        ["PublicLoadBalancer"] = publicLoadBalancer.GetResourceName(),
        ["PrivateLoadBalancer"] = privateLoadBalancer.GetResourceName(),
    };
});

