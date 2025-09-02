# React WebSocket GraphQL Client

A React application that connects to a GraphQL server via WebSockets for real-time communication.

## Features

- **WebSocket GraphQL Connection**: Connects to GraphQL servers using WebSocket transport
- **Real-time Subscriptions**: Support for GraphQL subscriptions to receive real-time data
- **GraphQL Queries**: Execute GraphQL queries and display results
- **Connection Management**: Manual connect/disconnect functionality
- **Real-time Chat**: Example chat implementation using GraphQL subscriptions
- **Modern UI**: Clean, responsive interface with tabbed navigation

## GraphQL Server Requirements

Your GraphQL server at `http://localhost:5095` should support:

### WebSocket Endpoint
- WebSocket endpoint at `ws://localhost:5095/graphql`
- Support for graphql-ws protocol

### Example Schema

The app expects the following GraphQL schema (or similar):

```graphql
type Query {
  hello: String
  messages: [Message!]!
}

type Mutation {
  sendMessage(content: String!, user: String!): Message!
}

type Subscription {
  messageAdded: Message!
}

type Message {
  id: ID!
  content: String!
  timestamp: String!
  user: String
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
   - Execute test queries
   - Start subscriptions
   - View real-time messages and data

2. **Real-time Chat Tab**:
   - Enter a username
   - Send messages
   - View real-time message updates
   - Automatic subscription to new messages

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
├── Chat.tsx             # Real-time chat component
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

### Chat Component (`Chat.tsx`)
- Example real-time chat implementation
- Demonstrates subscription usage
- Message sending and receiving

## Troubleshooting

### Connection Issues
1. Ensure your GraphQL server is running on `http://localhost:5095`
2. Verify WebSocket endpoint is available at `ws://localhost:5095/graphql`
3. Check that the server supports graphql-ws protocol

### CORS Issues
Make sure your GraphQL server allows connections from `http://localhost:5173` (or your dev server port).

### GraphQL Schema
The example queries assume specific schema types. Modify the queries in the components to match your server's schema.

## Example Server Setup

For testing, you can use this minimal GraphQL server setup:

```javascript
// server.js (Node.js + Apollo Server example)
import { ApolloServer } from 'apollo-server-express';
import { WebSocketServer } from 'ws';
import { useServer } from 'graphql-ws/lib/use/ws';
import { makeExecutableSchema } from '@graphql-tools/schema';

const typeDefs = `
  type Query {
    hello: String
    messages: [Message!]!
  }
  
  type Mutation {
    sendMessage(content: String!, user: String!): Message!
  }
  
  type Subscription {
    messageAdded: Message!
  }
  
  type Message {
    id: ID!
    content: String!
    timestamp: String!
    user: String
  }
`;

// Add your resolvers here
const resolvers = { /* ... */ };

const schema = makeExecutableSchema({ typeDefs, resolvers });

// Setup Apollo Server and WebSocket server
// ... server configuration
```

## License

This project is part of the Duende Software samples repository.
