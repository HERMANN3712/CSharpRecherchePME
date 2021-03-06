using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Text.RegularExpressions;
using System.Net;
using System.Text.Json;
using System.Collections.Generic;
using System.Diagnostics;
using HtmlAgilityPack;
using System.Web;

namespace CSharpRecherchePME
{
    public static class QueriesHttp
    {
        private const string URLJob = "https://labonneboite.pole-emploi.fr/suggest_job_labels?term=";
        private const string URLLocation = "https://labonneboite.pole-emploi.fr/autocomplete/locations?term=";
        private const string URLPME = "https://labonneboite.pole-emploi.fr/entreprises?l=@codepostal&occupation=@occupation&departments=@departement";
        static readonly HttpClient client = new HttpClient();
        private static async Task<Object> ExecuteQueryHttp(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(url))
                {

                    using (HttpContent content = response.Content)
                    {
                        string result = await content.ReadAsStringAsync();
                        if (!response.IsSuccessStatusCode)
                        {
                            return (HttpStatusCode)response.StatusCode;
                        }
                        return result;
                    }
                }
            }
        }

        private static HtmlNodeCollection ExecuteQueryPME(string url)
        {
            var xmlDoc = new HtmlDocument();
            var root = HtmlNode.CreateNode("<root></root>");
            xmlDoc.DocumentNode.AppendChild(root);

            string compare = "";

            for (int i = 0; i < 7; i++)
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                WebClient client = new WebClient();
                string html = client.DownloadString(url + String.Format("&d=30&from={0}&sort=score&to=800", i * 100 + 1));
                htmlDoc.LoadHtml(html);
                HtmlNodeCollection nodes = htmlDoc.DocumentNode.SelectNodes("//div[starts-with(@id,'company-')]");
                if(nodes == null) return null;

                if (i == 0)
                {
                    compare = nodes[0].Id;
                }
                else if (compare.Equals(nodes[0].Id)) break;
                root.AppendChildren(nodes);
            }

            return root.ChildNodes;
        }

        private static string GetResult(string url)
        {
            try
            {
                var result = ExecuteQueryHttp(url).Result;
                if (result is HttpStatusCode)
                {
                    string key = ((HttpStatusCode)result).ToString();
                    return Regex.Replace(key, "(\\B[A-Z])", " $1");

                }
                else if (result is string)
                {
                    return (string)result;
                }

                return null;

            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public static List<Job> GetListJobs(string search)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            var jobs = JsonSerializer.Deserialize<List<Job>>(GetResult(URLJob + search), options);
            return jobs;

        }

        public static List<Location> GetListLocations(string search)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            Debug.WriteLine(URLLocation + search);
            var locs = JsonSerializer.Deserialize<List<Location>>(GetResult(URLLocation + search), options);
            return locs;
        }

        public static List<PME> GetListPME(string job, string location, string departement)
        {
            var url = URLPME.Replace("@codepostal", location).Replace("@occupation", job).Replace("@departement", departement);
            //var url = "https://labonneboite.pole-emploi.fr/entreprises?j=Conduite+d%27engins+de+d%C3%A9placement+des+charges+(Cariste%2C+...)&l=31000&lat=43.613408&lon=1.44607&occupation=conduite-d-engins-de-deplacement-des-charges";

            var nodes = ExecuteQueryPME(url);
            if (nodes == null) return new List<PME>();
            
            List<PME> list = new List<PME>();
            
            foreach (HtmlNode node in nodes)
            {
                PME pme = new PME();

                string id = node.Id;
                pme.id = id;

                var n = new HtmlDocument();
                n.LoadHtml(node.OuterHtml);

                var title =  n.DocumentNode.SelectSingleNode("//h3[1]/span[1]").InnerText;
                title = HttpUtility.HtmlDecode(title).Replace(';', ',');
                pme.title = title;

                var tel = n.DocumentNode.SelectSingleNode("//a[starts-with(@href, 'tel:')]")?.InnerText ?? "no phone";
                pme.tel = tel;

                var detail = n.DocumentNode.SelectSingleNode("//h4[1]")?.InnerText ?? "";
                detail = HttpUtility.HtmlDecode(detail).Replace(';', ',');
                pme.detail = detail;

                var address = n.DocumentNode.SelectSingleNode("//p[@class = 'easy-copy-paste']")?.InnerText ?? "";
                address = HttpUtility.HtmlDecode(address).Replace(';', ',');
                address = Regex.Replace(address, @"\s+", " ");
                address = Regex.Replace(address, @"\n+", "\n");
                address = address.Trim();
                pme.address = address;

                var email = n.DocumentNode.SelectSingleNode("//a[starts-with(@href, 'mailto:')]")?.InnerText ?? "no mail";
                pme.email = email;

                list.Add(pme);
            }
            Debug.WriteLine(" ... " + list.Count);

            return list;
        }
    }

    public class Job
    {
        public string id { get; set; }
        public string label { get; set; }
        public string value { get; set; }
        public string occupation { get; set; }
        public double score { get; set; }

    }

    public class Location
    {
        public string city { get; set; }
        public string zipcode { get; set; }
        public string department { get; set; }
        public string label { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string value { get; set; }
    }

    public class PME
    {
        public string id { get; set; }
        public string tel { get; set; }
        public string email { get; set; }
        public string title { get; set; }
        public string detail { get; set; }        
        public string address { get; set; }
    }

}