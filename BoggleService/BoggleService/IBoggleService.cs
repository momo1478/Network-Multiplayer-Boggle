using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Web.Http;

namespace Boggle
{
    [ServiceContract]
    public interface IBoggleService
    {
        /// <summary>
        /// Sends back index.html as the response body.
        /// </summary>
        [WebGet(UriTemplate = "/api")]
        Stream API();

        /// <summary>
        /// Create User for Boggle API
        /// </summary>
        [WebInvoke(Method = "POST", UriTemplate = "/users")]
        CreateUserReturn CreateUser(UserInfo nickname);

        /// <summary>
        /// Join Game for Boggle API
        /// </summary>
        [WebInvoke(Method = "POST", UriTemplate = "/games")]
        JoinGameReturn JoinGame(JoinGameArgs args);

        /// <summary>
        /// Join Game for Boggle API
        /// </summary>
        [WebInvoke(Method = "PUT", UriTemplate = "/games")]
        void CancelJoinRequest(JoinGameArgs args);


        [WebInvoke(Method = "PUT", UriTemplate = "/games/{GameID}")]
        PlayWordReturn PlayWord(PlayWordArgs args, string GameID);


        [WebInvoke(Method = "GET", UriTemplate = "/games/{GameID}")]
        GetStatusReturn GetStatus(string GameID);

        [WebInvoke(Method = "GET", UriTemplate = "/games/{GameID}?brief={brief}")]
        GetStatusReturn GetStatusBrief(string GameID, string brief);





        //    /// <summary>
        //    /// Demo.  You can delete this.
        //    /// </summary>
        //    /// <param name="n"></param>
        //    /// <returns></returns>
        //    [WebGet(UriTemplate = "/numbers?length={n}")]
        //    IList<int> Numbers(string n);

        //    /// <summary>
        //    /// Demo.  You can delete this.
        //    /// </summary>
        //    [WebInvoke(Method = "POST", UriTemplate = "/first")]
        //    int GetFirst(IList<int> list);
        //}
    }
}
