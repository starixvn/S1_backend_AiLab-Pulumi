using System.Collections.Generic;
using Pulumi;
using Pulumi.Aws.Ec2;

return await Deployment.RunAsync(() =>
{
    var config = new Config();
    var prefix = config.Require("NamePrefix");
    var vpcId = config.Require("VpcId");

    var outPut = new Dictionary<string, object?>();
    var subnets = config.GetObject<IEnumerable<SubnetObject>>("Subnets");
    foreach (var item in subnets)
    {
        var name = $"{prefix}-{item.Name}";
        var subnet = new Subnet(name, new SubnetArgs
        {
            Tags = new InputMap<string>
            {
                { "Name", name }
            },
            VpcId = vpcId,
            AvailabilityZone = item.AvailabilityZone,
            CidrBlock = item.Cidr
        });

        outPut.Add(item.Name, subnet.Id);
    }

    return outPut;
});

public class SubnetObject
{
    public string Name { get; set; }
    public string AvailabilityZone { get; set; }
    public string Cidr { get; set; }
}