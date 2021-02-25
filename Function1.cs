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

            string tableTag = req.Headers["Tag"].ToString();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var xmlDoc = new XmlDocument();
            XmlDocument tmfDoc = new XmlDocument();
            xmlDoc.LoadXml(requestBody);
            XmlNode wellsXml = xmlDoc.GetElementsByTagName(""+tableTag+"").Item(0);

            if(wellsXml.ChildNodes.Count<1)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("NO Data from SAP")
                };

            }
            /* var start = xmlDoc.InnerXml.IndexOf("<"+tableTag+">") + 10;
             var end = xmlDoc.InnerXml.IndexOf("</" + tableTag + ">") - start;
             var ded = xmlDoc.InnerXml.Substring(start, end)*/;
            tmfDoc.LoadXml("<root>\r\n" +
                     wellsXml.InnerXml +
              "</root>\r\n");
            var arpf = string.Empty;
            string csvResponse = ConvertToCSV(tmfDoc);
            if(tableTag== "EtTableData")
            {
                arpf = "GECDF845iFFCSV";

            }
            else
            {

                arpf = "GECDF810iFFCSV";
            }
            string restResponse = await PostCsvWellsfargo(csvResponse,arpf);
            
            if (restResponse == string.Empty)
            {

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(csvResponse)
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
        private static async Task<string> PostCsvWellsfargo(String requestBody,String arpf)
        {
            var client = new RestClient("https://beta-smg.tradinggrid.gxs.com/invoke/gxs.https/receive");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-type", "application/HTTPstream");
            request.AddHeader("actionrequest", "upload");
            request.AddHeader("aprf",arpf);
            request.AddHeader("password", "^}~~ER~\\vbY_7:K");
            request.AddHeader("receiverid", "GECDFAIIN");
            request.AddHeader("userid", "royalhttps");
          request.AddParameter("application/HTTPstream", requestBody.Replace(".0",".00"), ParameterType.RequestBody);
            var restResponse = await client.ExecuteAsync(request);
           // Console.WriteLine(restResponse.Content);
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
                                           //add column data
                        foreach (DataRow currentRow in table.Rows)
                        {
                            string strRow = string.Empty;
                            for (int y = 0; y <= intColumnCount - 1; y++)
                            {
                                // "\""+ "\""
                                strRow += currentRow[y].ToString();

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
