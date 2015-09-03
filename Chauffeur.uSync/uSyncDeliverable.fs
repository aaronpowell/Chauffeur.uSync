namespace Chauffeur.uSync

open System.IO
open Chauffeur
open Chauffeur.Host
open System.IO.Abstractions
open SnapshotModule
open DeliverableMakerModule

[<DeliverableName("usync")>]
type uSyncDeliverable(reader, writer, fileSystem : IFileSystem, uSyncSettings : ISettings, chauffeurSettings : IChauffeurSettings) =
    inherit Deliverable(reader, writer)

    override this.Run(command, args) =
        let noArgs (out : TextWriter) =
            async {
                do! out.WriteLineAsync("No subcommand for usync provided") |> Async.AwaitTask
                return DeliverableResponse.Continue
            }

        let chauffeurDirError (out : TextWriter) =
            async {
                do! out.WriteLineAsync("Failed to load Chauffeur folder") |> Async.AwaitTask
                return DeliverableResponse.Continue
            }

        let invalidSubCommand (out : TextWriter) subCommand =
            async {
                do! out.WriteLineAsync(sprintf "The subcommand '%s' is not supported by usync" subCommand) |> Async.AwaitTask
                return DeliverableResponse.Continue
            }

        let uSyncRoot =
            let couldParse, siteRoot = chauffeurSettings.TryGetSiteRootDirectory()
            let siteRoot' = fileSystem.DirectoryInfo.FromDirectoryName(siteRoot).FullName
            fileSystem.Path.Combine(siteRoot', uSyncSettings.Folder) |> fileSystem.DirectoryInfo.FromDirectoryName

        let couldParse, chauffeurFolder = chauffeurSettings.TryGetChauffeurDirectory()
        let usyncSnapshot' = usyncSnapshot { FullName = uSyncRoot.FullName } fileSystem outputFolder
        let l = args |> Array.toList
        match l with
        | [] -> noArgs this.Out
        | _ when not couldParse -> chauffeurDirError this.Out
        | "snapshot" :: _ when couldParse ->
            let outputStream = this.Out
            let inputStream = this.In
            async {
                let chauffeurFolder' = fileSystem.DirectoryInfo.FromDirectoryName(chauffeurFolder).FullName
                let! dir = usyncSnapshot' outputStream inputStream chauffeurFolder'
                let! result = prompt inputStream outputStream
                if result = "Y" then
                    let deliveryPath = createDeliverable fileSystem dir chauffeurFolder'
                    outputStream.WriteLine(sprintf "Delivery has been created at %s" deliveryPath)
                return DeliverableResponse.Continue
            }
        | sc :: _ -> invalidSubCommand this.Out sc
        |> Async.StartAsTask

    interface IProvideDirections with
        member x.Directions() =
            x.Out.WriteLineAsync "usync <command>" |> ignore
            x.Out.WriteLineAsync "\tRuns commands against the uSync export" |> ignore
            x.Out.WriteLineAsync "usync snapshot" |> ignore
            x.Out.WriteLineAsync "\tTakes the latest uSync export and moves it to the Chauffeur folder" |> ignore
            x.Out.WriteLineAsync "\tPrompts user to create a delivery as well"
