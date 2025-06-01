import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";
import Authenticator from './authenticator.util'

class RefreshListener {
    private connection: HubConnection | null = null;
    
  constructor() {
    //this.initialize();
  }
    
  public initialize() {
    try {
      const jwtAuthenticationToken = Authenticator.getJwtAuthenticationTokenString();
      console.debug(`ws: got authentiation token ${jwtAuthenticationToken}`)
      this.connection = new HubConnectionBuilder()
        // from https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-9.0#bearer-token-authentication
        .withUrl("/hubs/refresh", { accessTokenFactory: () => jwtAuthenticationToken })
        .withAutomaticReconnect()
        .build();

      this.connection?.on("UserConnected", () => {
        console.log("Websockets connected");
      });

      this.connection?.onclose(async () => {
        await this.start();
      });

      this.start();
    } catch (error) {
      if (error instanceof (Error)) console.error(`Websockets not started: ${error.message}`);
      else console.error(`Websockets not started: ${error}`);
    }
  }

  public async start() {
    await this.connection?.start().catch(error => console.log(`websockets failed to start: ${error}`));
    console.debug('websockets started');
  }

  /**
   * Registers a callback for RefreshMessage
   * @param callback
   */
  public onRefresh(callback: (path: string) => void) {
    this.connection?.on("RefreshMessage", callback);
  }

  /**
   * Deregisters a specific callback for RefreshMessage
   * @param callback must be a reference the same function as was provided to onRefresh(name, callback)
   */
  public offRefresh(callback: (path: string) => void) {
    this.connection?.off("RefreshMessage", callback);
  }

  public sendRefreshAllConnectedClients(message: string) {
    this.connection?.invoke('RefreshAllConnectedClients', message)
      .catch(err => console.error('Error while sending message: ', err));
  }
}

export default new RefreshListener()