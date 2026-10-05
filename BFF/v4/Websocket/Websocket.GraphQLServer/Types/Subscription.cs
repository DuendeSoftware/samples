namespace Websocket.GraphQLServer.Types;

public class Subscription
{
    [Subscribe]
    public Book BookAdded([EventMessage] Book book) => book;

}