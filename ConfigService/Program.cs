using System.Collections.Generic;
using Pulumi;
using Aws = Pulumi.Aws;
using Awsx = Pulumi.Awsx;

return await Deployment.RunAsync(() => 
{
    //設定環境
    var env = "DEV";
    var serviceName = "ConfigService";

    //設定docker image與版本號
    var registry = "13.213.82.69/configservice/configservice";
    var version = $"3772c1b";

    //設定執行參數
    var config = new Config();
    var containerPort = config.GetInt32("containerPort") ?? 80;
    var cpu = config.GetInt32("cpu") ?? 512;
    var memory = config.GetInt32("memory") ?? 128;

    //設定cluster與loadbalancer
    var cluster = new Aws.Ecs.Cluster($"{env}-{serviceName}");
    var loadbalancer = new Awsx.Lb.ApplicationLoadBalancer("loadbalancer");

    //建立服務
    var imageUri = $"{registry}:${version}";
    var service = new Awsx.Ecs.EC2Service("service", new()
    {
        Cluster = cluster.Arn,
        TaskDefinitionArgs = new Awsx.Ecs.Inputs.EC2ServiceTaskDefinitionArgs
        {
            Container = new Awsx.Ecs.Inputs.TaskDefinitionContainerDefinitionArgs
            { 
                Name = $"{env}-{serviceName}-{version}",
                Image = imageUri,
                Cpu = cpu,
                Memory = memory,
                Essential = true,
                PortMappings = new[]
                {
                    new Awsx.Ecs.Inputs.TaskDefinitionPortMappingArgs
                    {
                        ContainerPort = containerPort,
                        TargetGroup = loadbalancer.DefaultTargetGroup,
                    },
                },
            },
        },
    });

    return new Dictionary<string, object?>
    {
        ["url"] = loadbalancer.LoadBalancer.Apply(loadBalancer => Output.Format($"http://{loadBalancer.DnsName}")),
    };
});

