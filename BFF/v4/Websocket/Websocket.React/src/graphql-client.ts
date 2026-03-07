import { createClient, type Client } from 'graphql-ws';

export interface GraphQLClient {
  client: Client;
  subscribe: (query: string, variables?: Record<string, any>) => AsyncIterableIterator<any>;
  query: (query: string, variables?: Record<string, any>) => Promise<any>;
}

export function createGraphQLClient(url: string): GraphQLClient {
  // Convert HTTP URL to WebSocket URL for GraphQL subscriptions
  const wsUrl = url.replace(/^http/, 'ws') + '/graphql';
  
  const client = createClient({
    url: wsUrl,
    connectionParams: {
      // Add any authentication headers or connection parameters here
    },
    shouldRetry: () => true,
    retryAttempts: 5,
  });

  const subscribe = (query: string, variables?: Record<string, any>) => {
    return client.iterate({
      query,
      variables,
    });
  };

  const query = async (query: string, variables?: Record<string, any>): Promise<any> => {
    return new Promise((resolve, reject) => {
      let result: any;
      const unsubscribe = client.subscribe(
        {
          query,
          variables,
        },
        {
          next: (data) => {
            result = data;
          },
          error: (error) => {
            reject(error);
            unsubscribe();
          },
          complete: () => {
            resolve(result);
            unsubscribe();
          },
        }
      );
    });
  };

  return {
    client,
    subscribe,
    query,
  };
}
