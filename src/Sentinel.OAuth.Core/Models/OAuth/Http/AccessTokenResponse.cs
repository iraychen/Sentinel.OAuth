﻿namespace Sentinel.OAuth.Core.Models.OAuth.Http
{
    using Newtonsoft.Json;
    using System.Diagnostics;

    /// <summary>
    /// Represents an OAuth access token response
    /// </summary>
    [DebuggerDisplay("access_token: {AccessToken}, refresh_token: {RefreshToken}, id_token: {IdToken}, token_type: {TokenType}, expires_in: {ExpiresIn}")]
    public class AccessTokenResponse
    {
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        /// <value>The access token.</value>
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        /// <summary>Gets or sets the identifier token.</summary>
        /// <value>The identifier token.</value>
        [JsonProperty("id_token", NullValueHandling = NullValueHandling.Ignore)]
        public string IdToken { get; set; }

        /// <summary>
        /// Gets or sets the refresh token.
        /// </summary>
        /// <value>The refresh token.</value>
        [JsonProperty("refresh_token", NullValueHandling = NullValueHandling.Ignore)]
        public string RefreshToken { get; set; }

        /// <summary>
        /// Gets or sets the type of the token.
        /// </summary>
        /// <value>The type of the token.</value>
        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds until the token expires.
        /// </summary>
        /// <value>The number of seconds until the token expires.</value>
        [JsonProperty("expires_in")]
        public double ExpiresIn { get; set; }
    }
}