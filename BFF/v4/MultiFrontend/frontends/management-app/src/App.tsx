import { useState, useEffect } from 'react'
import reactLogo from './assets/react.svg'
import viteLogo from '/vite.svg'
import './App.css'

interface Claim{
  type: string;
  value: string;
}

function App() {
  const [count, setCount] = useState(0)
  const [user, setUser] = useState<Claim[]>([]);
  const [loading, setLoading] = useState(true);

  // Fetch user info on mount
  useEffect(() => {
    fetch('/management/bff/user', { credentials: 'include' })
      .then(async (res) => {
        if (!res.ok) {
          setUser([]);
        } else {
          const claims = await res.json();
          if (Array.isArray(claims) && claims.length > 0) {
            setUser(claims);
          } else {
            setUser([]);
          }
        }
        setLoading(false);
      })
      .catch(() => {
        setUser([]);
        setLoading(false);
      });
  }, []);

  // Helper to get claim value by type
  const getClaim = (type: string) => {
    if (!user) return null;
    const claim = user.find((c: any) => c.type === type);
    return claim ? claim.value : null;
  };

  if (loading) {
    return <div>Loading...</div>;
  }

  return (
    <>
      <div>
        <a href="https://vite.dev" target="_blank">
          <img src={viteLogo} className="logo" alt="Vite logo" />
        </a>
        <a href="https://react.dev" target="_blank">
          <img src={reactLogo} className="logo react" alt="React logo" />
        </a>
      </div>
      <h1>Vite + React</h1>
      <div className="card">
        <button onClick={() => setCount((count) => count + 1)}>
          count is {count}
        </button>
        <p>
          Edit <code>src/App.tsx</code> and save to test HMR
        </p>
      </div>
      {/* Login/Logout UI */}
      <div style={{ marginTop: '2rem' }}>
          {!user.length ? (
            <button onClick={() => window.location.href = '/management/bff/login'}>
              Login
            </button>
          ) : (
            <>
              <button onClick={() => window.location.href = getClaim('bff:logout_url') || '/management/bff/logout'}>
                Logout
              </button>
              <div style={{ marginTop: '1rem', textAlign: 'left' }}>
                <h3>User Info</h3>
                <ul>
                  <li><strong>Name:</strong> {getClaim('name')}</li>
                  <li><strong>Subject:</strong> {getClaim('sub')}</li>
                  <li><strong>IdP:</strong> {getClaim('idp')}</li>
                  <li><strong>SID:</strong> {getClaim('sid')}</li>
                  <li><strong>Logout URL:</strong> {getClaim('bff:logout_url')}</li>
                </ul>
              </div>
            </>
          )}
      </div>
      <p className="read-the-docs">
        Click on the Vite and React logos to learn more
      </p>
    </>
  )
}

export default App
