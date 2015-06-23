namespace Chauffeur.uSync

open jumps.umbraco.usync

type ISettings =
    abstract member Folder: string

type uSyncSettingsWrapper() =
    interface ISettings with
        member this.Folder = uSyncSettings.Folder