using Pulumi.Automation;
using System.Reflection;

//設定傳入參數
var serviceName = args.Any() ? args[0] : throw new ArgumentException("Please input ServiceName (Bonus/Payment .... )");
var namePrefix = args.Any() ? args[1] : throw new ArgumentException("Please input NamePrefix (AI1/AI2/AI3 .... )");
var env = args.Any() ? args[2] : throw new ArgumentException("Please input EnvironmentName (DEV/RLS/PRD .... )");
var loadBalancerStack = args.Any() ? args[3] : throw new ArgumentException("Please input LoadBalancerStack (EC2-TargetGroup Stack Name)");
var port = args.Any() ? args[4] : throw new ArgumentException("Please input port for TargetGroup.");
var isDestroy = args.Length > 5 && (args[5] == "true" || args[3] == "1");

Console.WriteLine($"Deploy EC2-TargetGroup {env} - {namePrefix}-{serviceName}");

//設定LocalWorkspace的工作目錄
var executingDir = new DirectoryInfo(Assembly.GetExecutingAssembly().Location).Parent.FullName;
var workDir = Path.Combine(executingDir, "..", "..", "..", "..", "EC2-TargetGroup");
var stackName = $"{serviceName}-{env}";
var stackArgs = new LocalProgramArgs(stackName, workDir);
var stack = await LocalWorkspace.CreateOrSelectStackAsync(stackArgs);
await stack.SetConfigAsync("NamePrefix", new ConfigValue(namePrefix));
await stack.SetConfigAsync("ENV", new ConfigValue(env));
await stack.SetConfigAsync("LoadBalancerStack", new ConfigValue(loadBalancerStack));
await stack.SetConfigAsync("ContainerPort", new ConfigValue(port));

Console.WriteLine($"Stack: {stackName} is refreshing ...");
await stack.RefreshAsync(new RefreshOptions { OnStandardOutput = Console.WriteLine });
Console.WriteLine($"Stack: {stackName} is refresh complete.");

if (isDestroy)
{
    Console.WriteLine("destroying stack...");
    await stack.DestroyAsync(new DestroyOptions { OnStandardOutput = Console.WriteLine });
    Console.WriteLine("stack destroy complete");
}
else
{
    Console.WriteLine("updating stack...");
    var result = await stack.UpAsync(new UpOptions { OnStandardOutput = Console.WriteLine });

    if (result.Summary.ResourceChanges != null)
    {
        Console.WriteLine("update summary:");
        foreach (var change in result.Summary.ResourceChanges)
        {
            Console.WriteLine($"    {change.Key}: {change.Value}");
        }
    }
    Console.WriteLine($"TargetGroupStack: {stackName}");
    Console.WriteLine($"PublicTargetGroup: {result.Outputs["PublicTargetGroup"].Value}");
    Console.WriteLine($"PrivateTargetGroup: {result.Outputs["PrivateTargetGroup"].Value}");
}

