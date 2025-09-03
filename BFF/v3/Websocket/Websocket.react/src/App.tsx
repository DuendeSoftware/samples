import { useState, useEffect } from 'react'
import './App.css'
import { useGraphQL } from './useGraphQL'

type TabType = 'testing';

interface ClaimRecord {
  [type: string]: any;
  value?: string;
  valueType?: string;
}

function App() {
  const [activeTab, setActiveTab] = useState<TabType>('testing')
  const [messages, setMessages] = useState<string[]>([])
  const [queryResult, setQueryResult] = useState<any>(null)
  const [subscriptionData, setSubscriptionData] = useState<any[]>([])

  // Authentication state
  const [isLoggedIn, setIsLoggedIn] = useState<boolean>(false)
  const [userClaims, setUserClaims] = useState<ClaimRecord[]>([])
  const [authLoading, setAuthLoading] = useState<boolean>(true)

  const serverUrl = 'wss://localhost:7140'
  const { isConnected, error, connect, disconnect, query, subscribe } = useGraphQL({
    url: serverUrl,
    autoConnect: false // Don't auto-connect until we're authenticated
  })

  // Check authentication status on startup
  useEffect(() => {
    checkAuthStatus()
    setMessages(prev => [...prev, 'Start a subscription first. Then add a book. This should trigger the subscription to report an added book.']);
  }, [])

  // Auto-connect to GraphQL when logged in
  useEffect(() => {
    if (isLoggedIn && !isConnected) {
      connect()
    }
  }, [isLoggedIn, isConnected, connect])

  const checkAuthStatus = async () => {
    try {
      setAuthLoading(true)
      const response = await fetch('/bff/user', {
        headers: {
          'x-csrf': '1'
        },
        credentials: 'include' // Include cookies for authentication
      })

      if (response.ok) {
        const claims = await response.json()
        console.log(claims);
        if (claims && Object.keys(claims).length > 0) {
          setIsLoggedIn(true)
          setUserClaims(claims)
          setMessages(prev => [...prev, 'User authenticated successfully'])
        } else {
          setIsLoggedIn(false)
          setUserClaims([])
        }
      } else {
        setIsLoggedIn(false)
        setUserClaims([])
        setMessages(prev => [...prev, 'User not authenticated'])
      }
    } catch (err) {
      setIsLoggedIn(false)
      setUserClaims([])
    } finally {
      setAuthLoading(false)
    }
  }

  const handleLogin = () => {
    // Redirect to BFF login endpoint
    window.location.href = '/bff/login'
  }

  const handleLogout = async () => {
    window.location.href = userClaims.find(claim => claim.type === 'bff:logout_url' && claim.value)?.value || '/bff/logout'
  }

  const getUserDisplayName = (): string => {
    if (!userClaims.length) return 'Unknown User'

    var name = userClaims.find(claim => claim.type === 'name' && claim.value)?.value;

    if (name) {
      return name;
    }
    return 'Unknown User';
  }

  // Example GraphQL query
  const handleQuery = async () => {
    try {
      const result = await query(`
        query {
          books {
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

  // Show loading state while checking authentication
  if (authLoading) {
    return (
      <div className="app">
        <div className="auth-loading">
          <h2>Checking authentication...</h2>
        </div>
      </div>
    )
  }

  // // Show login screen if not authenticated
  // if (!isLoggedIn) {
  //   return (
  //     <div className="app">
  //       <div className="auth-container">
  //         <h1>React WebSocket GraphQL Client</h1>
  //         <div className="login-section">
  //           <h2>Authentication Required</h2>
  //           <p>Please log in to access the GraphQL client.</p>
  //           <button onClick={handleLogin} className="login-button">
  //             Login
  //           </button>
  //         </div>
  //       </div>
  //     </div>
  //   )
  // }

  return (
    <div className="app">
      <div className="header">
        <h1>React WebSocket GraphQL Client</h1>
        <div className="user-info">
          <span className="welcome-message">Hello, {getUserDisplayName()}!</span>
          <button onClick={handleLogout} className="logout-button">
            Logout
          </button>
        </div>
      </div>

      <div className="tabs">
        <button
          className={`tab ${activeTab === 'testing' ? 'active' : ''}`}
          onClick={() => setActiveTab('testing')}
        >
          GraphQL Testing
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
              <button onClick={handleSubscription} disabled={!isConnected}>
                Subscribe to Books
              </button>
              <button onClick={handleAddBook} disabled={!isConnected}>
                Add Sample Book
              </button>
              <button onClick={handleQuery} disabled={!isConnected}>
                Query Books
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
    </div>
  )
}

export default App
