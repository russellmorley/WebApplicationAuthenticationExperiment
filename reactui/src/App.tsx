import { useEffect, useState } from 'react'
import reactLogo from './assets/react.svg'
import viteLogo from '/vite.svg'
import './App.css'
import RefreshListener from './utils/refreshlistener.util'
import Authenticator from './utils/authenticator.util'
import GoogleLoginButton from './components/googleloginbutton.component'

export type Device = {
  date: string;
  temperatureC: string;
  temperatureF: string;
  summary: string;
}

function App() {
  const [count, setCount] = useState(0)

   const [devices, setDevices] = useState<Array<Device>>([]);

 

  async function getDevices(): Promise<boolean>{
    try {
      const headers = Authenticator.getAuthenticationHeaders({})
      console.debug(`Headers: ${JSON.stringify(headers)}`)
      const response = await fetch('/api/weatherforecast', headers);
      if (!response.ok) {
        console.error(`Fetch /api/weatherforecast failed: ${response.status}: ${response.statusText}: ${response.text}`);
        return false;
      }
      const devicesResponse = await response.json();
      console.debug(JSON.stringify(devicesResponse)); 
      setDevices(devicesResponse);
      return true;
    } catch (error) {
      if (error instanceof(Error)) console.error(error.message);
      else console.error(error);
      return false;
    }
  }

  const onRefreshCallback: (path: string) => void = async () => {
    await getDevices();
  };

   useEffect(() => {
     let ignore = false;
     (async () => {
       if (!ignore) {
         if (await Authenticator.handleIfAuthenticating()) {
           RefreshListener.initialize();
           RefreshListener.onRefresh(onRefreshCallback);
           getDevices();
         }
       }
     })();
     return () => {
       RefreshListener.offRefresh(onRefreshCallback);
       ignore = true;
     }
   }, []);

  return (
    <>
      <div>
        <a href="https://vite.dev" target="_blank">
          <img src={viteLogo} className="logo" alt="Vite logo" />
        </a>
        <a href="https://react.dev" target="_blank">
          <img src={reactLogo} className="logo react" alt="React logo" />
        </a>
      </div>
      <h1>Vite + React</h1>
      <div className="card">
        <button onClick={() => setCount((count) => count + 1)}>
          count is {count}
        </button>
        <p>
          Edit <code>src/App.tsx</code> and save to test HMR
        </p>
      </div>
      <div>
        <GoogleLoginButton></GoogleLoginButton>
      </div>
      <div>
        <ul>
          {devices.map((device, index) => (
            <li key={index}>{device.date};{device.temperatureF};{device.summary}</li>
          ))}
        </ul>
      </div>
      <p className="read-the-docs">
        Click on the Vite and React logos to learn more
      </p>
    </>
  )
}

export default App
