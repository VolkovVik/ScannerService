using System.Management;
using Serilog;

namespace ScannerService {
    class ManagementDevice {
        public static string GetScannerPort( string id = "USB\\VID_1FBB&PID_3600" ) {
            var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity");
            ManagementObject comPort = null;
            foreach ( var o in searcher.Get() ) {
                var queryObj = (ManagementObject) o;
                if ( !queryObj [ "DeviceID" ].ToString().StartsWith( id ) ) continue;
                comPort = queryObj;
                break;
            }

            if ( comPort == null ) {
                Log.Error( "[Task Manager] Com port name not found" );
                return string.Empty;
            }
            Log.Information( comPort [ "DeviceID" ].ToString() );
            Log.Information( comPort [ "Name" ].ToString() );
            Log.Information( comPort [ "Status" ].ToString() );

            var name = comPort["Name"].ToString();
            var i1 = name.IndexOf('(');
            var i2 = name.IndexOf(')');
            var portName = name.Substring(i1 + 1, i2 - i1 - 1);
            Log.Information( $"[Task Manager] Com port {portName} founded" );
            return portName;
        }
    }
}
