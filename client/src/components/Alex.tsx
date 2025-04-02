import {useEffect, useState} from "react";
import {AuthRequestDto, BroadcastToAlex, Question, QuestionClient, StringConstants} from "../generated-client.ts";
import {AuthApi, QuestionApi, randomUid} from "./App.tsx";
import toast from "react-hot-toast";
import {useWsClient} from "ws-request-hook";

export default function Alex() {
    const [pass, setPass] = useState<string | undefined>(undefined);
    const [jwt, setJwt] = useState<string | undefined>(undefined);
    const [questions, setQuestions] = useState<Question[]>([]);
    const {readyState, onMessage} = useWsClient();


    useEffect(() => {
        if (readyState !== 1) return;
        const jwt = localStorage.getItem('jwt');
        if(jwt){
            setJwt(jwt);
            AuthApi.authWithJwt(jwt,  randomUid).then(e => {
                toast("Welcome back")
            })

        }
        const unsub = onMessage<BroadcastToAlex>(StringConstants.BroadcastToAlex, (dto) => {
            toast("you got mail");
            console.log(dto);
            setQuestions(prevQuestions => [ dto.question!,...prevQuestions,]);
        });
        return () => unsub();
    }, [readyState]);

    
    const getPreviousQuestions = () => {
        if (jwt) {
            //Here you can also use the second and third parameter to paginate the results, but the client always just gets the default recent 5
            QuestionApi.getPreviousQuestions(jwt, undefined,  undefined).then(res => {
                setQuestions(res)
            })
        }
    }
    const clearQuestions = () => {
        setQuestions([])
    }


    return (<>

        {
            jwt == undefined && <>
                <input className="input" placeholder="pass" type="password" value={pass} onChange={e =>setPass(e.target.value)} />
                <button className="btn btn-primary" onClick={() => {
                    const dto: AuthRequestDto = {
                        password: pass!,
                        clientId: randomUid,
                    };
                    AuthApi.login(dto).then((result) => {
                        localStorage.setItem('jwt', result.jwt)
                        setJwt(result.jwt)
                        toast.success('Logged in')
                    }).catch(e =>{
                        toast.error('Failed to login');
                    })
                }}>Sign in</button>
            </>
        }

        {
            questions.map(q => <div key={q.id}>{q.questiontext}</div>)
        }
   
        {
            jwt && <>
                <button className="btn btn-primray" onClick={() => clearQuestions()}>Clear questions</button>
            <button className="btn btn-secondary" onClick={() => getPreviousQuestions()}>Get previous questions</button>
            </>
            
            
        }
    </>);
}