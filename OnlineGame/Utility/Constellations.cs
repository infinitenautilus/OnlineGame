using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OnlineGame.Utility
{
    public class Constellations
    {
        public static string TIMESTAMP
        {
            get
            {
                return DateTime.Now.ToString("yyyy-dd-MM hh:mm:ss.fff");
            }
        }

        public static string GAMENAME
        {
            get
            {
                return "ORC";
            }
        }

        public static string STORAGEDIR
        {
            get
            {
                return @"E:\Temp\Mud\";
            }
        }

        public static string LOGFILE
        {
            get
            {
                return $@"{STORAGEDIR}Logfile.txt";
            }
        }

        public static int HOSTPORT
        {
            get
            {
                return 9998;
            }
        }

        public static string HOSTADDRESSSTRING
        {
            get
            {
                return "10.0.0.85"; 
            }
        }

        public static IPAddress HOSTADDRESS
        {
            get
            {
                return IPAddress.Parse(HOSTADDRESSSTRING);
            }
        }
    }
}
