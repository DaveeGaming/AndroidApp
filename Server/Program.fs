module server.App

open System
open System.Linq
open System.Collections.Generic
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.SignalR
open Giraffe

type TestHub(users: Dictionary<string, string>) =
    inherit Hub()
   
    
    
    override this.OnDisconnectedAsync(err) =
        
        for user in users do
            if String.Equals(user.Value, this.Context.ConnectionId) then
                printfn $"{user.Key} disconnected"
                this.Clients.All.SendAsync("Message", "server", $"{user.Key} left") |> ignore
        Threading.Tasks.Task.CompletedTask
    
    
    member this.sendMessage(user:string,message: string) =
        printfn $"{user} sent: {message}"
        this.Clients.All.SendAsync("Message", user, message) |> Async.AwaitTask |> Async.RunSynchronously
    
    member this.ConnectUser(user: string) =
        if users.ContainsKey(user) then
            printfn $"User {user} tried to login with an existing name"
            this.Clients.Caller.SendAsync("ConnectUser", false) |> ignore
        else
            printfn $"User {user} successfully logged in"
            users.Add(user, this.Context.ConnectionId)
            this.Clients.Caller.SendAsync("ConnectUser", true) |> ignore
            this.Clients.All.SendAsync("Message", "server", $"{user} joined") |> ignore
let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> htmlFile "./www/index.html"
                route "/ping" >=> text "pong"
            ]
        setStatusCode 404 >=> text "Not Found" ]


let configureApp (app : IApplicationBuilder) =
    app 
         .UseRouting()
         .UseEndpoints(fun endpoints -> endpoints.MapHub<TestHub>("/test") |> ignore )
         .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddSingleton<Dictionary<string,string>>() |> ignore
    services.AddGiraffe() |> ignore
    services.AddSignalR() |> ignore


[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "www")
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .UseContentRoot(contentRoot)
                    .UseWebRoot(webRoot)
                    .UseUrls("http://0.0.0.0:5000")
                    .Configure(Action<IApplicationBuilder> configureApp)
                    .ConfigureServices(configureServices)
                    |> ignore)
        .Build()
        .Run()
    0