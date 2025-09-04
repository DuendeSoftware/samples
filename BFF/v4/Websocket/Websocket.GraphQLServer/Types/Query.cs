using System.Security.Claims;

namespace Websocket.GraphQLServer.Types;

[QueryType]
public static class Query
{
    public static Book[] GetBooks(ClaimsPrincipal claimsPrincipal, [Service]FakeDatabase db)
    {
        return db.Books.ToArray();
    }
}

public class FakeDatabase
{
    public List<Book> Books { get; } = [new Book("C# in depth.", new Author("Jon Skeet"))];
}
