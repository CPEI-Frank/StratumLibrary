using System;


namespace StratumTest
{
    using Stratum;

    class StratumTest
    {
        static void Main(string[] args)
        {
            Stratum s = new Stratum("192.168.1.100", 40001);

            while (true)
            {
				var res = s.Invoke<Newtonsoft.Json.Linq.JObject>("blockchain.headers.subscribe", new object[] {});

                // var res = s.Invoke<string>("blockchain.transaction.get", "101379cb55ac431c435db40b4325f858568b0de3d8bd652a23a19e5d62521a72");

                //                var res = s.Invoke<Newtonsoft.Json.Linq.JObject>("blockchain.address.get_balance", "4PQtUNZ2aBYpZpVMPV2Qgz1PitCqgoT388");
                //                var res = s.Invoke<Newtonsoft.Json.Linq.JArray>("blockchain.address.get_history", "4PQtUNZ2aBYpZpVMPV2Qgz1PitCqgoT388");
                //                var res = s.Invoke<Newtonsoft.Json.Linq.JArray>("blockchain.address.listunspent", "4PQtUNZ2aBYpZpVMPV2Qgz1PitCqgoT388");

                Console.Write(res.Result.ToString());
                Console.ReadLine();
            }
        }
    }
}
