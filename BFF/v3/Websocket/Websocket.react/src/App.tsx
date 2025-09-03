import { useState, useEffect } from 'react'
import './App.css'
import { useGraphQL } from './useGraphQL'
import { BookManager } from './BookManager'

type TabType = 'testing' | 'books';

interface UserClaims {
  [key: string]: any;
  name?: string;
  sub?: string;
}

function App() {
  const [activeTab, setActiveTab] = useState<TabType>('testing')
  const [messages, setMessages] = useState<string[]>([])
  const [queryResult, setQueryResult] = useState<any>(null)
  const [subscriptionData, setSubscriptionData] = useState<any[]>([])
  
  // Authentication state
  const [isLoggedIn, setIsLoggedIn] = useState<boolean>(false)
  const [userClaims, setUserClaims] = useState<UserClaims | null>(null)
  const [authLoading, setAuthLoading] = useState<boolean>(true)
  
  const serverUrl = 'http://localhost:5197'
  const { isConnected, error, connect, disconnect, query, subscribe } = useGraphQL({
    url: serverUrl,
    autoConnect: false // Don't auto-connect until we're authenticated
  })

  // Check authentication status on startup
  useEffect(() => {
    checkAuthStatus()
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
        if (claims && Object.keys(claims).length > 0) {
          setIsLoggedIn(true)
          setUserClaims(claims)
          setMessages(prev => [...prev, 'User authenticated successfully'])
        } else {
          setIsLoggedIn(false)
          setUserClaims(null)
        }
      } else {
        setIsLoggedIn(false)
        setUserClaims(null)
        setMessages(prev => [...prev, 'User not authenticated'])
      }
    } catch (err) {
      setIsLoggedIn(false)
      setUserClaims(null)
      setMessages(prev => [...prev, `Auth check error: ${err instanceof Error ? err.message : 'Unknown error'}`])
    } finally {
      setAuthLoading(false)
    }
  }

  const handleLogin = () => {
    // Redirect to BFF login endpoint
    window.location.href = '/bff/login'
  }

  const handleLogout = async () => {
    try {
      const response = await fetch('/bff/logout', {
        method: 'POST',
        credentials: 'include'
      })
      
      if (response.ok) {
        setIsLoggedIn(false)
        setUserClaims(null)
        disconnect() // Disconnect from GraphQL
        setMessages(prev => [...prev, 'Logged out successfully'])
        // Optionally redirect to home or login page
        window.location.href = '/'
      } else {
        setMessages(prev => [...prev, 'Logout failed'])
      }
    } catch (err) {
      setMessages(prev => [...prev, `Logout error: ${err instanceof Error ? err.message : 'Unknown error'}`])
    }
  }

  const getUserDisplayName = (): string => {
    if (!userClaims) return 'Unknown User'
    
    // Try different claim types for the user's name
    return userClaims.name || 
           userClaims.given_name || 
           userClaims.preferred_username || 
           userClaims.email || 
           userClaims.sub || 
           'Unknown User'
  }

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

  // Show login screen if not authenticated
  if (!isLoggedIn) {
    return (
      <div className="app">
        <div className="auth-container">
          <h1>React WebSocket GraphQL Client</h1>
          <div className="login-section">
            <h2>Authentication Required</h2>
            <p>Please log in to access the GraphQL client.</p>
            <button onClick={handleLogin} className="login-button">
              Login
            </button>
          </div>
        </div>
      </div>
    )
  }

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
