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

        private WebConsumer consumer;
        private string translatedText;

        public TranslationProvider()
        {
            consumer = new WebConsumer();
           
        }

        public string TranslateText(string input, string languagePair = "es|en")
        {
            var consumer = new WebConsumer();
            consumer.ResponseEnded +=new EventHandler<ResultEventArgs>(Sync_ResponseEnded);
            string url = String.Format("http://www.google.com/translate_t?hl=en&ie=UTF8&text={0}&langpair={1}", input, languagePair);
            consumer.GetUrlAsync(url);
            //translatedText = consumer.GetUrl(url);

            // Wait until the call is finished
            autoResetEvent.WaitOne();
            
            return translatedText;
        }

        /// <summary>
        /// Translate Text using Google Translate API's
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
            string url = String.Format("http://www.google.com/translate_t?hl=en&ie=UTF8&text={0}&langpair={1}", input, languagePair);
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
