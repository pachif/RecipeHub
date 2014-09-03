using System;
using System.IO;
using System.Net;
using System.Threading;

namespace Recipes.Provider
{
    public class WebConsumer
    {
        public event EventHandler<ResultEventArgs> ResponseEnded;
        private HttpWebRequest webRequest;

        public string ContentType { get; set; }

        public double Timeout { get; set; }

        public double CountDown { get; set; }

        public void GetUrlAsync(string url)
        {
            Uri uri = new Uri(url);
            webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Method = "GET";
            webRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            webRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/36.0.1985.143 Safari/537.36";
            if (!string.IsNullOrEmpty(ContentType))
                webRequest.ContentType = ContentType;

            
            webRequest.BeginGetResponse(ResponseCallBack, null);
        }

        public string GetUrl(string url)
        {
            Uri uri = new Uri(url);
            var webRequest = WebRequest.Create(uri);
            webRequest.Method = "GET";
            if (!string.IsNullOrEmpty(ContentType))
                webRequest.ContentType = ContentType;

            AutoResetEvent autoResetEvent = new AutoResetEvent(false);
            IAsyncResult asyncResult = webRequest.BeginGetRequestStream(r => autoResetEvent.Set(), null);
            
            // Wait until the call is finished
            asyncResult.AsyncWaitHandle.WaitOne();

            Stream streamResponse = webRequest.EndGetRequestStream(asyncResult);
            StreamReader streamRead = new StreamReader(streamResponse);
            return streamRead.ReadToEnd();
        }

        private void TriggerTimeoutException(object state)
        {
            var resultEventArgs = new ResultEventArgs { HasFail = true, Result = "Tiempo de Espera excedido. Servicio fuera de linea" };
            TriggerReponseEnded(state, resultEventArgs);
        }

        private void ResponseCallBack(IAsyncResult ar)
        {
            ResultEventArgs resultEventArgs = null;
            string htmldoc = string.Empty;
            try
            {
                var response = webRequest.EndGetResponse(ar);
                Stream streamResponse = response.GetResponseStream();
                StreamReader streamRead = new StreamReader(streamResponse);
                htmldoc = streamRead.ReadToEnd();
                streamResponse.Close();
                streamRead.Close();
                resultEventArgs = new ResultEventArgs { HasFail = false, Result = htmldoc };
            }
            catch (System.Net.WebException ex)
            {
                resultEventArgs = new ResultEventArgs { HasFail = true, Result = ex };
                //throw;
            }
            
            TriggerReponseEnded(ar.AsyncState,resultEventArgs);
        }

        private void TriggerReponseEnded(object sender, ResultEventArgs resultEventArgs)
        {
            if (ResponseEnded != null)
            {
                ResponseEnded(sender, resultEventArgs);
            }
        }
    }

    public class ResultEventArgs : EventArgs
    {
        public bool HasFail { get; set; }
        public object Result { get; set; }
    }
}
