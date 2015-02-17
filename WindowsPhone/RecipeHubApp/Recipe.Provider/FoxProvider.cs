using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using HtmlAgilityPack;
using System.Net;

namespace Recipes.Provider
{
    public class FoxProvider : ProviderBase
    {
        const string foxplayURL = "http://www.foxplay.com/ar/lifestyle/recipes/";

        /// <summary>
        /// Process a single recipe, and put the response in the result property
        /// </summary>
        /// <param name="id">the recepe id</param>
        public void ObtainRecipeById(string id)
        {
            // Assure Obtain Id
            var decExpr = new Regex("\\d+");
            string realId = decExpr.Match(id).Value;

            string url = foxplayURL + realId;
            var consumer = new WebConsumer();
            consumer.GetUrlAsync(url);
            consumer.ResponseEnded += new EventHandler<ResultEventArgs>(consumer_ResponseEnded);
        }

        public void SearchRecipeByName(string name)
        {
            SearchRecipeByName(name, 0);
        }

        public void SearchRecipeByName(string name, int page)
        {
            string realtext = name;
            CultureInfo currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            if (!currentCulture.TwoLetterISOLanguageName.ToLowerInvariant().Equals("es"))
            {
                var translator = new TranslationProvider();
                string languagePair = string.Format("{0}|es", currentCulture.TwoLetterISOLanguageName);
                // we made a separate async translation to avoid screen freezing
                translator.TranslateTextAsync(name, languagePair);
                translator.TranslationEnded += (s, e) =>
                {
                    if (e.HasFail)
                    {
                        // Happens when interrupted internet connection is occuring
                        FireProcessEnded(s, e);
                    }
                    else
                    {
                        realtext = (string)e.Result;
                        string url = string.Format("http://s.ficfiles.com/utilisima/get_rss.php?seeker=recetas&search={0}&page={1}", realtext, page);
                        var consumer = new WebConsumer();
                        consumer.GetUrlAsync(url);
                        consumer.ResponseEnded += new EventHandler<ResultEventArgs>(rss_ResponseEnded);
                    }
                };
            }
            else
            {
                string url = string.Format("http://s.ficfiles.com/utilisima/get_rss.php?seeker=recetas&search={0}&page={1}", realtext, page);
                var consumer = new WebConsumer();
                consumer.GetUrlAsync(url);
                consumer.ResponseEnded += new EventHandler<ResultEventArgs>(rss_ResponseEnded);
            }
        }

        public void ObtainMostRecents()
        {
            string url = "http://www.foxplay.com/ar/lifestyle/search/recipes";
            Debug.WriteLine("Llamando a {0}", foxplayURL);
            //var client = new WebClient();
            //client.DownloadStringCompleted += recent_ResponseEnded;
            //client.DownloadStringAsync(new Uri(foxplayURL));

            var consumer = new WebConsumer();
            //consumer.ContentType = "text/html";
            consumer.GetUrlAsync(url);
            consumer.ResponseEnded += new EventHandler<ResultEventArgs>(recent_ResponseEnded);
        }

        private void recent_ResponseEnded(object sender, ResultEventArgs e)
        {
            ResultEventArgs newEv = null;
            if (e.HasFail)
            {
                newEv = e;
            }
            else
            {
                newEv = new ResultEventArgs { Result = ProcessResponse((string)e.Result) };
            }

            FireProcessEnded(sender, newEv);
        }

        private void rss_ResponseEnded(object sender, ResultEventArgs e)
        {
            ResultEventArgs newEv = null;
            if (e.HasFail)
            {
                newEv = e;
            }
            else
            {
                newEv = new ResultEventArgs { Result = ProcessRssResponse((string)e.Result) };
            }

            FireProcessEnded(sender, newEv);
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

        private List<BusinessObjects.Recipe> ProcessResponse(string raw)
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(raw);

            var recetaNodes = htmlDoc.DocumentNode.SelectNodes("//*[@id='recetas']/li");

            List<BusinessObjects.Recipe> list = recetaNodes.Count > 1 ? ExtractNormalLoop(recetaNodes) : ExtractFileWiseLoop(recetaNodes);
            
            return list;
        }

        private List<BusinessObjects.Recipe> ExtractFileWiseLoop(HtmlNodeCollection recetaNodes)
        {
            var list = new List<BusinessObjects.Recipe>();
            HtmlNode recetaNode = recetaNodes.SingleOrDefault(x => x.Name == "li");
            while (recetaNode != null)
            {
                var item = PreprocessRecipe(recetaNode);

                list.Add(item);

                recetaNode = recetaNode.ChildNodes.SingleOrDefault(x => x.Name == "li");
            }
            return list;
        }

        private List<BusinessObjects.Recipe> ExtractNormalLoop(HtmlNodeCollection recetaNodes)
        {
            var list = new List<BusinessObjects.Recipe>();
            foreach (HtmlNode recetaNode in recetaNodes)
            {

                string title = recetaNode.SelectSingleNode("article").SelectSingleNode("a").SelectSingleNode("header").SelectSingleNode("h1").InnerText;
                string author = recetaNode.SelectSingleNode("article").SelectSingleNode("a").SelectSingleNode("header").SelectSingleNode("h2").InnerText;

                var item = PreprocessRecipe(recetaNode);

                list.Add(item);
            }
            return list;
        }

        private BusinessObjects.Recipe PreprocessRecipe(HtmlNode recetaNode)
        {
            string title = recetaNode.SelectSingleNode("article").SelectSingleNode("a").SelectSingleNode("header").SelectSingleNode("h1").InnerText;
            string author = recetaNode.SelectSingleNode("article").SelectSingleNode("a").SelectSingleNode("header").SelectSingleNode("h2").InnerText;

            HtmlNode anchor = recetaNode.SelectSingleNode("article").SelectSingleNode("a");
            HtmlNode imgNode = anchor.SelectSingleNode("figure").SelectSingleNode("img");
            string image = string.Empty;
            if (imgNode != null && imgNode.Attributes.Contains("src"))
                image = imgNode.Attributes["src"].Value;
            string link = string.Empty;
            if (anchor != null && anchor.Attributes.Contains("href"))
                link = anchor.Attributes["href"].Value;

            var item = new BusinessObjects.Recipe
            {
                Author = author.Replace("Por: ", string.Empty),
                Title = title,
                ImageUrl = image,
                LinkUrl = link
            };
            DateTime now = DateTime.Now;
            Debug.WriteLine("----> Translate Started at {0}", now);
            TranslateRecipeMicrosoft(item);
            TimeSpan span = DateTime.Now.Subtract(now);
            Debug.WriteLine("----> Translate Time Elapsed {0}", span.ToString());
            return item;
        }

        private BusinessObjects.Recipe ConvertIntoRecipe(string doc)
        {
            BusinessObjects.Recipe re = new BusinessObjects.Recipe();
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(doc);
            HtmlNode titleNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='headerItem']/section/header/h1");
            re.Title = HttpUtility.HtmlDecode(titleNode.InnerText);
            HtmlNode mainIngrNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='headerItem']/section/header/p");
            re.MainIngredient = mainIngrNode.InnerText.Remove(0, "ingrediente principal : ".Length - 1);
            re.MainIngredient = HttpUtility.HtmlDecode(re.MainIngredient);
            HtmlNode categoryNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='headerItem']/section/header/h3");
            re.Category = HttpUtility.HtmlDecode(categoryNode.InnerText);
            HtmlNode portNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='mainContent']/div/section/div/div[1]/h5");
            re.Portions = ExtractForksNumber(portNode);
            var authorNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='headerItem']/section/header/h2");
            re.Author = HttpUtility.HtmlDecode(authorNode.InnerText.Remove(0, "por:".Length).Trim());
            var imgNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='headerItem']/section/figure/img");
            if (imgNode!= null && imgNode.Attributes.Contains("src"))
                re.ImageUrl = imgNode.Attributes["src"].Value;

            re.Ingridients = ProcessIngredients(htmlDoc);
            re.Procedure = ProcessProcedure(htmlDoc);

            HandleAlarms(re.Procedure, ref re);

            return re;
        }

        private List<string> ProcessIngredients(HtmlDocument htmlDoc)
        {
            var ingridients = new List<string>();
            HtmlNode ingrNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='mainContent']/div/section/div/div[1]/ul/li");
            while (ingrNode != null) {
                var textNodes = ingrNode.SelectNodes("p");
                if (textNodes != null)
                {
                    foreach (var node in textNodes)
                    {
                        ingridients.Add(node.InnerText);
                    }
                }
                ingrNode = ingrNode.ChildNodes.SingleOrDefault(x => x.Name == "li");
            } 
            return ingridients;
        }

        private string ProcessProcedure(HtmlDocument htmlDoc)
        {
            HtmlNode procNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='mainContent']/div/section/div/div[2]/p");
            if (procNode != null)
            {
                return HttpUtility.HtmlDecode(procNode.InnerText.Replace("\t", string.Empty));
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                var nodes = htmlDoc.DocumentNode.SelectNodes("//*[@id='mainContent']/div/section/div/div[2]/ul/li");
                if (nodes.Count > 1)
                {
                    foreach (HtmlNode node in nodes)
                    {
                        var parags = node.SelectNodes("p");
                        if (parags != null)
                        {
                            foreach (var item in parags)
                            {
                                string outText = HttpUtility.HtmlDecode(item.InnerText.Replace("\t", string.Empty));
                                sb.Append(outText);
                            }
                        }
                    }
                }
                else
                {
                    HtmlNode node = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='mainContent']/div/section/div/div[2]/ul/li");
                    while (node != null)
                    {
                        var parags = node.SelectNodes("p");
                        if (parags != null)
                        {
                            foreach (var item in parags)
                            {
                                string outText = HttpUtility.HtmlDecode(item.InnerText.Replace("\t", string.Empty));
                                sb.Append(outText);
                            }
                        }
                        node = node.ChildNodes.SingleOrDefault(x => x.Name == "li");
                    }
                }

                return sb.ToString();
            }
            throw new Exception("Procedure not found");
        }

        private static int ExtractForksNumber(HtmlNode portNode)
        {
            int port = 0;
            var numberEx = new Regex("\\w+\\W*\\s+(?<num>\\d+)\\s+");
            if (portNode != null && numberEx.IsMatch(portNode.InnerText))
            {
                int.TryParse(numberEx.Match(portNode.InnerText).Groups["num"].Value, out port);
            }
            return port;
        }
    }
}
