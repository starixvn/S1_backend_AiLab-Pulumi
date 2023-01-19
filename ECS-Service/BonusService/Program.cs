using Pulumi;
using AILab.Core;

return await Deployment.RunAsync(AILabFargateService.CreateFargateService());
