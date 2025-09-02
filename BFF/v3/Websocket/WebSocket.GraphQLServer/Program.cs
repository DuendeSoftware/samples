using HotChocolate.Subscriptions;
using WebSocket.GraphQLServer.Types;

var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL()
    .AddInMemorySubscriptions()
    .AddSubscriptionType<Subscription>()
    .AddMutationType<Mutation>()
    .AddMutationConventions()
    .AddTypes();

var app = builder.Build();

app.UseWebSockets();
app.MapGraphQL();

app.RunWithGraphQLCommands(args);
app.UseRouting();



public class Subscription
{
    [Subscribe]
    public Book BookAdded([EventMessage] Book book) => book;

}

public class Mutation
{
    public async Task<Book> AddBook(Book book, [Service] ITopicEventSender sender)
    {
        await sender.SendAsync("BookAdded", book);

        // Omitted code for brevity
        return book;
    }


}
