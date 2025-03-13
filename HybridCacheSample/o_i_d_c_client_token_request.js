import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 100, // Virtual Users
  duration: '60s', // Duration of the test
};

// Configuration
const oidcProviderUrl = 'http://localhost:8080'; // Replace with your OIDC provider URL
const clientIds = ['m2m.client']; // Replace with your client ID
const clientSecret = '511536EF-F270-4058-80CA-1C89C192F69A'; // Replace with your client secret
const scope = 'scope1'; // Replace with the scopes you need

export default function () {
  // 1. Construct the token request body
  const tokenRequestBody = {
    grant_type: 'client_credentials',
    client_id: clientIds[Math.floor(Math.random() * clientIds.length)],
    client_secret: clientSecret,
    scope: scope,
  };

  // 2. Make the token request
  const tokenResponse = http.post(`${oidcProviderUrl}/connect/token`, tokenRequestBody);

  // 3. Check the token response
  check(tokenResponse, {
    'Token request status is 200': (r) => r.status === 200,
  });

  if (tokenResponse.status === 200) {
    const token = tokenResponse.json().access_token;
    console.log('Successfully retrieved token:', token);

    // You can now use the token in subsequent requests
    // Example:
    // const headers = {
    //   Authorization: `Bearer ${token}`,
    // };
    // const apiResponse = http.get('YOUR_API_ENDPOINT', { headers: headers });
    // check(apiResponse, {
    //   'API request status is 200': (r) => r.status === 200,
    // });
  } else {
    console.error('Failed to retrieve token:', tokenResponse.status, tokenResponse.body);
  }

  sleep(1); // Sleep for 1 second between iterations
}
