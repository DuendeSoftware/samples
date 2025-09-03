using HotChocolate.Subscriptions;

namespace Websocket.GraphQLServer.Types;

public class Mutation
{
    public async Task<Book> AddBook(Book book, [Service] ITopicEventSender sender)
    {
        await sender.SendAsync("BookAdded", book);

        // Omitted code for brevity
        return book;
    }


}