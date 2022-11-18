using System.Collections.Generic;
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
    var vpcId = config.Require("vpcId");

    var containerPort = config.GetInt32("containerPort") ?? 80;
    var cpu = config.GetInt32("cpu") ?? 4;
    var memory = config.GetInt32("memory") ?? 2;
    var desiredCount = config.GetInt32("desiredCount") ?? 1;

    var imageUri = $"{registry}:{version}";
    var serviceName = $"{brand}-{env}-{name}-{version}";
    var loadbalancer = new Awsx.Lb.ApplicationLoadBalancer($"{brand}-{env}-LB");

    //建立服務
    var service = new Awsx.Ecs.EC2Service(serviceName, new()
    {
        Cluster = $"{brand}-{env}",
        DesiredCount = desiredCount,
        NetworkConfiguration = new Aws.Ecs.Inputs.ServiceNetworkConfigurationArgs
        {
            Subnets = new InputList<string>
            {
                "arn:aws:ec2:ap-southeast-1:761771912479:subnet/subnet-09332247c64aff074",
            }
        },
        TaskDefinitionArgs = new Awsx.Ecs.Inputs.EC2ServiceTaskDefinitionArgs
        {
            Container = new Awsx.Ecs.Inputs.TaskDefinitionContainerDefinitionArgs
            {
                Name = serviceName,
                Image = imageUri,
                Cpu = cpu,
                Memory = memory,
                Essential = true,
                PortMappings = new[]
                {
                    new Awsx.Ecs.Inputs.TaskDefinitionPortMappingArgs
                    {
                        ContainerPort = containerPort,
                        TargetGroup =  new Aws.LB.TargetGroup($"{env}-{name}-{version}-TG", new()
                        {
                            Port = 80,
                            Protocol = "HTTP",
                            VpcId = vpcId
                        }),
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

