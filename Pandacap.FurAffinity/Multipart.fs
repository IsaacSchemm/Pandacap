module private Multipart

open System.Net.Http

type MultipartSegmentValue =
| Field of string
| File of byte[]

let from segments =
    let content = new MultipartFormDataContent()
    for segment in segments do
        match segment with
        | name, Field value -> content.Add(new StringContent(value), name)
        | name, File data -> content.Add(new ByteArrayContent(data), name, "image.dat")
    content
