#load "src/HalSharp/Hal.fs"
#load "src/HalSharp/ObjectInterpreter.fs"
#load "paket-files/include-scripts/net40/include.newtonsoft.json.fsx"

open Hal
open Newtonsoft.Json

type Payment = {
    subtotal: decimal
    tax: decimal
    freight: decimal
    total: decimal
}

type Coupon = {
    ``type``: string
    amount: decimal
    code: string
}

let payment = {
    subtotal = 49m
    tax = 0m
    freight = 5m
    total = 44m
}

let coupon = {
    ``type`` = "dollarOff"
    amount = 10m
    code = "A0318A97"
}

let resource: Resource<obj> = {
    Resource.empty with
        payload = payment :> obj |> Some
}

printfn "%A" (resource |> ObjectInterpreter.toJson |> JsonConvert.SerializeObject)