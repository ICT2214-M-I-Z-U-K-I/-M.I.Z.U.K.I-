#pragma warning disable CA2252, CA1416, CS8618, CS8600, CS8625
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Quic;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    internal class MizukiHeader
    {
        public byte Opcode { get; set; } // 1
        public Guid SelfUuid { get; set; } // 16
        public Guid TargetUuid { get; set; } // 16
        public uint DataLength { get; set; } // 4
        public string FileName = null; // 0
    }
}
