import { useEffect, useState, useRef, useCallback } from 'react';
import { createGraphQLClient, type GraphQLClient } from './graphql-client';

export interface UseGraphQLOptions {
  url: string;
  autoConnect?: boolean;
}

export interface UseGraphQLReturn {
  client: GraphQLClient | null;
  isConnected: boolean;
  error: string | null;
  connect: () => void;
  disconnect: () => void;
  subscribe: (query: string, variables?: Record<string, any>) => AsyncIterableIterator<any> | null;
  query: (query: string, variables?: Record<string, any>) => Promise<any>;
}

export function useGraphQL({ url, autoConnect = true }: UseGraphQLOptions): UseGraphQLReturn {
  const [client, setClient] = useState<GraphQLClient | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const clientRef = useRef<GraphQLClient | null>(null);

  const connect = useCallback(() => {
    try {
      setError(null);
      const newClient = createGraphQLClient(url);
      clientRef.current = newClient;
      setClient(newClient);
      setIsConnected(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to connect');
      setIsConnected(false);
    }
  }, [url]);

  const disconnect = useCallback(() => {
    if (clientRef.current) {
      clientRef.current.client.dispose();
      clientRef.current = null;
      setClient(null);
      setIsConnected(false);
    }
  }, []);

  const subscribe = useCallback((query: string, variables?: Record<string, any>) => {
    if (!clientRef.current) {
      console.warn('GraphQL client not connected');
      return null;
    }
    return clientRef.current.subscribe(query, variables);
  }, []);

  const query = useCallback(async (query: string, variables?: Record<string, any>) => {
    if (!clientRef.current) {
      throw new Error('GraphQL client not connected');
    }
    return clientRef.current.query(query, variables);
  }, []);

  useEffect(() => {
    if (autoConnect) {
      connect();
    }

    return () => {
      disconnect();
    };
  }, [autoConnect, connect, disconnect]);

  return {
    client,
    isConnected,
    error,
    connect,
    disconnect,
    subscribe,
    query,
  };
}
