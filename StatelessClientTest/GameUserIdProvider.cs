using Microsoft.AspNetCore.SignalR;

namespace StatelessClientTest
{
    public class GameUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            return connection.GetHttpContext().Request.Query["username"].ToString();
        }
    }
}
