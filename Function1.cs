using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Xml;
using System.Text;
using System.Data;
using RestSharp;
using System.Net.Http;
using System.Net;

namespace re_platform_fapp_xml_send_csv_wellsfargo
{
    public static class Function1
    {
        [FunctionName("Post_Csv_GXS")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var xmlDoc = new XmlDocument();
            XmlDocument tmfDoc = new XmlDocument();
            xmlDoc.LoadXml(requestBody);
            var start = xmlDoc.InnerXml.IndexOf("<EtTableData>") + 13;
            var end = xmlDoc.InnerXml.IndexOf("</EtTableData>") - start;
            var ded = xmlDoc.InnerXml.Substring(start, end);
            tmfDoc.LoadXml("<root>\r\n" +
                     ded +
              "</root>\r\n");

            string csvResponse = ConvertToCSV(tmfDoc);
            string restResponse = await PostCsvWellsfargo(csvResponse);
            if (restResponse == string.Empty)
            {

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("Data Sent to GXS")
                };

            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("error occured")
                };
            }
        }   
        private static async Task<string> PostCsvWellsfargo(String requestBody)
        {
            var client = new RestClient("https://beta-smg.tradinggrid.gxs.com/invoke/gxs.https/receive");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-type", "application/HTTPstream");
            request.AddHeader("actionrequest", "upload");
            request.AddHeader("aprf", "*BIN");
            request.AddHeader("password", "^}~~ER~\\vbY_7:K");
            request.AddHeader("receiverid", "GECDFAIIN");
            request.AddHeader("userid", "royalhttps");
            request.AddParameter("application/HTTPstream", requestBody, ParameterType.RequestBody);
            var restResponse = await client.ExecuteAsync(request);
            Console.WriteLine(restResponse.Content);
            return restResponse.Content;
        }


        private static string ConvertToCSV(XmlDocument jsonContent)
        {

            StringReader theReader = new StringReader(jsonContent.InnerXml);
            DataSet theDataSet = new DataSet();
            theDataSet.ReadXml(theReader);

            StringBuilder content = new StringBuilder();

            if (theDataSet.Tables.Count >= 1)
            {
                DataTable table = theDataSet.Tables[0];

                if (table.Rows.Count > 0)
                {
                    DataRow dr1 = (DataRow)table.Rows[0];
                    int intColumnCount = dr1.Table.Columns.Count;
                    int index = 1;

                    //add column names
                    foreach (DataColumn item in dr1.Table.Columns)
                    {
                        content.Append(String.Format("\"{0}\"", item.ColumnName));
                        if (index < intColumnCount)
                            content.Append(",");
                        else
                            content.Append("\r\n");
                        index++;
                    }

                    //add column data
                    foreach (DataRow currentRow in table.Rows)
                    {
                        string strRow = string.Empty;
                        for (int y = 0; y <= intColumnCount - 1; y++)
                        {
                            strRow += "\"" + currentRow[y].ToString() + "\"";

                            if (y < intColumnCount - 1 && y >= 0)
                                strRow += ",";
                        }
                        content.Append(strRow + "\r\n");
                    }
                }

            }

            return content.ToString();
        }
    }
}