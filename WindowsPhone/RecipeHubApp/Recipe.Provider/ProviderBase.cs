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
    public class ProviderBase
    {
        public event EventHandler<ResultEventArgs> ProcessEnded;

        private const string SECRET = "+xuNZPbl5oQxoCkq89EayNsqG1e6JNWQIJ5vNKhdEkM=";
        private const  string KEY = "recipehub";
        private MicrosoftTransProvider traslator;

        protected void FireProcessEnded(object sender, ResultEventArgs ev)
        {
            if (ProcessEnded != null)
                ProcessEnded(sender, ev);
        }

        protected static void HandleAlarms(string source, ref BusinessObjects.Recipe recipe)
        {
            recipe.Alarms = new List<BusinessObjects.Alarm>();
            var alarmMinEx = new Regex(".(?<text>(\\w+\\s)+)(?<minutos>\\d+)\\sminutos");
            if (alarmMinEx.IsMatch(source))
            {
                foreach (Match match in alarmMinEx.Matches(source))
                {
                    var alarm = new BusinessObjects.Alarm
                    {
                        Minutes = double.Parse(match.Groups["minutos"].Value),
                        Name = match.Groups["text"].Value
                    };
                    recipe.Alarms.Add(alarm);
                }
            }
            var alarmHourEx = new Regex(".(?<text>(\\w+\\s)+)(?<hora>\\d+)\\shora(s*)");
            if (alarmHourEx.IsMatch(source))
            {
                foreach (Match match in alarmHourEx.Matches(source))
                {
                    var alarm = new BusinessObjects.Alarm
                    {
                        Minutes = double.Parse(match.Groups["hora"].Value) * 60,
                        Name = match.Groups["text"].Value
                    };
                    recipe.Alarms.Add(alarm);
                }
            }
        }

        protected List<BusinessObjects.Recipe> ProcessRssResponse(string htmldoc)
        {
            var list = new List<BusinessObjects.Recipe>();
            XElement channel = XElement.Parse(htmldoc).Elements().First();
            var recetas = (from itm in channel.Elements()
                           where itm.Name.LocalName.Equals("item")
                           select itm).ToList();
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
                DateTime now = DateTime.Now;
                Debug.WriteLine("----> Translate Started at {0}", now);
                TranslateRecipeMicrosoft(item);
                TimeSpan span = DateTime.Now.Subtract(now);
                Debug.WriteLine("----> Translate Time Elapsed {0}", span.ToString());

                list.Add(item);
            }

            return list;
        }

        #region Translations

        public void TranslateRecipeMicrosoft(BusinessObjects.Recipe recipe)
        {
            
            CultureInfo currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            if (!currentCulture.TwoLetterISOLanguageName.ToLowerInvariant().Equals("es"))
            {
                if (traslator == null)
                {
                    traslator = new MicrosoftTransProvider(KEY, SECRET); 
                }
                string target = currentCulture.TwoLetterISOLanguageName;

                recipe.Title = traslator.Translate(HttpUtility.HtmlDecode(recipe.Title), target);
                recipe.MainIngredient = traslator.Translate(recipe.MainIngredient, target);
                recipe.Category = traslator.Translate(recipe.Category, target);

                if (!string.IsNullOrEmpty(recipe.Procedure))
                {
                    string procedure = recipe.Procedure.Replace("\r\n\r\n", " [n] ").Replace("\n                    ", " [n] ");
                    procedure = traslator.Translate(procedure, target);
                    procedure = HttpUtility.HtmlDecode(procedure);
                    recipe.Procedure = procedure.Replace("[n] ", "\n").Replace("[N] ", "\n").Replace("[ n] ", "\n").Replace("[n ] ", "\n");
                }

                if (recipe.Ingridients != null && recipe.Ingridients.Count > 0)
                {
                    recipe.Ingridients = TranslateList(traslator, recipe.Ingridients, target);
                }

                if (recipe.Alarms != null && recipe.Alarms.Count > 0)
                {
                    var list = new List<string>();
                    recipe.Alarms.ForEach(al => al.Name = traslator.Translate(al.Name, target));
                }
            }
        }

        private List<string> TranslateList(MicrosoftTransProvider traslator, List<string> list, string targetLeng)
        {
            List<string> result = new List<string>();
            string tr = list.First();
            for (int i = 1; i < list.Count; i++)
            {
                tr += ". " + HttpUtility.HtmlDecode(list.ElementAt(i));
            }
            tr = traslator.Translate(tr, targetLeng);
            string[] ltr = tr.Split('.');
            for (int j = 0; j < ltr.Length; j++)
            {
                result.Add(ltr[j].Trim());
            }

            return result;
        }

        #endregion
        
        #region Old Google Translation

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

            if (pos2 < pos) { return; }

            string subs = htmldoc.Substring(pos + leftSide.Length, pos2 - pos);
            // Set the image or Video
            string imgPattern = "<img src=\"";
            int imgIndex = subs.IndexOf(imgPattern);
            if (imgIndex > 0)
            {
                recipe.ImageUrl = subs.Substring(imgIndex + imgPattern.Length, subs.IndexOf('"', imgIndex + imgPattern.Length) - (imgIndex + imgPattern.Length));
            }
            else
            {
                //TODO: Handle Video
                pos2 = htmldoc.IndexOf("</article>", pos2 + "</article>".Length);
                subs = htmldoc.Substring(pos + leftSide.Length, pos2 - pos);
            }
            string articlesubs = subs.Substring(subs.IndexOf("<ul class"), subs.Length - subs.IndexOf("<ul class"));

            //- Separate Details
            var nameEx = new Regex("<strong>(?<Nombre>(\\w+\\W*|\\w+\\s+\\w+\\W)\\s+)</strong>");
            var cleanEx = new Regex("</strong>|<li|</ul></article><div class=|</ul></article><div class");
            if (nameEx.IsMatch(articlesubs))
            {
                var dic = new Dictionary<string, string>();
                var matches = nameEx.Matches(articlesubs);
                int i = 0;
                foreach (Match match in matches)
                {
                    i++;
                    string name = match.Groups["Nombre"].Value;

                    int length = match.Groups["Nombre"].Length;
                    int startIndex = match.Groups["Nombre"].Index + length;
                    string value = string.Empty;
                    if (i < matches.Count)
                        value = articlesubs.Substring(startIndex, matches[i].Index - startIndex - 1);
                    else
                        value = articlesubs.Substring(startIndex, articlesubs.Length - startIndex - 1);

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

                var cleanEx = new Regex("<p>|</p>|</article>|</section>|<span>|</span>|<span class=\"naranga\">|</strong>|<strong>");
                HandleAlarms(subs, ref recipe);
                if (!subs.Contains("<strong>") || subs.Substring(0, 3) == "<p>")
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
        #endregion
    }
}
