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
using System.IO;
using System.Threading;

namespace Recipes.Provider
{
    public class WebConsumer
    {
        public event EventHandler<ResultEventArgs> ResponseEnded;
        private HttpWebRequest webRequest;
        private string postData;

        public string ContentType { get; set; }

        public void GetUrlAsync(string url)
        {
            Uri uri = new Uri(url);
            webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Method = "GET";
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
            catch (Exception ex)
            {
                resultEventArgs = new ResultEventArgs { HasFail = true, Result = ex };
                //throw;
            }
            
            if (ResponseEnded != null)
            {
                ResponseEnded(htmldoc, resultEventArgs);
            }
        }
    }

    public class ResultEventArgs : EventArgs
    {
        public bool HasFail { get; set; }
        public object Result { get; set; }
    }
}
