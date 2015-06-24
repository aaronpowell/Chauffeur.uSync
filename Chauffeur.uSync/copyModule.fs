module copyModule

open System.IO
open System.IO.Abstractions
open System.Reflection

type FileMetadata =
    { Path : string
      DestinationPath : string }

let findExports (fileSystem : IFileSystem) folder =
    fileSystem.Directory.GetFiles(folder, "*.config", SearchOption.AllDirectories)
    |> Seq.cast<string>
    |> Seq.map fileSystem.FileInfo.FromFileName

let outputFolder combine dest =
    let now = System.DateTimeOffset.Now
    let timestamp = now.ToString "yyyy-MM-dd-hh-mm"
    combine (dest, timestamp)

let mapFiles combine (rootFolder : string) dest (files : seq<FileInfoBase>) =
    let pathResolver (fullName : string) =
        let relativePath = fullName.Replace(rootFolder, "")
        combine (dest, relativePath)
    files |> Seq.map (fun file ->
                 { Path = file.FullName
                   DestinationPath = combine (dest, pathResolver file.FullName) })

let copyExportedFiles copy createFolder files =
    files
    |> Seq.map (fun file ->
           createFolder file.DestinationPath
           file)
    |> Seq.iter (fun file -> copy (file.Path, file.DestinationPath))
