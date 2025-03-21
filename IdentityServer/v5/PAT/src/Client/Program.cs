// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        public static async Task Main()
        {
            var client = new HttpClient();

            while (true)
            {
                Console.WriteLine("Token:");
                var token = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(token)) break;

                var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:5002/identity");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("error:" + response.StatusCode);
                }

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(content);
                Console.ReadKey();
            }


        }
    }
}
