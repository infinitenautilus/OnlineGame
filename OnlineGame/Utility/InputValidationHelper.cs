using System;
using System.Threading.Tasks;
using OnlineGame.Network.Client;

namespace OnlineGame.Utility
{
    public static class InputValidationHelper
    {
        /// <summary>
        /// Repeatedly collects input and validates it using a provided function.
        /// The caller handles prompting and validation logic.
        /// </summary>
        /// <param name="inputProvider">A function to provide input from the client (e.g., prompt and receive).</param>
        /// <param name="validationCondition">A function that returns true if the input is valid.</param>
        /// <param name="maxRetries">Maximum number of retry attempts.</param>
        /// <returns>The validated input, or null if validation fails after retries.</returns>
        public static async Task<string?> GetValidatedInputAsync(
            Func<Task<string?>> inputProvider,
            Func<string?, bool> validationCondition,
            int maxRetries = 5)
        {
            int attempts = 0;

            while (attempts < maxRetries)
            {
                string? input = await inputProvider();

                if (validationCondition(input))
                {
                    return input;
                }

                attempts++;
            }

            return null; // Return null after max retries.
        }
    }
}
