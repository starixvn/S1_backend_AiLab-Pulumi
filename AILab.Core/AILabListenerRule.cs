using Pulumi;
using Pulumi.Aws.LB;

namespace AILab.Core
{
    public static class AILabListenerRule
    {
        public static void Add80PortListenerRule(Input<string> listenerArn, Input<string> targetGroupArn)
        {
            var listenerRule = new ListenerRule($"80-listenerRule", new()
            {
                ListenerArn = listenerArn,
                Priority = 100,
                Actions = new[]
                {
                    new Pulumi.Aws.LB.Inputs.ListenerRuleActionArgs
                    {
                        Type = "forward",
                        TargetGroupArn = targetGroupArn,
                    },
                },
                Conditions = new[]
                {
                    new Pulumi.Aws.LB.Inputs.ListenerRuleConditionArgs
                    {
                        PathPattern = new Pulumi.Aws.LB.Inputs.ListenerRuleConditionPathPatternArgs
                        {
                            Values = new[]
                            {
                                "/static/*",
                            },
                        },
                    },
                    new Pulumi.Aws.LB.Inputs.ListenerRuleConditionArgs
                    {
                        HostHeader = new Pulumi.Aws.LB.Inputs.ListenerRuleConditionHostHeaderArgs
                        {
                            Values = new[]
                            {
                                "admin.*",
                            },
                        },
                    },
                },
            });
        }
    }
}
