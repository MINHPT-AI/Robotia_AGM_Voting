using Microsoft.Extensions.Hosting;

namespace Mms.PrintAgent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "MMS Print Agent";
            });

            Console.WriteLine("MMS Print Agent stub started.");

            var host = builder.Build();
            host.Run();
        }
    }
}
