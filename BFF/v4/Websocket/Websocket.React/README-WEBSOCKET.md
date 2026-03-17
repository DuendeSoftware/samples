# React WebSocket GraphQL Client

A React application that connects to a HotChocolate GraphQL server via WebSockets for real-time communication.

## Features

- **WebSocket GraphQL Connection**: Connects to GraphQL servers using WebSocket transport
- **Real-time Subscriptions**: Support for GraphQL subscriptions to receive real-time data
- **GraphQL Queries**: Execute GraphQL queries and display results
- **Connection Management**: Manual connect/disconnect functionality
- **Book Management**: Example book management system using GraphQL subscriptions and mutations
- **Modern UI**: Clean, responsive interface with tabbed navigation

## HotChocolate Server Requirements

Your GraphQL server at `http://localhost:5095` should support:

### WebSocket Endpoint
- WebSocket endpoint at `ws://localhost:5095/graphql`
- Support for graphql-ws protocol
- HotChocolate v15+ with subscription support

### Expected Schema

The app works with the HotChocolate sample schema:

```graphql
type Query {
  book: Book!
}

type Mutation {
  addBook(book: BookInput!): Book!
  publishBook(title: String!, author: String!): Book!
}

type Subscription {
  bookAdded: Book!
  publishBook: Book!
}

type Book {
  title: String!
  author: Author!
}

type Author {
  name: String!
}

input BookInput {
  title: String!
  author: AuthorInput!
}

input AuthorInput {
  name: String!
}
```

## Getting Started

### Prerequisites
- Node.js 18+ 
- npm or yarn
- GraphQL server running on `http://localhost:5095`

### Installation

```bash
# Install dependencies
npm install

# Start development server
npm run dev
```

### Usage

1. **GraphQL Testing Tab**:
   - View connection status
   - Execute test queries (get sample book)
   - Start subscriptions (book events)
   - View real-time messages and data

2. **Book Manager Tab**:
   - View current books
   - Add new books using mutations
   - Publish books using mutations  
   - Subscribe to book events (bookAdded or publishBook)
   - Real-time updates when books are added or published

## Dependencies

- **react**: UI framework
- **graphql**: GraphQL client utilities
- **graphql-ws**: WebSocket GraphQL client for subscriptions
- **vite**: Build tool
- **typescript**: Type safety

## Configuration

The server URL is configured in `src/App.tsx`:

```typescript
const serverUrl = 'http://localhost:5095'
```

Change this to match your GraphQL server URL.

## File Structure

```
src/
├── App.tsx              # Main application component with tabs
├── App.css              # Application styles
├── BookManager.tsx      # Book management component
├── graphql-client.ts    # GraphQL WebSocket client utility
├── useGraphQL.ts        # React hook for GraphQL operations
├── main.tsx             # Application entry point
└── ...
```

## Key Components

### GraphQL Client (`graphql-client.ts`)
- Creates WebSocket connection to GraphQL server
- Handles queries and subscriptions
- Automatic reconnection logic

### useGraphQL Hook (`useGraphQL.ts`)
- React hook for GraphQL operations
- Connection state management
- Easy-to-use query and subscription methods

### BookManager Component (`BookManager.tsx`)
- Example book management implementation
- Demonstrates subscription usage (bookAdded, publishBook)
- Book adding and publishing with mutations
- Real-time book updates

## Troubleshooting

### Connection Issues
1. Ensure your GraphQL server is running on `http://localhost:5095`
2. Verify WebSocket endpoint is available at `ws://localhost:5095/graphql`
3. Check that the server supports graphql-ws protocol

### CORS Issues
Make sure your GraphQL server allows connections from `http://localhost:5173` (or your dev server port).

### GraphQL Schema
The example queries match the HotChocolate server schema. The server provides:
- A sample book query that returns "C# in depth." by "Jon Skeet"
- Mutations for adding and publishing books
- Subscriptions for real-time book events

## Example Server (Already Provided)

The HotChocolate GraphQL server is already set up in the `WebSocket.GraphQLServer` project with:

- **Query**: `book` - Returns sample book
- **Mutations**: `addBook` and `publishBook` - Add or publish books
- **Subscriptions**: `bookAdded` and `publishBook` - Real-time book events
- **Types**: `Book` (title, author) and `Author` (name)

To start the server:
```bash
cd ../WebSocket.GraphQLServer
dotnet run
```

## License

This project is part of the Duende Software samples repository.
