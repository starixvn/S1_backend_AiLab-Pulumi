using Pulumi.Automation;
using System.Reflection;

//設定傳入參數
var stackName = args.Any() ? args[0] : throw new ArgumentException("Please input StackName (Bonus-DEV/Config-DEV .... )");
var isDestroy = args.Length > 1 && (args[1] == "true" || args[1] == "1");

Console.WriteLine($"Deploy EC2-TargetGroup {stackName}");

//設定LocalWorkspace的工作目錄
var executingDir = new DirectoryInfo(Assembly.GetExecutingAssembly().Location).Parent.FullName;
var workDir = Path.Combine(executingDir, "..", "..", "..", "..", "EC2-TargetGroup");
var stackArgs = new LocalProgramArgs(stackName, workDir);
var stack = await LocalWorkspace.CreateOrSelectStackAsync(stackArgs);

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

