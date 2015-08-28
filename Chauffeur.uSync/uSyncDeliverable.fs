namespace Chauffeur.uSync

open System.IO
open Chauffeur
open Chauffeur.Host
open System.IO.Abstractions
open snapshotModule
open deliverableMakerModule

[<DeliverableName("usync")>]
type uSyncDeliverable(reader, writer, fileSystem : IFileSystem, uSyncSettings : ISettings, chauffeurSettings : IChauffeurSettings) =
    inherit Deliverable(reader, writer)
    override this.Run(command, args) =
        let noArgs (out : TextWriter) =
            async {
                do! out.WriteLineAsync("No subcommand for usync provided") |> Async.AwaitVoidTask
                return DeliverableResponse.Continue
            }

        let chauffeurDirError (out : TextWriter) =
            async {
                do! out.WriteLineAsync("Failed to load Chauffeur folder") |> Async.AwaitVoidTask
                return DeliverableResponse.Continue
            }

        let invalidSubCommand (out : TextWriter) subCommand =
            async {
                do! out.WriteLineAsync(sprintf "The subcommand '%s' is not supported by usync" subCommand)
                    |> Async.AwaitVoidTask
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
        | _ :: _ when not couldParse -> chauffeurDirError this.Out
        | "snapshot" :: _ when couldParse ->
            let chauffeurFolder' = fileSystem.DirectoryInfo.FromDirectoryName(chauffeurFolder).FullName
            let dir = usyncSnapshot' this.Out this.In chauffeurFolder' |> Async.RunSynchronously

            match prompt this.In this.Out |> Async.RunSynchronously with
            | "Y" ->
                let deliveryPath = createDeliverable fileSystem dir chauffeurFolder'
                this.Out.WriteLine(sprintf "Delivery has been created at %s" deliveryPath)
                ()
            | _ -> ()

            async { return DeliverableResponse.Continue }
        | sc :: _ -> invalidSubCommand this.Out sc
        |> Async.StartAsTask
