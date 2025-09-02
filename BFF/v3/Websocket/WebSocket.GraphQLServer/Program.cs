var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL()
    .AddInMemorySubscriptions()
    .AddTypes();

var app = builder.Build();

app.UseWebSockets();
app.MapGraphQL();

app.RunWithGraphQLCommands(args);
app.UseRouting();



