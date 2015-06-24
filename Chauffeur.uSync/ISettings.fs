namespace Chauffeur.uSync

open jumps.umbraco.usync

type ISettings =
    abstract Folder : string

type uSyncSettingsWrapper() =
    interface ISettings with
        member this.Folder = uSyncSettings.Folder.Replace("~/", "")
