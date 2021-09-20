using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CommonInfrastructure
{
    public static class HttpExtension
    {

        public class AuthInformation
        {
            public string? AuthType { get; set; }
            public string? Login { get; set; }
            public string? Password { get; set; }
        }

        public static void UpdateCredential(this WebRequest request, AuthInformation authInfo)
        {
            if (authInfo.AuthType == "NetworkCredential")
            {
                request.Credentials = new NetworkCredential(authInfo.Login, authInfo.Password);
            }
            else if (authInfo.AuthType == "HeadersBasic")
            {
                var basicAuthToken = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo.Login + ":" + authInfo.Password));
                request.Headers["Authorization"] = "Basic " + basicAuthToken;
            }
            else
            {
                throw new NotSupportedException("Undefined AuthType");
            }
        }


        private const int DefaultReTriesCount = 3;

        public static async Task<XDocument> LoadXmlFromRequest(string uri, 
            TimeSpan timeout, 
            Func<WebRequest, Task> addCredintal,
            Func<Task> updateCredintal
            )
        {
            var reTry = 0;
            var unauthorizedReTry = false;
            Exception? ex = null;


            while (reTry < DefaultReTriesCount)
            {
                reTry++;

                var request = WebRequest.Create(uri);
                request.Timeout = (int) timeout.TotalMilliseconds;
                await addCredintal(request);
                try
                {
                    using var response = (HttpWebResponse)(await request.GetResponseAsync());
                    if (response.StatusCode == HttpStatusCode.OK)
                        return await XDocument.LoadAsync(response.GetResponseStream(), LoadOptions.None, CancellationToken.None);
                }
                catch (WebException e) when (e.Response is HttpWebResponse response 
                                             && (
                                                 response.StatusCode == HttpStatusCode.Unauthorized
                                                    || response.StatusCode == HttpStatusCode.Forbidden
                                                 )
                                             )
                {
                    if (unauthorizedReTry)
                        throw;

                    Console.WriteLine("Unauthorized/Fobitten exception " + request.RequestUri + " exception " + e);
                    unauthorizedReTry = true;
                    await updateCredintal();
                }
                catch (WebException e) when (e.Status == WebExceptionStatus.Timeout)
                {
                    Console.WriteLine("Timeout exception " + request.RequestUri + " exception " + e);
                    timeout *= 2;
                    ex = e;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Some exception " + request.RequestUri + " exception " + e);
                    ex = e;
                }
                
                await Task.Delay(1000);
            }

            if (reTry >= DefaultReTriesCount)
            {
                if (ex != null)
                    throw ex; // throw last exception
                else
                    throw new NotSupportedException("Something wrong. Retryed maximum times.");
            }

            throw new NotSupportedException("Something wrong");
        }
    }
}