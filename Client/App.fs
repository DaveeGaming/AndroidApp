namespace balls

open System
open Fabulous
open Fabulous.Maui
open Microsoft.AspNetCore.SignalR.Client

open type Fabulous.Maui.View

module App =
    
    type Model = {
        Hub: HubConnection option
        Currmessage: string
        Username: string
        App: bool
        Messages: string list
        Loginplaceholder: string
    }

    type Msg =
        | UserName of string
        | Enter
        | VerifyUsername of bool
        | Message
        | CurrentMessage of string
        | ReceiveMessage of string * string
  
    let initmodel = { 
        Hub = None
        Username = ""
        Currmessage = ""
        App = false
        Messages = []
        Loginplaceholder = "Enter your name" 
    }
    let init () =
        initmodel, Cmd.none
    
    let listener (dispatch: Msg -> unit) (hub: HubConnection) =
        hub.On<string, string>("Message", fun user msg -> dispatch <| ReceiveMessage(user,msg)) |> ignore
        hub.On<bool>("ConnectUser", fun success -> dispatch <| VerifyUsername success) |> ignore
    
    let update msg model =
        match msg with
        | VerifyUsername success ->
            if success then
                { model with App = true }, Cmd.none
            else
                { model with Loginplaceholder = "Name taken"; Username = "" }, Cmd.none
        | UserName name -> { model with Username = name }, Cmd.none
        | Enter ->
            if model.Hub.IsNone then
                let hub =
                    HubConnectionBuilder()
                        .WithUrl("http://women-football.gl.at.ply.gg:5903/test")
                        .WithAutomaticReconnect()
                        .Build()
                        
                hub.StartAsync() |> ignore
                
                let cmd = Cmd.ofSub (fun (dispatch: Msg -> unit) -> listener dispatch hub)
                hub.SendAsync("ConnectUser", model.Username) |> Async.AwaitTask |> ignore
                { model with Hub = Some hub}, cmd
             else
                model.Hub.Value.SendAsync("ConnectUser", model.Username) |> Async.AwaitTask |> ignore
                model, Cmd.none
                 
        | Message ->
            if String.IsNullOrWhiteSpace(model.Currmessage) then
                model, Cmd.none
            else
                model.Hub.Value.InvokeAsync("sendMessage", model.Username, model.Currmessage.Trim()) |> Async.AwaitTask |> ignore
                { model with Currmessage = "" }, Cmd.none
        | CurrentMessage s -> {model with Currmessage = s }, Cmd.none
        | ReceiveMessage(user,msg) -> {model with Messages = $"{user}: {msg}" :: model.Messages }, Cmd.none
                        

    let login (model: Model) =
        (VStack() {
            HStack() {
                Label("Enter name: ").padding(10).centerVertical()
                Entry(model.Username, UserName)
                    .placeholder(model.Loginplaceholder)
                    .onCompleted(Enter)
                Button("Login", Enter).margin(5,0,0,0)
            }
        }).center()
        
    let app (model: Model) =
        VStack() {
            (VStack(){
                Label("Chat app")
                    .margin(0,30,0,0)
                    .font(size = 30.)
                    .centerHorizontal()
                Label(model.Username)
                    .centerHorizontal()
                (HStack() {
                    Entry(model.Currmessage, CurrentMessage)
                        .placeholder("message")
                        .minimumWidth(300.)
                        .margin(0,0,10,0)
                        .onCompleted(Message)
                    Button("Send", Message)
                }).margin(0,10,0,0)
                VStack() {
                    for msg in model.Messages do
                        Label(msg).margin(0,2,0,5)
                }
            }).centerHorizontal()
        }
    let view (model: Model) =
        Application(
            ContentPage(content = (if model.App then app else login) model )
        )
    
        

    let program = Program.statefulWithCmd init update view