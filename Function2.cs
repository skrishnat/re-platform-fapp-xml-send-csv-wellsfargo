using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using ChoETL;
using System.Data;

namespace re_platform_fapp_xml_send_csv_wellsfargo
{
    public static class Function2
    {
        [FunctionName("Csv_to_xml")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
           
            string clientHeader = @"TransType" + "," + "CpuId" + "," +
     "MfgCode" + "," + "CustDlrNo" + "," + "ControlNo" + "," +
      "TdfApprNo" + "," + "RequestAmt" + "," + "RequestAmtSign" + "," +
      "TransDate" + "," + "TransTime" + "," + "PoNo" + "," +
      "FunctionCode" + "," + "SalesOrderNo"  +  Environment.NewLine;

            var xmlRes = string.Empty;
            string responseMessage = clientHeader + requestBody;
            StringBuilder sb = new StringBuilder();
            DataSet ds = new DataSet();
           
            string[]lines= responseMessage.Trim().Split(Environment.NewLine);
            string[] Fields;
            Fields = lines[0].Split(new char[] { ',' });
            int Cols = Fields.GetLength(0);
            DataTable dt = new DataTable();
            for (int i = 0; i < Cols; i++)
                dt.Columns.Add(Fields[i], typeof(string));
            DataRow Row;
            for (int i = 1; i < lines.GetLength(0); i++)
            {
                Fields = lines[i].Split(new char[] { ',' });
                Row = dt.NewRow();
                for (int f = 0; f < Cols; f++)
                    Row[f] = Fields[f];
                dt.Rows.Add(Row);
                
            }
            ds.Tables.Add(dt);
            xmlRes = ds.GetXml();
            return new OkObjectResult(xmlRes.Replace("Table1", "item").Replace("NewDataSet", "ItTable1"));
        }
    }
}
