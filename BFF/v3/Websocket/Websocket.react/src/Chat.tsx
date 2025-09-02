import { useState, useEffect } from 'react';
import { useGraphQL } from './useGraphQL';

interface Message {
  id: string;
  content: string;
  timestamp: string;
  user?: string;
}

interface ChatProps {
  serverUrl: string;
}

export function Chat({ serverUrl }: ChatProps) {
  const [messages, setMessages] = useState<Message[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [username, setUsername] = useState('User');
  const [isSubscribed, setIsSubscribed] = useState(false);

  const { isConnected, error, query, subscribe } = useGraphQL({
    url: serverUrl,
    autoConnect: true
  });

  // Subscribe to new messages
  const startMessageSubscription = async () => {
    if (isSubscribed) return;

    try {
      const subscription = subscribe(`
        subscription {
          messageAdded {
            id
            content
            timestamp
            user
          }
        }
      `);

      if (subscription) {
        setIsSubscribed(true);
        
        for await (const result of subscription) {
          if (result.data?.messageAdded) {
            const newMsg: Message = result.data.messageAdded;
            setMessages(prev => [...prev, newMsg]);
          }
        }
      }
    } catch (err) {
      console.error('Subscription error:', err);
      setIsSubscribed(false);
    }
  };

  // Send a message
  const sendMessage = async () => {
    if (!newMessage.trim() || !isConnected) return;

    try {
      await query(`
        mutation SendMessage($content: String!, $user: String!) {
          sendMessage(content: $content, user: $user) {
            id
            content
            timestamp
            user
          }
        }
      `, {
        content: newMessage,
        user: username
      });

      setNewMessage('');
    } catch (err) {
      console.error('Send message error:', err);
    }
  };

  // Load initial messages
  const loadMessages = async () => {
    try {
      const result = await query(`
        query {
          messages {
            id
            content
            timestamp
            user
          }
        }
      `);

      if (result.data?.messages) {
        setMessages(result.data.messages);
      }
    } catch (err) {
      console.error('Load messages error:', err);
    }
  };

  useEffect(() => {
    if (isConnected) {
      loadMessages();
      startMessageSubscription();
    }
  }, [isConnected]);

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  };

  return (
    <div className="chat-container">
      <h2>Real-time Chat</h2>
      
      <div className="chat-status">
        <p>Status: <span className={isConnected ? 'connected' : 'disconnected'}>
          {isConnected ? 'Connected' : 'Disconnected'}
        </span></p>
        {error && <p className="error">Error: {error}</p>}
        <p>Subscription: <span className={isSubscribed ? 'active' : 'inactive'}>
          {isSubscribed ? 'Active' : 'Inactive'}
        </span></p>
      </div>

      <div className="chat-messages">
        {messages.map((message) => (
          <div key={message.id} className="chat-message">
            <div className="message-header">
              <span className="message-user">{message.user || 'Anonymous'}</span>
              <span className="message-time">
                {new Date(message.timestamp).toLocaleTimeString()}
              </span>
            </div>
            <div className="message-content">{message.content}</div>
          </div>
        ))}
      </div>

      <div className="chat-input">
        <input
          type="text"
          placeholder="Username"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          className="username-input"
        />
        <div className="message-input-container">
          <textarea
            placeholder="Type your message..."
            value={newMessage}
            onChange={(e) => setNewMessage(e.target.value)}
            onKeyPress={handleKeyPress}
            disabled={!isConnected}
            className="message-input"
            rows={3}
          />
          <button
            onClick={sendMessage}
            disabled={!isConnected || !newMessage.trim()}
            className="send-button"
          >
            Send
          </button>
        </div>
      </div>
    </div>
  );
}
