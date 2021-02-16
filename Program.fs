open System
open System.IO
open System.Diagnostics
open Microsoft.Win32
open Argu

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


let getBrowserPath () =
    let key = 
        @"Software\Microsoft\Windows\shell\Associations\UrlAssociations\http\UserChoice"

    use userKey = Registry.CurrentUser.OpenSubKey(key, false)
    //if null, failwith access failed to registry
    let progId = userKey.GetValue("Progid").ToString()

    let browserExe =
        match progId.ToLower() with
        | p when p.Contains("chrome") -> "chrome.exe" 
        | p when p.Contains("firefox") -> "firefox.exe" 
        | p when p.Contains("safari") -> "safari.exe" 
        | p when p.Contains("opera") -> "opera.exe" 
        | _ -> failwith("Browser not supported")


    let path = $"""{progId}\shell\open\command"""
    use pathKey = 
        Registry.ClassesRoot.OpenSubKey(path) 
    //if null, failwith path to browser not found 

    let browserDir = 
        FileInfo(pathKey.GetValue( null ).ToString().ToLower().Replace( "\"", "" )).Directory.FullName

    let browserPath = Path.Join(browserDir, browserExe)
    browserPath

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


    let browserPath = getBrowserPath ()
    (*
        Todo: 
            1. Add ability to get version number
            2. Adjust args for diff browsers
    *)

    Process.Start(browserPath, final) |> ignore
    0 // return an integer exit code
