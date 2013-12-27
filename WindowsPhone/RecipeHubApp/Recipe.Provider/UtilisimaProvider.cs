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

namespace Recipes.Provider
{
    public class UtilisimaProvider
    {
        public event EventHandler<ResultEventArgs> ProcessEnded;

        /// <summary>
        /// Process a single recipe, and put the response in the result property
        /// </summary>
        /// <param name="id">the recepe id</param>
        public void ObtainRecipeById(string id)
        {
            // Assure Obtain Id
            var decExpr = new Regex("\\d+");
            string realId = decExpr.Match(id).Value;

            string url = string.Format("http://m.utilisima.com/ar/recetas/{0}", realId);
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
                    realtext = (string)e.Result;
                    string url = string.Format("http://s.ficfiles.com/utilisima/get_rss.php?seeker=recetas&search={0}&page={1}", realtext, page);
                    var consumer = new WebConsumer();
                    consumer.GetUrlAsync(url);
                    consumer.ResponseEnded += new EventHandler<ResultEventArgs>(recent_ResponseEnded);
                };
            }
            else
            {
                string url = string.Format("http://s.ficfiles.com/utilisima/get_rss.php?seeker=recetas&search={0}&page={1}", realtext, page);
                var consumer = new WebConsumer();
                consumer.GetUrlAsync(url);
                consumer.ResponseEnded += new EventHandler<ResultEventArgs>(recent_ResponseEnded);
            }
        }

        public void ObtainMostRecents()
        {
            string url = string.Format("http://s.ficfiles.com/utilisima/get_rss.py?seeker=recetas&order_by=Publish_date");
            Debug.WriteLine("Llamando a {0}", url);
            var consumer = new WebConsumer();
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
                newEv = new ResultEventArgs { Result = ProcessRssResponse((string)e.Result) };
            }

            if (ProcessEnded != null)
                ProcessEnded(sender, newEv);
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
                var recipe = ProcessHtmlResponse((string)e.Result);
                TranslateRecipe(recipe);
                newEv = new ResultEventArgs { Result = recipe };
            }
            if (ProcessEnded != null)
                ProcessEnded(sender, newEv);
        }

        private void search_ResponseEnded(object sender, ResultEventArgs e)
        {
            ProcessHtmlResponseMultiple((string)e.Result);

            if (ProcessEnded != null)
                ProcessEnded(sender, e);
        }

        private void ProcessHtmlResponseMultiple(string doc)
        {
            throw new NotImplementedException();
        }

        private List<BusinessObjects.Recipe> ProcessRssResponse(string htmldoc)
        {
            var list = new List<BusinessObjects.Recipe>();
            var recetas = ObtainRecipes(htmldoc);
            foreach (XElement xElement in recetas)
            {
                string title = xElement.Elements().SingleOrDefault(x => x.Name.LocalName.Equals("title")).Value;
                string author = xElement.Elements().SingleOrDefault(x => x.Name.LocalName.Equals("author")).Value;
                string image = xElement.Elements().SingleOrDefault(x => x.Name.LocalName.Equals("image")).Value;
                string link = xElement.Elements().SingleOrDefault(x => x.Name.LocalName.Equals("link")).Value;
                var item = new BusinessObjects.Recipe
                {
                    Author = author,
                    Title = title,
                    ImageUrl = image,
                    LinkUrl = link
                };
                TranslateRecipe(item);
                list.Add(item);
            }

            return list;
        }

        private static List<XElement> ObtainRecipes(string htmldoc)
        {
            XElement doc = XElement.Parse(htmldoc);
            XElement channel = doc.Elements().First();
            var recetas = (from itm in channel.Elements()
                           where itm.Name.LocalName.Equals("item")
                           select itm).ToList();
            return recetas;
        }

        private BusinessObjects.Recipe ProcessHtmlResponse(string htmldoc)
        {
            BusinessObjects.Recipe re = new BusinessObjects.Recipe();
            string expNRecetaLeft = "<h1 itemprop=\"name\" class=\"recipes fn\">";
            string expNReceta = expNRecetaLeft + "(?<Title>(\\w+\\s*)+)<";
            Regex ex = new Regex(expNReceta, RegexOptions.Multiline | RegexOptions.IgnoreCase);

            if (ex.IsMatch(htmldoc))
            {
                string match = ex.Match(htmldoc).Groups["Title"].Value;
                re.Title = match;
            }

            HandleDetails(htmldoc, ref re);

            HandleIngredients(htmldoc, ref re);

            HandleProcedim(htmldoc, ref re);

            return re;
        }

        private static void HandleIngredients(string htmldoc, ref BusinessObjects.Recipe recipe)
        {
            string expIngrLeft =
                "<li class=\"ingredient\" itemprop=\"ingredients\"><label for=\"radio\\d\" class=\"name\">";
            string expIngr = expIngrLeft + "(?<Ingredientes>(\\w+\\s*)+)<";
            Regex ex = new Regex(expIngr, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (ex.IsMatch(htmldoc))
            {
                recipe.Ingridients = new List<string>();
                var matches = ex.Matches(htmldoc);
                foreach (Match match in matches)
                {
                    recipe.Ingridients.Add(match.Groups["Ingredientes"].Value);
                }
            }
        }

        private static void HandleDetails(string htmldoc, ref BusinessObjects.Recipe recipe)
        {
            const string leftSide = "<article id=\"detail\">";
            //- Otras propiedades
            int pos = htmldoc.IndexOf(leftSide);
            int pos2 = htmldoc.IndexOf("</article>");

            if (pos2 > pos)
            {
                string subs = htmldoc.Substring(pos + leftSide.Length, pos2 - pos);

                // Set the image
                string imgPattern = "<img src=\"";
                int imgIndex = subs.IndexOf(imgPattern);
                recipe.ImageUrl = subs.Substring(imgIndex + imgPattern.Length, subs.IndexOf('"', imgIndex + imgPattern.Length) - (imgIndex + imgPattern.Length));

                string another = subs.Substring(subs.IndexOf("<ul class"), subs.Length - subs.IndexOf("<ul class"));
                //- Separate Details
                var nameEx = new Regex("<strong>(?<Nombre>(\\w+\\W*|\\w+\\s+\\w+\\W)\\s+)</strong>");
                var cleanEx = new Regex("</strong>|<li|</ul></article><div class=|</ul></article><div class");
                if (nameEx.IsMatch(another))
                {
                    var dic = new Dictionary<string, string>();
                    var matches = nameEx.Matches(another);
                    int i = 0;
                    foreach (Match match in matches)
                    {
                        i++;
                        string name = match.Groups["Nombre"].Value;

                        int length = match.Groups["Nombre"].Length;
                        int startIndex = match.Groups["Nombre"].Index + length;
                        string value = string.Empty;
                        if (i < matches.Count)
                            value = another.Substring(startIndex, matches[i].Index - startIndex - 1);
                        else
                            value = another.Substring(startIndex, another.Length - startIndex - 1);

                        value = cleanEx.Replace(value, string.Empty);
                        switch (name)
                        {
                            case "Autor: ":
                                string authorName = ExtractValueFromLink(value);
                                recipe.Author = authorName;
                                break;
                            case "Categoría: ":
                                recipe.Category = value;
                                break;
                            case "Porciones: ":
                                int portions = 0;
                                Int32.TryParse(value, out portions);
                                recipe.Portions = portions;
                                break;
                            case "Ingrediente Principal: ":
                                recipe.MainIngredient = ExtractValueFromLink(value);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        private static string ExtractValueFromLink(string value)
        {
            Regex ex = new Regex(">(\\w+\\s*)+<");
            string authorName = ex.Match(value).Value.Replace("<", string.Empty).Replace(">", string.Empty);
            return authorName;
        }

        private static void HandleProcedim(string htmldoc, ref BusinessObjects.Recipe recipe)
        {
            const string leftSide = "<article id=\"steps\">";
            int pos = htmldoc.IndexOf(leftSide);
            int pos2 = htmldoc.IndexOf("</article>", pos);

            if (pos2 > pos)
            {
                string subs = htmldoc.Substring(pos + leftSide.Length, pos2 - pos)
                                     .Replace("<h3>PROCEDIMIENTO</h3>", string.Empty);

                subs = PreprocessText(subs);

                var cleanEx = new Regex("<p>|</p>|</article>|</section>|<span>|</span>|<span class=\"naranga\">|</strong>");
                HandleAlarms(subs, ref recipe);
                if (!subs.Contains("<strong>"))
                {
                    subs = cleanEx.Replace(subs, string.Empty);
                    recipe.Procedure = System.Net.HttpUtility.HtmlDecode(subs).Replace("<br/>", "\n").Trim();
                }
                else
                {
                    //- It has several subprocedures. Separate Titles
                    var separateEx = new Regex("<strong>");

                    if (separateEx.IsMatch(subs))
                    {
                        var dic = new Dictionary<string, string>();
                        var matches = separateEx.Matches(subs);
                        int i = 0;
                        foreach (Match match in matches)
                        {
                            i++;
                            int length = match.Length;
                            int startIndex = match.Index + length;
                            int startValueIndex = subs.IndexOf("</strong>", match.Index);
                            int endIndex = startValueIndex;
                            string name = cleanEx.Replace(subs.Substring(startIndex, endIndex - startIndex), string.Empty);
                            string value = string.Empty;
                            if (i < matches.Count)
                                value = subs.Substring(startValueIndex, matches[i].Index - startValueIndex);
                            else
                                value = subs.Substring(startValueIndex, subs.Length - startValueIndex);

                            value = cleanEx.Replace(value, string.Empty);
                            value = value.Replace("<br/>", "\n");
                            if (!dic.ContainsKey(name))
                                dic.Add(name, value);
                            else
                            {
                                string key = string.Format("{0} {1}", name, i);
                                dic.Add(key, value);
                            }
                        }
                        StringBuilder sb = new StringBuilder();
                        foreach (KeyValuePair<string, string> keyValuePair in dic)
                        {
                            sb.AppendFormat("{0}\n{1}\n", keyValuePair.Key, keyValuePair.Value);
                        }

                        recipe.Procedure = System.Net.HttpUtility.HtmlDecode(sb.ToString()).Trim();
                    }
                }
            }
        }

        private static void HandleAlarms(string source,ref BusinessObjects.Recipe recipe)
        {
            var alarmEx = new Regex(".(?<text>(\\w+\\s)+)(?<minutos>\\d+)\\sminutos");
            if (alarmEx.IsMatch(source))
            {
                recipe.Alarms = new List<BusinessObjects.Alarm>();
                int i = 1;
                foreach (Match match in alarmEx.Matches(source))
                {
                    var alarm = new BusinessObjects.Alarm
                    {
                        Minutes = double.Parse(match.Groups["minutos"].Value),
                        Name = match.Groups["text"].Value
                    };
                    recipe.Alarms.Add(alarm);
                }
            }

        }

        private static string PreprocessText(string subs)
        {
            // Replace empty substitles
            var prepProcessingEx = new Regex("<strong><span class=\"naranga\"></span></strong>");
            subs = prepProcessingEx.Replace(subs, string.Empty);
            // Remove images
            var imgEx = new Regex("<img");
            if (imgEx.IsMatch(subs))
            {
                var matches = imgEx.Matches(subs);
                List<string> imgList = (from Match match in matches
                                        select subs.Substring(match.Index, subs.IndexOf('>', match.Index) - match.Index + 1)).ToList();

                subs = imgList.Aggregate(subs, (current, imgSource) => current.Replace(imgSource, string.Empty));
            }

            return subs;
        }

        private void TranslateRecipe(BusinessObjects.Recipe recipe)
        {
            CultureInfo currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            if (!currentCulture.TwoLetterISOLanguageName.ToLowerInvariant().Equals("es"))
            {
                var traslator = new TranslationProvider();
                string languagePair = "es|" + currentCulture.TwoLetterISOLanguageName;
                
                recipe.Title = traslator.TranslateText(recipe.Title, languagePair);
                recipe.MainIngredient = traslator.TranslateText(recipe.MainIngredient, languagePair);
                recipe.Category = traslator.TranslateText(recipe.Category, languagePair);

                if (!string.IsNullOrEmpty(recipe.Procedure))
                {
                    string procedure = recipe.Procedure.Replace("\n", "[n] ");
                    procedure = traslator.TranslateText(procedure, languagePair);
                    procedure = HttpUtility.HtmlDecode(procedure);
                    recipe.Procedure = procedure.Replace("[n] ", "\n").Replace("[N] ", "\n").Replace("[ n] ", "\n").Replace("[n ] ", "\n");
                }

                if (recipe.Ingridients != null && recipe.Ingridients.Count > 0)
                {
                    var list = new List<string>();
                    recipe.Ingridients.ForEach(ingr => list.Add(traslator.TranslateText(ingr, languagePair)));
                    recipe.Ingridients = list;
                }

                if (recipe.Alarms != null && recipe.Alarms.Count > 0)
                {
                    var list = new List<string>();
                    recipe.Alarms.ForEach(al => al.Name = traslator.TranslateText(al.Name, languagePair));
                }
            }
        }

        private void TranslateRecipeMicrosoft(BusinessObjects.Recipe recipe)
        {
            CultureInfo currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            if (!currentCulture.TwoLetterISOLanguageName.ToLowerInvariant().Equals("es"))
            {
                var traslator = new MicrosoftTransProvider(currentCulture.TwoLetterISOLanguageName);
                string languagePair = "es|" + currentCulture.TwoLetterISOLanguageName;
                
                traslator.TranslateAsync(recipe.Title);
                traslator.TranslationCompleted += (s, e) => { recipe.Title = e.TranslatedText; };

                traslator.TranslateAsync(recipe.MainIngredient);
                traslator.TranslationCompleted += (s, e) => { recipe.MainIngredient = e.TranslatedText; };

                traslator.TranslateAsync(recipe.Category);
                traslator.TranslationCompleted += (s, e) => { recipe.Category = e.TranslatedText; };

                if (!string.IsNullOrEmpty(recipe.Procedure))
                {
                    string procedure = recipe.Procedure.Replace("\n", "[n] ");
                    traslator.TranslateAsync(procedure);
                    traslator.TranslationCompleted += (s, e) => { procedure = e.TranslatedText; };
                    procedure = HttpUtility.HtmlDecode(procedure);
                    recipe.Procedure = procedure.Replace("[n] ", "\n").Replace("[N] ", "\n").Replace("[ n] ", "\n").Replace("[n ] ", "\n");
                }


                if (recipe.Ingridients != null && recipe.Ingridients.Count > 0)
                {
                    string built = string.Empty;
                    recipe.Ingridients.ForEach(ingr => built += ingr + "|" );
                    traslator.TranslateAsync(built);
                    traslator.TranslationCompleted += (s, e) => {
                        string[] trans = e.TranslatedText.Split('|');
                        for (int i = 0; i < trans.Length; i++)
                        {
                            if (string.IsNullOrEmpty(trans[i])) continue;
                            recipe.Ingridients[i] = trans[i];
                        }
                    };
                }

                if (recipe.Alarms != null && recipe.Alarms.Count > 0)
                {
                    string built = string.Empty;
                    recipe.Alarms.ForEach(al => built += al.Name + "|");
                    traslator.TranslateAsync(built);
                    traslator.TranslationCompleted += (s, e) =>
                    {
                        string[] trans = e.TranslatedText.Split('|');
                        for (int i = 0; i < trans.Length; i++)
                        {
                            if (string.IsNullOrEmpty(trans[i])) continue;
                            recipe.Alarms[i].Name = trans[i];
                        }
                    };
                }
            }
        }
    }
}
