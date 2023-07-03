using Pulumi.Automation;
using System.Reflection;

//設定傳入參數
var env = args.Any() ? args[0] : throw new ArgumentException("Please input EnvironmentName (DEV/RLS/PRD .... )");
var projectName = args.Any() ? args[1] : throw new ArgumentException("Please input ProjectName (ConfigService/MemberSite .... )");
var version = args.Any() ? args[2] : throw new ArgumentException("Please input version (docker image tag)");
var isDestroy = args.Length > 3 && (args[3] == "true" || args[3] == "1");
Console.WriteLine($"Deploy FargateService {env} - {projectName} - {version}");

//設定LocalWorkspace的工作目錄
var executingDir = new DirectoryInfo(Assembly.GetExecutingAssembly().Location).Parent.FullName;
var workDir = Path.Combine(executingDir, "..", "..", "..", "..", "ECS-Service", projectName);
var workspace = await LocalWorkspace.CreateAsync(new LocalWorkspaceOptions()
{
    WorkDir = workDir
});
var stacks = await workspace.ListStacksAsync();

//檢查所有stack，stack的config有符合env就會進行更新
//2023-07-03 Mickey 調整 By evn 去抓自己的 ENV=>yaml
foreach (var stackName in stacks.Where(x => x.Name == env).Select(i => i.Name))
{
    var stackArgs = new LocalProgramArgs(stackName, workDir);
    var stack = await LocalWorkspace.SelectStackAsync(stackArgs);
    var configEnv = await stack.GetConfigAsync("ENV");

    //檢查環境
    if (configEnv != null && configEnv.Value.Contains(env))
    {
        Console.WriteLine($"=========== Start Stack:{stackName} ===========");

        Console.WriteLine($"Stack: {stackName} is refreshing ...");
        await stack.RefreshAsync(new RefreshOptions { OnStandardOutput = Console.WriteLine });
        Console.WriteLine($"Stack: {stackName} is refresh complete.");
        await stack.SetConfigAsync("Version", new ConfigValue(version));

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
            Console.WriteLine($"ServiceName: {result.Outputs["ServiceName"].Value}");
        }

        Console.WriteLine($"=========== End Stack:{stackName} ===========");
    }
}
