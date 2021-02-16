open System
open System.Diagnostics
open Argu

[<NoAppSettings>]
type CliArguments =
    | [<MainCommand; ExactlyOnce>] Search_Query of string
    | [<AltCommandLine("-w")>] W
    | [<AltCommandLine("-i")>] I

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | W -> "open search in a new browser window."
            | I -> "open search in incognito mode."
            | Search_Query _ -> "search criteria."

[<EntryPoint>]
let main argv =

    let errorHandler = ProcessExiter(colorizer = function ErrorCode.HelpText -> None | _ -> Some ConsoleColor.Red)
    let parser = ArgumentParser.Create<CliArguments>(programName = "ws", errorHandler = errorHandler)

    let results = parser.ParseCommandLine argv
    let incog = results.Contains I 
    let newW = results.Contains W

    let chromeArgs = 
        sprintf "%s %s" (if incog then "--incognito" else "") (if newW then "--new-window" else "")

    let final =
        match results.GetResult Search_Query with
        | q when not (q.StartsWith("http")) -> $""" {chromeArgs} "? {q}" """
        | q -> $"{chromeArgs} {q}"

    (*
        Todo: 
            1. Find path of chrome insead of hardcoding 
            2. Add support for multiple browsers
            3. Automatically open in default browser
            4. Add ability to get version number
    *)

    Process.Start(@"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe", final) |> ignore
    0 // return an integer exit code
