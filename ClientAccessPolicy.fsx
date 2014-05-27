open System.IO
open System.Threading
open System.Net
open System.Net.Sockets

[<Literal>]
let PolicyRequest = "<policy-file-request/>"

[<Literal>]
let PolicyFileResponse = "ClientAccessPolicy.xml"

let read_request ( clientReader : StreamReader ) = async { 
        let charArray : char array = Array.zeroCreate 32
        let! bytesRead = clientReader.ReadAsync(charArray, 0, 32) |> Async.AwaitTask
        let requestString = new string(charArray, 0, bytesRead)
        return requestString 
}

let policy_server sourceIp = 
    let policy = File.ReadAllText(PolicyFileResponse)
    let server = new TcpListener(IPAddress.Parse(sourceIp), 943)
    server.Start()
    printfn "server started on ip %s" sourceIp
    while true do
        let client = server.AcceptTcpClient()
        printfn "client connected"
        let policyAsync = async {
            let clientStream = client.GetStream()
            use clientReader = new StreamReader(clientStream)
            let! request = read_request(clientReader)
            printfn "%s" request
            match request = PolicyRequest with
            | true -> 
                use writer = new StreamWriter(client.GetStream())
                writer.AutoFlush <- true
                do! writer.WriteAsync(policy).ContinueWith(fun t-> ()) |> Async.AwaitTask
                printfn "client policy sent."
            | _ -> printfn "wrong policy request" 
            printfn "closing client..."
            client.Close()
        }
        policyAsync |> Async.Start

policy_server fsi.CommandLineArgs.[1]
