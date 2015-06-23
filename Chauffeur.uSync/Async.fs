module Async

    open System
    open System.Threading.Tasks
    let AwaitVoidTask (task : Task) : Async<unit> =
        Async.FromContinuations(fun (cont, econt, ccont) ->
            task.ContinueWith(fun task ->
                if task.IsFaulted then econt task.Exception
                elif task.IsCanceled then ccont (OperationCanceledException())
                else cont ()) |> ignore)