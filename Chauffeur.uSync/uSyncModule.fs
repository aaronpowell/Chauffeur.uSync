module uSyncModule

open System.IO
open System.IO.Abstractions
open System.Reflection

let findExports (fileSystem: IFileSystem) folder =
    fileSystem.Directory.EnumerateFiles(folder, "*.xml")
        |> Seq.cast<string>
        |> Seq.map fileSystem.FileInfo.FromFileName

let copyExportedFiles (fileSystem: IFileSystem) dest (files: seq<FileInfoBase>) =
    let now = System.DateTimeOffset.Now
    let timestamp = now.ToString "yyyy-MM-dd-hh-mm"

    files
        |> Seq.map (fun file -> (file, fileSystem.Path.Combine(dest, timestamp, file.Name)))
        |> Seq.iter (fun (file, dest) -> fileSystem.File.Copy(file.FullName, dest))