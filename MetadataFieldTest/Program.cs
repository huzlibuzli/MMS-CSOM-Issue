using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using File = System.IO.File;

namespace MetadataFieldTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Document Path:");
            //var path = Console.ReadLine();
            //var path = "C://tmp/Dokument.docx";
            var path = "C://tmp/Intranet_Organisation.pdf";
            
            Console.WriteLine("URL of Site Collection:");
            //var libraryUrl = Console.ReadLine();
            var siteUrl = "http://dev2016vm1/sites/mmstest/";
            //var siteUrl = "http://dev2013v/OE/00122/00002/00001/";
            Console.WriteLine("Name of Document Library:");
            //var libraryUrl = Console.ReadLine();
            var libraryName = "Documents";
            //var libraryName = "DokTest";
           // Console.WriteLine("Name of Metadata Field:");
            //var fieldName = Console.ReadLine();
            var fieldName = "Managed";
            //var fieldName = "DMS_Schlagwoerter";
            //Console.WriteLine("Name of Metadata Field:");
            //var fieldName = Console.ReadLine();
            //var fieldValue = "Test|d2c51dbc-6f7c-4ee8-8785-cbe36e359125";
            var fieldValue = "Wert1|23463c06-0435-4cd7-8fb3-08f7e648e291";
            var url = siteUrl + "_api/web/lists/getbytitle('" + libraryName + "')/rootfolder/files/add(url='" + DateTime.Now.Ticks + path.Substring(path.LastIndexOf('/') + 1) + "', overwrite=true)";
            UploadToSharePoint(path, siteUrl, url, fieldName, fieldValue).Wait();
            Console.WriteLine("Upload finished.");
        }

        private static async Task UploadToSharePoint(string path, string siteUrl, string requestUrl, string fieldName, string fieldValue)
        {
            using (var handler = new HttpClientHandler { UseDefaultCredentials = true })
            {
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json; odata=verbose");

                    var contextInfo = await client.PostAsync(siteUrl + "_api/contextinfo", null);
                    var contextResponseContent = await contextInfo.Content.ReadAsStringAsync().ConfigureAwait(true);
                    JToken contextObj = JsonConvert.DeserializeObject<dynamic>(contextResponseContent);
                    var digestValue = contextObj["d"]["GetContextWebInformation"]["FormDigestValue"];
                    client.DefaultRequestHeaders.Add("X-RequestDigest", (string) digestValue);


                    var streamContent = new StreamContent(File.OpenRead(path));
                    var byteArrayContent = new ByteArrayContent(streamContent.ReadAsByteArrayAsync().Result);
                    byteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                    HttpResponseMessage uploadResponse = await client.PostAsync(requestUrl, byteArrayContent).ConfigureAwait(true);
                    var uploadResponseContent = await uploadResponse.Content.ReadAsStringAsync().ConfigureAwait(true);

                    if (uploadResponse.IsSuccessStatusCode)
                    {
                        JToken spFileObj = JsonConvert.DeserializeObject<dynamic>(uploadResponseContent);
                        ClientContext context = new ClientContext(Regex.Replace(requestUrl, "/_api.*", "", RegexOptions.IgnoreCase));
                        var web = context.Web;
                        var file = web.GetFileByServerRelativeUrl((string) spFileObj.SelectToken("d.ServerRelativeUrl"));
                        var fileItem = file.ListItemAllFields;
                        context.Load(web);
                        context.Load(file);
                        context.Load(fileItem);
                        context.ExecuteQuery();

                        var wasCheckedOut = file.CheckOutType != CheckOutType.None;
                        if (!wasCheckedOut)
                        {
                            file.CheckOut();
                        }


                        fileItem.ParseAndSetFieldValue(fieldName, fieldValue);

                        fileItem.Update();
                        if (!wasCheckedOut)
                        {
                            file.CheckIn(string.Empty, CheckinType.OverwriteCheckIn);
                        }

                        context.ExecuteQuery();
                    }
                }
            }
        }
    }
}
