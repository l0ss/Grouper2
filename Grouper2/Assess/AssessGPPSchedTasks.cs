using System;
using Newtonsoft.Json.Linq;

namespace Grouper2.Assess
{
    public class AssessGPPSchedTasks
    {
            public static JObject GetAssessedSchedTasks(JObject GPPSchedTasks)
            {
                JProperty AssessedGPPSchedTasksTaskProp = new JProperty("Task", GPPSchedTasks["Task"]);
                JProperty AssessedGPPSchedTasksImmediateTaskProp = new JProperty("ImmediateTaskV2", GPPSchedTasks["ImmediateTaskV2"]);
                JObject AssessedGPPSchedTasksAllJson = new JObject(AssessedGPPSchedTasksTaskProp, AssessedGPPSchedTasksImmediateTaskProp);
                return AssessedGPPSchedTasksAllJson;
                //Utility.DebugWrite("GPP is about SchedTasks");
                //Console.WriteLine(GPPSchedTasks["Task"]);
                //Console.WriteLine(GPPSchedTasks["ImmediateTaskV2"]);
            }
    }
}
