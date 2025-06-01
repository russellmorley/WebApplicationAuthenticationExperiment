class Authenticator {
  public static readonly AuthenticateQueryParamKey = 'authenticate';
  tokenStorage: TokenStorage;
  constructor() {
    this.tokenStorage = new TokenStorage();
  }

  /**
   * 
   * @returns true if there is a jwtSecurityToken, or false if there isn't,
   * either from the action of this method or previously existing.
   */
  public async handleIfAuthenticating(): Promise<boolean>  {
    try {
      const searchParams = new URLSearchParams(window.location.search);
      if (searchParams.get(Authenticator.AuthenticateQueryParamKey)) {
        const url = '/api/get-tokens-from-google-code';
        const response = await fetch(url);
        if (!response.ok) {
          console.error(`Authenticating, but attempt to get jwtSecurityToken failed ${JSON.stringify(response)}`);
          this.tokenStorage.removeJwtSecurityToken();
          return false;
        }
        const tokens = await response.json();
        this.tokenStorage.setTokens(tokens);
        console.debug(`Authenticating, and attempt to retrieve and save tokens succeeded: ${JSON.stringify(tokens)}`);
        return true;
      } else {
        return this.tokenStorage.hasJwtSecurityTokenString();
      }
    } finally {
      this.removeAuthenticationQueryParams();
    }
  } 

  public async refreshJwtAccessToken(): Promise<boolean> {
    const refreshToken = this.tokenStorage.getRefreshTokenString();
    if (!refreshToken) {
      console.debug('no refresh token string in storage');
      return false;
    }

    try {
      const response = await fetch('/refresh-token', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ refreshToken })
      });

      if (response.ok) {
        const data = await response.json();
        const { jwtSecurityToken } = data;

        this.tokenStorage.setJwtSecurityToken(jwtSecurityToken);

        return true;
      } else {
        console.error(`Request to refresh access token using refresh token ${refreshToken} failed: ${response.status}: ${response.text}`);
        return false;
      }
    } catch (error) {
      console.error('Error refreshing access token:', error);
      return false;
    }
  }

  public getAuthenticationHeaders(options: object): object {
    const jwtSecurityTokenString = this.getJwtAuthenticationTokenString();// this.tokenStorage.getJwtSecurityTokenString();
    //if (!jwtSecurityTokenString) throw new Error("Don't have jwtSecurityTokenString");
    const opts = options as { ['headers']: object;[key: string]: unknown };
    if (opts.headers === undefined)
      opts.headers = {};
    const hdrs = opts.headers as { ['Authorization']: string;['Content-Type']: string;[key: string]: unknown };
    hdrs['Authorization'] = `Bearer ${jwtSecurityTokenString}`;
    hdrs['Content-Type'] = 'application/json';
    return options;
  }

  public getJwtAuthenticationTokenString(): string {
    const jwtSecurityTokenString = this.tokenStorage.getJwtSecurityTokenString();
    if (!jwtSecurityTokenString) throw new Error("Don't have jwtSecurityTokenString")
    return jwtSecurityTokenString;
  }

  private removeAuthenticationQueryParams() {
    // Get the current URL
    const url = new URL(window.location.href);

    // Remove the specified query parameter
      url.searchParams.delete(Authenticator.AuthenticateQueryParamKey);

    // Update the URL in the address bar without reloading the page
    window.history.replaceState({}, '', url);
  }
}

class TokenStorage {
  static readonly jwtSecurityTokenKey = "jwtSecurityToken";
  static readonly refreshTokenKey = "refreshToken";

  public removeJwtSecurityToken() {
    localStorage.removeItem(TokenStorage.jwtSecurityTokenKey);
  }

  public setTokens(tokens: { jwtSecurityTokenString: string, refreshToken: string }) {
    localStorage.setItem(TokenStorage.jwtSecurityTokenKey, tokens.jwtSecurityTokenString);
    localStorage.setItem(TokenStorage.refreshTokenKey, JSON.stringify(tokens.refreshToken));
  }

  public getJwtSecurityTokenString(): string | null {
    return localStorage.getItem(TokenStorage.jwtSecurityTokenKey);
  }

  public hasJwtSecurityTokenString(): boolean {
    return localStorage.getItem(TokenStorage.jwtSecurityTokenKey) == null ? false : true;
  }

  public getRefreshTokenString(): string | null {
    return localStorage.getItem(TokenStorage.refreshTokenKey);
  }

  public setJwtSecurityToken(tokens: { jwtSecurityTokenString: string }) {
    localStorage.setItem(TokenStorage.jwtSecurityTokenKey, tokens.jwtSecurityTokenString);
  }
}

export default new Authenticator(); 