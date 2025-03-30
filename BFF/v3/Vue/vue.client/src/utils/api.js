/**
 * Utility function for making authenticated API calls to the BFF.
 * Includes credentials and adds X-CSRF header
 * Handles basic error checking and JSON parsing.
 *
 * @param {RequestInfo | URL} url The API endpoint URL (e.g., '/todos').
 * @param {RequestInit} [options] Optional fetch options.
 * @returns {Promise<any>} The parsed JSON response body or undefined for 204.
 * @throws {Error} If the request fails.
 */
export async function fetchApi(url, options = {}) {
  const defaultOptions = {
    credentials: 'include',
    headers: {
      'Accept': 'application/json',
      'Content-Type': 'application/json',
      'X-CSRF': '1',
      ...options.headers,
    },
  };

  const mergedOptions = {
    ...options,
    ...defaultOptions,
    headers: {
      ...defaultOptions.headers,
      ...options.headers
    }
  };

  try {
    const response = await fetch(url, mergedOptions);
    if (!response.ok) {
      let errorData = `Request failed with status ${response.status}`;
      try {
        const errorJson = await response.json();
        errorData = errorJson.detail || errorJson.title || JSON.stringify(errorJson);
      } catch (e) {
      }
      console.error(`API Error (${url}): ${response.status} - ${errorData}`);
      throw new Error(`API Error: ${errorData}`);
    }

    if (response.status === 204 || response.headers.get('content-length') === '0') {
      return undefined;
    }

    return response.json();

  } catch (error) {
    console.error(`Network or Fetch error calling API (${url}):`, error);
    throw error;
  }
}
