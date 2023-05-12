using BenchmarkDotNet.Running;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.IO;

namespace OptimizeMePlease
{

    /// <summary>
    /// Steps: 
    /// 
    /// 1. Create a database with name "OptimizeMePlease"
    /// 2. Run application Debug/Release mode for the first time. IWillPopulateData method will get the script and populate
    /// created db.
    /// 3. Comment or delete IWillPopulateData() call from Main method. 
    /// 4. Go to BenchmarkService.cs class
    /// 5. Start coding within GetAuthors_Optimized method
    /// GOOD LUCK! :D 
    /// </summary>
    public class Program
    {
        static void Main(string[] args)
        {
            //Debugging 
            BenchmarkService benchmarkService = new BenchmarkService();
            var result1 = benchmarkService.GetAuthors();
            var result2 = benchmarkService.GetAuthors_Optimized();
            var result3 = benchmarkService.GetAuthors_Optimized_Compiled();

            BenchmarkRunner.Run<BenchmarkService>();


            //Comment me after first execution, please.
            //IWillPopulateData();

        }

        public static void IWillPopulateData()
        {
            string sqlConnectionString = @"Server=localhost;Database=OptimizeMePlease;Trusted_Connection=True;Integrated Security=true;MultipleActiveResultSets=true";

            string workingDirectory = Environment.CurrentDirectory;
            string path = Path.Combine(Directory.GetParent(workingDirectory).Parent.Parent.FullName, @"script.sql");
            string script = File.ReadAllText(path);

            SqlConnection conn = new SqlConnection(sqlConnectionString);

            Server server = new Server(new ServerConnection(conn));

            server.ConnectionContext.ExecuteNonQuery(script);
        }
    }
}
