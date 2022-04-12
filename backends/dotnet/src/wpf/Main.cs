using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.IO;

namespace InjectedWorker
{
    public class Main
    {
        private static WPF.WPFControlsHandler Handler = new WPF.WPFControlsHandler();

        public static int StartServer(string arg)
        {
            int pid = Process.GetCurrentProcess().Id;
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(String.Format("pywinauto_{0}", pid), PipeDirection.InOut, 1, PipeTransmissionMode.Message))
            {
                while (true)
                {
                    try
                    {
                        Debug.Print("Wait for connection...");
                        pipeServer.WaitForConnection();
                        Debug.Print("Connected");

                        while (pipeServer.IsConnected)
                        {
                            string request = ReadTextFromStream(pipeServer);
                            if (request == null)
                                break;
                            string response = Handler.ProcessRequest(request);
                            var data = Encoding.UTF8.GetBytes(response);
                            pipeServer.Write(data, 0, data.Length);
                            pipeServer.Flush();
                            pipeServer.WaitForPipeDrain();
                        }
                        pipeServer.Disconnect();
                    }
                    catch (IOException e)
                    {
                        Debug.Print("ERROR: {0}", e.Message);
                        pipeServer.Disconnect();
                    }
                }
            }

#pragma warning disable CS0162 // Unreachable code detected
            return 0;
#pragma warning restore CS0162 // Unreachable code detected
        }

        private static string ReadTextFromStream(NamedPipeServerStream stream)
        {
            byte[] buffer = new byte[4*1024];
            MemoryStream ms = new MemoryStream();
            do 
            { 
                ms.Write(buffer, 0, stream.Read(buffer, 0, buffer.Length)); 
            }
            while (!stream.IsMessageComplete);

            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}
