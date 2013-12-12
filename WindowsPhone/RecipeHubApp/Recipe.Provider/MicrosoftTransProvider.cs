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

namespace Recipes.Provider
{
    public class AdmAccessToken
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public string expires_in { get; set; }
        public string scope { get; set; }
    }

    public class MicrosoftTransProvider
    {
        private string translatedText;
        public event EventHandler<TranslationEventArgs> TranslationCompleted;

        public MicrosoftTransProvider(string language)
        {
            LanguageSource = language;
        }

        public string TextToTranslate { get; set; }

        public string LanguageSource { get; set; }

        public void TranslateAsync(string text)
        {
            // Initialize the strTextToTranslate here, while we're on the UI thread
            TextToTranslate = text;
            // STEP 1: Create the request for the OAuth service that will
            // get us our access tokens.
            String strTranslatorAccessURI = "https://datamarket.accesscontrol.windows.net/v2/OAuth2-13";
            System.Net.WebRequest req = System.Net.WebRequest.Create(strTranslatorAccessURI);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            // Important to note -- the call back here isn't that the request was processed by the server
            // but just that the request is ready for you to do stuff to it (like writing the details)
            // and then post it to the server.
            IAsyncResult writeRequestStreamCallback =
              (IAsyncResult)req.BeginGetRequestStream(new AsyncCallback(RequestStreamReady), req);

        }

        private void RequestStreamReady(IAsyncResult ar)
        {
            // STEP 2: The request stream is ready. Write the request into the POST stream
            string clientID = "<<Your Client ID>>";
            string clientSecret = "<<Your Client Secret>>";
            String strRequestDetails = string.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=http://api.microsofttranslator.com", HttpUtility.UrlEncode(clientID), HttpUtility.UrlEncode(clientSecret));

            // note, this isn't a new request -- the original was passed to beginrequeststream, so we're pulling a reference to it back out. It's the same request

            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)ar.AsyncState;
            // now that we have the working request, write the request details into it
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(strRequestDetails);
            System.IO.Stream postStream = request.EndGetRequestStream(ar);
            postStream.Write(bytes, 0, bytes.Length);
            postStream.Close();
            // now that the request is good to go, let's post it to the server
            // and get the response. When done, the async callback will call the
            // GetResponseCallback function
            request.BeginGetResponse(new AsyncCallback(GetResponseCallback), request);
        }

        private void GetResponseCallback(IAsyncResult ar)
        {
            // STEP 3: Process the response callback to get the token
            // we'll then use that token to call the translator service
            // Pull the request out of the IAsynch result
            HttpWebRequest request = (HttpWebRequest)ar.AsyncState;
            // The request now has the response details in it (because we've called back having gotten the response
            HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(ar);
            // Using JSON we'll pull the response details out, and load it into an AdmAccess token class (defined earlier in this module)
            // IMPORTANT (and vague) -- despite the name, in Windows Phone, this is in the System.ServiceModel.Web library,
            // and not the System.Runtime.Serialization one -- so you will need to have a reference to System.ServiceModel.Web

            System.Runtime.Serialization.Json.DataContractJsonSerializer serializer = new
            System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(AdmAccessToken));
            AdmAccessToken token = (AdmAccessToken)serializer.ReadObject(response.GetResponseStream());

            string uri = string.Format("http://api.microsofttranslator.com/v2/Http.svc/Translate?text={0}&from={1}&to=es",
                System.Net.HttpUtility.UrlEncode(TextToTranslate), LanguageSource);
            System.Net.WebRequest translationWebRequest = System.Net.HttpWebRequest.Create(uri);
            // The authorization header needs to be "Bearer" + " " + the access token
            string headerValue = "Bearer " + token.access_token;
            translationWebRequest.Headers["Authorization"] = headerValue;
            // And now we call the service. When the translation is complete, we'll get the callback
            IAsyncResult writeRequestStreamCallback = (IAsyncResult)translationWebRequest.BeginGetResponse(new AsyncCallback(translationReady), translationWebRequest);

        }

        private void translationReady(IAsyncResult ar)
        {
            // STEP 4: Process the translation
            // Get the request details
            HttpWebRequest request = (HttpWebRequest)ar.AsyncState;
            // Get the response details
            HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(ar);
            // Read the contents of the response into a string
            System.IO.Stream streamResponse = response.GetResponseStream();
            System.IO.StreamReader streamRead = new System.IO.StreamReader(streamResponse);
            string responseString = streamRead.ReadToEnd();
            // Translator returns XML, so load it into an XDocument
            // Note -- you need to add a reference to the System.Linq.XML namespace
            XDocument xTranslation = XDocument.Parse(responseString);
            string strTest = xTranslation.Root.FirstNode.ToString();
            // Check if the event has attached listeners to raise
            if (TranslationCompleted != null)
            {
                var ev = new TranslationEventArgs { TranslatedText = strTest };
                TranslationCompleted(null, ev);
            }
        }
    }
}
