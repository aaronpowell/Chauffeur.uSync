namespace Chauffeur.uSync

open System.IO
open Chauffeur
open Chauffeur.Host
open System.IO.Abstractions
open copyModule

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

        let usyncSnapshot (out : TextWriter) (in' : TextReader) chauffeurFolder =
            async {
                let couldParse, siteRoot = chauffeurSettings.TryGetSiteRootDirectory()
                let siteRoot' = fileSystem.DirectoryInfo.FromDirectoryName(siteRoot).FullName
                let uSyncRoot' =
                    fileSystem.Path.Combine(siteRoot', uSyncSettings.Folder)
                    |> fileSystem.DirectoryInfo.FromDirectoryName

                let createFolder path =
                    let f = fileSystem.FileInfo.FromFileName path
                    fileSystem.Directory.CreateDirectory f.Directory.FullName |> ignore

                let deliverableFolder = outputFolder fileSystem.Path.Combine chauffeurFolder
                uSyncRoot'.FullName
                |> findExports fileSystem
                |> mapFiles fileSystem.Path.Combine uSyncRoot'.FullName deliverableFolder
                |> copyExportedFiles fileSystem.File.Copy createFolder
                do! out.WriteLineAsync(sprintf "uSync files copied to %s" deliverableFolder) |> Async.AwaitVoidTask
                do! out.WriteAsync("Do you wish to create a Delivery (Y/n)?") |> Async.AwaitVoidTask
                let answer = in'.ReadLine()
                return DeliverableResponse.Continue
            }

        let invalidSubCommand (out : TextWriter) subCommand =
            async {
                do! out.WriteLineAsync(sprintf "The subcommand '%s' is not supported by usync" subCommand)
                    |> Async.AwaitVoidTask
                return DeliverableResponse.Continue
            }

        let couldParse, chauffeurFolder = chauffeurSettings.TryGetChauffeurDirectory()
        let l = args |> Array.toList
        match l with
        | [] -> noArgs this.Out
        | _ :: _ when not couldParse -> chauffeurDirError this.Out
        | "snapshot" :: _ when couldParse -> usyncSnapshot this.Out this.In chauffeurFolder
        | sc :: _ -> invalidSubCommand this.Out sc
        |> Async.StartAsTask
