module Tests.Catch

open System.Threading.Tasks

open Reaction

open Expecto
open Tests.Utils

exception MyError of string

[<Tests>]
let tests = testList "Query Tests" [

    testAsync "Test catch no error" {
        // Arrange
        let xs = fromNotification [ OnNext 1; OnNext 2; OnNext 3; OnCompleted ]
        let ys = fromNotification [ OnNext 4; OnNext 5; OnNext 6; OnCompleted ]
        let zs = xs |> AsyncRx.catch (fun _ -> ys)
        let obv = TestObserver<int>()

        // Act
        let! sub = zs.SubscribeAsync obv
        do! obv.AwaitIgnore ()

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnNext 1; OnNext 2; OnNext 3; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }


    testAsync "Test catch error" {
        // Arrange
        let error = MyError "error"
        let xs = fromNotification [ OnNext 1; OnNext 2; OnNext 3; OnError error ]
        let ys = fromNotification [ OnNext 4; OnNext 5; OnNext 6; OnCompleted ]
        let zs = xs |> AsyncRx.catch (fun _ -> ys)
        let obv = TestObserver<int>()

        // Act
        let! sub = zs.SubscribeAsync obv
        do! obv.AwaitIgnore ()

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnNext 1; OnNext 2; OnNext 3; OnNext 4; OnNext 5; OnNext 6; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test catch error exception is propagated" {
        // Arrange
        let error = MyError "ing"
        let xs = fromNotification [ OnNext "test"; OnError error ]
        let zs = xs |> AsyncRx.catch (fun err ->
            let msg =
                match err with
                | MyError msg -> msg
                | _ -> "error"

            AsyncRx.single msg)
        let obv = TestObserver<string> ()

        // Act
        let! sub = zs.SubscribeAsync obv
        do! obv.AwaitIgnore ()

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected = [ OnNext "test"; OnNext "ing"; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test catch error twice" {
        // Arrange
        let error = MyError "error"
        let xs = fromNotification [ OnNext 1; OnError error ]
        let ys1 = fromNotification [ OnNext 2; OnError error ]
        let ys2 = fromNotification [ OnNext 3; OnCompleted ]
        let iter = [ys1; ys2] |> Seq.ofList |> fun x -> x.GetEnumerator ()
        let zs = xs |> AsyncRx.catch (fun _ ->
            iter.MoveNext () |> ignore
            iter.Current
        )
        let obv = TestObserver<int>()

        // Act
        let! sub = zs.SubscribeAsync obv
        do! obv.AwaitIgnore ()

        // Assert
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnNext 1; OnNext 2; OnNext 3;OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }
]