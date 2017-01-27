module internal Hypermedia.Shared

let merge (maps: Map<_,_> seq): Map<_,_> = 
    List.concat (maps |> Seq.map Map.toList) |> Map.ofList

