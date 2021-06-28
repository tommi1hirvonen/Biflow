using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerUtils
{
    public record SchedulerCommand(SchedulerCommand.CommandType Type, string JobId, string ScheduleId, string CronExpression)
    {
        public enum CommandType
        {
            [EnumMember(Value = "ADD")]
            Add,
            [EnumMember(Value = "DELETE")]
            Delete,
            [EnumMember(Value = "PAUSE")]
            Pause,
            [EnumMember(Value = "RESUME")]
            Resume,
            [EnumMember(Value = "SYNCHRONIZE")]
            Synchronize,
            [EnumMember(Value = "STATUS")]
            Status
        }
    }
}
