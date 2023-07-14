namespace NetCash

[<AbstractClass; Sealed>]
type internal Singleton<'a when 'a: (new: unit -> 'a)> private () =
    static let instance = new 'a ()
    static member Instance = instance

[<AutoOpen>]
module internal Pervasives =
    let inline isUncheckedDefault<'a when 'a: equality> value = Unchecked.defaultof<'a> = value

    let inline defaultUncheckedArg def obj =
        if isUncheckedDefault obj then
            def
        else
            obj

    let inline defaultUncheckedArgWith thunk obj =
        if isUncheckedDefault obj then
            thunk ()
        else
            obj
