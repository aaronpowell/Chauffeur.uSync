namespace Chauffeur.uSync

open System.IO
open Chauffeur
open Chauffeur.Host
open System.IO.Abstractions
open snapshotModule

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

        let couldParse, chauffeurFolder = chauffeurSettings.TryGetChauffeurDirectory()
        let usyncSnapshot' = usyncSnapshot chauffeurSettings.TryGetSiteRootDirectory fileSystem outputFolder uSyncSettings.Folder
        let l = args |> Array.toList
        match l with
        | [] -> noArgs this.Out
        | _ :: _ when not couldParse -> chauffeurDirError this.Out
        | "snapshot" :: _ when couldParse -> usyncSnapshot' this.Out this.In chauffeurFolder
        | sc :: _ -> invalidSubCommand this.Out sc
        |> Async.StartAsTask
