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

    [Subscribe]
    [Topic(nameof(Mutation.PublishBook))]
    public Book PublishBook([EventMessage] Book book) => book;
}

public class Mutation
{
    public async Task<Book> AddBook(Book book, [Service] ITopicEventSender sender)
    {
        await sender.SendAsync("BookAdded", book);

        // Omitted code for brevity
        return book;
    }

    public async Task<Book> PublishBook(string title, string author, [Service] ITopicEventSender eventSender, CancellationToken ct)
    {
        var book = new Book(title, new Author(author));

        await eventSender.SendAsync(nameof(PublishBook), book, ct);
        return book;
    }

}
