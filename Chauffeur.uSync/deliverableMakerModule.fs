module DeliverableMakerModule

open System.IO
open System.IO.Abstractions
open Chauffeur
open SnapshotModule

type DeliverableInfo =
    { Type : string
      Path : string }

type Delivery =
    { Deliverables : seq<DeliverableInfo> }

let createCommand step =
    match step.Type with
    | "DocumentType" -> sprintf "ct import \"%s\"" step.Path
    | _ -> sprintf "unknown %s \"%s\"" step.Type step.Path

let createDeliverable (fileSystem : IFileSystem) (outputFolder : DirectoryMetadata) (chauffeurFolder : string) =
    let folder' = fileSystem.DirectoryInfo.FromDirectoryName outputFolder.FullName
    let directories = folder'.GetDirectories()

    let commands =
        directories
        |> Seq.collect (fun dir ->
            dir.GetFiles("*.config", SearchOption.AllDirectories)
            |> Seq.map (fun file ->
                { Type = dir.Name
                  Path = file.FullName.Replace(chauffeurFolder, "") }))
        |> Seq.map createCommand
        |> Seq.toArray

    let path = fileSystem.Path.Combine(chauffeurFolder, sprintf "%s.delivery" folder'.Name)
    fileSystem.File.WriteAllLines (path, commands)
    path

let prompt (in' : TextReader) (out : TextWriter) =
    async {
        do! out.WriteAsync "Do you want to create a Deliverable (Y/n)? " |> Async.AwaitTask
        return in'.ReadLine().ToUpper()
    }
