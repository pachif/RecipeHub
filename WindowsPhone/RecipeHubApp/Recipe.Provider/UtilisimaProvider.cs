using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml.Linq;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Globalization;
using HtmlAgilityPack;

namespace Recipes.Provider
{
    public class UtilisimaProvider : ProviderBase
    {
        const string utilisimaURL = "http://m.utilisima.com/ar/recetas/{0}";

        /// <summary>
        /// Process a single recipe, and put the response in the result property
        /// </summary>
        /// <param name="id">the recepe id</param>
        public void ObtainRecipeById(string id)
        {
            // Assure Obtain Id
            var decExpr = new Regex("\\d+");
            string realId = decExpr.Match(id).Value;

            string url = string.Format(utilisimaURL, realId);
            var consumer = new WebConsumer();
            consumer.GetUrlAsync(url);
            consumer.ResponseEnded += new EventHandler<ResultEventArgs>(consumer_ResponseEnded);
        }

        private void consumer_ResponseEnded(object sender, ResultEventArgs e)
        {
            ResultEventArgs newEv = null;
            if (e.HasFail)
            {
                newEv = e;
            }
            else
            {
                //var recipe = ProcessHtmlResponse((string)e.Result);
                var recipe = ConvertIntoRecipe((string)e.Result);
                DateTime now = DateTime.Now;
                Debug.WriteLine("----> Translate Started at {0}", now);
                TranslateRecipeMicrosoft(recipe);
                TimeSpan span = DateTime.Now.Subtract(now);
                Debug.WriteLine("----> Translate Time Elapsed {0}", span.ToString());
                newEv = new ResultEventArgs { Result = recipe };
            }
            FireProcessEnded(sender, newEv);
        }

        private BusinessObjects.Recipe ConvertIntoRecipe(string doc)
        {
            BusinessObjects.Recipe re = new BusinessObjects.Recipe();
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(doc);
            HtmlNode titleNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='headerItem']/section/header/h1");
            re.Title = titleNode.InnerText;
            HtmlNode mainIngrNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='headerItem']/section/header/p");
            re.MainIngredient = mainIngrNode.InnerText.Remove(0, "ingrediente principal : ".Length - 1);
            HtmlNode categoryNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='headerItem']/section/header/h3");
            re.Category = categoryNode.InnerText;
            HtmlNode portNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='mainContent']/div/section/div/div[1]/h5");
            re.Portions = ExtractForksNumber(portNode);
            var authorNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='headerItem']/section/header/h2");
            re.Author = authorNode.InnerText.Remove(0, "por:".Length).Trim();
            var imgNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='headerItem']/section/figure/img");
            if (imgNode.Attributes.Contains("src"))
                re.ImageUrl = imgNode.Attributes["src"].Value;
            
            re.Ingridients = ProcessIngredients(htmlDoc);
            re.Procedure = ProcessProcedure(htmlDoc);

            HandleAlarms(re.Procedure, ref re);

            return re;
        }

        private List<string> ProcessIngredients(HtmlDocument htmlDoc)
        {
            var ingridients = new List<string>();
            int i = 1;
            HtmlNodeCollection ingrNodes = null;
            do
            {
                string xpath = string.Format("//*[@id='mainContent']/div/section/div/div[1]/ul/li[{0}]/p", i);
                ingrNodes = htmlDoc.DocumentNode.SelectNodes(xpath);
                if (ingrNodes != null)
                {
                    foreach (var node in ingrNodes)
                    {
                        ingridients.Add(node.InnerText);
                    }
                    i++; 
                }
            } while (ingrNodes != null);
            return ingridients;
        }

        private string ProcessProcedure(HtmlDocument htmlDoc)
        {
            HtmlNode procNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='mainContent']/div/section/div/div[2]/p");
            if (procNode != null)
            {
                return procNode.InnerText;
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                HtmlNode node = null;
                int i = 1;
                do
                {
                    string xpath = string.Format("//*[@id='mainContent']/div/section/div/div[2]/ul/li[{0}]/p", i);
                    node = htmlDoc.DocumentNode.SelectSingleNode(xpath);
                    if (node != null)
                    {
                        string outText = System.Net.HttpUtility.HtmlDecode(node.InnerHtml);
                        sb.Append(outText);
                        i++;
                    }
                } while (node!=null);
                return sb.ToString();
            }
            throw new Exception("Procedure not found");
        }

        private static int ExtractForksNumber(HtmlNode portNode)
        {
            int port = 0;
            var numberEx = new Regex("\\w+\\W*\\s+(?<num>\\d+)\\s+");
            if (portNode!= null && numberEx.IsMatch(portNode.InnerText))
            {
                int.TryParse(numberEx.Match(portNode.InnerText).Groups["num"].Value, out port);
            }
            return port;
        }
    }
}
