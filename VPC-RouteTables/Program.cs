using System.Collections.Generic;
using System.Linq;
using AILabConfig;
using Pulumi;
using Pulumi.Aws.Ec2;
using Pulumi.Aws.Ec2.Inputs;

return await Deployment.RunAsync(() =>
{
    var config = new Config();
    var prefix = config.Require("NamePrefix");
    var vpcId = config.Require("VpcId");
    var stack = config.Require("Stack");

    var outPut = new Dictionary<string, object?>();
    var routeTables = config.GetObject<IEnumerable<RouteTableObject>>("RouteTables");
    foreach (var item in routeTables)
    {
        var name = $"{prefix}-{item.Name}";
        if (item.Type == "public")
        {
            var publicRt = new RouteTable(name, new RouteTableArgs()
            {
                VpcId = vpcId,
                Routes = item.Rules.Select(a => new RouteTableRouteArgs()
                {
                    CidrBlock = a.Cidr,
                    GatewayId = a.Target
                }).ToArray(),
                Tags = new InputMap<string>
                {
                    { "Name", name }
                }
            });
            outPut.Add(item.Name, publicRt.Id);
        }
        else if (item.Type == "private") 
        {
            var awsConfig = new AILabAwsConfig("PRD");
            var reference = new StackReference($"{awsConfig.PulumiOrganization}/{awsConfig.NatGatewayProject}/{stack}");
            var natGatewayId = reference.RequireOutput(item.Rules.First().Target).Apply(v => v.ToString());
            var privateRt = new RouteTable(name, new RouteTableArgs()
            {
                VpcId = vpcId,
                Routes = item.Rules.Select(a => new RouteTableRouteArgs()
                {
                    CidrBlock = a.Cidr,
                    NatGatewayId = natGatewayId
                }).ToArray(),
                Tags = new InputMap<string>
                {
                    { "Name", name }
                }
            });
            outPut.Add(item.Name, privateRt.Id);
        }
    }

    return outPut;
});


public class Rule
{
    public string Cidr { get; set; }
    public string Target { get; set; }
}

public class RouteTableObject
{
    public string Type { get; set; }
    public string Name { get; set; }
    public IList<Rule> Rules { get; set; }
}