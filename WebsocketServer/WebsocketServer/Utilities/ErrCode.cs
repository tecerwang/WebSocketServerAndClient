using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketServer.Utilities
{
    static class ErrCode
    {
        public static int Unkown = -100;

        public static int OK = 0;
        
        public static int AlreadyInGroup = 1000;
        public static int NotInGroup = 1000;
    }
}
