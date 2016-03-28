using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Timers;

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
        /// Demo.  You can delete this.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        [WebGet(UriTemplate = "/numbers?length={n}")]
        IList<int> Numbers(string n);

        /// <summary>
        /// Demo.  You can delete this.
        /// </summary>
        [WebInvoke(Method = "POST", UriTemplate = "/first")]
        int GetFirst(IList<int> list);
    }
}
