﻿using Pulumi;
using AILab.Core;
using Pulumi.Automation;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Reflection;

namespace ConfigService
{
    class Program
    {
        static async Task Main(string[] args) 
        {
            var env = args.Any() ? args[0] : throw new ArgumentException("Please input EnvironmentName (DEV/RLS/PRD .... )");
            var projectName = args.Any() ? args[1] : throw new ArgumentException("Please input ProjectName (ConfigService/MemberSite .... )");

            var executingDir = new DirectoryInfo(Assembly.GetExecutingAssembly().Location).Parent.FullName;
            var workDir = Path.Combine(executingDir, "..", "..", "..", "..", "ECS-Service", projectName);

            var workspace = await LocalWorkspace.CreateAsync(new LocalWorkspaceOptions()
            {
                WorkDir = workDir
            });
            var stacks = await workspace.ListStacksAsync();

            foreach (var stackName in stacks.Select(i => i.Name))
            {       
                var stackArgs = new LocalProgramArgs(stackName, workDir);
                var stack = await LocalWorkspace.SelectStackAsync(stackArgs);
                var configEnv = await stack.GetConfigAsync("ENV");
                
                //檢查相同環境才執行
                if (configEnv != null && configEnv.Value.Contains(env))
                {
                    Console.WriteLine($"ENV is {env}");
                    Console.WriteLine($"Stack Name is {stackName}");
                    Console.WriteLine($"Stack: {stackName} is refreshing ...");
                    await stack.RefreshAsync(new RefreshOptions { OnStandardOutput = Console.WriteLine });
                    Console.WriteLine($"Stack: {stackName} is refresh complete.");

                    var isDestroy = args.Any() && args[2] == "destroy";
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
                                Console.WriteLine($"    {change.Key}: {change.Value}");
                        }

                        Console.WriteLine($"ServiceName: {result.Outputs["ServiceName"].Value}");
                    }
                }
            }
        }
    }
}