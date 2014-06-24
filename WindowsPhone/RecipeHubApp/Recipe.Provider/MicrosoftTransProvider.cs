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
using System.Xml.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Globalization;
using System.ComponentModel;
using System.Xml;

namespace Recipes.Provider
{
    public class AdmAccessToken
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public string expires_in { get; set; }
        public string scope { get; set; }
    }

    internal class AdmAuthentication
    {
        private static AutoResetEvent autoResetEvent = new AutoResetEvent(false);

        private const string DATAMARKET_ACCESS_URI = "https://datamarket.accesscontrol.windows.net/v2/OAuth2-13";
        private string clientID;
        private string cientSecret;
        private string request;
        AdmAccessToken token;

        public AdmAuthentication(string clientID, string clientSecret)
        {
            this.clientID = clientID;
            this.cientSecret = clientSecret;

            //If clientid or client secret has special characters, encode before sending request
            this.request = string.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=http://api.microsofttranslator.com", Uri.EscapeDataString(clientID), Uri.EscapeDataString(clientSecret));
        }

        public AdmAccessToken GetAccessToken()
        {
            //Prepare OAuth request 
            WebRequest webRequest = WebRequest.Create(DATAMARKET_ACCESS_URI);
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Method = "POST";
            webRequest.BeginGetRequestStream(MsTranslateTextRequestCallBack, webRequest);

            autoResetEvent.WaitOne();

            return token;
        }

        private void MsTranslateTextRequestCallBack(IAsyncResult asyncResult)
        {
            HttpWebRequest webRequest = asyncResult.AsyncState as HttpWebRequest;

            byte[] bytes = Encoding.UTF8.GetBytes(request);
            using (System.IO.Stream outputStream = webRequest.EndGetRequestStream(asyncResult))
            {
                outputStream.Write(bytes, 0, bytes.Length);
            }
            webRequest.BeginGetResponse(MsTranslateTextResponseCallBack, webRequest);
        }

        private void MsTranslateTextResponseCallBack(IAsyncResult result)
        {
            var response = result.AsyncState as HttpWebRequest;
            WebResponse webResponse = response.EndGetResponse(result);
            var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(AdmAccessToken));

            using (Stream stream = webResponse.GetResponseStream())
            {
                // Get deserialized object from JSON stream 
                token = (AdmAccessToken)serializer.ReadObject(stream);
                autoResetEvent.Set();
            }
        }
    }

    public class MicrosoftTransProvider
    {
        private const string BASE_URL = "http://api.microsofttranslator.com/v2/Http.svc/";
        private const string LANGUAGES_URI = BASE_URL + "GetLanguagesForSpeak";
        private const string SPEAK_URI = BASE_URL + "Speak?text={0}&language={1}&format={2}&options={3}";
        private const string TRANSLATE_URI = BASE_URL + "Translate?text={0}&to={1}&contentType=text/plain";
        private const string DETECT_URI = BASE_URL + "Detect?text={0}";

        private static AutoResetEvent autoResetEvent = new AutoResetEvent(false);
        private DateTime tokenRequestTime;
        private int tokenValiditySeconds;
        private string headerValue;

        public event EventHandler<TranslationEventArgs> TranslationCompleted;

        #region Properties

        /// <summary>
        /// Gets or sets the Application Client ID that is necessary to use <strong>Microsoft Translator Service</strong>.
        /// </summary>
        /// <value>The Application Client ID.</value>
        /// <remarks>
        /// <para>Go to <strong>Azure DataMarket</strong> at https://datamarket.azure.com/developer/applications to register your application and obtain a Client ID.</para>
        /// <para>You also need to go to https://datamarket.azure.com/dataset/1899a118-d202-492c-aa16-ba21c33c06cb and subscribe the <strong>Microsoft Translator Service</strong>. There are many options, based on the amount of characters per month. The service is free up to 2 million characters per month.</para>
        /// </remarks>        
        public string ClientID { get; set; }

        /// <summary>
        /// Gets or sets the Application Client Secret that is necessary to use <strong>Microsoft Translator Service</strong>.
        /// </summary>
        /// <remarks>
        /// <value>The Application Client Secret.</value>
        /// <para>Go to <strong>Azure DataMaket</strong> at https://datamarket.azure.com/developer/applications to register your application and obtain a Client Secret.</para>
        /// <para>You also need to go to https://datamarket.azure.com/dataset/1899a118-d202-492c-aa16-ba21c33c06cb and subscribe the <strong>Microsoft Translator Service</strong>. There are many options, based on the amount of characters per month. The service is free up to 2 million characters per month.</para>
        /// </remarks>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets the string representing the supported language code to speak the text in.
        /// </summary>
        /// <value>The string representing the supported language code to speak the text in. The code must be present in the list of codes returned from the method <see cref="GetLanguages"/>.</value>
        /// <seealso cref="GetLanguages"/>
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="System.Net.WebProxy"/> class that contains the proxy definition to be used to send request over the Internet.
        /// </summary>
        /// <value>A <see cref="System.Net.WebProxy"/> class that contains the proxy definition to be used to send request over the Internet.</value>

        /// <summary>
        /// Gets or sets a value indicating whether the sentence to be spoken must be translated in the specified language.
        /// </summary>
        /// <value><strong>true</strong> if the sentence to be spoken must be translated in the specified language; otherwise, <strong>false</strong>.</value>
        /// <remarks>If you don't need to translate to text to be spoken, you can speed-up the the library setting the <strong>AutomaticTranslation</strong> property to <strong>false</strong>. In this way, the specified text is passed as is to the other methods, without performing any translation. The default value is <strong>true</strong>.</remarks>
        public bool AutomaticTranslation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the language of the text must be automatically detected before text-to-speech.
        /// </summary>
        /// <value><strong>true</strong> if the language of the text must be automatically detected; otherwise, <strong>false</strong>.</value>
        /// <remarks>The <strong>AutoDetectLanguage</strong> property is used when the following methods are invoked:
        /// <list type="bullet">
        /// <term><see cref="GetSpeakStream(string)"/></term>
        /// <term><see cref="Speak(string)"/></term>
        /// <term><see cref="GetSpeakStreamAsync(string)"/></term>
        /// <term><see cref="SpeakAsync(string)"/></term>
        /// </list>
        /// <para>When these methods are called, if the <strong>AutoDetectLanguage</strong> property is set to <strong>true</strong>, the language of the text is auto-detected before speech stream request. Otherwise, the language specified in the <seealso cref="Language"/> property is used.</para>
        /// <para>If the language to use is explicitly specified, using the versions of the methods that accept it, no auto-detection is performed.</para>
        /// <para>The default value is <strong>true</strong>.</para>
        /// </remarks>
        /// <seealso cref="Language"/>
        public bool AutoDetectLanguage { get; set; }

        public string TranslatedText { get; set; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <strong>MicrosoftTransProvider</strong> class.
        /// </summary>
        /// <param name="clientID">The Application Client ID.</param>
        /// <param name="clientSecret">The Application Client Secret.</param>
        public MicrosoftTransProvider(string clientID, string clientSecret)
        {
            ClientID = clientID;
            ClientSecret = clientSecret;
            
            AutomaticTranslation = true;
            AutoDetectLanguage = true;
        }

        /// <summary>
        /// Translates a text string into the language specified in the <seealso cref="Language"/> property.
        /// </summary>
        /// <returns>A string representing the translated text.</returns>
        /// <param name="text">A string representing the text to translate.</param>
        /// <exception cref="ArgumentException">
        /// <list type="bullet">
        /// <term>The <see cref="ClientID"/> or <see cref="ClientSecret"/> properties haven't been set.</term>
        /// <term>The <paramref name="text"/> parameter is longer than 1000 characters.</term>
        /// </list>
        /// </exception>
        /// <exception cref="ArgumentNullException">The <paramref name="text"/> parameter is <strong>null</strong> (<strong>Nothing</strong> in Visual Basic) or empty.</exception>
        /// <remarks><para>This method will block until the translated text is returned. If you want to perform a non-blocking request and to be notified when the operation is completed, use the <see cref="TranslateAsync(string)"/> method instead.</para>
        /// <para>For more information, go to http://msdn.microsoft.com/en-us/library/ff512421.aspx.
        /// </para>
        /// </remarks>
        /// <seealso cref="Language"/> 
        /// <seealso cref="TranslateAsync(string)"/>
        public string Translate(string text)
        {
            return this.Translate(text, Language);
        }

        /// <summary>
        /// Translates a text string into the specified language. 
        /// </summary>
        /// <returns>A string representing the translated text.</returns>
        /// <param name="text">A string representing the text to translate.</param>
        /// <param name="to">A string representing the language code to translate the text into. The code must be present in the list of codes returned from the <see cref="GetLanguages"/> method.</param>
        /// <exception cref="ArgumentException">
        /// <list type="bullet">
        /// <term>The <see cref="ClientID"/> or <see cref="ClientSecret"/> properties haven't been set.</term>
        /// <term>The <paramref name="text"/> parameter is longer than 1000 characters.</term>
        /// </list>
        /// </exception>
        /// <remarks><para>This method will block until the translated text is returned. If you want to perform a non-blocking request and to be notified when the operation is completed, use the <see cref="TranslateAsync(string, string)"/> method instead.</para>
        /// <para>For more information, go to http://msdn.microsoft.com/en-us/library/ff512421.aspx.
        /// </para>
        /// </remarks>
        /// <seealso cref="Language"/> 
        /// <seealso cref="TranslateAsync(string, string)"/>
        public string Translate(string text, string to)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Check if it is necessary to obtain/update access token.
            this.UpdateToken();

            if (string.IsNullOrEmpty(to))
                to = Language;

            string uri = string.Format(TRANSLATE_URI, Uri.EscapeDataString(text), to);

            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.Headers[HttpRequestHeader.Authorization] = headerValue;

            httpWebRequest.BeginGetResponse(TranslateSyncCallBack, httpWebRequest);

            autoResetEvent.WaitOne();

            return TranslatedText;
        }

        private void TranslateSyncCallBack(IAsyncResult result)
        {
            try
            {
                var response = (HttpWebRequest)result.AsyncState;
                var webresponse = response.EndGetResponse(result);
                using (Stream stream = webresponse.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream);
                    string results = reader.ReadToEnd();
                    var element = XElement.Parse(results);
                    results = element.Value;
                    TranslatedText = results;
                }
            }
            catch (Exception)
            {
                TranslatedText = "Error";
            }
            autoResetEvent.Set();
        }

        /// <summary>
        /// Translates a text string into the language specified in the <seealso cref="Language"/> property.
        /// </summary>
        /// <returns>A string representing the translated text.</returns>
        /// <param name="text">A string representing the text to translate.</param>
        /// <exception cref="ArgumentException">
        /// <list type="bullet">
        /// <term>The <see cref="ClientID"/> or <see cref="ClientSecret"/> properties haven't been set.</term>
        /// <term>The <paramref name="text"/> parameter is longer than 1000 characters.</term>
        /// </list>
        /// </exception>
        /// <exception cref="ArgumentNullException">The <paramref name="text"/> parameter is <strong>null</strong> (<strong>Nothing</strong> in Visual Basic) or empty.</exception>
        /// <remarks><para>This method perform a non-blocking request for translation. When the operation completes, the <see cref="TranslateCompleted"/> event is raised.</para>
        /// <para>For more information, go to http://msdn.microsoft.com/en-us/library/ff512421.aspx.
        /// </para>
        /// </remarks>
        /// <seealso cref="Language"/> 
        /// <seealso cref="TranslateCompleted"/>
        public void TranslateAsync(string text)
        {
            this.TranslateAsync(text, Language);
        }

        /// <summary>
        /// Translates a text string into the specified language.
        /// </summary>
        /// <returns>A string representing the translated text.</returns>
        /// <param name="text">A string representing the text to translate.</param>
        /// <param name="to">A string representing the language code to translate the text into. The code must be present in the list of codes returned from the <see cref="GetLanguages"/> method.</param>
        /// <exception cref="ArgumentException">
        /// <list type="bullet">
        /// <term>The <see cref="ClientID"/> or <see cref="ClientSecret"/> properties haven't been set.</term>
        /// <term>The <paramref name="text"/> parameter is longer than 1000 characters.</term>
        /// </list>
        /// </exception>
        /// <exception cref="ArgumentNullException">The <paramref name="text"/> parameter is <strong>null</strong> (<strong>Nothing</strong> in Visual Basic) or empty.</exception>
        /// <remarks><para>This method perform a non-blocking request for translation. When the operation completes, the <see cref="TranslateCompleted"/> event is raised.</para>
        /// <para>For more information, go to http://msdn.microsoft.com/en-us/library/ff512421.aspx.
        /// </para>
        /// </remarks>
        /// <seealso cref="Language"/> 
        /// <seealso cref="TranslateCompleted"/>
        public void TranslateAsync(string text, string to)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentNullException("text");

            // Check if it is necessary to obtain/update access token.
            this.UpdateToken();

            if (string.IsNullOrEmpty(to))
                to = Language;

            string uri = string.Format(TRANSLATE_URI, Uri.EscapeDataString(text), to);

            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.Headers[HttpRequestHeader.Authorization] = headerValue;

            httpWebRequest.BeginGetResponse(TranslateCompletedCallback, httpWebRequest);
        }

        private void TranslateCompletedCallback(IAsyncResult ar)
        {
            Exception error = null;
            AsyncOperation async = (AsyncOperation)ar.AsyncState;
           
            try
            {
                var response = (HttpWebRequest)ar;
                using (Stream stream = response.EndGetRequestStream(ar))
                {
                    StreamReader streamRead = new StreamReader(stream);
                    string results = streamRead.ReadToEnd();

                    TranslatedText = results;
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }

            AsyncCompletedEventArgs completedArgs = new AsyncCompletedEventArgs(error, false, TranslatedText);
            OnTranslateCompleted(completedArgs);
        }

        private void OnTranslateCompleted(AsyncCompletedEventArgs e)
        {
            if (this.TranslationCompleted != null)
            {
                TranslationEventArgs args = new TranslationEventArgs(e.UserState as string);
                TranslationCompleted(this, args);
            }
        }

        private void UpdateToken()
        {
            if (string.IsNullOrWhiteSpace(ClientID))
                throw new ArgumentException("Invalid Client ID. Go to Azure Marketplace and sign up for Microsoft Translator: https://datamarket.azure.com/developer/applications");

            if (string.IsNullOrWhiteSpace(ClientSecret))
                throw new ArgumentException("Invalid Client Secret. Go to Azure Marketplace and sign up for Microsoft Translator: https://datamarket.azure.com/developer/applications");

            if ((DateTime.Now - tokenRequestTime).TotalSeconds > tokenValiditySeconds)
            {
                // Token has expired. Make a request for a new one.
                tokenRequestTime = DateTime.Now;
                AdmAuthentication admAuth = new AdmAuthentication(ClientID, ClientSecret);
                var admToken = admAuth.GetAccessToken();

                tokenValiditySeconds = int.Parse(admToken.expires_in);
                headerValue = "Bearer " + admToken.access_token;
            }
        }

        
    }
}
