import {WsClientProvider} from 'ws-request-hook';
import {useEffect, useState} from "react";
const baseUrl = import.meta.env.VITE_API_BASE_URL
const prod = import.meta.env.PROD

export const randomUid = crypto.randomUUID()

export default function App() {
    
    const [url, setUrl] = useState<string | undefined>(undefined)
    useEffect(() => {
        const finalUrl = prod ? 'wss://' + baseUrl + '?id=' + randomUid : 'ws://' + baseUrl + '?id=' + randomUid;
setUrl(finalUrl);
    }, [prod, baseUrl]);
    
    return (<>

        {
            url &&
        <WsClientProvider url={url}>

            <div className="flex flex-col">
                <div>
                    { !prod && <SuperSimpleKahootCloneGui /> }
                </div>

            </div>
        </WsClientProvider>
        }
    </>)
}