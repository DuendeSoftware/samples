using System.Security.Claims;

namespace Websocket.GraphQLServer.Types;

[QueryType]
public static class Query
{
    public static Book GetBook(ClaimsPrincipal claimsPrincipal)
    {
        return new Book("C# in depth.", new Author("Jon Skeet"));
    }
}
