using System.Collections.Generic;
using Pulumi;
using Pulumi.Aws.Ec2;

return await Deployment.RunAsync(() =>
{
    var config = new Config();
    var prefix = config.Require("NamePrefix");
    var vpcId = config.Require("VpcId");
    var stack = config.Require("Stack");

    var outPut = new Dictionary<string, object?>();
    var natGateways = config.GetObject<IEnumerable<NatGatewayObject>>("NatGateways");
    foreach (var item in natGateways)
    {
        var reference = new StackReference($"ailab-backend/VPC-Subnet/{stack}");
        var subnetId = reference.RequireOutput(item.Subnet).Apply(v => v.ToString());

        var name = $"{prefix}-{item.Name}";
        var ngw = new NatGateway(name, new NatGatewayArgs
        {
            SubnetId = subnetId,
            AllocationId = item.Eip,
            Tags = new InputMap<string>
            {
                { "Name", name }
            }
        });

        outPut.Add(item.Name, ngw.Id);
    }

    return outPut;
});

public class NatGatewayObject
{
    public string Name { get; set; }
    public string Subnet { get; set; }
    public string Eip { get; set; }
}