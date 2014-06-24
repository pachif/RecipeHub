using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Threading;

namespace Recipes.Provider
{
    public class TranslationProvider
    {
        public event EventHandler<ResultEventArgs> TranslationEnded;
        private static AutoResetEvent autoResetEvent = new AutoResetEvent(false);
        // Google Translator
        private const string Apikey = "AIzaSyAwmPpAerCBw1wCx5heD5-2zaBqGzJwNEQ";
        private const string baseURL = "http://www.google.com/translate_t?hl=en&ie=UTF8&text={0}&langpair={1}";
        private const string baseAPIURL = "https://www.googleapis.com/language/translate/v2?key={0}&q={1}&source={2}&target={3}";
        
        private WebConsumer consumer;
        private string translatedText;

        public TranslationProvider()
        {
           consumer = new WebConsumer();
        }

        /// <summary>
        /// Translate text using Google Translate GET operation synchronously
        /// </summary>
        /// <param name="input">Input string</param>
        /// <param name="languagePair">2 letter Language Pair, delimited by "|".
        /// E.g. "ar|en" language pair means to translate from Arabic to English</param>
        /// <returns>Translated to String</returns>
        public string TranslateText(string input, string languagePair = "es|en")
        {
            var consumer = new WebConsumer();
            consumer.ResponseEnded +=new EventHandler<ResultEventArgs>(Sync_ResponseEnded);
            string url = String.Format(baseURL, input, languagePair);
            consumer.GetUrlAsync(url);
            //translatedText = consumer.GetUrl(url);

            // Wait until the call is finished
            autoResetEvent.WaitOne();
            
            return translatedText;
        }

        /// <summary>
        /// Translates text using Google Translate GET operation asynchronously
        /// Google URL - http://www.google.com/translate_t?hl=en&ie=UTF8&text={0}&langpair={1}
        /// </summary>
        /// <param name="input">Input string</param>
        /// <param name="languagePair">2 letter Language Pair, delimited by "|".
        /// E.g. "ar|en" language pair means to translate from Arabic to English</param>
        /// <returns>Translated to String</returns>
        public void TranslateTextAsync(string input, string languagePair = "es|en")
        {
            var consumer = new WebConsumer();
            consumer.ResponseEnded += new EventHandler<ResultEventArgs>(consumer_ResponseEnded);
            string url = String.Format(baseURL, input, languagePair);
            consumer.GetUrlAsync(url);
        }

        private void Sync_ResponseEnded(object sender, ResultEventArgs e)
        {
            ResultEventArgs newEv = null;
            if (e.HasFail)
            {
                newEv = e;
            }
            else
            {
                translatedText = ProcessResponse(e);
            }
            autoResetEvent.Set();
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
                newEv = new ResultEventArgs { Result = ProcessResponse(e) };
            }
            if (TranslationEnded != null)
                TranslationEnded(sender, newEv);
        }

        private string ProcessResponse(ResultEventArgs e)
        {
            translatedText = (string)e.Result;
            string pattern = "TRANSLATED_TEXT='";
            int indexFrom = translatedText.IndexOf(pattern);
            int indexTo = translatedText.IndexOf("'", indexFrom + pattern.Length);
            return translatedText.Substring(indexFrom + pattern.Length, indexTo - (indexFrom + pattern.Length));
        }
    }
}
