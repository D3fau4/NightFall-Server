using System;

namespace NightFall_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            // args[0] = Firmware folder
            // args[1] = Keyset path
            // args[2] = Firmware version (0.0.0)
            // args[3] = int version
            // args[4] = Server folder

            Console.WriteLine("Generating Server FS");
            Server.generateServerFS(args[4]);
            Server.generateLastInfo(args[3], args[2], args[4]);
            Console.WriteLine("Generating fw.json");
            Server.generateJson(args[0], args[1], args[2], args[3], args[4]);
            Console.WriteLine("Copying ncas to server");
            Server.CopyNCAfiles(args[0], args[4]);
        }
    }
}
