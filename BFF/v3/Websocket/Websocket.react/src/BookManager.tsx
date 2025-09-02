import { useState, useEffect } from 'react';
import { useGraphQL } from './useGraphQL';

interface Author {
  name: string;
}

interface Book {
  title: string;
  author: Author;
}

interface BookManagerProps {
  serverUrl: string;
}

export function BookManager({ serverUrl }: BookManagerProps) {
  const [books, setBooks] = useState<Book[]>([]);
  const [newBookTitle, setNewBookTitle] = useState('');
  const [newBookAuthor, setNewBookAuthor] = useState('');
  const [isSubscribed, setIsSubscribed] = useState(false);
  const [subscriptionType, setSubscriptionType] = useState<'bookAdded' | 'publishBook'>('bookAdded');

  const { isConnected, error, query, subscribe } = useGraphQL({
    url: serverUrl,
    autoConnect: true
  });

  // Subscribe to book events
  const startBookSubscription = async () => {
    if (isSubscribed) return;

    try {
      const subscription = subscribe(`
        subscription {
          ${subscriptionType} {
            title
            author {
              name
            }
          }
        }
      `);

      if (subscription) {
        setIsSubscribed(true);
        
        for await (const result of subscription) {
          if (result.data?.[subscriptionType]) {
            const newBook: Book = result.data[subscriptionType];
            setBooks(prev => {
              // Avoid duplicates by checking if book already exists
              const exists = prev.some(book => 
                book.title === newBook.title && book.author.name === newBook.author.name
              );
              return exists ? prev : [...prev, newBook];
            });
          }
        }
      }
    } catch (err) {
      console.error('Subscription error:', err);
      setIsSubscribed(false);
    }
  };

  // Stop current subscription
  const stopSubscription = () => {
    setIsSubscribed(false);
    // Note: In a real implementation, you'd want to store the subscription
    // reference and properly unsubscribe
  };

  // Add a book using the addBook mutation
  const addBook = async () => {
    if (!newBookTitle.trim() || !newBookAuthor.trim() || !isConnected) return;

    try {
      const book = {
        title: newBookTitle,
        author: { name: newBookAuthor }
      };

      await query(`
        mutation AddBook($book: BookInput!) {
          addBook(book: $book) {
            title
            author {
              name
            }
          }
        }
      `, {
        book
      });

      setNewBookTitle('');
      setNewBookAuthor('');
    } catch (err) {
      console.error('Add book error:', err);
    }
  };

  // Publish a book using the publishBook mutation
  const publishBook = async () => {
    if (!newBookTitle.trim() || !newBookAuthor.trim() || !isConnected) return;

    try {
      await query(`
        mutation PublishBook($title: String!, $author: String!) {
          publishBook(title: $title, author: $author) {
            title
            author {
              name
            }
          }
        }
      `, {
        title: newBookTitle,
        author: newBookAuthor
      });

      setNewBookTitle('');
      setNewBookAuthor('');
    } catch (err) {
      console.error('Publish book error:', err);
    }
  };

  // Get the sample book from the query
  const getSampleBook = async () => {
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
      `);

      if (result.data?.book) {
        const book: Book = result.data.book;
        setBooks(prev => {
          const exists = prev.some(b => 
            b.title === book.title && b.author.name === book.author.name
          );
          return exists ? prev : [...prev, book];
        });
      }
    } catch (err) {
      console.error('Get sample book error:', err);
    }
  };

  useEffect(() => {
    if (isConnected) {
      getSampleBook();
      startBookSubscription();
    }
  }, [isConnected, subscriptionType]);

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      addBook();
    }
  };

  const clearBooks = () => {
    setBooks([]);
  };

  return (
    <div className="book-manager-container">
      <h2>Book Manager</h2>
      
      <div className="book-status">
        <p>Status: <span className={isConnected ? 'connected' : 'disconnected'}>
          {isConnected ? 'Connected' : 'Disconnected'}
        </span></p>
        {error && <p className="error">Error: {error}</p>}
        <p>Subscription: <span className={isSubscribed ? 'active' : 'inactive'}>
          {isSubscribed ? `Active (${subscriptionType})` : 'Inactive'}
        </span></p>
      </div>

      <div className="subscription-controls">
        <h3>Subscription Type</h3>
        <div className="subscription-options">
          <label>
            <input
              type="radio"
              value="bookAdded"
              checked={subscriptionType === 'bookAdded'}
              onChange={(e) => {
                setSubscriptionType(e.target.value as 'bookAdded');
                if (isSubscribed) {
                  stopSubscription();
                }
              }}
            />
            Book Added
          </label>
          <label>
            <input
              type="radio"
              value="publishBook"
              checked={subscriptionType === 'publishBook'}
              onChange={(e) => {
                setSubscriptionType(e.target.value as 'publishBook');
                if (isSubscribed) {
                  stopSubscription();
                }
              }}
            />
            Publish Book
          </label>
        </div>
      </div>

      <div className="book-operations">
        <h3>Operations</h3>
        <div className="operation-controls">
          <button onClick={getSampleBook} disabled={!isConnected}>
            Get Sample Book
          </button>
          <button onClick={clearBooks}>
            Clear Books
          </button>
        </div>
      </div>

      <div className="book-list">
        <h3>Books ({books.length})</h3>
        <div className="books">
          {books.map((book, index) => (
            <div key={index} className="book-item">
              <div className="book-title">{book.title}</div>
              <div className="book-author">by {book.author.name}</div>
            </div>
          ))}
          {books.length === 0 && (
            <p className="no-books">No books yet. Add some books or get the sample book!</p>
          )}
        </div>
      </div>

      <div className="book-input">
        <h3>Add New Book</h3>
        <div className="input-group">
          <input
            type="text"
            placeholder="Book Title"
            value={newBookTitle}
            onChange={(e) => setNewBookTitle(e.target.value)}
            onKeyPress={handleKeyPress}
            disabled={!isConnected}
            className="book-title-input"
          />
          <input
            type="text"
            placeholder="Author Name"
            value={newBookAuthor}
            onChange={(e) => setNewBookAuthor(e.target.value)}
            onKeyPress={handleKeyPress}
            disabled={!isConnected}
            className="book-author-input"
          />
        </div>
        <div className="button-group">
          <button
            onClick={addBook}
            disabled={!isConnected || !newBookTitle.trim() || !newBookAuthor.trim()}
            className="add-book-button"
          >
            Add Book
          </button>
          <button
            onClick={publishBook}
            disabled={!isConnected || !newBookTitle.trim() || !newBookAuthor.trim()}
            className="publish-book-button"
          >
            Publish Book
          </button>
        </div>
      </div>
    </div>
  );
}
