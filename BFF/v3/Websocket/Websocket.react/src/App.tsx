import { useState, useEffect } from 'react'
import './App.css'
import { useGraphQL } from './useGraphQL'
import { BookManager } from './BookManager'

type TabType = 'testing' | 'books';

function App() {
  const [activeTab, setActiveTab] = useState<TabType>('testing')
  const [messages, setMessages] = useState<string[]>([])
  const [queryResult, setQueryResult] = useState<any>(null)
  const [subscriptionData, setSubscriptionData] = useState<any[]>([])
  
  const serverUrl = 'http://localhost:5197'
  const { isConnected, error, connect, disconnect, query, subscribe } = useGraphQL({
    url: serverUrl,
    autoConnect: true
  })

  // Example GraphQL query
  const handleQuery = async () => {
    try {
      const result = await query(`
        query {
          book {
            title
            author {
              name
            }
          }
        }
      `)
      setQueryResult(result)
      setMessages(prev => [...prev, `Query executed: ${JSON.stringify(result)}`])
    } catch (err) {
      setMessages(prev => [...prev, `Query error: ${err instanceof Error ? err.message : 'Unknown error'}`])
    }
  }

  // Example GraphQL subscription
  const handleSubscription = async () => {
    try {
      const subscription = subscribe(`
        subscription {
          bookAdded {
            title
            author {
              name
            }
          }
        }
      `)

      if (subscription) {
        setMessages(prev => [...prev, 'Starting book subscription...'])
        
        for await (const result of subscription) {
          setSubscriptionData(prev => [...prev, result])
          setMessages(prev => [...prev, `Subscription data: ${JSON.stringify(result)}`])
        }
      }
    } catch (err) {
      setMessages(prev => [...prev, `Subscription error: ${err instanceof Error ? err.message : 'Unknown error'}`])
    }
  }

  // Example AddBook mutation
  const handleAddBook = async () => {
    try {
      const result = await query(`
        mutation{
          addBook(input:  {
            book:  {
                author:  {
                  name: "name"
                },
                title: "name"
            }
          }){
            book{
              title
            }
          }
        }
      `)
      setQueryResult(result)
      setMessages(prev => [...prev, `AddBook mutation executed: ${JSON.stringify(result)}`])
    } catch (err) {
      setMessages(prev => [...prev, `AddBook error: ${err instanceof Error ? err.message : 'Unknown error'}`])
    }
  }

  const clearMessages = () => {
    setMessages([])
    setQueryResult(null)
    setSubscriptionData([])
  }

  useEffect(() => {
    if (isConnected) {
      setMessages(prev => [...prev, 'Connected to GraphQL WebSocket server'])
    } else if (error) {
      setMessages(prev => [...prev, `Connection error: ${error}`])
    }
  }, [isConnected, error])

  return (
    <div className="app">
      <h1>React WebSocket GraphQL Client</h1>
      
      <div className="tabs">
        <button 
          className={`tab ${activeTab === 'testing' ? 'active' : ''}`}
          onClick={() => setActiveTab('testing')}
        >
          GraphQL Testing
        </button>
        <button 
          className={`tab ${activeTab === 'books' ? 'active' : ''}`}
          onClick={() => setActiveTab('books')}
        >
          Book Manager
        </button>
      </div>

      {activeTab === 'testing' && (
        <>
          <div className="connection-status">
            <h2>Connection Status</h2>
            <p>Status: <span className={isConnected ? 'connected' : 'disconnected'}>
              {isConnected ? 'Connected' : 'Disconnected'}
            </span></p>
            <p>Server: {serverUrl}/graphql</p>
            {error && <p className="error">Error: {error}</p>}
            
            <div className="connection-controls">
              <button onClick={connect} disabled={isConnected}>
                Connect
              </button>
              <button onClick={disconnect} disabled={!isConnected}>
                Disconnect
              </button>
            </div>
          </div>

          <div className="graphql-operations">
            <h2>GraphQL Operations</h2>
            <div className="operation-controls">
              <button onClick={handleQuery} disabled={!isConnected}>
                Get Sample Book
              </button>
              <button onClick={handleSubscription} disabled={!isConnected}>
                Subscribe to Books
              </button>
              <button onClick={handleAddBook} disabled={!isConnected}>
                Add Sample Book
              </button>
              <button onClick={clearMessages}>
                Clear Messages
              </button>
            </div>
          </div>

          <div className="messages">
            <h2>Messages ({messages.length})</h2>
            <div className="message-list">
              {messages.map((message, index) => (
                <div key={index} className="message">
                  [{new Date().toLocaleTimeString()}] {message}
                </div>
              ))}
            </div>
          </div>

          {queryResult && (
            <div className="query-result">
              <h2>Latest Query Result</h2>
              <pre>{JSON.stringify(queryResult, null, 2)}</pre>
            </div>
          )}

          {subscriptionData.length > 0 && (
            <div className="subscription-data">
              <h2>Subscription Data ({subscriptionData.length} items)</h2>
              <div className="subscription-list">
                {subscriptionData.map((data, index) => (
                  <div key={index} className="subscription-item">
                    <pre>{JSON.stringify(data, null, 2)}</pre>
                  </div>
                ))}
              </div>
            </div>
          )}
        </>
      )}

      {activeTab === 'books' && (
        <BookManager serverUrl={serverUrl} />
      )}
    </div>
  )
}

export default App
