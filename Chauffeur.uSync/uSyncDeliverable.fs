namespace Chauffeur.uSync

open Chauffeur
open Chauffeur.Host
open System.IO.Abstractions
open uSyncModule

[<DeliverableName("usync")>]
type uSyncDeliverable(reader, writer, fileSystem: IFileSystem, uSyncSettings: ISettings, chauffeurSettings: IChauffeurSettings) =
    inherit Deliverable(reader, writer)

    override this.Run(command, args) =
        let couldParse, chauffeurFolder = chauffeurSettings.TryGetChauffeurDirectory()

        if not couldParse then
            let out = this.Out;
            async {
                do! out.WriteLineAsync("Failed to load Chauffeur folder") |> Async.AwaitVoidTask

                return DeliverableResponse.Continue
            } |> Async.StartAsTask
        else
            async {
                let couldParse, siteRoot = chauffeurSettings.TryGetSiteRootDirectory()
                fileSystem.Path.Combine(siteRoot, uSyncSettings.Folder)
                    |> findExports fileSystem
                    |> copyExportedFiles fileSystem chauffeurFolder

                return DeliverableResponse.Continue
            } |> Async.StartAsTask