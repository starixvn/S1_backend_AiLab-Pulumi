using System.Collections.Generic;
using System.Text.Json;
using Pulumi;
using Aws = Pulumi.Aws;
using Awsx = Pulumi.Awsx;

return await Deployment.RunAsync(() =>
{
    //todo:取得外部傳入的參數
    var brand = $"ailab";
    var env = "DEV";
    var version = $"3772c1b";

    //設定執行參數
    var config = new Config();
    var name = config.Require("name");
    var registry = config.Require("registry");
    var network = config.RequireObject<JsonElement>("network");
    var vpcId = network.GetProperty("vpcId").GetRawText();
    var subnets = new InputList<string>();
    foreach (var subnet in network.GetProperty("subnets").EnumerateArray())
    {
        var subnetName = subnet.GetRawText();
        if (!string.IsNullOrEmpty(subnetName))
        {
            subnets.Add(subnetName);
        }    
    }

    var containerPort = config.GetInt32("containerPort") ?? 80;
    var cpu = config.GetInt32("cpu") ?? 4;
    var memory = config.GetInt32("memory") ?? 2;
    var desiredCount = config.GetInt32("desiredCount") ?? 1;

    var imageUri = $"{registry}:{version}";
    var serviceName = $"{brand}-{env}-{name}-{version}";
    var cluster = new Aws.Ecs.Cluster($"{brand}-{env}");
    var loadbalancer = new Awsx.Lb.ApplicationLoadBalancer($"{brand}-{env}-LB");
    var repo = new Awsx.Ecr.Repository("repo");
    var image = new Awsx.Ecr.Image("image", new()
    {
        RepositoryUrl = repo.Url,
        Path = "./app",
    });

    //建立服務
    var service = new Awsx.Ecs.EC2Service(serviceName, new()
    {
        Cluster = cluster.Arn,
        DesiredCount = desiredCount,
        NetworkConfiguration = new Aws.Ecs.Inputs.ServiceNetworkConfigurationArgs
        {
            Subnets = subnets,
            SecurityGroups =  new InputList<string> { "sg-08231292e5f0287c9" }
        },
        TaskDefinitionArgs = new Awsx.Ecs.Inputs.EC2ServiceTaskDefinitionArgs
        {
            Container = new Awsx.Ecs.Inputs.TaskDefinitionContainerDefinitionArgs
            {
                Name = serviceName,
                Image = image.ImageUri,
                Cpu = cpu,
                Memory = memory,
                Essential = true,
                PortMappings = new[]
                {
                    new Awsx.Ecs.Inputs.TaskDefinitionPortMappingArgs
                    {
                        ContainerPort = containerPort,
                        ////TargetGroup =  new Aws.LB.TargetGroup($"{env}-{name}-{version}-TG", new()
                        //{
                        //    Port = 80,
                        //    Protocol = "HTTP",
                        //    VpcId = vpcId
                        //}),
                        TargetGroup = loadbalancer.DefaultTargetGroup  
                    },
                },
            },
        },
    });

    return new Dictionary<string, object?>
    {
        ["serviceName"] = service.GetResourceName(),
    };
});

