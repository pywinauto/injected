using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.IO;

namespace InjectedWorker
{
    public static class Server
    {
        private static IRequestHandler Handler = new WPF.WPFControlsHandler();

        public static int Start(string arg)
        {
            int pid = Process.GetCurrentProcess().Id;
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(String.Format("pywinauto_{0}", pid), PipeDirection.InOut, 1, PipeTransmissionMode.Message))
            {
                bool stop = false;
                while (!stop)
                {
                    try
                    {
                        Debug.Print("Wait for connection...");
                        pipeServer.WaitForConnection();
                        Debug.Print("Connected");

                        while (pipeServer.IsConnected)
                        {
                            string request = ReadTextFromStream(pipeServer);
                            if (request == null || request == "disconnect")
                            {
                                Debug.Print("Disconnect");
                                break;
                            }
                            else if (request == "shutdown")
                            {
                                Debug.Print("Shutdown server");
                                stop = true;
                                break;
                            }

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

            return 0;
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
