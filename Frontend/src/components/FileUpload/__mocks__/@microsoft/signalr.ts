export const LogLevel = { Information: 1 };

export class HubConnectionBuilder {
  withUrl() { return this; }
  withAutomaticReconnect() { return this; }
  configureLogging() { return this; }
  withServerTimeout() { return this; }
  withKeepAliveInterval() { return this; }
  withStatefulReconnect() { return this; }
  build() {
    return {
      start: () => Promise.resolve(),
      on: (event: string, callback: (progress1: number, progress2: number) => void) => {
        if (event === 'ReceiveProgress') {
          callback(0.5, 0.75);
        }
      },
      onclose: () => {}
    };
  }
}
