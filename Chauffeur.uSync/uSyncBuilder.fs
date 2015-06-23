namespace Chauffeur.uSync

open Chauffeur

type uSyncBuilder() =
    interface IBuildDependencies with
        member this.Build container =
            container.Register<uSyncSettingsWrapper>().As<ISettings>()
                |> ignore