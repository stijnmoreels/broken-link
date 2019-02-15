type BrokenLink =
    { Link : string
      File : string }

module BrokenLink =

    open System.Collections.Generic
    open System.IO
    open System.Net
    open System.Text.RegularExpressions
    open System.Threading.Tasks

    let matchLinksInContent txt =
        let ms = Regex.Matches (txt, "https?:\/\/([\w\-_]+(?:(?:\.[\w\-_]+)+))([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?")
        seq { for m in ms do yield m.Value }

    let private sendHttpReq url = async {
        let req = HttpWebRequest.CreateHttp (url : string)
        use! res = req.GetResponseAsync () |> Async.AwaitTask
        return url, (res :?> HttpWebResponse).StatusCode }

    let detectContent file txt = async {
        let! links =
            matchLinksInContent txt
            |> Seq.map sendHttpReq
            |> Async.Parallel
        return Seq.choose (fun (link, status) -> 
            if status = HttpStatusCode.OK then None
            else Some { Link = link; File = file }) links }

    let detectFile file =
        File.ReadAllText file |> detectContent file

    let detectFiles files =
        files |> Seq.map detectFile |> Async.Parallel

