import {useState} from "react";
import {QuestionApi} from "./App.tsx";
import {CreateQuestionDto, StringConstants} from "../generated-client.ts";
import toast from "react-hot-toast";


export default function Question()  {
    
    const [q, setQ] = useState('')
    
    return (<>
        <input className="input" value={q} placeholder="write something alex can see" onChange={event => setQ(event.target.value)}/>
        <button className="btn btn-primary" onClick={() => {
            const dto: CreateQuestionDto = {
                questionText: q
            }
            QuestionApi.addQuestion(dto).then(() => {
                toast.success('Question sent')
            }).catch(() => {
                toast.error('Failed to send question')
            })
        }}>Send</button>
    </>)
}