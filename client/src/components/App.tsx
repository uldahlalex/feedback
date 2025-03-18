import Question from './Question.tsx';
import {WsClientProvider} from 'ws-request-hook';
import {useEffect, useState} from "react";
import {BrowserRouter, Route, Routes, useParams} from "react-router";
const baseUrl = import.meta.env.VITE_API_BASE_URL
const prod = import.meta.env.PROD
import {AuthClient} from "../generated-client.ts";
import {QuestionClient} from "../generated-client.ts";
import Alex from "./Alex.tsx";
export const randomUid = crypto.randomUUID()

const prodRestUrl = prod ? 'https://' + baseUrl : 'http://' + baseUrl;

export const AuthApi = new AuthClient(prodRestUrl);
export const QuestionApi = new QuestionClient(prodRestUrl);

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
                    <BrowserRouter>
                        <Routes>
                            <Route path="/" element={<Question />}></Route>
                            <Route path='/alex' element={<Alex />}></Route>
                            
                            
                        </Routes>
                    </BrowserRouter>
                
                    
                    
                </div>

            </div>
        </WsClientProvider>
        }
    </>)
}